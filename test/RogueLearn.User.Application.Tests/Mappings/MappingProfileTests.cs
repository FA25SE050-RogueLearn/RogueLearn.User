using FluentAssertions;
using RogueLearn.User.Application.Mappings;

namespace RogueLearn.User.Application.Tests.Mappings;

public class MappingProfileTests
{
    [Fact]
    public void Can_Instantiate_MappingProfile()
    {
        var profile = new MappingProfile();
        profile.Should().NotBeNull();
    }
}