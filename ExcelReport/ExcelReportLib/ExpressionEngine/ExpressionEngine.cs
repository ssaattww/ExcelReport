using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Threading;
using ExcelReportLib.DSL;

namespace ExcelReportLib.ExpressionEngine;

public sealed class ExpressionEngine : IExpressionEngine, IExpressionEvaluator
{
    private readonly ConcurrentDictionary<string, Lazy<CompiledExpression>> _cache =
        new(StringComparer.Ordinal);

    public int CachedExpressionCount => _cache.Count;

    public ExpressionResult Evaluate(string expression, ExpressionContext context)
    {
        if (context is null)
        {
            return ExpressionResult.Failure("Expression context is required.", usedCache: false);
        }

        var normalizedExpression = NormalizeExpression(expression);
        if (normalizedExpression.Length == 0)
        {
            return ExpressionResult.Failure("Expression is empty.", usedCache: false);
        }

        var usedCache = _cache.TryGetValue(normalizedExpression, out var cached);
        cached ??= _cache.GetOrAdd(
            normalizedExpression,
            key => new Lazy<CompiledExpression>(() => Compile(key), LazyThreadSafetyMode.ExecutionAndPublication));

        var compiled = cached.Value;
        if (compiled.CompileIssue is not null)
        {
            return ExpressionResult.Failure(CloneIssue(compiled.CompileIssue), usedCache);
        }

        try
        {
            var value = compiled.Accessor!(context);
            return ExpressionResult.Success(value, usedCache);
        }
        catch (Exception ex)
        {
            return ExpressionResult.Failure($"Runtime error: {ex.Message}", usedCache);
        }
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

    private static CompiledExpression Compile(string expression)
    {
        var parser = new ExpressionParser(expression);
        if (!parser.TryParse(out var parsedExpression, out var errorMessage))
        {
            return CompiledExpression.Failure(errorMessage);
        }

        if (!IsSupportedRoot(parsedExpression.RootName))
        {
            return CompiledExpression.Failure(
                $"Unsupported root '{parsedExpression.RootName}'. Use root, data, or vars.");
        }

        object? Accessor(ExpressionContext context)
        {
            var current = ResolveRoot(parsedExpression.RootName, context);

            foreach (var segment in parsedExpression.Segments)
            {
                if (current is null)
                {
                    return null;
                }

                current = segment switch
                {
                    MemberSegment member => ResolveMember(current, member.Name),
                    IndexSegment index => ResolveIndex(current, index.Key),
                    _ => throw new InvalidOperationException("Unknown expression segment."),
                };
            }

            return current;
        }

        return CompiledExpression.Success(Accessor);
    }

    private static bool IsSupportedRoot(string rootName) =>
        rootName is "root" or "data" or "vars";

    private static object? ResolveRoot(string rootName, ExpressionContext context) =>
        rootName switch
        {
            "root" => context.Root,
            "data" => context.Data,
            "vars" => context.Vars,
            _ => throw new InvalidOperationException(
                $"Unsupported root '{rootName}'. Use root, data, or vars."),
        };

    private static object? ResolveMember(object current, string memberName)
    {
        if (TryResolveDictionaryValue(current, memberName, out var dictionaryValue))
        {
            return dictionaryValue;
        }

        var type = current.GetType();
        var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (property is not null && property.GetIndexParameters().Length == 0)
        {
            return property.GetValue(current);
        }

        var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (field is not null)
        {
            return field.GetValue(current);
        }

        throw new InvalidOperationException(
            $"Member '{memberName}' was not found on type '{type.Name}'.");
    }

    private static object? ResolveIndex(object current, object key)
    {
        if (key is string stringKey && TryResolveDictionaryValue(current, stringKey, out var dictionaryValue))
        {
            return dictionaryValue;
        }

        if (key is int index)
        {
            if (current is IList list)
            {
                if (index < 0 || index >= list.Count)
                {
                    throw new InvalidOperationException(
                        $"Index {index} is out of range for a collection with {list.Count} items.");
                }

                return list[index];
            }

            if (current is Array array)
            {
                if (index < 0 || index >= array.Length)
                {
                    throw new InvalidOperationException(
                        $"Index {index} is out of range for an array with {array.Length} items.");
                }

                return array.GetValue(index);
            }

            if (current is string text)
            {
                if (index < 0 || index >= text.Length)
                {
                    throw new InvalidOperationException(
                        $"Index {index} is out of range for a string with {text.Length} characters.");
                }

                return text[index];
            }
        }

        var defaultMemberAttribute = current.GetType().GetCustomAttribute<DefaultMemberAttribute>();
        if (!string.IsNullOrWhiteSpace(defaultMemberAttribute?.MemberName))
        {
            var indexer = current.GetType()
                .GetProperty(defaultMemberAttribute.MemberName, BindingFlags.Instance | BindingFlags.Public);
            if (indexer is not null)
            {
                var parameters = indexer.GetIndexParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(key))
                {
                    return indexer.GetValue(current, new[] { key });
                }
            }
        }

        throw new InvalidOperationException(
            $"Index access is not supported for type '{current.GetType().Name}'.");
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

    private sealed class CompiledExpression
    {
        private CompiledExpression(Func<ExpressionContext, object?>? accessor, Issue? compileIssue)
        {
            Accessor = accessor;
            CompileIssue = compileIssue;
        }

        public Func<ExpressionContext, object?>? Accessor { get; }

        public Issue? CompileIssue { get; }

        public static CompiledExpression Success(Func<ExpressionContext, object?> accessor) =>
            new(accessor, null);

        public static CompiledExpression Failure(string message) =>
            new(
                null,
                new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.ExpressionSyntaxError,
                    Message = message,
                });
    }

    private sealed record ParsedExpression(string RootName, IReadOnlyList<ExpressionSegment> Segments);

    private abstract record ExpressionSegment;

    private sealed record MemberSegment(string Name) : ExpressionSegment;

    private sealed record IndexSegment(object Key) : ExpressionSegment;

    private sealed class ExpressionParser
    {
        private readonly string _text;
        private int _position;

        public ExpressionParser(string text)
        {
            _text = text;
        }

        public bool TryParse(out ParsedExpression parsedExpression, out string errorMessage)
        {
            parsedExpression = new ParsedExpression(string.Empty, Array.Empty<ExpressionSegment>());

            SkipWhitespace();
            if (!TryParseIdentifier(out var rootName))
            {
                errorMessage = GetError("Expression must start with root, data, or vars.");
                return false;
            }

            var segments = new List<ExpressionSegment>();
            while (true)
            {
                SkipWhitespace();
                if (IsEnd)
                {
                    parsedExpression = new ParsedExpression(rootName, segments);
                    errorMessage = string.Empty;
                    return true;
                }

                if (Current == '.')
                {
                    _position++;
                    SkipWhitespace();
                    if (!TryParseIdentifier(out var memberName))
                    {
                        errorMessage = GetError("A member name is required after '.'.");
                        return false;
                    }

                    segments.Add(new MemberSegment(memberName));
                    continue;
                }

                if (Current == '[')
                {
                    _position++;
                    SkipWhitespace();
                    if (!TryParseIndexKey(out var key, out errorMessage))
                    {
                        return false;
                    }

                    SkipWhitespace();
                    if (IsEnd || Current != ']')
                    {
                        errorMessage = GetError("Missing closing ']'.");
                        return false;
                    }

                    _position++;
                    segments.Add(new IndexSegment(key));
                    continue;
                }

                errorMessage = GetError($"Unsupported token '{Current}'.");
                return false;
            }
        }

        private bool TryParseIdentifier(out string identifier)
        {
            identifier = string.Empty;
            if (IsEnd || (!char.IsLetter(Current) && Current != '_'))
            {
                return false;
            }

            var start = _position;
            _position++;

            while (!IsEnd && (char.IsLetterOrDigit(Current) || Current == '_'))
            {
                _position++;
            }

            identifier = _text[start.._position];
            return true;
        }

        private bool TryParseIndexKey(out object key, out string errorMessage)
        {
            key = string.Empty;
            errorMessage = string.Empty;

            if (IsEnd)
            {
                errorMessage = GetError("An index value is required.");
                return false;
            }

            if (Current is '"' or '\'')
            {
                return TryParseStringLiteral(out key, out errorMessage);
            }

            if (!TryParseInteger(out var intValue))
            {
                errorMessage = GetError("Only integer and string indexers are supported.");
                return false;
            }

            key = intValue;
            return true;
        }

        private bool TryParseInteger(out int value)
        {
            value = 0;
            if (IsEnd)
            {
                return false;
            }

            var negative = false;
            if (Current == '-')
            {
                negative = true;
                _position++;
            }

            if (IsEnd || !char.IsDigit(Current))
            {
                return false;
            }

            var start = _position;
            while (!IsEnd && char.IsDigit(Current))
            {
                _position++;
            }

            if (!int.TryParse(_text[start.._position], out value))
            {
                return false;
            }

            if (negative)
            {
                value = -value;
            }

            return true;
        }

        private bool TryParseStringLiteral(out object value, out string errorMessage)
        {
            value = string.Empty;
            errorMessage = string.Empty;

            var delimiter = Current;
            _position++;

            var builder = new StringBuilder();
            while (!IsEnd)
            {
                var current = Current;
                _position++;

                if (current == delimiter)
                {
                    value = builder.ToString();
                    return true;
                }

                if (current == '\\')
                {
                    if (IsEnd)
                    {
                        errorMessage = GetError("Incomplete escape sequence.");
                        return false;
                    }

                    var escaped = Current;
                    _position++;
                    builder.Append(escaped switch
                    {
                        '\\' => '\\',
                        '"' => '"',
                        '\'' => '\'',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => escaped,
                    });
                    continue;
                }

                builder.Append(current);
            }

            errorMessage = GetError("Unterminated string literal.");
            return false;
        }

        private void SkipWhitespace()
        {
            while (!IsEnd && char.IsWhiteSpace(Current))
            {
                _position++;
            }
        }

        private string GetError(string message) =>
            $"{message} (position {_position + 1})";

        private bool IsEnd => _position >= _text.Length;

        private char Current => _text[_position];
    }
}
