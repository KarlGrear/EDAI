namespace EDAI.Tests.Pipeline;

public sealed class TemplateEngineTests
{
    private const string TriggerJson =
        """{"event":"FSDJump","StarSystem":"Sol","StarPos":[-25.09,0.0,28.0],"Population":100000,"Factions":[{"Name":"A","Allegiance":"Federation"},{"Name":"B","Allegiance":"Empire"}]}""";

    [Fact]
    public void NoTokens_ReturnsTemplateUnchanged()
    {
        var result = TemplateEngine.Apply("Hello world", TriggerJson, null);
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void EmptyTemplate_ReturnsEmpty()
    {
        var result = TemplateEngine.Apply("", TriggerJson, null);
        Assert.Equal("", result);
    }

    [Fact]
    public void TriggerToken_WholeJson_ReturnsTriggerJson()
    {
        var result = TemplateEngine.Apply("|trigger|", TriggerJson, null);
        Assert.Equal(TriggerJson, result);
    }

    [Fact]
    public void TriggerDotStringField_ReturnsFieldValue()
    {
        var result = TemplateEngine.Apply("|trigger.StarSystem|", TriggerJson, null);
        Assert.Equal("Sol", result);
    }

    [Fact]
    public void TriggerDotEventField_ReturnsEventType()
    {
        var result = TemplateEngine.Apply("|trigger.event|", TriggerJson, null);
        Assert.Equal("FSDJump", result);
    }

    [Fact]
    public void TriggerArrayIndex_ReturnsIndexedElement()
    {
        var result = TemplateEngine.Apply("|trigger.StarPos[0]|", TriggerJson, null);
        Assert.Equal("-25.09", result);
    }

    [Fact]
    public void TriggerArrayIndexSecond_ReturnsSecondElement()
    {
        var result = TemplateEngine.Apply("|trigger.StarPos[1]|", TriggerJson, null);
        Assert.Equal("0.0", result);
    }

    [Fact]
    public void CountTriggerArrayElements_UsingWildcard_ReturnsCount()
    {
        var result = TemplateEngine.Apply("|count(trigger.Factions[*])|", TriggerJson, null);
        Assert.Equal("2", result);
    }

    [Fact]
    public void CountTriggerArray_WithoutWildcard_ReturnsOneMatch()
    {
        // $.Factions matches the array itself (1 match); use [*] to count elements
        var result = TemplateEngine.Apply("|count(trigger.Factions)|", TriggerJson, null);
        Assert.Equal("1", result);
    }

    [Fact]
    public void TriggerFilterExpression_ReturnsMatchingValue()
    {
        var result = TemplateEngine.Apply(
            "|trigger.Factions[?@.Allegiance==\"Federation\"].Name|",
            TriggerJson, null);
        Assert.Equal("A", result);
    }

    [Fact]
    public void CountWithFilter_ReturnsMatchCount()
    {
        var result = TemplateEngine.Apply(
            "|count(trigger.Factions[?@.Allegiance==\"Federation\"])|",
            TriggerJson, null);
        Assert.Equal("1", result);
    }

    [Fact]
    public void TriggerWildcardPath_MultipleMatchesJoinedWithComma()
    {
        var result = TemplateEngine.Apply("|trigger.Factions[*].Name|", TriggerJson, null);
        Assert.Equal("A, B", result);
    }

    [Fact]
    public void ResultToken_WholeJson_ReturnsResultJson()
    {
        var resultJson = """{"threat_level":"High"}""";
        var result = TemplateEngine.Apply("|result|", TriggerJson, resultJson);
        Assert.Equal(resultJson, result);
    }

    [Fact]
    public void ResultDotField_ReturnsFieldValue()
    {
        var resultJson = """{"threat_level":"High","score":42}""";
        var result = TemplateEngine.Apply("|result.threat_level|", TriggerJson, resultJson);
        Assert.Equal("High", result);
    }

    [Fact]
    public void ResultNullJson_ResultDotTokenLeavesAsIs()
    {
        var result = TemplateEngine.Apply("|result.threat_level|", TriggerJson, null);
        Assert.Equal("|result.threat_level|", result);
    }

    [Fact]
    public void SecondaryToken_WholeArray_ReturnsArrayJson()
    {
        var secondary = """[{"event":"FSDJump"},{"event":"Scan"}]""";
        var result = TemplateEngine.Apply("|secondary|", TriggerJson, null, secondaryJson: secondary);
        Assert.Equal(secondary, result);
    }

    [Fact]
    public void SecondaryIndexedField_ReturnsFieldValue()
    {
        var secondary = """[{"event":"FSDJump","StarSystem":"Sol"},{"event":"Scan"}]""";
        var result = TemplateEngine.Apply("|secondary[0].StarSystem|", TriggerJson, null, secondaryJson: secondary);
        Assert.Equal("Sol", result);
    }

    [Fact]
    public void SecondaryIndexedElement_NoPath_ReturnsElementJson()
    {
        var secondary = """[{"event":"FSDJump"}]""";
        var result = TemplateEngine.Apply("|secondary[0]|", TriggerJson, null, secondaryJson: secondary);
        Assert.Equal("""{"event":"FSDJump"}""", result);
    }

    [Fact]
    public void CountSecondary_ReturnsTotalCount()
    {
        var secondary = """[{"event":"FSDJump"},{"event":"Scan"},{"event":"Docked"}]""";
        var result = TemplateEngine.Apply("|count(secondary)|", TriggerJson, null, secondaryJson: secondary);
        Assert.Equal("3", result);
    }

    [Fact]
    public void SecondaryOutOfBounds_LeavesTokenAsIs()
    {
        var secondary = """[{"event":"FSDJump"}]""";
        var result = TemplateEngine.Apply("|secondary[5].StarSystem|", TriggerJson, null, secondaryJson: secondary);
        Assert.Equal("|secondary[5].StarSystem|", result);
    }

    [Fact]
    public void NoSecondaryJson_SecondaryTokenLeavesAsIs()
    {
        var result = TemplateEngine.Apply("|secondary[0].event|", TriggerJson, null);
        Assert.Equal("|secondary[0].event|", result);
    }

    [Fact]
    public void AuxProvider_ResolvesKnownIdentifier()
    {
        var statusJson = """{"FireGroup":2}""";
        var result = TemplateEngine.Apply("|status.FireGroup|", TriggerJson, null,
            id => id == "status" ? statusJson : null);
        Assert.Equal("2", result);
    }

    [Fact]
    public void AuxProvider_UnknownFile_LeavesTokenAsIs()
    {
        var result = TemplateEngine.Apply("|market.Commodities|", TriggerJson, null, _ => null);
        Assert.Equal("|market.Commodities|", result);
    }

    [Fact]
    public void NullAuxProvider_AuxTokenLeavesAsIs()
    {
        var result = TemplateEngine.Apply("|status.FireGroup|", TriggerJson, null, null);
        Assert.Equal("|status.FireGroup|", result);
    }

    [Fact]
    public void UnknownTriggerPath_LeavesTokenAsIs()
    {
        var result = TemplateEngine.Apply("|trigger.NonExistentField|", TriggerJson, null);
        Assert.Equal("|trigger.NonExistentField|", result);
    }

    [Fact]
    public void NullTriggerJson_TriggerDotTokenLeavesAsIs()
    {
        var result = TemplateEngine.Apply("|trigger.StarSystem|", null, null);
        Assert.Equal("|trigger.StarSystem|", result);
    }

    [Fact]
    public void MalformedTriggerJson_LeavesTokenAsIs()
    {
        var result = TemplateEngine.Apply("|trigger.StarSystem|", "not valid json", null);
        Assert.Equal("|trigger.StarSystem|", result);
    }

    [Fact]
    public void MultipleTokensInTemplate_AllResolved()
    {
        var result = TemplateEngine.Apply(
            "System: |trigger.StarSystem| Event: |trigger.event|",
            TriggerJson, null);
        Assert.Equal("System: Sol Event: FSDJump", result);
    }

    [Fact]
    public void MixedTextAndTokens_OnlyTokensReplaced()
    {
        var result = TemplateEngine.Apply("Jumping to |trigger.StarSystem|!", TriggerJson, null);
        Assert.Equal("Jumping to Sol!", result);
    }

    [Fact]
    public void CountOnScalarTrigger_LeavesTokenAsIs()
    {
        var result = TemplateEngine.Apply("|count(trigger)|", TriggerJson, null);
        Assert.Equal("|count(trigger)|", result);
    }

    [Fact]
    public void CountOnScalarResult_LeavesTokenAsIs()
    {
        var result = TemplateEngine.Apply("|count(result)|", TriggerJson, """{"x":1}""");
        Assert.Equal("|count(result)|", result);
    }

    [Fact]
    public void CountSecondaryField_UsingWildcard_ReturnsElementCount()
    {
        var secondary = """[{"event":"Scan","Bodies":[{"Name":"A"},{"Name":"B"},{"Name":"C"}]}]""";
        var result = TemplateEngine.Apply("|count(secondary[0].Bodies[*])|", TriggerJson, null, secondaryJson: secondary);
        Assert.Equal("3", result);
    }

    [Fact]
    public void NumericTriggerField_ReturnsStringRepresentation()
    {
        var result = TemplateEngine.Apply("|trigger.Population|", TriggerJson, null);
        Assert.Equal("100000", result);
    }

    [Fact]
    public void AuxProvider_CachesNodePerFileId()
    {
        int callCount = 0;
        string? Provider(string id)
        {
            callCount++;
            return id == "status" ? """{"FireGroup":1}""" : null;
        }

        TemplateEngine.Apply("|status.FireGroup| and |status.FireGroup|", TriggerJson, null, Provider);
        Assert.Equal(1, callCount);
    }
}
