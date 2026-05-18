namespace EDAI.Tests.Scripting;

/// <summary>
/// Tests that compile and execute Roslyn scripts end-to-end through ScriptingService.
/// These are integration-style tests — first run is slower due to Roslyn JIT warm-up.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ScriptingServiceExecutionTests
{
    private readonly ScriptingService _service = new(NullLogger<ScriptingService>.Instance);

    private static ScriptGlobals MakeGlobals(string? triggerJson = null)
    {
        var session = new Mock<ISessionService>();
        session.Setup(s => s.ReadJson()).Returns((string?)null);
        return new ScriptGlobals(session.Object)
        {
            Trigger = triggerJson != null ? JsonNode.Parse(triggerJson) : null,
        };
    }

    // ── EvaluateConditionAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateConditionAsync_ReturnTrue_ReturnsTrue()
    {
        var result = await _service.EvaluateConditionAsync("return true;", MakeGlobals());
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateConditionAsync_ReturnFalse_ReturnsFalse()
    {
        var result = await _service.EvaluateConditionAsync("return false;", MakeGlobals());
        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateConditionAsync_ArithmeticComparison_ReturnsTrue()
    {
        var result = await _service.EvaluateConditionAsync("return 2 + 2 == 4;", MakeGlobals());
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateConditionAsync_EmptyScript_ReturnsTrue()
    {
        var result = await _service.EvaluateConditionAsync("", MakeGlobals());
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateConditionAsync_AccessTriggerEventField_ReturnsTrue()
    {
        var globals = MakeGlobals("""{"event":"FSDJump","StarSystem":"Sol"}""");
        var result = await _service.EvaluateConditionAsync(
            """return Trigger?["event"]?.ToString() == "FSDJump";""", globals);
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateConditionAsync_AccessTriggerEventField_ReturnsFalse()
    {
        var globals = MakeGlobals("""{"event":"Docked"}""");
        var result = await _service.EvaluateConditionAsync(
            """return Trigger?["event"]?.ToString() == "FSDJump";""", globals);
        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateConditionAsync_NullTrigger_NullSafeReturnsTrue()
    {
        var globals = MakeGlobals(null);
        var result = await _service.EvaluateConditionAsync("return Trigger == null;", globals);
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateConditionAsync_SecurityViolation_ReturnsFalse()
    {
        var result = await _service.EvaluateConditionAsync(
            "var a = typeof(string).Assembly; return true;", MakeGlobals());
        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateConditionAsync_SameScriptTwice_UsesCachedCompilation()
    {
        var script = "return 1 == 1;";
        var globals = MakeGlobals();
        var r1 = await _service.EvaluateConditionAsync(script, globals);
        var r2 = await _service.EvaluateConditionAsync(script, globals);
        Assert.True(r1);
        Assert.True(r2);
    }

    [Fact]
    public async Task EvaluateConditionAsync_RuntimeException_ReturnsFalse()
    {
        var result = await _service.EvaluateConditionAsync(
            "throw new Exception(\"boom\"); return true;", MakeGlobals());
        Assert.False(result);
    }

    // ── RunProcessScriptAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RunProcessScriptAsync_SetsAnnouncement_ReturnsIt()
    {
        var globals = MakeGlobals();
        var result = await _service.RunProcessScriptAsync(
            """Result.Announcement = "Hello Commander";""", globals);
        Assert.Equal("Hello Commander", result.Announcement);
    }

    [Fact]
    public async Task RunProcessScriptAsync_SetsDisplay_ReturnsIt()
    {
        var globals = MakeGlobals();
        var result = await _service.RunProcessScriptAsync(
            """Result.Display = "Sol";""", globals);
        Assert.Equal("Sol", result.Display);
    }

    [Fact]
    public async Task RunProcessScriptAsync_SetsCustomField_ReturnsIt()
    {
        var globals = MakeGlobals();
        var result = await _service.RunProcessScriptAsync(
            """Result["risk"] = "critical";""", globals);
        Assert.Equal("critical", result["risk"]);
    }

    [Fact]
    public async Task RunProcessScriptAsync_EmptyScript_ReturnsEmptyResult()
    {
        var globals = MakeGlobals();
        var result = await _service.RunProcessScriptAsync("", globals);
        Assert.Null(result.Announcement);
        Assert.Null(result.Display);
    }

    [Fact]
    public async Task RunProcessScriptAsync_SecurityViolation_ReturnsEmptyResult()
    {
        var globals = MakeGlobals();
        var result = await _service.RunProcessScriptAsync(
            """var a = typeof(string).Assembly; Result.Announcement = "bad";""", globals);
        Assert.Null(result.Announcement);
    }

    [Fact]
    public async Task RunProcessScriptAsync_UsesTriggerData_ComputesDerivedValue()
    {
        var globals = MakeGlobals("""{"event":"FSDJump","StarSystem":"Sol"}""");
        var result = await _service.RunProcessScriptAsync(
            """
            var system = Trigger?["StarSystem"]?.ToString() ?? "Unknown";
            Result.Announcement = $"Arrived at {system}";
            """, globals);
        Assert.Equal("Arrived at Sol", result.Announcement);
    }

    [Fact]
    public async Task RunProcessScriptAsync_RuntimeException_ReturnsEmptyResult()
    {
        var globals = MakeGlobals();
        var result = await _service.RunProcessScriptAsync(
            "throw new Exception(\"boom\");", globals);
        Assert.Null(result.Announcement);
    }

    // ── RunForTestAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RunForTestAsync_ConditionScript_ReturnsTrueString()
    {
        var globals = MakeGlobals();
        var result = await _service.RunForTestAsync("return true;", isProcessScript: false, globals);
        Assert.Equal("true", result);
    }

    [Fact]
    public async Task RunForTestAsync_ConditionScript_ReturnsFalseString()
    {
        var globals = MakeGlobals();
        var result = await _service.RunForTestAsync("return false;", isProcessScript: false, globals);
        Assert.Equal("false", result);
    }

    [Fact]
    public async Task RunForTestAsync_ProcessScript_ReturnsJsonResult()
    {
        var globals = MakeGlobals();
        var result = await _service.RunForTestAsync(
            """Result.Announcement = "test";""", isProcessScript: true, globals);
        Assert.Contains("\"Announcement\"", result);
        Assert.Contains("\"test\"", result);
    }

    [Fact]
    public async Task RunForTestAsync_SecurityViolation_ThrowsInvalidOperationException()
    {
        var globals = MakeGlobals();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RunForTestAsync(
                "var a = typeof(string).Assembly; return true;",
                isProcessScript: false,
                globals));
    }

    [Fact]
    public async Task RunForTestAsync_EmptyConditionScript_ReturnsFalseString()
    {
        var globals = MakeGlobals();
        var result = await _service.RunForTestAsync("", isProcessScript: false, globals);
        Assert.Equal("false", result);
    }

    [Fact]
    public async Task RunForTestAsync_EmptyProcessScript_ReturnsEmptyJson()
    {
        var globals = MakeGlobals();
        var result = await _service.RunForTestAsync("", isProcessScript: true, globals);
        Assert.Equal("{}", result);
    }
}
