using System.Collections;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using ExcelReportLib.DSL;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ExcelReportLib.ExpressionEngine;

/// <summary>
/// Represents expression engine.
/// </summary>
public sealed class ExpressionEngine : IExpressionEngine, IExpressionEvaluator
{
    private static readonly ScriptOptions DefaultScriptOptions = CreateDefaultScriptOptions();

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
        var scriptText = BuildScriptText(expression, rootBinding, dataBinding);
        var scriptOptions = CreateScriptOptions(additionalReferences);
        var cacheKey = BuildCacheKey(expression, rootBinding.CacheToken, dataBinding.CacheToken);

        return new ScriptCompileContext(
            cacheKey,
            scriptText,
            scriptOptions,
            rootBinding.UseDynamic,
            dataBinding.UseDynamic);
    }

    private static string BuildScriptText(string expression, ContextBinding rootBinding, ContextBinding dataBinding) =>
        rootBinding.Declaration + Environment.NewLine +
        dataBinding.Declaration + Environment.NewLine +
        "dynamic vars = varsObj;" + Environment.NewLine +
        $"return (object?)({expression});";

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
            return true;
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
            return true;
        }

        var fullName = type.FullName;
        if (string.IsNullOrWhiteSpace(fullName))
        {
            typeName = string.Empty;
            return false;
        }

        typeName = "global::" + fullName.Replace('+', '.');
        return true;
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
        try
        {
            var script = CSharpScript.Create<object?>(
                compileContext.ScriptText,
                compileContext.ScriptOptions,
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
        string ScriptText,
        ScriptOptions ScriptOptions,
        bool UseDynamicRoot,
        bool UseDynamicData);
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


