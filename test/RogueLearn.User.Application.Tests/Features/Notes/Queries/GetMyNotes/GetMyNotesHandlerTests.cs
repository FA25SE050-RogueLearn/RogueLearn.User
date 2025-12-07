using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Notes.Queries.GetMyNotes;

public class GetMyNotesHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsNotesWithRelations()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetMyNotesHandler>>();
        var sut = new GetMyNotesHandler(noteRepo, tagRepo, mapper, logger);

        var authUserId = Guid.NewGuid();
        var notes = new List<Note> { new() { Id = Guid.NewGuid(), AuthUserId = authUserId, Title = "T", Content = new object() } };
        noteRepo.GetByUserAsync(authUserId, Arg.Any<CancellationToken>()).Returns(notes);
        mapper.Map<List<NoteDto>>(Arg.Any<List<Note>>()).Returns(new List<NoteDto> { new() { Id = notes[0].Id, AuthUserId = authUserId, Title = "T", Content = notes[0].Content, CreatedAt = notes[0].CreatedAt, IsPublic = notes[0].IsPublic, TagIds = new List<Guid>(), UpdatedAt = notes[0].UpdatedAt } });

        tagRepo.GetTagIdsForNoteAsync(notes[0].Id, Arg.Any<CancellationToken>()).Returns(new[] { Guid.NewGuid() });

        var result = await sut.Handle(new GetMyNotesQuery(authUserId), CancellationToken.None);
        result.Count.Should().Be(1);
        result[0].TagIds.Should().HaveCount(1);
    }
}