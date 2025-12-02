using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;
using RogueLearn.User.Application.Features.Notes.Queries.GetNoteById;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Notes.Queries.GetNoteById;

public class GetNoteByIdHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_ReturnsNull(Guid noteId)
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var skillRepo = Substitute.For<INoteSkillRepository>();
        var questRepo = Substitute.For<INoteQuestRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetNoteByIdHandler>>();
        var sut = new GetNoteByIdHandler(noteRepo, tagRepo, skillRepo, questRepo, mapper, logger);

        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns((Note?)null);
        var result = await sut.Handle(new GetNoteByIdQuery { Id = noteId }, CancellationToken.None);
        result.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public async Task Handle_Found_ReturnsDto(Guid noteId)
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var skillRepo = Substitute.For<INoteSkillRepository>();
        var questRepo = Substitute.For<INoteQuestRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetNoteByIdHandler>>();
        var sut = new GetNoteByIdHandler(noteRepo, tagRepo, skillRepo, questRepo, mapper, logger);

        var note = new Note { Id = noteId, AuthUserId = Guid.NewGuid(), Title = "T" };
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);
        mapper.Map<NoteDto>(note).Returns(new NoteDto { Id = noteId });

        tagRepo.GetTagIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new[] { Guid.NewGuid() });
        skillRepo.GetSkillIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new[] { Guid.NewGuid() });
        questRepo.GetQuestIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new[] { Guid.NewGuid() });

        var result = await sut.Handle(new GetNoteByIdQuery { Id = noteId }, CancellationToken.None);
        result!.Id.Should().Be(noteId);
        result.TagIds.Should().HaveCount(1);
        result.SkillIds.Should().HaveCount(1);
        result.QuestIds.Should().HaveCount(1);
    }
}