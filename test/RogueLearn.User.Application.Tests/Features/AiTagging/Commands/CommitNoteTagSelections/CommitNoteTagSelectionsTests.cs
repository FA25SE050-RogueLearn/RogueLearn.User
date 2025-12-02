using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.AiTagging.Commands.CommitNoteTagSelections;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.AiTagging.Commands.CommitNoteTagSelections;

public class CommitNoteTagSelectionsTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NoteNotFoundOrForbidden_Throws(CommitNoteTagSelectionsCommand cmd)
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        noteRepo.GetByIdAsync(cmd.NoteId, Arg.Any<CancellationToken>()).Returns((Note?)null);

        var sut = new CommitNoteTagSelectionsCommandHandler(noteRepo, tagRepo, noteTagRepo);
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingTagBySlug_IsSelected()
    {
        var authUserId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var cmd = new CommitNoteTagSelectionsCommand
        {
            AuthUserId = authUserId,
            NoteId = noteId,
            SelectedTagIds = new List<Guid>(),
            NewTagNames = new List<string> { "Data Science" }
        };

        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<ITagRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();

        noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(new Note { Id = noteId, AuthUserId = authUserId });
        var existing = new Tag { Id = Guid.NewGuid(), AuthUserId = authUserId, Name = "Data Science" };
        tagRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tag, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<Tag> { existing });
        noteTagRepo.GetTagIdsForNoteAsync(noteId, Arg.Any<CancellationToken>()).Returns(new List<Guid>());
        noteTagRepo.AddAsync(noteId, existing.Id, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var sut = new CommitNoteTagSelectionsCommandHandler(noteRepo, tagRepo, noteTagRepo);
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.AddedTagIds.Should().Contain(existing.Id);
        res.TotalTagsAssigned.Should().Be(1);
    }
}