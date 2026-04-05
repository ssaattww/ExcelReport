using System.Collections;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using ExcelReportLib.DSL;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExcelReportLib.ExpressionEngine;

/// <summary>
/// Represents expression engine.
/// </summary>
public sealed class ExpressionEngine : IExpressionEngine, IExpressionEvaluator
{
    private static readonly ScriptOptions DefaultScriptOptions = CreateDefaultScriptOptions();
    private static readonly ExcelFormulaHelpers DefaultExcelFormulaHelpers = ExcelFormulaHelpers.Instance;

    private static readonly IReadOnlyDictionary<string, string> DynamicLinqRewriteMap =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Where"] = "__dynWhere",
            ["Select"] = "__dynSelect",
            ["Sum"] = "__dynSum",
            ["Count"] = "__dynCount",
            ["Any"] = "__dynAny",
            ["All"] = "__dynAll",
            ["First"] = "__dynFirst",
            ["FirstOrDefault"] = "__dynFirstOrDefault",
        };

    private readonly ConcurrentDictionary<string, Lazy<CompiledExpression>> _cache =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the cached expression count.
    /// </summary>
    public int CachedExpressionCount => _cache.Count;

    /// <summary>
    /// Evaluates an expression against the provided context.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <param name="context">The context.</param>
    /// <returns>The resulting expression result.</returns>
    public ExpressionResult Evaluate(string expression, ExpressionContext context)
    {
        if (context is null)
        {
            return ExpressionResult.FailureCompilation("Expression context is required.", usedCache: false);
        }

        var normalizedExpression = NormalizeExpression(expression);
        if (normalizedExpression.Length == 0)
        {
            return ExpressionResult.FailureCompilation("Expression is empty.", usedCache: false);
        }

        var compileContext = CreateScriptCompileContext(normalizedExpression, context);

        var usedCache = _cache.TryGetValue(compileContext.CacheKey, out var cached);
        cached ??= _cache.GetOrAdd(
            compileContext.CacheKey,
            _ => new Lazy<CompiledExpression>(
                () => Compile(compileContext),
                LazyThreadSafetyMode.ExecutionAndPublication));

        var compiled = cached.Value;
        if (compiled.CompileIssue is not null)
        {
            return ExpressionResult.Failure(CloneIssue(compiled.CompileIssue), usedCache);
        }

        try
        {
            var globals = new ScriptGlobals
            {
                rootObj = compileContext.UseDynamicRoot ? DynamicValue.Wrap(context.Root) : context.Root,
                dataObj = compileContext.UseDynamicData ? DynamicValue.Wrap(context.Data) : context.Data,
                varsObj = new DynamicVars(context.Vars),
                xl = DefaultExcelFormulaHelpers,
            };

            var rawValue = compiled.Runner!(globals).GetAwaiter().GetResult();
            var value = DynamicValue.Unwrap(rawValue);
            return ExpressionResult.Success(value, usedCache);
        }
        catch (Exception ex)
        {
            if (IsNullAccessRuntimeError(ex))
            {
                return ExpressionResult.Success(null, usedCache);
            }

            return ExpressionResult.FailureRuntime($"Runtime error: {UnwrapExceptionMessage(ex)}", usedCache);
        }
    }
    /// <summary>
    /// Globals object for Roslyn script execution.
    /// </summary>
    public sealed class ScriptGlobals
    {
        /// <summary>
        /// Gets or sets wrapped root object.
        /// </summary>
        public object? rootObj { get; init; }

        /// <summary>
        /// Gets or sets wrapped data object.
        /// </summary>
        public object? dataObj { get; init; }

        /// <summary>
        /// Gets or sets wrapped vars object.
        /// </summary>
        public object? varsObj { get; init; }

        /// <summary>
        /// Gets or sets Excel formula helper object.
        /// </summary>
        public dynamic xl { get; init; } = null!;
    }

    /// <summary>
    /// Helper functions available from expression scripts as <c>xl</c>.
    /// </summary>
    public sealed class ExcelFormulaHelpers
    {
        /// <summary>
        /// Shared immutable instance.
        /// </summary>
        public static ExcelFormulaHelpers Instance { get; } = new();

        /// <summary>
        /// Builds escaped sheet token like <c>'Summary'</c>.
        /// </summary>
        /// <param name="sheetName">Target sheet name.</param>
        /// <returns>Escaped sheet token.</returns>
        public string Sheet(string? sheetName)
        {
            var safeName = NormalizeRequiredText(sheetName, nameof(sheetName));
            return $"'{safeName.Replace("'", "''", StringComparison.Ordinal)}'";
        }

        /// <summary>
        /// Builds sheet-qualified reference like <c>'Summary'!A1</c>.
        /// </summary>
        /// <param name="sheetName">Target sheet name.</param>
        /// <param name="reference">A1-style cell or range reference.</param>
        /// <returns>Sheet-qualified reference text.</returns>
        public string Ref(string? sheetName, string? reference)
        {
            var safeReference = NormalizeRequiredText(reference, nameof(reference));
            return $"{Sheet(sheetName)}!{safeReference}";
        }

        /// <summary>
        /// Builds formula text for a sheet-qualified reference like <c>='Summary'!A1</c>.
        /// </summary>
        /// <param name="sheetName">Target sheet name.</param>
        /// <param name="reference">A1-style cell or range reference.</param>
        /// <returns>Formula text.</returns>
        public string FormulaRef(string? sheetName, string? reference) =>
            $"={Ref(sheetName, reference)}";

        private static string NormalizeRequiredText(string? value, string parameterName)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                throw new ArgumentException($"Parameter '{parameterName}' must not be null or whitespace.", parameterName);
            }

            return trimmed;
        }
    }

    /// <summary>
    /// Dynamic vars wrapper for script indexer access.
    /// </summary>
    public sealed class DynamicVars
    {
        private readonly IReadOnlyDictionary<string, object?> _vars;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicVars"/> class.
        /// </summary>
        /// <param name="vars">Variable dictionary.</param>
        public DynamicVars(IReadOnlyDictionary<string, object?> vars)
        {
            _vars = vars ?? new Dictionary<string, object?>();
        }

        /// <summary>
        /// Gets a wrapped value by key.
        /// </summary>
        /// <param name="key">Variable key.</param>
        public object? this[string key] =>
            _vars.TryGetValue(key, out var value)
                ? DynamicValue.Wrap(value)
                : null;
    }

    /// <summary>
    /// Dynamic reflection wrapper used by Roslyn expressions.
    /// </summary>
    public sealed class DynamicValue : DynamicObject, IEnumerable
    {
        private readonly object _value;

        private DynamicValue(object value)
        {
            _value = value;
        }

        /// <summary>
        /// Wraps a value for dynamic member/index access when needed.
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <returns>Wrapped or original value.</returns>
        public static object? Wrap(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is DynamicValue)
            {
                return value;
            }

            return IsSimpleType(value.GetType())
                ? value
                : new DynamicValue(value);
        }

        /// <summary>
        /// Unwraps a value previously wrapped by <see cref="Wrap(object?)"/>.
        /// </summary>
        /// <param name="value">Wrapped or raw value.</param>
        /// <returns>Unwrapped value.</returns>
        public static object? Unwrap(object? value) =>
            value is DynamicValue wrapped
                ? wrapped._value
                : value;

        /// <inheritdoc/>
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (TryResolveDictionaryValue(_value, binder.Name, out var dictionaryValue))
            {
                result = Wrap(dictionaryValue);
                return true;
            }

            var type = _value.GetType();
            var property = type.GetProperty(binder.Name, BindingFlags.Instance | BindingFlags.Public);
            if (property is not null && property.GetIndexParameters().Length == 0)
            {
                result = Wrap(property.GetValue(_value));
                return true;
            }

            var field = type.GetField(binder.Name, BindingFlags.Instance | BindingFlags.Public);
            if (field is not null)
            {
                result = Wrap(field.GetValue(_value));
                return true;
            }

            result = null;
            return false;
        }

        /// <inheritdoc/>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            if (indexes.Length == 1)
            {
                var key = Unwrap(indexes[0]);

                if (key is string stringKey && TryResolveDictionaryValue(_value, stringKey, out var dictionaryValue))
                {
                    result = Wrap(dictionaryValue);
                    return true;
                }

                if (key is int index)
                {
                    if (_value is IList list)
                    {
                        result = index >= 0 && index < list.Count
                            ? Wrap(list[index])
                            : null;
                        return index >= 0 && index < list.Count;
                    }

                    if (_value is Array array)
                    {
                        result = index >= 0 && index < array.Length
                            ? Wrap(array.GetValue(index))
                            : null;
                        return index >= 0 && index < array.Length;
                    }

                    if (_value is string text)
                    {
                        result = index >= 0 && index < text.Length
                            ? text[index]
                            : null;
                        return index >= 0 && index < text.Length;
                    }
                }

                if (TryResolveIndexer(_value, key, out var indexResult))
                {
                    result = Wrap(indexResult);
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <inheritdoc/>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            var inputArgs = args ?? Array.Empty<object?>();
            var methods = _value.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.Name == binder.Name)
                .ToArray();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != inputArgs.Length)
                {
                    continue;
                }

                if (!TryConvertArguments(inputArgs, parameters, out var converted))
                {
                    continue;
                }

                result = Wrap(method.Invoke(_value, converted));
                return true;
            }

            result = null;
            return false;
        }

        /// <inheritdoc/>
        public override string? ToString() => _value.ToString();

        /// <inheritdoc/>
        public IEnumerator GetEnumerator()
        {
            if (_value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    yield return Wrap(item);
                }

                yield break;
            }

            throw new InvalidOperationException($"Type '{_value.GetType().Name}' is not enumerable.");
        }

        private static bool TryResolveIndexer(object target, object? key, out object? value)
        {
            value = null;

            var defaultMember = target.GetType().GetCustomAttribute<DefaultMemberAttribute>();
            if (defaultMember is null)
            {
                return false;
            }

            var indexer = target.GetType().GetProperty(defaultMember.MemberName, BindingFlags.Instance | BindingFlags.Public);
            if (indexer is null)
            {
                return false;
            }

            var parameters = indexer.GetIndexParameters();
            if (parameters.Length != 1)
            {
                return false;
            }

            if (!TryConvertArgument(key, parameters[0].ParameterType, out var converted))
            {
                return false;
            }

            value = indexer.GetValue(target, new[] { converted });
            return true;
        }

        private static bool TryConvertArguments(object?[] args, ParameterInfo[] parameters, out object?[] converted)
        {
            converted = new object?[args.Length];

            for (var i = 0; i < args.Length; i++)
            {
                if (!TryConvertArgument(Unwrap(args[i]), parameters[i].ParameterType, out var convertedValue))
                {
                    return false;
                }

                converted[i] = convertedValue;
            }

            return true;
        }

        private static bool TryConvertArgument(object? value, Type targetType, out object? converted)
        {
            var nullableTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value is null)
            {
                converted = null;
                return !nullableTarget.IsValueType || Nullable.GetUnderlyingType(targetType) is not null;
            }

            if (nullableTarget.IsInstanceOfType(value))
            {
                converted = value;
                return true;
            }

            try
            {
                converted = Convert.ChangeType(value, nullableTarget, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                converted = null;
                return false;
            }
        }

        private static bool IsSimpleType(Type type) =>
            type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan) ||
            type == typeof(Guid);
    }

    private static string NormalizeExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return string.Empty;
        }

        var trimmed = expression.Trim();
        return trimmed.StartsWith("@(", StringComparison.Ordinal) && trimmed.EndsWith(")", StringComparison.Ordinal)
            ? trimmed[2..^1].Trim()
            : trimmed;
    }

    private static ScriptCompileContext CreateScriptCompileContext(string expression, ExpressionContext context)
    {
        var additionalReferences = new HashSet<Assembly>();
        var rootBinding = BuildContextBinding("root", "rootObj", context.Root?.GetType(), additionalReferences);
        var dataBinding = BuildContextBinding("data", "dataObj", context.Data?.GetType(), additionalReferences);
        var scriptOptions = CreateScriptOptions(additionalReferences);
        var cacheKey = BuildCacheKey(expression, rootBinding.CacheToken, dataBinding.CacheToken);

        return new ScriptCompileContext(
            cacheKey,
            expression,
            rootBinding,
            dataBinding,
            scriptOptions,
            rootBinding.UseDynamic,
            dataBinding.UseDynamic);
    }

    private static string BuildScriptText(
        string expression,
        ContextBinding rootBinding,
        ContextBinding dataBinding,
        bool includeDynamicLinqHelpers = false)
    {
        var script =
            rootBinding.Declaration + Environment.NewLine +
            dataBinding.Declaration + Environment.NewLine +
            "dynamic vars = varsObj;" + Environment.NewLine;

        if (includeDynamicLinqHelpers)
        {
            script += BuildDynamicLinqHelperScript() + Environment.NewLine;
        }

        return script + $"return (object?)({expression});";
    }

    private static string BuildDynamicLinqHelperScript() =>
        """
        IEnumerable<dynamic?> __dynToSeq(object? source)
        {
            if (source is null)
            {
                return Array.Empty<dynamic?>();
            }

            if (source is string text)
            {
                return text.Cast<dynamic?>();
            }

            if (source is IEnumerable enumerable)
            {
                return enumerable.Cast<object?>()
                    .Select(static item => (dynamic?)global::ExcelReportLib.ExpressionEngine.ExpressionEngine.DynamicValue.Wrap(item));
            }

            throw new InvalidOperationException("Dynamic LINQ source must be IEnumerable.");
        }

        IEnumerable<dynamic?> __dynWhere(object? source, Func<dynamic?, bool> predicate) =>
            __dynToSeq(source).Where(item => predicate(item));

        IEnumerable<dynamic?> __dynSelect(object? source, Func<dynamic?, dynamic?> selector) =>
            __dynToSeq(source).Select(item => (dynamic?)global::ExcelReportLib.ExpressionEngine.ExpressionEngine.DynamicValue.Wrap(selector(item)));

        dynamic __dynSum(object? source)
        {
            dynamic total = 0;
            var hasValue = false;
            foreach (var item in __dynToSeq(source))
            {
                if (item is null)
                {
                    continue;
                }

                if (!hasValue)
                {
                    total = item;
                    hasValue = true;
                    continue;
                }

                total += item;
            }

            return hasValue ? total : 0;
        }

        dynamic __dynSum(object? source, Func<dynamic?, dynamic?> selector)
        {
            dynamic total = 0;
            var hasValue = false;
            foreach (var item in __dynToSeq(source))
            {
                var value = selector(item);
                if (value is null)
                {
                    continue;
                }

                if (!hasValue)
                {
                    total = value;
                    hasValue = true;
                    continue;
                }

                total += value;
            }

            return hasValue ? total : 0;
        }

        int __dynCount(object? source) => __dynToSeq(source).Count();

        int __dynCount(object? source, Func<dynamic?, bool> predicate) =>
            __dynToSeq(source).Count(item => predicate(item));

        bool __dynAny(object? source) => __dynToSeq(source).Any();

        bool __dynAny(object? source, Func<dynamic?, bool> predicate) =>
            __dynToSeq(source).Any(item => predicate(item));

        bool __dynAll(object? source, Func<dynamic?, bool> predicate) =>
            __dynToSeq(source).All(item => predicate(item));

        dynamic? __dynFirst(object? source) => __dynToSeq(source).First();

        dynamic? __dynFirst(object? source, Func<dynamic?, bool> predicate) =>
            __dynToSeq(source).First(item => predicate(item));

        dynamic? __dynFirstOrDefault(object? source) => __dynToSeq(source).FirstOrDefault();

        dynamic? __dynFirstOrDefault(object? source, Func<dynamic?, bool> predicate) =>
            __dynToSeq(source).FirstOrDefault(item => predicate(item));
        """;

    private static ContextBinding BuildContextBinding(
        string variableName,
        string sourceName,
        Type? runtimeType,
        ISet<Assembly> additionalReferences)
    {
        if (TryGetScriptTypeName(runtimeType, out var typeName))
        {
            CollectReferencedAssemblies(runtimeType!, additionalReferences);
            return new ContextBinding(
                $"var {variableName} = {sourceName} is {typeName} __typed_{variableName} ? __typed_{variableName} : default({typeName});",
                typeName + "@" + runtimeType!.Assembly.FullName,
                UseDynamic: false);
        }

        return new ContextBinding(
            $"dynamic {variableName} = {sourceName};",
            CacheToken: "dynamic",
            UseDynamic: true);
    }

    private static string BuildCacheKey(string expression, string rootToken, string dataToken) =>
        expression + Environment.NewLine +
        "#root:" + rootToken + Environment.NewLine +
        "#data:" + dataToken;

    private static bool TryGetScriptTypeName(Type? type, out string typeName)
    {
        typeName = string.Empty;
        if (type is null || !IsScriptVisibleType(type))
        {
            return false;
        }

        return TryFormatScriptTypeName(type, out typeName);
    }

    private static bool TryFormatScriptTypeName(Type type, out string typeName)
    {
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType is null || !TryFormatScriptTypeName(elementType, out var elementTypeName))
            {
                typeName = string.Empty;
                return false;
            }

            var commas = new string(',', type.GetArrayRank() - 1);
            typeName = $"{elementTypeName}[{commas}]";
            return IsValidScriptTypeName(typeName);
        }

        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            var genericFullName = genericDefinition.FullName;
            if (string.IsNullOrWhiteSpace(genericFullName))
            {
                typeName = string.Empty;
                return false;
            }

            var tickIndex = genericFullName.IndexOf('`');
            if (tickIndex >= 0)
            {
                genericFullName = genericFullName[..tickIndex];
            }

            var genericArguments = type.GetGenericArguments();
            var argumentTypeNames = new string[genericArguments.Length];
            for (var i = 0; i < genericArguments.Length; i++)
            {
                if (!TryFormatScriptTypeName(genericArguments[i], out argumentTypeNames[i]))
                {
                    typeName = string.Empty;
                    return false;
                }
            }

            typeName = $"global::{genericFullName.Replace('+', '.')}<{string.Join(", ", argumentTypeNames)}>";
            return IsValidScriptTypeName(typeName);
        }

        var fullName = type.FullName;
        if (string.IsNullOrWhiteSpace(fullName))
        {
            typeName = string.Empty;
            return false;
        }

        typeName = "global::" + fullName.Replace('+', '.');
        return IsValidScriptTypeName(typeName);
    }


    private static bool IsValidScriptTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        var syntax = SyntaxFactory.ParseTypeName(typeName);
        return !syntax.ContainsDiagnostics;
    }
    private static bool IsScriptVisibleType(Type type)
    {
        if (type.IsByRef || type.IsPointer || type.IsGenericTypeDefinition || type.ContainsGenericParameters)
        {
            return false;
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            return elementType is not null && IsScriptVisibleType(elementType);
        }

        if (typeof(DynamicValue).IsAssignableFrom(type))
        {
            return false;
        }

        if (IsAnonymousType(type) || !IsPubliclyAccessible(type))
        {
            return false;
        }

        if (!type.IsGenericType)
        {
            return true;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            if (!IsScriptVisibleType(argument))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPubliclyAccessible(Type type)
    {
        var current = type;
        while (current is not null)
        {
            if (!current.IsPublic && !current.IsNestedPublic)
            {
                return false;
            }

            current = current.DeclaringType!;
        }

        return true;
    }

    private static bool IsAnonymousType(Type type) =>
        type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
        && type.Name.Contains("AnonymousType", StringComparison.Ordinal)
        && (type.Name.StartsWith("<>", StringComparison.Ordinal) || type.Name.StartsWith("VB$", StringComparison.Ordinal));

    private static void CollectReferencedAssemblies(Type type, ISet<Assembly> additionalReferences)
    {
        if (type.IsByRef || type.IsPointer)
        {
            var elementType = type.GetElementType();
            if (elementType is null)
            {
                return;
            }

            type = elementType;
        }

        additionalReferences.Add(type.Assembly);

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType is not null)
            {
                CollectReferencedAssemblies(elementType, additionalReferences);
            }

            return;
        }

        if (type.DeclaringType is not null)
        {
            CollectReferencedAssemblies(type.DeclaringType, additionalReferences);
        }

        if (!type.IsGenericType)
        {
            return;
        }

        additionalReferences.Add(type.GetGenericTypeDefinition().Assembly);
        foreach (var argument in type.GetGenericArguments())
        {
            if (!argument.IsGenericParameter)
            {
                CollectReferencedAssemblies(argument, additionalReferences);
            }
        }
    }

    private static CompiledExpression Compile(ScriptCompileContext compileContext)
    {
        var primaryScriptText = BuildScriptText(
            compileContext.Expression,
            compileContext.RootBinding,
            compileContext.DataBinding,
            includeDynamicLinqHelpers: false);

        var primaryCompiled = CompileCore(primaryScriptText, compileContext.ScriptOptions);
        if (primaryCompiled.CompileIssue is null)
        {
            return primaryCompiled;
        }

        if (!compileContext.UseDynamicRoot && !compileContext.UseDynamicData)
        {
            return primaryCompiled;
        }

        var rewrittenExpression = RewriteDynamicLinqExpression(compileContext.Expression);
        if (string.Equals(rewrittenExpression, compileContext.Expression, StringComparison.Ordinal))
        {
            return primaryCompiled;
        }

        var fallbackScriptText = BuildScriptText(
            rewrittenExpression,
            compileContext.RootBinding,
            compileContext.DataBinding,
            includeDynamicLinqHelpers: true);

        var fallbackCompiled = CompileCore(fallbackScriptText, compileContext.ScriptOptions);
        return fallbackCompiled.CompileIssue is null ? fallbackCompiled : primaryCompiled;
    }

    private static CompiledExpression CompileCore(string scriptText, ScriptOptions scriptOptions)
    {
        try
        {
            var script = CSharpScript.Create<object?>(
                scriptText,
                scriptOptions,
                typeof(ScriptGlobals));

            var diagnostics = script.Compile();
            var errors = diagnostics
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(static diagnostic => diagnostic.ToString())
                .ToArray();

            if (errors.Length > 0)
            {
                return CompiledExpression.FailureCompilation(string.Join(Environment.NewLine, errors));
            }

            return CompiledExpression.Success(script.CreateDelegate());
        }
        catch (CompilationErrorException ex)
        {
            var message = string.Join(Environment.NewLine, ex.Diagnostics.Select(static diagnostic => diagnostic.ToString()));
            return CompiledExpression.FailureCompilation(message);
        }
        catch (Exception ex)
        {
            return CompiledExpression.FailureCompilation(ex.Message);
        }
    }


    private static string RewriteDynamicLinqExpression(string expression)
    {
        ExpressionSyntax syntax;
        try
        {
            syntax = SyntaxFactory.ParseExpression(expression);
        }
        catch
        {
            return expression;
        }

        if (syntax.ContainsDiagnostics)
        {
            return expression;
        }

        var rewrittenSyntax = (ExpressionSyntax)new DynamicLinqInvocationRewriter().Visit(syntax)!;
        return rewrittenSyntax.ToFullString();
    }
    private static ScriptOptions CreateDefaultScriptOptions()
    {
        var references = new HashSet<Assembly>
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(IList).Assembly,
            typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly,
            typeof(ExpressionContext).Assembly,
        };

        return ScriptOptions.Default
            .WithReferences(references)
            .WithImports(
                "System",
                "System.Linq",
                "System.Collections",
                "System.Collections.Generic");
    }

    private static ScriptOptions CreateScriptOptions(ISet<Assembly> additionalReferences)
    {
        if (additionalReferences.Count == 0)
        {
            return DefaultScriptOptions;
        }

        var references = additionalReferences
            .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location));

        return DefaultScriptOptions.WithReferences(references);
    }
    private static bool IsNullAccessRuntimeError(Exception ex)
    {
        var current = ex;
        while (current is TargetInvocationException invocationException && invocationException.InnerException is not null)
        {
            current = invocationException.InnerException;
        }

        if (current is NullReferenceException)
        {
            return true;
        }

        return current is Microsoft.CSharp.RuntimeBinder.RuntimeBinderException binderException
            && binderException.Message.Contains("null reference", StringComparison.OrdinalIgnoreCase);
    }

    private static string UnwrapExceptionMessage(Exception ex)
    {
        var current = ex;
        while (current is TargetInvocationException invocationException && invocationException.InnerException is not null)
        {
            current = invocationException.InnerException;
        }

        return current.Message;
    }

    private static bool TryResolveDictionaryValue(object current, string key, out object? value)
    {
        if (current is IReadOnlyDictionary<string, object?> readOnlyDictionary &&
            readOnlyDictionary.TryGetValue(key, out value))
        {
            return true;
        }

        if (current is IDictionary<string, object?> dictionary &&
            dictionary.TryGetValue(key, out value))
        {
            return true;
        }

        if (current is IDictionary legacyDictionary && legacyDictionary.Contains(key))
        {
            value = legacyDictionary[key];
            return true;
        }

        value = null;
        return false;
    }

    private static Issue CloneIssue(Issue issue) =>
        new()
        {
            Severity = issue.Severity,
            Kind = issue.Kind,
            Message = issue.Message,
            Span = issue.Span,
        };

    private sealed record ContextBinding(string Declaration, string CacheToken, bool UseDynamic);

    private sealed record ScriptCompileContext(
        string CacheKey,
        string Expression,
        ContextBinding RootBinding,
        ContextBinding DataBinding,
        ScriptOptions ScriptOptions,
        bool UseDynamicRoot,
        bool UseDynamicData);

    private sealed class DynamicLinqInvocationRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var visitedNode = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;

            if (visitedNode.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return visitedNode;
            }

            var methodName = memberAccess.Name.Identifier.ValueText;
            if (!DynamicLinqRewriteMap.TryGetValue(methodName, out var helperName))
            {
                return visitedNode;
            }

            if (IsStaticLinqTarget(memberAccess.Expression))
            {
                return visitedNode;
            }

            var sourceArgument = SyntaxFactory.Argument(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.ParenthesizedExpression(memberAccess.Expression.WithoutTrivia())));

            var rewrittenArguments = visitedNode.ArgumentList.Arguments
                .Insert(0, sourceArgument);
            var rewrittenNode = SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName(helperName),
                SyntaxFactory.ArgumentList(rewrittenArguments));

            return rewrittenNode.WithTriviaFrom(visitedNode);
        }

        private static bool IsStaticLinqTarget(ExpressionSyntax expression)
        {
            if (expression is IdentifierNameSyntax identifier)
            {
                return identifier.Identifier.ValueText is "Enumerable" or "Queryable";
            }

            var text = expression.ToString();
            return text.EndsWith(".Enumerable", StringComparison.Ordinal)
                || text.EndsWith(".Queryable", StringComparison.Ordinal);
        }
    }
    private sealed class CompiledExpression
    {
        private CompiledExpression(ScriptRunner<object?>? runner, Issue? compileIssue)
        {
            Runner = runner;
            CompileIssue = compileIssue;
        }

        /// <summary>
        /// Gets the compiled script runner when expression compilation succeeds.
        /// </summary>
        public ScriptRunner<object?>? Runner { get; }

        /// <summary>
        /// Gets compilation issue when expression compilation fails.
        /// </summary>
        public Issue? CompileIssue { get; }

        public static CompiledExpression Success(ScriptRunner<object?> runner) =>
            new(runner, null);

        public static CompiledExpression FailureCompilation(string message) =>
            new(
                null,
                new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.ExpressionSyntaxError,
                    Message = message,
                });
    }
}
