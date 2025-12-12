using System.Security.Claims;
using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;
using RogueLearn.User.Application.Features.Notes.Commands.DeleteNote;
using RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNoteFromUpload;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNoteWithAiTags;
using RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;
using RogueLearn.User.Application.Features.Notes.Queries.GetNoteById;
using RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTags;
using RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;
using RogueLearn.User.Application.Features.AiTagging.Commands.CommitNoteTagSelections;
using RogueLearn.User.Application.Models;
using FluentValidation;

namespace RogueLearn.User.Api.Tests.Controllers;

public class NotesControllerTests
{
    private static NotesController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new NotesController(mediator);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                }, "Test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task CreateNoteFromUpload_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateNoteFromUploadCommand>(), Arg.Any<CancellationToken>())
                .Returns(new CreateNoteResponse());
        var controller = CreateController(mediator, Guid.NewGuid());
        var bytes = Encoding.UTF8.GetBytes("hello");
        var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "file", "note.txt") { Headers = new HeaderDictionary(), ContentType = "text/plain" };
        var res = await controller.CreateNoteFromUpload(file, CancellationToken.None);
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateNoteFromUpload_Returns_BadRequest_On_Empty_File()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.CreateNoteFromUpload(null!, CancellationToken.None);
        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMyNotes_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetMyNotesQuery>(), Arg.Any<CancellationToken>()).Returns(new List<NoteDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetMyNotes(null, CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetNoteById_Returns_NotFound_When_Missing()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetNoteByIdQuery>(), Arg.Any<CancellationToken>())!.Returns((NoteDto?)null);
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetNoteById(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetNoteById_Returns_Ok_When_Public()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetNoteByIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(new NoteDto { Id = Guid.NewGuid(), Title = "t", Content = "c", IsPublic = true, AuthUserId = Guid.NewGuid() });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetNoteById(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetNoteById_Returns_NotFound_When_Private_And_Unauthenticated()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetNoteByIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(new NoteDto { Id = Guid.NewGuid(), Title = "t", Content = "c", IsPublic = false, AuthUserId = Guid.NewGuid() });

        var controller = new NotesController(mediator);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var res = await controller.GetNoteById(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetNoteById_Returns_NotFound_When_Private_And_Not_Owner()
    {
        var mediator = Substitute.For<IMediator>();
        var ownerId = Guid.NewGuid();
        mediator.Send(Arg.Any<GetNoteByIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(new NoteDto { Id = Guid.NewGuid(), Title = "t", Content = "c", IsPublic = false, AuthUserId = ownerId });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetNoteById(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetNoteById_Returns_Ok_When_Private_And_Owner()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Any<GetNoteByIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(new NoteDto { Id = Guid.NewGuid(), Title = "t", Content = "c", IsPublic = false, AuthUserId = userId });
        var controller = CreateController(mediator, userId);
        var res = await controller.GetNoteById(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateNote_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateNoteCommand>(), Arg.Any<CancellationToken>())
                .Returns(new CreateNoteResponse());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.CreateNote(new CreateNoteCommand(), CancellationToken.None);
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task UpdateNote_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateNoteCommand>(), Arg.Any<CancellationToken>())
                .Returns(new UpdateNoteResponse());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.UpdateNote(Guid.NewGuid(), new UpdateNoteCommand(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteNote_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.DeleteNote(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Suggest_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<SuggestNoteTagsQuery>(), Arg.Any<CancellationToken>())
                .Returns(new SuggestNoteTagsResponse { Suggestions = new List<TagSuggestionDto>() });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.Suggest(new SuggestNoteTagsQuery(), CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SuggestFromUpload_Returns_BadRequest_On_Null_File()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.SuggestFromUpload(null!, 10, CancellationToken.None);
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SuggestFromUpload_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<SuggestNoteTagsFromUploadQuery>(), Arg.Any<CancellationToken>())
                .Returns(new SuggestNoteTagsResponse { Suggestions = new List<TagSuggestionDto>() });
        var controller = CreateController(mediator, Guid.NewGuid());
        var bytes = Encoding.UTF8.GetBytes("abc");
        var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "file", "x.txt") { Headers = new HeaderDictionary(), ContentType = "text/plain" };
        var res = await controller.SuggestFromUpload(file, 5, CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Commit_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CommitNoteTagSelectionsCommand>(), Arg.Any<CancellationToken>())
                .Returns(new CommitNoteTagSelectionsResponse { NoteId = Guid.NewGuid(), AddedTagIds = new List<Guid>(), CreatedTags = new List<CreatedTagDto>(), TotalTagsAssigned = 0 });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.Commit(new CommitNoteTagSelectionsCommand(), CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateWithAiTags_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateNoteWithAiTagsCommand>(), Arg.Any<CancellationToken>())
                .Returns(new CreateNoteWithAiTagsResponse { NoteId = Guid.NewGuid(), Suggestions = new List<TagSuggestionDto>(), AppliedTagIds = new List<Guid>(), CreatedTags = new List<CreatedTagDto>(), TotalTagsAssigned = 0 });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.CreateWithAiTags(new CreateNoteWithAiTagsCommand(), CancellationToken.None);
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateWithAiTags_Returns_BadRequest_On_ValidationException()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateNoteWithAiTagsCommand>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<CreateNoteWithAiTagsResponse>(new ValidationException("fail")));
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.CreateWithAiTags(new CreateNoteWithAiTagsCommand(), CancellationToken.None);
        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateWithAiTagsFromUpload_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateNoteWithAiTagsCommand>(), Arg.Any<CancellationToken>())
                .Returns(new CreateNoteWithAiTagsResponse { NoteId = Guid.NewGuid(), Suggestions = new List<TagSuggestionDto>(), AppliedTagIds = new List<Guid>(), CreatedTags = new List<CreatedTagDto>(), TotalTagsAssigned = 0 });
        var controller = CreateController(mediator, Guid.NewGuid());
        var bytes = Encoding.UTF8.GetBytes("file");
        var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "file", "note.txt") { Headers = new HeaderDictionary(), ContentType = "text/plain" };
        var res = await controller.CreateWithAiTagsFromUpload(file, 5, true, null, false, CancellationToken.None);
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateWithAiTagsFromUpload_Returns_BadRequest_On_ValidationException()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateNoteWithAiTagsCommand>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<CreateNoteWithAiTagsResponse>(new ValidationException("bad")));
        var controller = CreateController(mediator, Guid.NewGuid());
        var bytes = Encoding.UTF8.GetBytes("file");
        var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "file", "note.txt") { Headers = new HeaderDictionary(), ContentType = "text/plain" };
        var res = await controller.CreateWithAiTagsFromUpload(file, 5, true, null, false, CancellationToken.None);
        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
