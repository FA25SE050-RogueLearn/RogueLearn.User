using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNoteWithAiTags;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Tests.Features.Notes.Commands.CreateNoteWithAiTags;

public class CreateNoteWithAiTagsHandlerTests
{
    [Fact]
    public async Task Handle_CreatesNoteFromRawText()
    {
        var noteRepo = Substitute.For<INoteRepository>();
        var sugg = Substitute.For<ITaggingSuggestionService>();
        var summarizer = Substitute.For<ISummarizationPlugin>();
        var fileSummarizer = Substitute.For<IFileSummarizationPlugin>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<ILogger<CreateNoteWithAiTagsCommandHandler>>();

        noteRepo.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Note>());
        summarizer.SummarizeTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new { content = "ok" });
        sugg.SuggestAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new List<Application.Models.TagSuggestionDto>());

        var sut = new CreateNoteWithAiTagsCommandHandler(noteRepo, sugg, summarizer, fileSummarizer, mediator, logger);
        var res = await sut.Handle(new CreateNoteWithAiTagsCommand { AuthUserId = Guid.NewGuid(), RawText = "hello", Title = null, ApplySuggestions = false }, CancellationToken.None);
        res.Title.Should().Be("New Note");
        res.Suggestions.Should().BeEmpty();
    }
}