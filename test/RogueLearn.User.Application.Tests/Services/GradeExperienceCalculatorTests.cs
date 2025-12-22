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
    public void GetTierInfo_Returns_Correct_Tier_And_Pool()
    {
        var calc = new GradeExperienceCalculator();
        calc.GetTierInfo(1).Tier.Should().Be(1);
        calc.GetTierInfo(4).Tier.Should().Be(2);
        calc.GetTierInfo(7).Tier.Should().Be(3);
    }
}