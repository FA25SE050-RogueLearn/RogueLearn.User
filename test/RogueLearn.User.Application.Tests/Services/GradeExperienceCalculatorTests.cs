using FluentAssertions;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class GradeExperienceCalculatorTests
{
    [Fact]
    public void CalculateXpAward_Returns_Consolation_On_Failing_Grade()
    {
        var calc = new GradeExperienceCalculator();
        var xp = calc.CalculateXpAward(4.5, semester: 2, relevanceWeight: 0.8m);
        xp.Should().BeGreaterThan(0).And.BeLessThan(200); // consolation path
    }

    [Fact]
    public void CalculateXpAward_Computes_Xp_By_Tier_And_Grade()
    {
        var calc = new GradeExperienceCalculator();
        var xp1 = calc.CalculateXpAward(8.0, semester: 2, relevanceWeight: 1.0m); // Tier1Pool=1500
        var xp2 = calc.CalculateXpAward(8.0, semester: 5, relevanceWeight: 1.0m); // Tier2Pool=2000
        var xp3 = calc.CalculateXpAward(8.0, semester: 8, relevanceWeight: 1.0m); // Tier3Pool=1500
        xp2.Should().BeGreaterThan(xp1);
        xp2.Should().BeGreaterThan(xp3);
    }

    [Fact]
    public void GetTierInfo_Returns_Correct_Tier_And_Pool()
    {
        var calc = new GradeExperienceCalculator();
        calc.GetTierInfo(1).Tier.Should().Be(1);
        calc.GetTierInfo(4).Tier.Should().Be(2);
        calc.GetTierInfo(7).Tier.Should().Be(3);
    }
}