using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using EDAI.Core.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.Scripting;

public sealed class ScriptingService : IScriptingService
{
    private readonly ILogger<ScriptingService> _logger;

    private volatile ScriptingPermissions _permissions = new();

    // Lazy<runner?> — null means compilation failed; cached so each unique script compiles once.
    private readonly ConcurrentDictionary<string, Lazy<ScriptRunner<bool>?>> _conditionCache = new();
    private readonly ConcurrentDictionary<string, Lazy<ScriptRunner<object?>?>> _processCache = new();

    public ScriptingService(ILogger<ScriptingService> logger)
    {
        _logger = logger;
    }

    public void UpdatePermissions(ScriptingPermissions permissions)
    {
        _permissions = permissions;
        _conditionCache.Clear();
        _processCache.Clear();
    }

    public IReadOnlyList<string> ValidateScript(string script)
    {
        if (string.IsNullOrWhiteSpace(script)) return [];

        // Parse in script mode so top-level statements and return are valid
        var parseOptions = CSharpParseOptions.Default.WithKind(SourceCodeKind.Script);
        var tree = CSharpSyntaxTree.ParseText(script, parseOptions);

        // Security check
        var walker = new ScriptSecurityWalker();
        walker.Visit(tree.GetRoot());
        if (walker.Violations.Count > 0)
        {
            _logger.LogWarning("Script rejected — security violations: {V}", string.Join("; ", walker.Violations));
            return walker.Violations;
        }

        // Syntax error check — catches typos and invalid statements
        var syntaxErrors = tree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"({d.Location.GetLineSpan().StartLinePosition.Line + 1},{d.Location.GetLineSpan().StartLinePosition.Character + 1}): {d.GetMessage()}")
            .ToList();

        if (syntaxErrors.Count > 0)
            _logger.LogWarning("Script rejected — syntax errors: {E}", string.Join("; ", syntaxErrors));

        return syntaxErrors;
    }

    public async Task<bool> EvaluateConditionAsync(
        string script, ScriptGlobals globals, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(script)) return true;

        var violations = ValidateScript(script);
        if (violations.Count > 0)
        {
            _logger.LogWarning("Condition script rejected by security walker: {V}", string.Join("; ", violations));
            return false;
        }

        var key  = CacheKey(script);
        var lazy = _conditionCache.GetOrAdd(key, _ => new Lazy<ScriptRunner<bool>?>(() =>
        {
            try
            {
                var compiled = CSharpScript.Create<bool>(script, BuildOptions(), typeof(ScriptGlobals));
                return compiled.CreateDelegate();
            }
            catch (CompilationErrorException ex)
            {
                _logger.LogWarning("Condition script compilation failed: {E}",
                    string.Join("; ", ex.Diagnostics));
                return null;
            }
        }));

        var runner = lazy.Value;
        if (runner == null) return false;

        try
        {
            return await runner(globals, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Condition script execution failed");
            return false;
        }
    }

    public async Task<ScriptResult> RunProcessScriptAsync(
        string script, ScriptGlobals globals, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(script)) return globals.Result;

        var violations = ValidateScript(script);
        if (violations.Count > 0)
        {
            _logger.LogWarning("Process script rejected by security walker: {V}", string.Join("; ", violations));
            return globals.Result;
        }

        var key  = CacheKey(script);
        var lazy = _processCache.GetOrAdd(key, _ => new Lazy<ScriptRunner<object?>?>(() =>
        {
            try
            {
                var compiled = CSharpScript.Create(script, BuildOptions(), typeof(ScriptGlobals));
                return compiled.CreateDelegate();
            }
            catch (CompilationErrorException ex)
            {
                _logger.LogWarning("Process script compilation failed: {E}",
                    string.Join("; ", ex.Diagnostics));
                return null;
            }
        }));

        var runner = lazy.Value;
        if (runner != null)
        {
            try
            {
                await runner(globals, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Process script execution failed");
            }
        }

        return globals.Result;
    }

    public async Task<string> RunForTestAsync(
        string script, bool isProcessScript, ScriptGlobals globals, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(script))
            return isProcessScript ? "{}" : "false";

        var violations = ValidateScript(script);
        if (violations.Count > 0)
            throw new InvalidOperationException("Security violations:\n" + string.Join("\n", violations));

        var options = BuildOptions();

        var consoleCapture = new StringWriter();
        var previousOut    = Console.Out;
        Console.SetOut(consoleCapture);
        try
        {
            string result;
            if (isProcessScript)
            {
                await CSharpScript.RunAsync(script, options, globals, typeof(ScriptGlobals), ct)
                    .ConfigureAwait(false);
                result = globals.Result.ToJson();
            }
            else
            {
                var state = await CSharpScript.RunAsync<bool>(script, options, globals, typeof(ScriptGlobals), ct)
                    .ConfigureAwait(false);
                result = state.ReturnValue ? "true" : "false";
            }

            var captured = consoleCapture.ToString();
            if (!string.IsNullOrEmpty(captured))
                result += "\n\n--- Console Output ---\n" + captured.TrimEnd();

            return result;
        }
        finally
        {
            Console.SetOut(previousOut);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private ScriptOptions BuildOptions()
    {
        var p = _permissions;

        var references = new List<Assembly>
        {
            typeof(object).Assembly,              // System.Runtime
            typeof(Enumerable).Assembly,          // System.Linq
            typeof(JsonNode).Assembly,            // System.Text.Json
            typeof(StringBuilder).Assembly,       // System.Text
            typeof(ScriptResult).Assembly,        // EDAI.Core
        };

        var imports = new List<string>
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "System.Text.Json",
            "System.Text.Json.Nodes",
        };

        if (p.FileSystem)        imports.Add("System.IO");
        if (p.ProcessExecution)  imports.Add("System.Diagnostics");
        if (p.Reflection)        imports.Add("System.Reflection");

        if (p.Network)
        {
            imports.Add("System.Net.Http");
            references.Add(typeof(HttpClient).Assembly);
        }

        return ScriptOptions.Default
            .WithReferences(references)
            .WithImports(imports)
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithAllowUnsafe(false);
    }

    private string CacheKey(string script)
    {
        var p   = _permissions;
        var key = $"{script}|{p.FileSystem}|{p.Network}|{p.ProcessExecution}|{p.Reflection}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }
}
