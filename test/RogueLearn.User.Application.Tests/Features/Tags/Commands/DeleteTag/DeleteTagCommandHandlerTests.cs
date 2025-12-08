using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Tags.Commands.DeleteTag;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Tags.Commands.DeleteTag;

public class DeleteTagCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFoundOrForbidden_NoDelete()
    {
        var repo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<DeleteTagCommandHandler>>();
        var sut = new DeleteTagCommandHandler(repo, noteTagRepo, logger);

        var cmd = new DeleteTagCommand { AuthUserId = Guid.NewGuid(), TagId = Guid.NewGuid() };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>());

        var tag = new Tag { Id = cmd.TagId, AuthUserId = System.Guid.NewGuid(), Name = "t" };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(tag);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_RemovesAssociationsAndDeletes()
    {
        var repo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<DeleteTagCommandHandler>>();
        var sut = new DeleteTagCommandHandler(repo, noteTagRepo, logger);
        var cmd = new DeleteTagCommand { AuthUserId = Guid.NewGuid(), TagId = Guid.NewGuid() };
        var tag = new Tag { Id = cmd.TagId, AuthUserId = cmd.AuthUserId, Name = "t" };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(tag);
        noteTagRepo.GetNoteIdsByTagIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(new System.Collections.Generic.List<System.Guid> { System.Guid.NewGuid() });

        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(cmd.TagId, Arg.Any<CancellationToken>());
    }
}