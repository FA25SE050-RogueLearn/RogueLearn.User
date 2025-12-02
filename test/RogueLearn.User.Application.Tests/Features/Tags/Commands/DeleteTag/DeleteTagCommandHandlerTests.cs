using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Tags.Commands.DeleteTag;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Tags.Commands.DeleteTag;

public class DeleteTagCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFoundOrForbidden_NoDelete(DeleteTagCommand cmd)
    {
        var repo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<DeleteTagCommandHandler>>();
        var sut = new DeleteTagCommandHandler(repo, noteTagRepo, logger);

        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>());

        var tag = new Tag { Id = cmd.TagId, AuthUserId = System.Guid.NewGuid(), Name = "t" };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(tag);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_RemovesAssociationsAndDeletes(DeleteTagCommand cmd)
    {
        var repo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<DeleteTagCommandHandler>>();
        var sut = new DeleteTagCommandHandler(repo, noteTagRepo, logger);

        var tag = new Tag { Id = cmd.TagId, AuthUserId = cmd.AuthUserId, Name = "t" };
        repo.GetByIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(tag);
        noteTagRepo.GetNoteIdsByTagIdAsync(cmd.TagId, Arg.Any<CancellationToken>()).Returns(new System.Collections.Generic.List<System.Guid> { System.Guid.NewGuid() });

        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(cmd.TagId, Arg.Any<CancellationToken>());
    }
}