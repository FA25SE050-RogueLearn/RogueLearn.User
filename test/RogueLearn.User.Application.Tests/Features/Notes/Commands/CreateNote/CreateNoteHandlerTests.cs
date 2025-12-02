using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.CreateNote;

public class CreateNoteHandlerTests
{
    [Fact]
    public async Task Handle_CreatesNoteAndNormalizesContent()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var tagRepo = Substitute.For<INoteTagRepository>();
        var skillRepo = Substitute.For<INoteSkillRepository>();
        var questRepo = Substitute.For<INoteQuestRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<CreateNoteHandler>>();

        Note? captured = null;
        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>())
            .Returns(ci => { captured = ci.Arg<Note>(); return captured!; });

        mapper.Map<CreateNoteResponse>(Arg.Any<Note>())
            .Returns(ci =>
            {
                var n = ci.Arg<Note>();
                return new CreateNoteResponse { Id = n.Id, AuthUserId = n.AuthUserId, Title = n.Title, Content = n.Content, IsPublic = n.IsPublic, CreatedAt = n.CreatedAt, UpdatedAt = n.UpdatedAt };
            });

        var sut = new CreateNoteHandler(noteRepo, tagRepo, skillRepo, questRepo, mapper, logger);
        var cmd = new CreateNoteCommand { AuthUserId = Guid.NewGuid(), Title = "Title", Content = "hello", IsPublic = true, TagIds = new List<Guid> { Guid.NewGuid() } };
        var res = await sut.Handle(cmd, CancellationToken.None);

        captured!.Content.Should().NotBeNull();
        captured!.Content.Should().BeAssignableTo<List<object>>();
        res.Title.Should().Be("Title");
        await tagRepo.ReceivedWithAnyArgs(1).AddAsync(default, default, default);
    }
}