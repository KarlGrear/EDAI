using EDAI.Core.Scripting;

namespace EDAI.Core.Interfaces;

public interface IScriptingService
{
    /// <summary>Static syntax + security check. Returns violation messages; empty means safe to compile.</summary>
    IReadOnlyList<string> ValidateScript(string script);

    /// <summary>Compiles and runs a condition script. Returns true if the condition passes.</summary>
    Task<bool> EvaluateConditionAsync(string script, ScriptGlobals globals, CancellationToken ct = default);

    /// <summary>Compiles and runs a process script. Returns the populated ScriptResult.</summary>
    Task<ScriptResult> RunProcessScriptAsync(string script, ScriptGlobals globals, CancellationToken ct = default);

    /// <summary>Replaces active permissions and clears the compiled-script cache.</summary>
    void UpdatePermissions(ScriptingPermissions permissions);

    /// <summary>
    /// Compiles and runs a script for the designer test runner.
    /// Returns the result JSON (process) or "true"/"false" (condition).
    /// Propagates compilation and runtime errors instead of swallowing them.
    /// </summary>
    Task<string> RunForTestAsync(string script, bool isProcessScript, ScriptGlobals globals, CancellationToken ct = default);
}
