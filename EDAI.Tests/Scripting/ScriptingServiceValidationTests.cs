namespace EDAI.Tests.Scripting;

public sealed class ScriptingServiceValidationTests
{
    private readonly ScriptingService _service = new(NullLogger<ScriptingService>.Instance);

    // ── Blank / null scripts ──────────────────────────────────────────────────

    [Fact]
    public void ValidateScript_NullScript_ReturnsNoViolations()
    {
        var errors = _service.ValidateScript(null!);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateScript_EmptyScript_ReturnsNoViolations()
    {
        var errors = _service.ValidateScript("");
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateScript_WhitespaceScript_ReturnsNoViolations()
    {
        var errors = _service.ValidateScript("   ");
        Assert.Empty(errors);
    }

    // ── Valid scripts ─────────────────────────────────────────────────────────

    [Fact]
    public void ValidateScript_ReturnTrue_NoErrors()
    {
        var errors = _service.ValidateScript("return true;");
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateScript_ReturnFalse_NoErrors()
    {
        var errors = _service.ValidateScript("return false;");
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateScript_ArithmeticExpression_NoErrors()
    {
        var errors = _service.ValidateScript("return 1 + 1 == 2;");
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateScript_NullCheckOnGlobal_NoErrors()
    {
        var errors = _service.ValidateScript("return Trigger != null;");
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateScript_StringManipulation_NoErrors()
    {
        var errors = _service.ValidateScript(
            """
            var s = "hello";
            return s.ToUpper() == "HELLO";
            """);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateScript_LinqUsage_NoErrors()
    {
        var errors = _service.ValidateScript(
            """
            var list = new List<int> { 1, 2, 3 };
            return list.Any(x => x > 2);
            """);
        Assert.Empty(errors);
    }

    // ── Security violations: reflection ──────────────────────────────────────

    [Fact]
    public void ValidateScript_DotAssemblyAccess_ReturnsViolation()
    {
        var errors = _service.ValidateScript("var a = typeof(string).Assembly;");
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains(".Assembly"));
    }

    [Fact]
    public void ValidateScript_GetMethod_ReturnsViolation()
    {
        var errors = _service.ValidateScript(
            """var m = typeof(string).GetMethod("ToString");""");
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains(".GetMethod"));
    }

    [Fact]
    public void ValidateScript_GetProperty_ReturnsViolation()
    {
        var errors = _service.ValidateScript(
            """var p = typeof(string).GetProperty("Length");""");
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains(".GetProperty"));
    }

    [Fact]
    public void ValidateScript_GetField_ReturnsViolation()
    {
        var errors = _service.ValidateScript(
            """var f = typeof(string).GetField("Empty");""");
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains(".GetField"));
    }

    // ── Security violations: Activator / Assembly ─────────────────────────────

    [Fact]
    public void ValidateScript_ActivatorCreateInstance_ReturnsViolation()
    {
        var errors = _service.ValidateScript(
            "var obj = Activator.CreateInstance(typeof(object));");
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ValidateScript_AssemblyLoad_ReturnsViolation()
    {
        var errors = _service.ValidateScript(
            """var asm = Assembly.Load("System");""");
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ValidateScript_AssemblyLoadFrom_ReturnsViolation()
    {
        var errors = _service.ValidateScript(
            """Assembly.LoadFrom("malicious.dll");""");
        Assert.NotEmpty(errors);
    }

    // ── Security violations: AppDomain ────────────────────────────────────────

    [Fact]
    public void ValidateScript_AppDomainReference_ReturnsViolation()
    {
        var errors = _service.ValidateScript("var d = AppDomain.CurrentDomain;");
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("AppDomain"));
    }

    // ── Security violations: unsafe / extern ──────────────────────────────────

    [Fact]
    public void ValidateScript_UnsafeBlock_ReturnsViolation()
    {
        var errors = _service.ValidateScript("unsafe { int x = 0; }");
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Unsafe") || e.Contains("unsafe"));
    }

    [Fact]
    public void ValidateScript_DllImportAttribute_ReturnsViolation()
    {
        var errors = _service.ValidateScript(
            """
            [DllImport("kernel32.dll")]
            static extern bool SomeFunction();
            """);
        Assert.NotEmpty(errors);
    }

    // ── Syntax errors ─────────────────────────────────────────────────────────

    [Fact]
    public void ValidateScript_MissingExpression_ReturnsSyntaxError()
    {
        var errors = _service.ValidateScript("int x = ;");
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ValidateScript_MissingClosingParen_ReturnsSyntaxError()
    {
        var errors = _service.ValidateScript("if (true {");
        Assert.NotEmpty(errors);
    }

    // ── Permission-independent caching ───────────────────────────────────────

    [Fact]
    public void ValidateScript_CalledTwiceWithSameScript_BothReturnSameResult()
    {
        var script = "return true;";
        var r1 = _service.ValidateScript(script);
        var r2 = _service.ValidateScript(script);
        Assert.Equal(r1.Count, r2.Count);
    }

    [Fact]
    public void UpdatePermissions_ClearsCachedScripts_ValidateStillWorks()
    {
        var script = "return true;";
        _service.ValidateScript(script);
        _service.UpdatePermissions(new ScriptingPermissions { FileSystem = true });
        var errors = _service.ValidateScript(script);
        Assert.Empty(errors);
    }
}
