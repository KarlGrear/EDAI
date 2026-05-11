using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Path;

namespace EDAI.Core.Pipeline;

/// <summary>
/// Resolves |expression| tokens in template strings using JSONPath syntax.
///
/// Built-in prefixes:
///   trigger.  — the triggering journal event JSON
///   result.   — the AI response JSON
///
/// Aux-file prefixes (resolved via <paramref name="auxProvider"/>):
///   status.       market.    modulesinfo.   navroute.
///   outfitting.   shiplocker.   shipyard.
///
/// Function wrappers (applied around any prefix expression):
///   count(prefix.path)  — number of JSONPath matches as a string
///
/// Examples:
///   |trigger.StarSystem|                                              → "Sol"
///   |trigger.StarPos[0]|                                             → "-25.09"
///   |trigger.Factions[?@.Allegiance=="Federation"].Name|             → "Fed Faction"
///   |count(trigger.Factions[?@.FactionState=="Election"])|           → "2"
///   |result.threat_level|                                            → "High"
///   |status.FireGroup|                                               → "0"
///   |navroute.Route[0].StarSystem|                                   → "Shinrarta Dezhra"
///
/// Filter expressions use RFC 9535 syntax: ?@.Field == "Value" (not the older ?(@.Field)).
/// Unknown tokens (unrecognised prefix or path not found) are left as-is.
/// </summary>
public static class TemplateEngine
{
    private static readonly Regex TokenRegex = new(@"\|([^|]+)\|", RegexOptions.Compiled);

    public static string Apply(
        string template,
        string? triggerJson,
        string? resultJson,
        Func<string, string?>? auxProvider = null)
    {
        if (!template.Contains('|')) return template;

        // Lazily parsed — only allocated when a token for that source is first encountered.
        JsonNode? triggerNode = null;
        JsonNode? resultNode  = null;
        Dictionary<string, JsonNode?>? auxNodes = null;

        return TokenRegex.Replace(template, m =>
        {
            var expr = m.Groups[1].Value.Trim();

            // ── count() wrapper ───────────────────────────────────────────────
            bool isCount = expr.StartsWith("count(", StringComparison.OrdinalIgnoreCase) && expr.EndsWith(")");
            var inner = isCount ? expr[6..^1].Trim() : expr;

            // ── Built-in: trigger / result ────────────────────────────────────
            if (inner.StartsWith("trigger.", StringComparison.OrdinalIgnoreCase))
            {
                triggerNode ??= ParseNode(triggerJson);
                if (triggerNode == null) return m.Value;
                var path = "$." + inner[8..];
                return (isCount ? EvaluateCount(triggerNode, path) : Evaluate(triggerNode, path)) ?? m.Value;
            }

            if (inner.StartsWith("result.", StringComparison.OrdinalIgnoreCase))
            {
                resultNode ??= ParseNode(resultJson);
                if (resultNode == null) return m.Value;
                var path = "$." + inner[7..];
                return (isCount ? EvaluateCount(resultNode, path) : Evaluate(resultNode, path)) ?? m.Value;
            }

            // ── Aux files: any identifier.path ───────────────────────────────
            if (auxProvider == null) return m.Value;

            var dot = inner.IndexOf('.');
            if (dot <= 0) return m.Value;

            var fileId     = inner[..dot];
            var pathSuffix = inner[(dot + 1)..];

            auxNodes ??= [];
            if (!auxNodes.TryGetValue(fileId, out var auxNode))
            {
                var json = auxProvider(fileId);
                auxNode = ParseNode(json);
                auxNodes[fileId] = auxNode;
            }

            if (auxNode == null) return m.Value;
            var auxPath = "$." + pathSuffix;
            return (isCount ? EvaluateCount(auxNode, auxPath) : Evaluate(auxNode, auxPath)) ?? m.Value;
        });
    }

    private static JsonNode? ParseNode(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonNode.Parse(json); }
        catch { return null; }
    }

    private static string? Evaluate(JsonNode root, string path)
    {
        try
        {
            var jsonPath = JsonPath.Parse(path);
            var result   = jsonPath.Evaluate(root);
            var matches  = result.Matches.ToList();

            if (matches.Count == 0) return null;
            if (matches.Count == 1) return Stringify(matches[0].Value);

            return string.Join(", ", matches.Select(r => Stringify(r.Value) ?? string.Empty));
        }
        catch { return null; }
    }

    private static string? EvaluateCount(JsonNode root, string path)
    {
        try
        {
            var jsonPath = JsonPath.Parse(path);
            var result   = jsonPath.Evaluate(root);
            return result.Matches.Count.ToString();
        }
        catch { return null; }
    }

    private static string? Stringify(JsonNode? node) => node switch
    {
        null        => null,
        JsonValue v => v.ToString(),
        _           => node.ToJsonString()
    };
}
