using System.Globalization;

namespace EDAI.Core.Pipeline;

/// <summary>
/// Evaluates a condition string after resolving template tokens.
/// A blank or null condition is always true.
///
/// Supported operators (standard precedence — AND binds tighter than OR):
///   Logical:    ||  &amp;&amp;
///   Comparison: ==  !=  &gt;=  &lt;=  &gt;  &lt;
///
/// Values: double-quoted strings, numeric literals, boolean literals (true/false).
/// Bare non-empty values that are not boolean literals are truthy.
/// Unknown tokens are left as-is after template resolution.
/// </summary>
public static class ConditionEvaluator
{
    private static readonly string[] ComparisonOps = ["==", "!=", ">=", "<=", ">", "<"];

    public static bool Evaluate(
        string? condition,
        string? triggerJson,
        string? resultJson,
        Func<string, string?>? auxProvider = null,
        string? secondaryJson = null)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;

        var resolved = TemplateEngine.Apply(condition, triggerJson, resultJson, auxProvider, secondaryJson).Trim();

        if (string.IsNullOrWhiteSpace(resolved)) return true;

        // OR has lowest precedence: any clause being true makes the whole thing true.
        return SplitOn(resolved, "||").Any(clause => EvaluateAndClause(clause.Trim()));
    }

    private static bool EvaluateAndClause(string clause)
    {
        if (string.IsNullOrWhiteSpace(clause)) return false;
        // AND: every part must be true.
        return SplitOn(clause, "&&").All(part => EvaluateComparison(part.Trim()));
    }

    private static bool EvaluateComparison(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) return false;
        if (bool.TryParse(expr, out var boolVal)) return boolVal;

        var found = FindComparisonOperator(expr);
        if (found is null)
            return !string.IsNullOrWhiteSpace(expr); // bare non-empty value is truthy

        var (op, idx) = found.Value;
        var left  = expr[..idx].Trim();
        var right = expr[(idx + op.Length)..].Trim();
        return Compare(left, right, op);
    }

    // Splits on a two-character logical operator (|| or &&), respecting quoted strings.
    private static IEnumerable<string> SplitOn(string expr, string separator)
    {
        bool inQuote = false;
        char quoteChar = '"';
        int start = 0;

        for (int i = 0; i < expr.Length; i++)
        {
            var c = expr[i];
            if (inQuote)
            {
                if (c == quoteChar) inQuote = false;
                continue;
            }
            if (c == '"' || c == '\'')
            {
                inQuote = true;
                quoteChar = c;
                continue;
            }
            if (i + separator.Length <= expr.Length && expr[i..].StartsWith(separator))
            {
                yield return expr[start..i];
                start = i + separator.Length;
                i = start - 1; // compensate for loop increment
            }
        }
        yield return expr[start..];
    }

    // Finds the first comparison operator outside quoted strings. Longest operators
    // are checked first so >= is never misread as >.
    private static (string op, int idx)? FindComparisonOperator(string expr)
    {
        bool inQuote = false;
        char quoteChar = '"';

        for (int i = 0; i < expr.Length; i++)
        {
            var c = expr[i];
            if (inQuote)
            {
                if (c == quoteChar) inQuote = false;
                continue;
            }
            if (c == '"' || c == '\'')
            {
                inQuote = true;
                quoteChar = c;
                continue;
            }
            foreach (var op in ComparisonOps)
            {
                if (expr[i..].StartsWith(op))
                    return (op, i);
            }
        }
        return null;
    }

    private static bool Compare(string left, string right, string op)
    {
        left  = Unquote(left);
        right = Unquote(right);

        if (double.TryParse(left,  NumberStyles.Any, CultureInfo.InvariantCulture, out var lNum) &&
            double.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out var rNum))
        {
            return op switch
            {
                "==" => lNum == rNum,
                "!=" => lNum != rNum,
                ">"  => lNum > rNum,
                "<"  => lNum < rNum,
                ">=" => lNum >= rNum,
                "<=" => lNum <= rNum,
                _    => false,
            };
        }

        var cmp = string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        return op switch
        {
            "==" => cmp == 0,
            "!=" => cmp != 0,
            ">"  => cmp > 0,
            "<"  => cmp < 0,
            ">=" => cmp >= 0,
            "<=" => cmp <= 0,
            _    => false,
        };
    }

    private static string Unquote(string s)
    {
        if (s.Length >= 2 &&
            ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
            return s[1..^1];
        return s;
    }
}
