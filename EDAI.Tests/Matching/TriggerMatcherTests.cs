namespace EDAI.Tests.Matching;

public sealed class TriggerMatcherTests
{
    private static TriggerMatcher MakeMatcher(params EventConfigurationModel[] configs)
    {
        var repo = new Mock<IEventConfigurationRepository>();
        repo.Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(configs.ToList());
        return new TriggerMatcher(repo.Object, NullLogger<TriggerMatcher>.Instance);
    }

    private static EventConfigurationModel Config(int id, params string[] events) => new()
    {
        Id = id,
        Title = $"Config {id}",
        IsEnabled = true,
        TriggeringEvents = [.. events],
    };

    [Fact]
    public async Task FindMatchesAsync_ExactEventType_ReturnsMatch()
    {
        var matcher = MakeMatcher(Config(1, "FSDJump"));
        var results = await matcher.FindMatchesAsync(new ParsedJournalEvent("FSDJump", "{}"));
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public async Task FindMatchesAsync_CaseInsensitive_ConfigLowerJournalUpper_ReturnsMatch()
    {
        var matcher = MakeMatcher(Config(1, "fsdjump"));
        var results = await matcher.FindMatchesAsync(new ParsedJournalEvent("FSDJump", "{}"));
        Assert.Single(results);
    }

    [Fact]
    public async Task FindMatchesAsync_CaseInsensitive_ConfigUpperJournalLower_ReturnsMatch()
    {
        var matcher = MakeMatcher(Config(1, "FSDJUMP"));
        var results = await matcher.FindMatchesAsync(new ParsedJournalEvent("fsdjump", "{}"));
        Assert.Single(results);
    }

    [Fact]
    public async Task FindMatchesAsync_DifferentEventType_ReturnsEmpty()
    {
        var matcher = MakeMatcher(Config(1, "Docked"));
        var results = await matcher.FindMatchesAsync(new ParsedJournalEvent("FSDJump", "{}"));
        Assert.Empty(results);
    }

    [Fact]
    public async Task FindMatchesAsync_MultipleConfigsSameEvent_ReturnsAll()
    {
        var matcher = MakeMatcher(Config(1, "FSDJump"), Config(2, "FSDJump"), Config(3, "Docked"));
        var results = await matcher.FindMatchesAsync(new ParsedJournalEvent("FSDJump", "{}"));
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Id == 1);
        Assert.Contains(results, r => r.Id == 2);
    }

    [Fact]
    public async Task FindMatchesAsync_ConfigWithMultipleTriggers_MatchesAny()
    {
        var matcher = MakeMatcher(Config(1, "FSDJump", "Docked", "Scan"));

        var fsd = await matcher.FindMatchesAsync(new ParsedJournalEvent("FSDJump", "{}"));
        var docked = await matcher.FindMatchesAsync(new ParsedJournalEvent("Docked", "{}"));
        var scan = await matcher.FindMatchesAsync(new ParsedJournalEvent("Scan", "{}"));

        Assert.Single(fsd);
        Assert.Single(docked);
        Assert.Single(scan);
    }

    [Fact]
    public async Task FindMatchesAsync_EmptyRepository_ReturnsEmpty()
    {
        var matcher = MakeMatcher();
        var results = await matcher.FindMatchesAsync(new ParsedJournalEvent("FSDJump", "{}"));
        Assert.Empty(results);
    }

    [Fact]
    public async Task FindMatchesAsync_PartialEventNameMatch_DoesNotMatch()
    {
        var matcher = MakeMatcher(Config(1, "FSD"));
        var results = await matcher.FindMatchesAsync(new ParsedJournalEvent("FSDJump", "{}"));
        Assert.Empty(results);
    }

    [Fact]
    public async Task FindMatchesAsync_CalledTwiceForDifferentEvents_IsolatesCorrectly()
    {
        var matcher = MakeMatcher(Config(1, "FSDJump"), Config(2, "Docked"));

        var fsd = await matcher.FindMatchesAsync(new ParsedJournalEvent("FSDJump", "{}"));
        var docked = await matcher.FindMatchesAsync(new ParsedJournalEvent("Docked", "{}"));

        Assert.Single(fsd);
        Assert.Equal(1, fsd[0].Id);
        Assert.Single(docked);
        Assert.Equal(2, docked[0].Id);
    }

    [Fact]
    public async Task FindMatchesAsync_CancellationTokenPassedThrough_CompletesNormally()
    {
        var matcher = MakeMatcher(Config(1, "FSDJump"));
        using var cts = new CancellationTokenSource();
        var results = await matcher.FindMatchesAsync(new ParsedJournalEvent("FSDJump", "{}"), cts.Token);
        Assert.Single(results);
    }
}
