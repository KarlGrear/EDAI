namespace EDAI.Tests.OpenAI;

public sealed class ResponseParserTests
{
    private static ResponseParser MakeParser(Func<string, string?>? auxProvider = null)
    {
        var auxReader = new Mock<IJournalAuxFileReader>();
        auxReader.Setup(a => a.Read(It.IsAny<string>()))
                 .Returns<string>(id => auxProvider?.Invoke(id));
        return new ResponseParser(auxReader.Object, NullLogger<ResponseParser>.Instance);
    }

    private static EventConfigurationModel BasicConfig => new() { Title = "Test" };

    [Fact]
    public void Parse_ValidJson_ExtractsStringFields()
    {
        var parser = MakeParser();
        var result = parser.Parse("""{"threat_level":"High","system":"Sol"}""", BasicConfig);

        Assert.Equal("High", result.Fields["threat_level"]);
        Assert.Equal("Sol", result.Fields["system"]);
    }

    [Fact]
    public void Parse_NumericFieldValue_ExtractedAsString()
    {
        var parser = MakeParser();
        var result = parser.Parse("""{"score":42}""", BasicConfig);
        Assert.Equal("42", result.Fields["score"]);
    }

    [Fact]
    public void Parse_BooleanFieldValue_ExtractedAsString()
    {
        var parser = MakeParser();
        var result = parser.Parse("""{"active":true}""", BasicConfig);
        // JsonElement.ToString() for boolean values returns PascalCase ("True"/"False")
        Assert.Equal("True", result.Fields["active"]);
    }

    [Fact]
    public void Parse_NullRawJson_EmptyFields()
    {
        var parser = MakeParser();
        var result = parser.Parse(null, BasicConfig);
        Assert.Empty(result.Fields);
    }

    [Fact]
    public void Parse_EmptyRawJson_EmptyFields()
    {
        var parser = MakeParser();
        var result = parser.Parse("", BasicConfig);
        Assert.Empty(result.Fields);
    }

    [Fact]
    public void Parse_WhitespaceRawJson_EmptyFields()
    {
        var parser = MakeParser();
        var result = parser.Parse("   ", BasicConfig);
        Assert.Empty(result.Fields);
    }

    [Fact]
    public void Parse_MalformedJson_EmptyFields()
    {
        var parser = MakeParser();
        var result = parser.Parse("{bad json", BasicConfig);
        Assert.Empty(result.Fields);
    }

    [Fact]
    public void Parse_FieldLookup_IsCaseInsensitive()
    {
        var parser = MakeParser();
        var result = parser.Parse("""{"ThreatLevel":"High"}""", BasicConfig);

        Assert.True(result.Fields.ContainsKey("threatlevel"));
        Assert.Equal("High", result.Fields["THREATLEVEL"]);
    }

    [Fact]
    public void Parse_NoDisplayFields_DisplayedOutputIsNull()
    {
        var parser = MakeParser();
        var result = parser.Parse("""{"threat_level":"High"}""", BasicConfig);
        Assert.Null(result.DisplayedOutput);
    }

    [Fact]
    public void Parse_NoAnnounceFields_AnnouncedOutputIsNull()
    {
        var parser = MakeParser();
        var result = parser.Parse("""{"threat_level":"High"}""", BasicConfig);
        Assert.Null(result.AnnouncedOutput);
    }

    [Fact]
    public void Parse_DisplayFieldsSet_BuildsDisplayedOutput()
    {
        var parser = MakeParser();
        var config = new EventConfigurationModel
        {
            Title = "Test",
            DisplayFields = ["Threat: |result.threat_level|"],
        };

        var result = parser.Parse("""{"threat_level":"High"}""", config,
            """{"event":"FSDJump"}""");

        Assert.Equal("Threat: High", result.DisplayedOutput);
    }

    [Fact]
    public void Parse_AnnounceFieldsSet_BuildsAnnouncedOutput()
    {
        var parser = MakeParser();
        var config = new EventConfigurationModel
        {
            Title = "Test",
            AnnounceFields = ["Threat level is |result.threat_level|"],
        };

        var result = parser.Parse("""{"threat_level":"High"}""", config,
            """{"event":"FSDJump"}""");

        Assert.Equal("Threat level is High", result.AnnouncedOutput);
    }

    [Fact]
    public void Parse_MultipleDisplayFields_JoinedWithNewline()
    {
        var parser = MakeParser();
        var config = new EventConfigurationModel
        {
            Title = "Test",
            DisplayFields = ["Line 1: |result.field1|", "Line 2: |result.field2|"],
        };

        var result = parser.Parse("""{"field1":"A","field2":"B"}""", config);

        Assert.Equal($"Line 1: A{Environment.NewLine}Line 2: B", result.DisplayedOutput);
    }

    [Fact]
    public void Parse_DisplayFieldWithBlankOutput_SkippedInJoin()
    {
        var parser = MakeParser();
        var config = new EventConfigurationModel
        {
            Title = "Test",
            DisplayFields = ["   ", "Line 2: |result.field2|"],
        };

        var result = parser.Parse("""{"field2":"B"}""", config);

        Assert.Equal("Line 2: B", result.DisplayedOutput);
    }

    [Fact]
    public void Parse_TriggerTokenInDisplayField_Resolved()
    {
        var parser = MakeParser();
        var config = new EventConfigurationModel
        {
            Title = "Test",
            DisplayFields = ["Jumped to |trigger.StarSystem|"],
        };

        var result = parser.Parse(null, config,
            """{"event":"FSDJump","StarSystem":"Sol"}""");

        Assert.Equal("Jumped to Sol", result.DisplayedOutput);
    }

    [Fact]
    public void Parse_SecondaryJsonPassedThrough_TemplateResolved()
    {
        var parser = MakeParser();
        var config = new EventConfigurationModel
        {
            Title = "Test",
            DisplayFields = ["Count: |count(secondary)|"],
        };

        var result = parser.Parse(null, config, null,
            """[{"event":"Scan"},{"event":"Docked"}]""");

        Assert.Equal("Count: 2", result.DisplayedOutput);
    }
}
