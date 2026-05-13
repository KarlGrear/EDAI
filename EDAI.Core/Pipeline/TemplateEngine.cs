using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Path;

namespace EDAI.Core.Pipeline;

/// <summary>
/// Resolves |expression| tokens in template strings using JSONPath syntax.
///
/// Built-in prefixes:
///   trigger.        — the triggering journal event JSON
///   result.         — the AI response JSON
///   secondary[N].   — element N of the secondary events JSON array
///
/// Aux-file prefixes (resolved via <paramref name="auxProvider"/>):
///   status.       market.    modulesinfo.   navroute.
///   outfitting.   shiplocker.   shipyard.
///
/// Function wrappers (applied around any prefix expression):
///   count(prefix.path)    — number of JSONPath matches as a string
///   count(secondary)      — total number of secondary events
///
/// Examples:
///   |trigger|                                                        → full trigger JSON string
///   |trigger.StarSystem|                                             → "Sol"
///   |trigger.StarPos[0]|                                             → "-25.09"
///   |trigger.Factions[?@.Allegiance=="Federation"].Name|             → "Fed Faction"
///   |count(trigger.Factions[?@.FactionState=="Election"])|           → "2"
///   |result|                                                         → full result JSON string
///   |result.threat_level|                                            → "High"
///   |secondary|                                                      → full secondary array JSON
///   |secondary[0]|                                                   → full first event JSON
///   |secondary[0].event|                                             → "FSDJump"
///   |secondary[0].StarSystem|                                        → "Sol"
///   |count(secondary)|                                               → "3"
///   |count(secondary[0].Factions)|                                   → "2"
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
        Func<string, string?>? auxProvider = null,
        string? secondaryJson = null)
    {
        if (!template.Contains('|')) return template;

        // Lazily parsed — only allocated when a token for that source is first encountered.
        JsonNode? triggerNode   = null;
        JsonNode? resultNode    = null;
        JsonNode? secondaryNode = null;
        Dictionary<string, JsonNode?>? auxNodes = null;

        return TokenRegex.Replace(template, m =>
        {
            var expr = m.Groups[1].Value.Trim();

            // ── count() wrapper ───────────────────────────────────────────────
            bool isCount = expr.StartsWith("count(", StringComparison.OrdinalIgnoreCase) && expr.EndsWith(")");
            var inner = isCount ? expr[6..^1].Trim() : expr;

            // ── Built-in: trigger / result ────────────────────────────────────
            if (string.Equals(inner, "trigger", StringComparison.OrdinalIgnoreCase))
                return isCount ? m.Value : (triggerJson ?? m.Value);

            if (inner.StartsWith("trigger.", StringComparison.OrdinalIgnoreCase))
            {
                triggerNode ??= ParseNode(triggerJson);
                if (triggerNode == null) return m.Value;
                var path = "$." + inner[8..];
                return (isCount ? EvaluateCount(triggerNode, path) : Evaluate(triggerNode, path)) ?? m.Value;
            }

            if (string.Equals(inner, "result", StringComparison.OrdinalIgnoreCase))
                return isCount ? m.Value : (resultJson ?? m.Value);

            if (inner.StartsWith("result.", StringComparison.OrdinalIgnoreCase))
            {
                resultNode ??= ParseNode(resultJson);
                if (resultNode == null) return m.Value;
                var path = "$." + inner[7..];
                return (isCount ? EvaluateCount(resultNode, path) : Evaluate(resultNode, path)) ?? m.Value;
            }

            // ── secondary[N].path or count(secondary) ────────────────────────
            if (inner.StartsWith("secondary", StringComparison.OrdinalIgnoreCase))
            {
                secondaryNode ??= ParseNode(secondaryJson);
                if (secondaryNode is not JsonArray arr) return m.Value;

                // |secondary| — full array JSON; count(secondary) — total count
                if (string.Equals(inner, "secondary", StringComparison.OrdinalIgnoreCase))
                    return isCount ? arr.Count.ToString() : (secondaryJson ?? m.Value);

                if (!inner.StartsWith("secondary[", StringComparison.OrdinalIgnoreCase))
                    return m.Value;

                var closeBracket = inner.IndexOf(']', 10);
                if (closeBracket < 0 || !int.TryParse(inner[10..closeBracket], out var idx))
                    return m.Value;
                if (idx < 0 || idx >= arr.Count) return m.Value;

                var element = arr[idx];
                if (element is null) return m.Value;

                var afterBracket = inner[(closeBracket + 1)..];
                if (afterBracket.StartsWith('.')) afterBracket = afterBracket[1..];

                if (string.IsNullOrEmpty(afterBracket))
                    return isCount ? m.Value : element.ToJsonString();

                var secPath = "$." + afterBracket;
                return (isCount ? EvaluateCount(element, secPath) : Evaluate(element, secPath)) ?? m.Value;
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
