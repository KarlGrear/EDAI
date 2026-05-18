namespace EDAI.Tests.Scripting;

public sealed class ScriptResultTests
{
    [Fact]
    public void ToJson_Empty_ReturnsEmptyObject()
    {
        var result = new ScriptResult();
        Assert.Equal("{}", result.ToJson());
    }

    [Fact]
    public void ToJson_WithAnnouncement_IncludesAnnouncement()
    {
        var result = new ScriptResult { Announcement = "Hello Commander" };
        var json = result.ToJson();
        Assert.Contains("\"Announcement\":", json);
        Assert.Contains("\"Hello Commander\"", json);
    }

    [Fact]
    public void ToJson_WithDisplay_IncludesDisplay()
    {
        var result = new ScriptResult { Display = "Threat: High" };
        var json = result.ToJson();
        Assert.Contains("\"Display\":", json);
        Assert.Contains("\"Threat: High\"", json);
    }

    [Fact]
    public void ToJson_WithCustomField_IncludesField()
    {
        var result = new ScriptResult { ["risk"] = "critical" };
        var json = result.ToJson();
        Assert.Contains("\"risk\":", json);
        Assert.Contains("\"critical\"", json);
    }

    [Fact]
    public void ToJson_AllFieldsSet_AllIncluded()
    {
        var result = new ScriptResult
        {
            Announcement = "Jump complete",
            Display = "Sol",
            ["risk"] = "low",
        };
        var json = result.ToJson();
        Assert.Contains("\"Announcement\"", json);
        Assert.Contains("\"Display\"", json);
        Assert.Contains("\"risk\"", json);
    }

    [Fact]
    public void ToJson_NullAnnouncement_NotIncluded()
    {
        var result = new ScriptResult { Announcement = null };
        Assert.Equal("{}", result.ToJson());
    }

    [Fact]
    public void ToJson_NullDisplay_NotIncluded()
    {
        var result = new ScriptResult { Display = null };
        Assert.Equal("{}", result.ToJson());
    }

    [Fact]
    public void ToJson_NullCustomField_NotIncluded()
    {
        var result = new ScriptResult { ["nullField"] = null };
        var json = result.ToJson();
        Assert.DoesNotContain("nullField", json);
    }

    [Fact]
    public void Indexer_Get_ReturnsSetValue()
    {
        var result = new ScriptResult { ["key"] = "value" };
        Assert.Equal("value", result["key"]);
    }

    [Fact]
    public void Indexer_IsCaseInsensitive_GetWithDifferentCase()
    {
        var result = new ScriptResult { ["MyKey"] = "value" };
        Assert.Equal("value", result["MYKEY"]);
        Assert.Equal("value", result["mykey"]);
    }

    [Fact]
    public void Indexer_OverwriteWithSameKey_LatestValueWins()
    {
        var result = new ScriptResult { ["key"] = "first" };
        result["KEY"] = "second";
        Assert.Equal("second", result["key"]);
    }

    [Fact]
    public void Indexer_NonExistentKey_ReturnsNull()
    {
        var result = new ScriptResult();
        Assert.Null(result["nonexistent"]);
    }

    [Fact]
    public void ToJson_MultipleCustomFields_AllSerialized()
    {
        var result = new ScriptResult
        {
            ["fieldA"] = "alpha",
            ["fieldB"] = "beta",
            ["fieldC"] = "gamma",
        };
        var json = result.ToJson();
        Assert.Contains("fieldA", json);
        Assert.Contains("fieldB", json);
        Assert.Contains("fieldC", json);
    }

    [Fact]
    public void ToJson_ProducesValidJson()
    {
        var result = new ScriptResult
        {
            Announcement = "Hello",
            Display = "World",
            ["custom"] = "value",
        };
        var json = result.ToJson();
        var node = JsonNode.Parse(json);
        Assert.NotNull(node);
        Assert.Equal("Hello", node!["Announcement"]?.ToString());
        Assert.Equal("World", node["Display"]?.ToString());
        Assert.Equal("value", node["custom"]?.ToString());
    }
}
