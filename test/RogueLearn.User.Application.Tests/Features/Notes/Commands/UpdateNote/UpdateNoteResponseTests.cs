using FluentAssertions;
using RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.UpdateNote;

public class UpdateNoteResponseTests
{
    [Fact]
    public void UpdateNoteResponse_SetsFields()
    {
        var dto = new UpdateNoteResponse
        {
            Id = Guid.NewGuid(),
            AuthUserId = Guid.NewGuid(),
            Title = "title",
            Content = "content",
            IsPublic = true,
            CreatedAt = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };
        dto.Title.Should().Be("title");
        dto.Content.Should().Be("content");
        dto.IsPublic.Should().Be(true);
        dto.CreatedAt.Should().Be(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero));
        dto.UpdatedAt.Should().Be(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }
}