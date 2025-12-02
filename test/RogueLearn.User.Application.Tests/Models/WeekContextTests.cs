using FluentAssertions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Models;

public class WeekContextTests
{
    [Fact]
    public void WeekContext_Can_Add_Topics_And_Resources()
    {
        var wc = new WeekContext { WeekNumber = 2, TotalWeeks = 12 };
        wc.TopicsToCover.Add("Sorting");
        wc.TopicsToCover.Add("Searching");
        wc.AvailableResources.Add(new ValidResource { Url = "http://ex", SourceContext = "Lecture" });
        wc.TopicsToCover.Count.Should().Be(2);
        wc.AvailableResources.Count.Should().Be(1);
    }
}