namespace EDAI.Tests.Pipeline;

public sealed class ConditionEvaluatorTests
{
    private const string TriggerJson =
        """{"event":"FSDJump","StarSystem":"Sol","Population":100000,"ThreatLevel":2}""";

    // ── Null / empty ─────────────────────────────────────────────────────────

    [Fact]
    public void NullCondition_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate(null, null, null));

    [Fact]
    public void EmptyCondition_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate("", null, null));

    [Fact]
    public void WhitespaceOnlyCondition_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate("   ", null, null));

    // ── Boolean literals ─────────────────────────────────────────────────────

    [Fact]
    public void LiteralTrue_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate("true", null, null));

    [Fact]
    public void LiteralFalse_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate("false", null, null));

    [Fact]
    public void LiteralTrueCaseInsensitive_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate("True", null, null));

    [Fact]
    public void LiteralFalseCaseInsensitive_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate("FALSE", null, null));

    // ── Bare value truthiness ─────────────────────────────────────────────────

    [Fact]
    public void BareNonEmptyValue_IsTruthy()
        => Assert.True(ConditionEvaluator.Evaluate("Sol", null, null));

    [Fact]
    public void BareNumericValue_IsTruthy()
        => Assert.True(ConditionEvaluator.Evaluate("42", null, null));

    // ── String equality ───────────────────────────────────────────────────────

    [Fact]
    public void StringEquality_Match_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.StarSystem| == \"Sol\"", TriggerJson, null));

    [Fact]
    public void StringEquality_NoMatch_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate(
            "|trigger.StarSystem| == \"Beagle Point\"", TriggerJson, null));

    [Fact]
    public void StringEquality_CaseInsensitive_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.StarSystem| == \"SOL\"", TriggerJson, null));

    [Fact]
    public void StringInequality_DifferentValues_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.StarSystem| != \"Beagle Point\"", TriggerJson, null));

    [Fact]
    public void StringInequality_SameValue_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate(
            "|trigger.StarSystem| != \"Sol\"", TriggerJson, null));

    [Fact]
    public void StringWithSpaces_QuotedCorrectly()
    {
        var trigger = """{"event":"FSDJump","StarSystem":"Beagle Point"}""";
        Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.StarSystem| == \"Beagle Point\"", trigger, null));
    }

    // ── Numeric comparisons ───────────────────────────────────────────────────

    [Fact]
    public void NumericEquality_SameValue_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.Population| == 100000", TriggerJson, null));

    [Fact]
    public void NumericEquality_DifferentValue_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate(
            "|trigger.Population| == 99999", TriggerJson, null));

    [Fact]
    public void NumericGreaterThan_Above_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.Population| > 50000", TriggerJson, null));

    [Fact]
    public void NumericGreaterThan_Below_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate(
            "|trigger.Population| > 200000", TriggerJson, null));

    [Fact]
    public void NumericGreaterThan_Equal_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate(
            "|trigger.Population| > 100000", TriggerJson, null));

    [Fact]
    public void NumericGreaterOrEqual_ExactMatch_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.Population| >= 100000", TriggerJson, null));

    [Fact]
    public void NumericLessThan_Below_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.Population| < 200000", TriggerJson, null));

    [Fact]
    public void NumericLessOrEqual_ExactMatch_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.Population| <= 100000", TriggerJson, null));

    [Fact]
    public void NumericInequality_DifferentValues_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.Population| != 99999", TriggerJson, null));

    [Fact]
    public void NumericComparison_LiteralBothSides()
        => Assert.True(ConditionEvaluator.Evaluate("42 == 42", null, null));

    [Fact]
    public void NumericComparison_LiteralBothSides_NotEqual()
        => Assert.False(ConditionEvaluator.Evaluate("42 == 43", null, null));

    // ── Logical OR ────────────────────────────────────────────────────────────

    [Fact]
    public void Or_FirstClauseTrue_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate("true || false", null, null));

    [Fact]
    public void Or_SecondClauseTrue_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate("false || true", null, null));

    [Fact]
    public void Or_BothFalse_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate("false || false", null, null));

    [Fact]
    public void Or_BothTrue_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate("true || true", null, null));

    // ── Logical AND ───────────────────────────────────────────────────────────

    [Fact]
    public void And_BothTrue_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate("true && true", null, null));

    [Fact]
    public void And_FirstFalse_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate("false && true", null, null));

    [Fact]
    public void And_SecondFalse_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate("true && false", null, null));

    [Fact]
    public void And_BothFalse_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate("false && false", null, null));

    // ── Operator precedence: AND tighter than OR ──────────────────────────────

    [Fact]
    public void Precedence_AndTighterThanOr_FalseOrTrueAndTrue_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate("false || true && true", null, null));

    [Fact]
    public void Precedence_AndTighterThanOr_FalseOrTrueAndFalse_ReturnsFalse()
        => Assert.False(ConditionEvaluator.Evaluate("false || true && false", null, null));

    [Fact]
    public void Precedence_AndTighterThanOr_TrueOrFalseAndFalse_ReturnsTrue()
        => Assert.True(ConditionEvaluator.Evaluate("true || false && false", null, null));

    // ── Template token resolution before evaluation ───────────────────────────

    [Fact]
    public void TemplateToken_ResolvedBeforeComparison()
        => Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.ThreatLevel| >= 1", TriggerJson, null));

    [Fact]
    public void TemplateToken_WithResult_ResolvesResultField()
    {
        var resultJson = """{"active":"true"}""";
        Assert.True(ConditionEvaluator.Evaluate(
            "|result.active| == \"true\"", TriggerJson, resultJson));
    }

    [Fact]
    public void UnresolvableToken_LeftAsIs_IsTruthy()
        => Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.Missing|", TriggerJson, null));

    // ── Complex multi-clause conditions ──────────────────────────────────────

    [Fact]
    public void Complex_AndAndOr_EvaluatesCorrectly()
    {
        // Sol AND (population > 50000) = true
        Assert.True(ConditionEvaluator.Evaluate(
            "|trigger.StarSystem| == \"Sol\" && |trigger.Population| > 50000",
            TriggerJson, null));
    }

    [Fact]
    public void Complex_TwoAndClauses_OneFails_ReturnsFalse()
    {
        Assert.False(ConditionEvaluator.Evaluate(
            "|trigger.StarSystem| == \"Sol\" && |trigger.Population| > 999999",
            TriggerJson, null));
    }
}
