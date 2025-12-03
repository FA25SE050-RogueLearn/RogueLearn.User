using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Notes.Commands.DeleteNote;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.DeleteNote;

public class DeleteNoteHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(DeleteNoteCommand cmd)
    {
        var repo = Substitute.For<INoteRepository>();
        var sut = new DeleteNoteHandler(repo);
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((Note?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Forbidden_Throws(DeleteNoteCommand cmd)
    {
        var repo = Substitute.For<INoteRepository>();
        var sut = new DeleteNoteHandler(repo);
        var note = new Note { Id = cmd.Id, AuthUserId = Guid.NewGuid() };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(note);
        cmd.AuthUserId = Guid.NewGuid();
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Deletes(DeleteNoteCommand cmd)
    {
        var repo = Substitute.For<INoteRepository>();
        var sut = new DeleteNoteHandler(repo);
        var note = new Note { Id = cmd.Id, AuthUserId = cmd.AuthUserId };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(note);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(cmd.Id, Arg.Any<CancellationToken>());
    }
}