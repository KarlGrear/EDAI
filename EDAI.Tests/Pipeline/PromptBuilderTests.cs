namespace EDAI.Tests.Pipeline;

public sealed class PromptBuilderTests
{
    private static PromptBuilder MakeBuilder(Func<string, string?>? auxProvider = null)
    {
        var auxReader = new Mock<IJournalAuxFileReader>();
        auxReader.Setup(a => a.Read(It.IsAny<string>()))
                 .Returns<string>(id => auxProvider?.Invoke(id));
        return new PromptBuilder(auxReader.Object);
    }

    private static PipelineContext SimpleContext(
        string prompt = "Analyze this event.",
        string triggerRaw = """{"event":"FSDJump","StarSystem":"Sol"}""",
        string? schema = null,
        IReadOnlyList<ParsedJournalEvent>? secondary = null)
    {
        var config = new EventConfigurationModel
        {
            Title = "Test",
            Prompt = prompt,
            ExpectedResultsSchema = schema,
        };
        return new PipelineContext
        {
            Config = config,
            TriggeringEvent = new ParsedJournalEvent("FSDJump", triggerRaw),
            SecondaryEvents = secondary ?? [],
        };
    }

    [Fact]
    public void Build_BasicPrompt_IncludesPromptText()
    {
        var result = MakeBuilder().Build(SimpleContext("Tell me about this jump."));
        Assert.Contains("Tell me about this jump.", result);
    }

    [Fact]
    public void Build_PromptWithTriggerToken_TokenResolved()
    {
        var result = MakeBuilder().Build(SimpleContext("Jump to |trigger.StarSystem|."));
        Assert.Contains("Jump to Sol.", result);
    }

    [Fact]
    public void Build_WithSchema_IncludesSchemaInstruction()
    {
        var result = MakeBuilder().Build(SimpleContext(schema: """{"threat_level":"string"}"""));
        Assert.Contains("Respond with a JSON object matching exactly this schema:", result);
        Assert.Contains("""{"threat_level":"string"}""", result);
    }

    [Fact]
    public void Build_NoSchema_SchemaInstructionAbsent()
    {
        var result = MakeBuilder().Build(SimpleContext());
        Assert.DoesNotContain("Respond with a JSON object", result);
    }

    [Fact]
    public void Build_WithSecondaryEvents_IncludesContextSection()
    {
        var secondary = new List<ParsedJournalEvent>
        {
            new("Scan", """{"event":"Scan","Body":"Sol 1"}"""),
        };
        var result = MakeBuilder().Build(SimpleContext(secondary: secondary));
        Assert.Contains("Additional context events:", result);
        Assert.Contains("""{"event":"Scan","Body":"Sol 1"}""", result);
    }

    [Fact]
    public void Build_NoSecondaryEvents_ContextSectionAbsent()
    {
        var result = MakeBuilder().Build(SimpleContext());
        Assert.DoesNotContain("Additional context events:", result);
    }

    [Fact]
    public void Build_MultipleSecondaryEvents_AllIncluded()
    {
        var secondary = new List<ParsedJournalEvent>
        {
            new("Scan", """{"event":"Scan"}"""),
            new("Docked", """{"event":"Docked"}"""),
        };
        var result = MakeBuilder().Build(SimpleContext(secondary: secondary));
        Assert.Contains("""{"event":"Scan"}""", result);
        Assert.Contains("""{"event":"Docked"}""", result);
    }

    [Fact]
    public void Build_SetsBuiltPromptOnContext()
    {
        var ctx = SimpleContext("Hello");
        MakeBuilder().Build(ctx);
        Assert.NotNull(ctx.BuiltPrompt);
        Assert.Contains("Hello", ctx.BuiltPrompt);
    }

    [Fact]
    public void Build_BuiltPromptMatchesReturnValue()
    {
        var ctx = SimpleContext();
        var builder = MakeBuilder();
        var returned = builder.Build(ctx);
        Assert.Equal(returned, ctx.BuiltPrompt);
    }

    [Fact]
    public void Build_PromptWithAuxToken_AuxProviderInvoked()
    {
        int auxCallCount = 0;
        string? Provider(string id) { auxCallCount++; return """{"FireGroup":3}"""; }
        var ctx = SimpleContext("Fire group: |status.FireGroup|");
        MakeBuilder(Provider).Build(ctx);
        Assert.True(auxCallCount > 0);
    }

    [Fact]
    public void Build_EmptyPrompt_DoesNotIncludeBlankLines()
    {
        var ctx = SimpleContext(prompt: "");
        var result = MakeBuilder().Build(ctx);
        Assert.True(string.IsNullOrWhiteSpace(result) || !result.StartsWith(Environment.NewLine));
    }

    [Fact]
    public void SystemPersona_HasExpectedContent()
    {
        Assert.Contains("Elite Dangerous", PromptBuilder.SystemPersona);
        Assert.Contains("JSON", PromptBuilder.SystemPersona);
    }
}
