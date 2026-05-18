namespace EDAI.Core.Scripting;

/// <summary>
/// Controls which additional namespaces are available to scripts.
/// All permissions are off by default — users opt in via App Settings.
/// </summary>
public sealed class ScriptingPermissions
{
    public bool FileSystem { get; set; }
    public bool Network { get; set; }
    public bool ProcessExecution { get; set; }
    public bool Reflection { get; set; }
}
