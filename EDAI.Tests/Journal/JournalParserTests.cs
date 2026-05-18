namespace EDAI.Tests.Journal;

public sealed class JournalParserTests
{
    private readonly JournalParser _parser = new(NullLogger<JournalParser>.Instance);

    [Fact]
    public void ValidLine_ReturnsEventWithCorrectType()
    {
        var result = _parser.TryParse("""{"timestamp":"2025-01-01T00:00:00Z","event":"FSDJump","StarSystem":"Sol"}""");
        Assert.NotNull(result);
        Assert.Equal("FSDJump", result.EventType);
    }

    [Fact]
    public void ValidLine_PreservesRawJson()
    {
        var raw = """{"event":"FSDJump","StarSystem":"Sol"}""";
        var result = _parser.TryParse(raw);
        Assert.NotNull(result);
        Assert.Equal(raw, result.RawJson);
    }

    [Fact]
    public void MissingEventProperty_ReturnsNull()
    {
        var result = _parser.TryParse("""{"timestamp":"2025-01-01T00:00:00Z","StarSystem":"Sol"}""");
        Assert.Null(result);
    }

    [Fact]
    public void EmptyEventValue_ReturnsNull()
    {
        var result = _parser.TryParse("""{"event":"","StarSystem":"Sol"}""");
        Assert.Null(result);
    }

    [Fact]
    public void WhitespaceEventValue_ReturnsNull()
    {
        var result = _parser.TryParse("""{"event":"   "}""");
        Assert.Null(result);
    }

    [Fact]
    public void NullEventValue_ReturnsNull()
    {
        var result = _parser.TryParse("""{"event":null}""");
        Assert.Null(result);
    }

    [Fact]
    public void MalformedJson_ReturnsNull()
    {
        var result = _parser.TryParse("{not valid json}");
        Assert.Null(result);
    }

    [Fact]
    public void JsonArray_ReturnsNull()
    {
        var result = _parser.TryParse("""[{"event":"FSDJump"}]""");
        Assert.Null(result);
    }

    [Fact]
    public void JsonNumber_ReturnsNull()
    {
        var result = _parser.TryParse("42");
        Assert.Null(result);
    }

    [Fact]
    public void JsonString_ReturnsNull()
    {
        var result = _parser.TryParse("\"just a string\"");
        Assert.Null(result);
    }

    [Fact]
    public void ValidLine_MultipleConcurrentParseCalls_AllSucceed()
    {
        var raw = """{"event":"Docked","StationName":"Jameson Memorial"}""";
        var results = Enumerable.Range(0, 10)
            .AsParallel()
            .Select(_ => _parser.TryParse(raw))
            .ToList();

        Assert.All(results, r => Assert.NotNull(r));
        Assert.All(results, r => Assert.Equal("Docked", r!.EventType));
    }

    [Fact]
    public void EventTypePreservedExactCase()
    {
        var result = _parser.TryParse("""{"event":"FSDJump"}""");
        Assert.NotNull(result);
        Assert.Equal("FSDJump", result.EventType);
    }

    [Fact]
    public void MinimalValidLine_OnlyEventProperty()
    {
        var result = _parser.TryParse("""{"event":"Music"}""");
        Assert.NotNull(result);
        Assert.Equal("Music", result.EventType);
    }
}
