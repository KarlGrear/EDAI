namespace EDAI.Tests.Models;

public sealed class PipelineContextTests
{
    private static EventConfigurationModel DefaultConfig => new() { Title = "Test" };

    [Fact]
    public void SecondaryJson_NoEvents_ReturnsNull()
    {
        var ctx = new PipelineContext
        {
            Config = DefaultConfig,
            TriggeringEvent = new ParsedJournalEvent("FSDJump", "{}"),
        };
        Assert.Null(ctx.SecondaryJson);
    }

    [Fact]
    public void SecondaryJson_OneEvent_WrapsInJsonArray()
    {
        var ctx = new PipelineContext
        {
            Config = DefaultConfig,
            TriggeringEvent = new ParsedJournalEvent("FSDJump", "{}"),
            SecondaryEvents = [new ParsedJournalEvent("Scan", """{"event":"Scan"}""")],
        };
        Assert.Equal("""[{"event":"Scan"}]""", ctx.SecondaryJson);
    }

    [Fact]
    public void SecondaryJson_MultipleEvents_ConcatenatedWithComma()
    {
        var ctx = new PipelineContext
        {
            Config = DefaultConfig,
            TriggeringEvent = new ParsedJournalEvent("FSDJump", "{}"),
            SecondaryEvents =
            [
                new ParsedJournalEvent("Scan", """{"event":"Scan"}"""),
                new ParsedJournalEvent("Docked", """{"event":"Docked"}"""),
            ],
        };
        Assert.Equal("""[{"event":"Scan"},{"event":"Docked"}]""", ctx.SecondaryJson);
    }

    [Fact]
    public void SecondaryJson_PropertyIsRecomputedEachAccess()
    {
        var ctx = new PipelineContext
        {
            Config = DefaultConfig,
            TriggeringEvent = new ParsedJournalEvent("FSDJump", "{}"),
        };
        Assert.Null(ctx.SecondaryJson);
        Assert.Null(ctx.SecondaryJson);
    }

    [Fact]
    public void ParsedJournalEvent_IsRecord_EqualsOnValues()
    {
        var a = new ParsedJournalEvent("FSDJump", "{}");
        var b = new ParsedJournalEvent("FSDJump", "{}");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ParsedJournalEvent_DifferentEventType_NotEqual()
    {
        var a = new ParsedJournalEvent("FSDJump", "{}");
        var b = new ParsedJournalEvent("Docked", "{}");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void PipelineContext_BuiltPrompt_InitiallyNull()
    {
        var ctx = new PipelineContext
        {
            Config = DefaultConfig,
            TriggeringEvent = new ParsedJournalEvent("FSDJump", "{}"),
        };
        Assert.Null(ctx.BuiltPrompt);
    }

    [Fact]
    public void PipelineContext_BuiltPrompt_CanBeSet()
    {
        var ctx = new PipelineContext
        {
            Config = DefaultConfig,
            TriggeringEvent = new ParsedJournalEvent("FSDJump", "{}"),
        };
        ctx.BuiltPrompt = "Hello";
        Assert.Equal("Hello", ctx.BuiltPrompt);
    }
}
