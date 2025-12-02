using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Tags.DTOs;
using RogueLearn.User.Application.Features.Tags.Queries.GetTagsForNote;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Tags.Queries.GetTagsForNote;

public class GetTagsForNoteQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoteNotFound_ThrowsNotFound()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<GetTagsForNoteQueryHandler>>();
        var sut = new GetTagsForNoteQueryHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var noteId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns((Note?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(new GetTagsForNoteQuery { AuthUserId = Guid.NewGuid(), NoteId = noteId }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoteNotOwned_ThrowsForbidden()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<GetTagsForNoteQueryHandler>>();
        var sut = new GetTagsForNoteQueryHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = Guid.NewGuid() });

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(new GetTagsForNoteQuery { AuthUserId = authId, NoteId = noteId }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ReturnsTags()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var logger = Substitute.For<ILogger<GetTagsForNoteQueryHandler>>();
        var sut = new GetTagsForNoteQueryHandler(noteRepo, tagRepo, noteTagRepo, logger);

        var authId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var t1 = new Tag { Id = Guid.NewGuid(), AuthUserId = authId, Name = "A" };
        var t2 = new Tag { Id = Guid.NewGuid(), AuthUserId = authId, Name = "B" };

        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authId });
        noteTagRepo.GetTagIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new List<Guid> { t1.Id, t2.Id });
        tagRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Tag> { t1, t2 });

        var res = await sut.Handle(new GetTagsForNoteQuery { AuthUserId = authId, NoteId = noteId }, CancellationToken.None);
        res.NoteId.Should().Be(noteId);
        res.Tags.Should().BeEquivalentTo(new List<TagDto> { new() { Id = t1.Id, Name = "A" }, new() { Id = t2.Id, Name = "B" } });
    }
}