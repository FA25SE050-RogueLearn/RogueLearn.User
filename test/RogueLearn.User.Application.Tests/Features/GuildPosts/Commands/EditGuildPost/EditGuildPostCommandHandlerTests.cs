using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.GuildPosts.Commands.EditGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.EditGuildPost;

public class EditGuildPostCommandHandlerTests
{
    [Fact]
    public async Task Handle_AttachmentsTryGetValue_ImagesMerged()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var imageStore = Substitute.For<IGuildPostImageStorage>();

        var guildId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var existingImages = new List<object> { "old1", "old2" };
        var post = new GuildPost { Id = postId, GuildId = guildId, AuthorId = authorId, Attachments = new Dictionary<string, object> { ["images"] = existingImages } };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);

        imageStore.SaveImagesAsync(guildId, postId, Arg.Any<IEnumerable<(byte[] Content, string ContentType, string FileName)>>()!, Arg.Any<CancellationToken>()).Returns(new[] { "new1", "new2" });

        var req = new EditGuildPostRequest
        {
            Title = "t",
            Content = "c",
            Tags = Array.Empty<string>(),
            Attachments = post.Attachments,
            Images = new List<GuildPostImageUpload> { new GuildPostImageUpload(new byte[] { 1 }, "image/png", "a.png"), new GuildPostImageUpload(new byte[] { 2 }, "image/png", "b.png") }
        };

        var sut = new EditGuildPostCommandHandler(postRepo, imageStore);
        await sut.Handle(new EditGuildPostCommand(guildId, postId, authorId, req), CancellationToken.None);

        await postRepo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => ((List<object>)((Dictionary<string, object>)p.Attachments!)["images"]).SequenceEqual(new List<object> { "old1", "old2", "new1", "new2" })), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditOthersPost_ThrowsForbidden()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var imageStore = Substitute.For<IGuildPostImageStorage>();

        var guildId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = new GuildPost { Id = postId, GuildId = guildId, AuthorId = authorId, Title = "t", Content = "c" };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);

        var req = new EditGuildPostRequest { Title = "x", Content = "y", Tags = Array.Empty<string>(), Attachments = null, Images = new List<GuildPostImageUpload>() };
        var sut = new EditGuildPostCommandHandler(postRepo, imageStore);
        var act = () => sut.Handle(new EditGuildPostCommand(guildId, postId, Guid.NewGuid(), req), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.ForbiddenException>().WithMessage("Cannot edit another user's post*");
    }

    [Fact]
    public async Task Handle_EditLockedPost_ThrowsForbidden()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var imageStore = Substitute.For<IGuildPostImageStorage>();

        var guildId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = new GuildPost { Id = postId, GuildId = guildId, AuthorId = userId, Title = "t", Content = "c", IsLocked = true };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);

        var req = new EditGuildPostRequest { Title = "x", Content = "y", Tags = Array.Empty<string>(), Attachments = null, Images = new List<GuildPostImageUpload>() };
        var sut = new EditGuildPostCommandHandler(postRepo, imageStore);
        var act = () => sut.Handle(new EditGuildPostCommand(guildId, postId, userId, req), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.ForbiddenException>().WithMessage("Post is locked by admin*");
    }
}
