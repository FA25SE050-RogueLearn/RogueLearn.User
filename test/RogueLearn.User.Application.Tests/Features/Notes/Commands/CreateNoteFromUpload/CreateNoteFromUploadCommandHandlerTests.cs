using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNoteFromUpload;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.CreateNoteFromUpload;

public class CreateNoteFromUploadCommandHandlerTests
{
    [Fact]
    public async Task Handle_FileSummarizationThrows_FallsBackAndCreates()
    {
        var repo = Substitute.For<INoteRepository>();
        var mapper = Substitute.For<IMapper>();
        var plugin = Substitute.For<IFileSummarizationPlugin>();
        var logger = Substitute.For<ILogger<CreateNoteFromUploadCommandHandler>>();

        plugin.When(p => p.SummarizeAsync(Arg.Any<AiFileAttachment>(), Arg.Any<CancellationToken>()))
            .Do(ci => { throw new Exception("AI failed"); });

        repo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        mapper.Map<CreateNoteResponse>(Arg.Any<Note>()).Returns(ci =>
        {
            var note = ci.Arg<Note>();
            return new CreateNoteResponse { Id = note.Id, Title = note.Title };
        });

        var sut = new CreateNoteFromUploadCommandHandler(repo, mapper, plugin, logger);
        var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Heading\nPara"));
        var cmd = new CreateNoteFromUploadCommand { AuthUserId = Guid.NewGuid(), FileStream = ms, ContentType = "text/plain", FileName = "note.txt" };
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Title.Should().Be("note");
        await repo.Received(1).AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }
}
