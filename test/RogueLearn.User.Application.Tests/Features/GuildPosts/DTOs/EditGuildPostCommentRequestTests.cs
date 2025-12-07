using FluentAssertions;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.DTOs;

public class EditGuildPostCommentRequestTests
{
    [Fact]
    public void Property_Set_And_Read()
    {
        var r = new EditGuildPostCommentRequest { Content = "updated" };
        r.Content.Should().Be("updated");
    }
}
