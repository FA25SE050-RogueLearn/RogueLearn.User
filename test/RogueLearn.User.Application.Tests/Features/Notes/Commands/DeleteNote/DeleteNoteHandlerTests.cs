using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Notes.Commands.DeleteNote;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.DeleteNote;

public class DeleteNoteHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var cmd = new DeleteNoteCommand { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid() };
        var repo = Substitute.For<INoteRepository>();
        var sut = new DeleteNoteHandler(repo);
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((Note?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Forbidden_Throws()
    {
        var cmd = new DeleteNoteCommand { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid() };
        var repo = Substitute.For<INoteRepository>();
        var sut = new DeleteNoteHandler(repo);
        var note = new Note { Id = cmd.Id, AuthUserId = Guid.NewGuid() };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(note);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Deletes()
    {
        var cmd = new DeleteNoteCommand { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid() };
        var repo = Substitute.For<INoteRepository>();
        var sut = new DeleteNoteHandler(repo);
        var note = new Note { Id = cmd.Id, AuthUserId = cmd.AuthUserId };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(note);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(cmd.Id, Arg.Any<CancellationToken>());
    }
}