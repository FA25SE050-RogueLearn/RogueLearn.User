using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.UpdateNote;

public class UpdateNoteHandlerTests
{
    [Fact]
    public async Task Handle_ThrowsNotFound()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var skillRepo = Substitute.For<INoteSkillRepository>();
        var questRepo = Substitute.For<INoteQuestRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateNoteHandler>>();
        noteRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Note?)null);
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, skillRepo, questRepo, mapper, logger);
        var act = () => sut.Handle(new UpdateNoteCommand { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Title = "t" }, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ThrowsForbidden()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var skillRepo = Substitute.For<INoteSkillRepository>();
        var questRepo = Substitute.For<INoteQuestRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateNoteHandler>>();
        var note = new Note { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Title = "t" };
        noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>()).Returns(note);
        var sut = new UpdateNoteHandler(noteRepo, tagRepo, skillRepo, questRepo, mapper, logger);
        var act = () => sut.Handle(new UpdateNoteCommand { Id = note.Id, AuthUserId = Guid.NewGuid(), Title = "t" }, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_UpdatesNoteAndRelationships()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var skillRepo = Substitute.For<INoteSkillRepository>();
        var questRepo = Substitute.For<INoteQuestRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<UpdateNoteHandler>>();

        var authId = Guid.NewGuid();
        var note = new Note { Id = Guid.NewGuid(), AuthUserId = authId, Title = "old", Content = new object() };
        noteRepo.GetByIdAsync(note.Id, Arg.Any<CancellationToken>()).Returns(note);
        noteRepo.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<UpdateNoteResponse>(Arg.Any<Note>()).Returns(ci => new UpdateNoteResponse { Id = ci.Arg<Note>().Id, Title = ci.Arg<Note>().Title });

        tagRepo.GetTagIdsForNoteAsync(note.Id, Arg.Any<CancellationToken>()).Returns(new List<Guid> { Guid.NewGuid() });
        skillRepo.GetSkillIdsForNoteAsync(note.Id, Arg.Any<CancellationToken>()).Returns(new List<Guid>());
        questRepo.GetQuestIdsForNoteAsync(note.Id, Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var sut = new UpdateNoteHandler(noteRepo, tagRepo, skillRepo, questRepo, mapper, logger);
        var res = await sut.Handle(new UpdateNoteCommand { Id = note.Id, AuthUserId = authId, Title = "new", Content = "text", TagIds = new List<Guid>(), SkillIds = null, QuestIds = null }, CancellationToken.None);
        res.Title.Should().Be("new");
        await tagRepo.ReceivedWithAnyArgs(1).RemoveAsync(default, default, default);
    }
}