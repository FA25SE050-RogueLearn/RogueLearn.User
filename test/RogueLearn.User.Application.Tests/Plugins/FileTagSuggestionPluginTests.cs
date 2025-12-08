using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class FileTagSuggestionPluginTests
{
    private static IChatCompletionService CreateChatService(Func<ChatHistory, ChatMessageContent> responder)
    {
        var chat = Substitute.For<IChatCompletionService>();
        chat.GetChatMessageContentsAsync(
            Arg.Any<ChatHistory>(),
            Arg.Any<PromptExecutionSettings?>(),
            Arg.Any<Kernel?>(),
            Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(ci => Task.FromResult<IReadOnlyList<ChatMessageContent>>(
                new List<ChatMessageContent> { responder(ci.Arg<ChatHistory>()) }));
        return chat;
    }
    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_ReturnsEmpty_OnMissingChatService()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment
        {
            FileName = "tags.txt",
            ContentType = "text/plain",
            Bytes = System.Text.Encoding.UTF8.GetBytes("content")
        };

        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 5, CancellationToken.None);
        json.Should().Contain("\"tags\":");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_PdfStream_MissingChatService_Fallback()
    {
        using var ms = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/pdf", FileName = "x.pdf", Stream = ms };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 5, CancellationToken.None);
        json.Should().Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_NullAttachment_ReturnsEmpty()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);
        var json = await sut.GenerateTagSuggestionsJsonAsync(null!, maxTags: 5, CancellationToken.None);
        var el = JsonSerializer.Deserialize<JsonElement>(json);
        el.GetProperty("tags").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_WithKnownTags_NullAttachment_ReturnsEmpty()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);
        var json = await sut.GenerateTagSuggestionsJsonAsync(null!, new[] { "jwt" }, maxTags: 5, CancellationToken.None);
        var el = JsonSerializer.Deserialize<JsonElement>(json);
        el.GetProperty("tags").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_OctetStreamStream_MissingChatService_Fallback()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/octet-stream", FileName = "blob.bin", Stream = ms };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 3, CancellationToken.None);
        json.Should().Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_WithChat_ReturnsCleanedJson()
    {
        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("```json\n{ \"tags\": [] }\n```") }));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hello"));
        var attachment = new AiFileAttachment { ContentType = "application/octet-stream", FileName = "x.bin", Stream = ms };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 2, CancellationToken.None);
        json.Should().StartWith("{").And.Contain("\"tags\"");
    }

    

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_PdfStream_WithChat_ReturnsCleanedJson()
    {
        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("```json\n{ \"tags\": [ ] }\n```") }));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        using var ms = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var attachment = new AiFileAttachment { ContentType = "application/pdf", FileName = "x.pdf", Stream = ms };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 2, CancellationToken.None);
        json.Should().StartWith("{").And.Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_WithKnownTags_ChatPath_CoversHistory()
    {
        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("```json\n{ \"tags\": [ {\"label\": \"jwt\", \"confidence\": 0.9, \"reason\": \"known\"} ] }\n```") }));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/octet-stream", FileName = "x.bin", Bytes = new byte[] { 1, 2, 3, 4 } };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, new[] { "jwt", "dotnet" }, maxTags: 3, CancellationToken.None);
        json.Should().StartWith("{").And.Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_PptxBytes_WithChat_ReturnsCleanedJson()
    {
        using var ms = new MemoryStream();
        using (var pres = DocumentFormat.OpenXml.Packaging.PresentationDocument.Create(ms, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
        {
            var presentationPart = pres.AddPresentationPart();
            presentationPart.Presentation = new DocumentFormat.OpenXml.Presentation.Presentation();
            var slidePart = presentationPart.AddNewPart<DocumentFormat.OpenXml.Packaging.SlidePart>();
            slidePart.Slide = new DocumentFormat.OpenXml.Presentation.Slide(new DocumentFormat.OpenXml.Presentation.CommonSlideData(new DocumentFormat.OpenXml.Presentation.ShapeTree()));
            var shapeTree = slidePart.Slide.CommonSlideData!.ShapeTree!;
            var shape = new DocumentFormat.OpenXml.Presentation.Shape();
            var textBody = new DocumentFormat.OpenXml.Presentation.TextBody();
            var paragraph = new DocumentFormat.OpenXml.Drawing.Paragraph();
            var run = new DocumentFormat.OpenXml.Drawing.Run();
            run.Append(new DocumentFormat.OpenXml.Drawing.Text("pptx bytes content"));
            paragraph.Append(run);
            textBody.Append(paragraph);
            shape.Append(textBody);
            shapeTree.Append(shape);
            presentationPart.Presentation.SlideIdList = new DocumentFormat.OpenXml.Presentation.SlideIdList();
            var slideId = new DocumentFormat.OpenXml.Presentation.SlideId() { RelationshipId = presentationPart.GetIdOfPart(slidePart), Id = (uint)256U };
            presentationPart.Presentation.SlideIdList.Append(slideId);
            presentationPart.Presentation.Save();
        }
        var bytes = ms.ToArray();

        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("```json\n{ \"tags\": [ ] }\n```") }));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation", FileName = "x.pptx", Bytes = bytes };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 2, CancellationToken.None);
        json.Should().StartWith("{").And.Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_DocxBytes_WithChat_ReturnsCleanedJson()
    {
        using var ms = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = wordDoc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            var body = main.Document.Body!;
            var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            var r = new DocumentFormat.OpenXml.Wordprocessing.Run();
            r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("docx bytes content"));
            p.AppendChild(r);
            body.AppendChild(p);
            main.Document.Save();
        }
        var bytes = ms.ToArray();

        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("```json\n{ \"tags\": [ ] }\n```") }));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", FileName = "x.docx", Bytes = bytes };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 2, CancellationToken.None);
        json.Should().StartWith("{").And.Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_ElseNoContent_WithChat_ReturnsCleanedJson()
    {
        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("```json\n{ \"tags\": [ ] }\n```") }));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/x-unknown", FileName = "x.bin" };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 2, CancellationToken.None);
        json.Should().StartWith("{").And.Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_ElseNoContent_ChatThrows_ReturnsEmptyFallback()
    {
        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => throw new InvalidOperationException("boom"));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/x-unknown", FileName = "x.bin" };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 2, CancellationToken.None);
        var el = JsonSerializer.Deserialize<JsonElement>(json);
        el.GetProperty("tags").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_DocxStream_WithChat_ReturnsCleanedJson_NoKnownTags()
    {
        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("```json\n{ \"tags\": [] }\n```") }));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        using var ms = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = wordDoc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            var body = main.Document.Body!;
            var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            var r = new DocumentFormat.OpenXml.Wordprocessing.Run();
            r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("docx content"));
            p.AppendChild(r);
            body.AppendChild(p);
            main.Document.Save();
        }
        ms.Position = 0;

        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", FileName = "x.docx", Stream = ms };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 3, CancellationToken.None);
        json.Should().StartWith("{").And.Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_PptxStream_WithChat_ReturnsCleanedJson()
    {
        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("```json\n{ \"tags\": [] }\n```") }));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        using var ms = new MemoryStream();
        using (var pres = DocumentFormat.OpenXml.Packaging.PresentationDocument.Create(ms, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
        {
            var presentationPart = pres.AddPresentationPart();
            presentationPart.Presentation = new DocumentFormat.OpenXml.Presentation.Presentation();
            var slidePart = presentationPart.AddNewPart<DocumentFormat.OpenXml.Packaging.SlidePart>();
            slidePart.Slide = new DocumentFormat.OpenXml.Presentation.Slide(new DocumentFormat.OpenXml.Presentation.CommonSlideData(new DocumentFormat.OpenXml.Presentation.ShapeTree()));
            var shapeTree = slidePart.Slide.CommonSlideData!.ShapeTree!;
            var shape = new DocumentFormat.OpenXml.Presentation.Shape();
            var textBody = new DocumentFormat.OpenXml.Presentation.TextBody();
            var bodyProps = new DocumentFormat.OpenXml.Drawing.BodyProperties();
            var listStyle = new DocumentFormat.OpenXml.Drawing.ListStyle();
            var paragraph = new DocumentFormat.OpenXml.Drawing.Paragraph();
            var run = new DocumentFormat.OpenXml.Drawing.Run();
            run.Append(new DocumentFormat.OpenXml.Drawing.Text("pptx content"));
            paragraph.Append(run);
            textBody.Append(bodyProps);
            textBody.Append(listStyle);
            textBody.Append(paragraph);
            shape.Append(textBody);
            shapeTree.Append(shape);
            presentationPart.Presentation.SlideIdList = new DocumentFormat.OpenXml.Presentation.SlideIdList();
            var slideId = new DocumentFormat.OpenXml.Presentation.SlideId() { RelationshipId = presentationPart.GetIdOfPart(slidePart), Id = (uint)256U };
            presentationPart.Presentation.SlideIdList.Append(slideId);
            presentationPart.Presentation.Save();
        }
        ms.Position = 0;

        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation", FileName = "x.pptx", Stream = ms };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 3, CancellationToken.None);
        json.Should().StartWith("{").And.Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_PdfBytes_WithChat_ReturnsCleanedJson()
    {
        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("```json\n{ \"tags\": [] }\n```") }));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/pdf", FileName = "x.pdf", Bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 } };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 2, CancellationToken.None);
        json.Should().StartWith("{").And.Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_TextBytes_ChatFenceOnly_FallsBackToPlainTextJson()
    {
        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("```") }));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment
        {
            ContentType = "text/plain",
            FileName = "tags.txt",
            Bytes = System.Text.Encoding.UTF8.GetBytes("C# JWT tutorial")
        };

        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 5, CancellationToken.None);
        json.Should().Contain("\"tags\"");
        var el = JsonSerializer.Deserialize<JsonElement>(json);
        el.GetProperty("tags").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_WithKnownTags_ChatFenceOnly_ReturnsEmptyString()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/octet-stream", FileName = "blob.bin" };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, new[] { "jwt" }, maxTags: 3, CancellationToken.None);
        var el = JsonSerializer.Deserialize<JsonElement>(json);
        el.GetProperty("tags").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void ProcessPowerPoint_Failure_ReturnsEmptyItems()
    {
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("not a pptx"));
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);
        var m = typeof(FileTagSuggestionPlugin).GetMethod("ProcessPowerPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (Microsoft.SemanticKernel.ChatCompletion.ChatMessageContentItemCollection)m!.Invoke(sut, new object[] { ms })!;
        items.Count.Should().Be(0);
    }

    [Fact]
    public void ProcessWordDocument_Failure_ReturnsEmptyItems()
    {
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("not a docx"));
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);
        var m = typeof(FileTagSuggestionPlugin).GetMethod("ProcessWordDocument", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (Microsoft.SemanticKernel.ChatCompletion.ChatMessageContentItemCollection)m!.Invoke(sut, new object[] { ms })!;
        items.Count.Should().Be(0);
    }

    [Fact]
    public void ExtractPlainText_TextType_EmptyBytes_ReturnsEmpty()
    {
        var m = typeof(FileTagSuggestionPlugin).GetMethod("ExtractPlainText", BindingFlags.NonPublic | BindingFlags.Static);
        var att = new AiFileAttachment { ContentType = "text/plain", Bytes = Array.Empty<byte>() };
        var s = (string)m!.Invoke(null, new object[] { att })!;
        s.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_WithKnownTags_UnknownType_NoContent_MissingChat_ReturnsEmptyStructure()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/x-unknown", FileName = "x.bin" };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, new[] { "dotnet" }, maxTags: 2, CancellationToken.None);
        var el = JsonSerializer.Deserialize<JsonElement>(json);
        el.GetProperty("tags").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_OctetBytes_WithChat_ReturnsCleanedJson()
    {
        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("```json\n{ \"tags\": [] }\n```") }));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/octet-stream", FileName = "x.bin", Bytes = new byte[] { 1, 2, 3 } };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 2, CancellationToken.None);
        json.Should().StartWith("{").And.Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_ChatThrows_ReturnsFallbackJson()
    {
        var builder = Kernel.CreateBuilder();
        var chat = CreateChatService(_ => throw new InvalidOperationException("boom"));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("docx text"));
        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", FileName = "x.docx", Stream = ms };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 2, CancellationToken.None);
        json.Should().Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_WithKnownTags_PptxNull_ReturnsEmpty()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation", FileName = "x.pptx" };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, new[] { "jwt" }, maxTags: 2, CancellationToken.None);
        var el = JsonSerializer.Deserialize<JsonElement>(json);
        el.GetProperty("tags").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_DocxStream_MissingChatService_Fallback()
    {
        using var ms = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = wordDoc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            var body = main.Document.Body!;
            var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            var r = new DocumentFormat.OpenXml.Wordprocessing.Run();
            r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("docx stream content"));
            p.AppendChild(r);
            body.AppendChild(p);
            main.Document.Save();
        }
        ms.Position = 0;

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", FileName = "x.docx", Stream = ms };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 5, CancellationToken.None);
        json.Should().Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_PptxStream_MissingChatService_Fallback()
    {
        using var ms = new MemoryStream();
        using (var pres = DocumentFormat.OpenXml.Packaging.PresentationDocument.Create(ms, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
        {
            var presentationPart = pres.AddPresentationPart();
            presentationPart.Presentation = new DocumentFormat.OpenXml.Presentation.Presentation();
            var slidePart = presentationPart.AddNewPart<DocumentFormat.OpenXml.Packaging.SlidePart>();
            slidePart.Slide = new DocumentFormat.OpenXml.Presentation.Slide(new DocumentFormat.OpenXml.Presentation.CommonSlideData(new DocumentFormat.OpenXml.Presentation.ShapeTree()));
            var shapeTree = slidePart.Slide.CommonSlideData!.ShapeTree!;
            var shape = new DocumentFormat.OpenXml.Presentation.Shape();
            var textBody = new DocumentFormat.OpenXml.Presentation.TextBody();
            var bodyProps = new DocumentFormat.OpenXml.Drawing.BodyProperties();
            var listStyle = new DocumentFormat.OpenXml.Drawing.ListStyle();
            var paragraph = new DocumentFormat.OpenXml.Drawing.Paragraph();
            var run = new DocumentFormat.OpenXml.Drawing.Run();
            run.Append(new DocumentFormat.OpenXml.Drawing.Text("pptx stream content"));
            paragraph.Append(run);
            textBody.Append(bodyProps);
            textBody.Append(listStyle);
            textBody.Append(paragraph);
            shape.Append(textBody);
            shapeTree.Append(shape);
            presentationPart.Presentation.SlideIdList = new DocumentFormat.OpenXml.Presentation.SlideIdList();
            var slideId = new DocumentFormat.OpenXml.Presentation.SlideId() { RelationshipId = presentationPart.GetIdOfPart(slidePart), Id = (uint)256U };
            presentationPart.Presentation.SlideIdList.Append(slideId);
            presentationPart.Presentation.Save();
        }
        ms.Position = 0;

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation", FileName = "x.pptx", Stream = ms };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 5, CancellationToken.None);
        json.Should().Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_OctetStreamBytes_MissingChatService_Fallback()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/octet-stream", FileName = "blob.bin", Bytes = new byte[] { 1, 2, 3, 4 } };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 3, CancellationToken.None);
        json.Should().Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_DocxStream_WithChat_ReturnsCleanedJson()
    {
        using var ms = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = wordDoc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            var body = main.Document.Body!;
            var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            var r = new DocumentFormat.OpenXml.Wordprocessing.Run();
            r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("docx stream content"));
            p.AppendChild(r);
            body.AppendChild(p);
            main.Document.Save();
        }
        ms.Position = 0;

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", FileName = "x.docx", Stream = ms };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 5, CancellationToken.None);
        json.Should().Contain("\"tags\"");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_WithKnownTags_PptxStream_MissingChatService_EmptyStructure()
    {
        using var ms = new MemoryStream();
        using (var pres = DocumentFormat.OpenXml.Packaging.PresentationDocument.Create(ms, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
        {
            var presentationPart = pres.AddPresentationPart();
            presentationPart.Presentation = new DocumentFormat.OpenXml.Presentation.Presentation();
            var slidePart = presentationPart.AddNewPart<DocumentFormat.OpenXml.Packaging.SlidePart>();
            slidePart.Slide = new DocumentFormat.OpenXml.Presentation.Slide(new DocumentFormat.OpenXml.Presentation.CommonSlideData(new DocumentFormat.OpenXml.Presentation.ShapeTree()));
            var shapeTree = slidePart.Slide.CommonSlideData!.ShapeTree!;
            var shape = new DocumentFormat.OpenXml.Presentation.Shape();
            var textBody = new DocumentFormat.OpenXml.Presentation.TextBody();
            var bodyProps = new DocumentFormat.OpenXml.Drawing.BodyProperties();
            var listStyle = new DocumentFormat.OpenXml.Drawing.ListStyle();
            var paragraph = new DocumentFormat.OpenXml.Drawing.Paragraph();
            var run = new DocumentFormat.OpenXml.Drawing.Run();
            run.Append(new DocumentFormat.OpenXml.Drawing.Text("pptx stream content"));
            paragraph.Append(run);
            textBody.Append(bodyProps);
            textBody.Append(listStyle);
            textBody.Append(paragraph);
            shape.Append(textBody);
            shapeTree.Append(shape);
            presentationPart.Presentation.SlideIdList = new DocumentFormat.OpenXml.Presentation.SlideIdList();
            var slideId = new DocumentFormat.OpenXml.Presentation.SlideId() { RelationshipId = presentationPart.GetIdOfPart(slidePart), Id = (uint)256U };
            presentationPart.Presentation.SlideIdList.Append(slideId);
            presentationPart.Presentation.Save();
        }
        ms.Position = 0;

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation", FileName = "x.pptx", Stream = ms };
        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, new[] { "jwt" }, maxTags: 5, CancellationToken.None);
        json.Should().Contain("\"tags\"");
    }

    [Fact]
    public void CleanToJson_StripsCodeFences_AndExtractsBraces()
    {
        var raw = "```json\n{ \"tags\": [] }\n```";
        var m = typeof(FileTagSuggestionPlugin).GetMethod("CleanToJson", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("{").And.EndWith("}");
        cleaned.Should().Contain("\"tags\"");
    }

    [Fact]
    public void NormalizeLabel_MapsCommonSynonyms()
    {
        var m = typeof(FileTagSuggestionPlugin).GetMethod("NormalizeLabel", BindingFlags.NonPublic | BindingFlags.Static);
        ((string)m!.Invoke(null, new object[] { "C#" })!).Should().Be("csharp");
        ((string)m!.Invoke(null, new object[] { ".NET" })!).Should().Be("dotnet");
        ((string)m!.Invoke(null, new object[] { "APIs" })!).Should().Be("api");
    }

    [Fact]
    public void NormalizeLabel_Singularizes_CommonPluralForms()
    {
        var m = typeof(FileTagSuggestionPlugin).GetMethod("NormalizeLabel", BindingFlags.NonPublic | BindingFlags.Static);
        ((string)m!.Invoke(null, new object[] { "stories" })!).Should().Be("story");
        ((string)m!.Invoke(null, new object[] { "buses" })!).Should().Be("bus");
        ((string)m!.Invoke(null, new object[] { "cats" })!).Should().Be("cat");
    }

    [Fact]
    public void BuildFallbackTagsJson_ProducesTags_FromTextAndKnownTags()
    {
        var m = typeof(FileTagSuggestionPlugin).GetMethod("BuildFallbackTagsJson", BindingFlags.NonPublic | BindingFlags.Static);
        var text = "C# JWT .NET tutorial tutorial beginner";
        var known = new[] { "JWT", ".NET" };
        var json = (string)m!.Invoke(null, new object[] { text, known, 5 })!;
        json.Should().Contain("\"tags\"");
        var el = JsonSerializer.Deserialize<JsonElement>(json);
        el.GetProperty("tags").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void BuildFallbackTagsJson_FiltersStopWords()
    {
        var m = typeof(FileTagSuggestionPlugin).GetMethod("BuildFallbackTagsJson", BindingFlags.NonPublic | BindingFlags.Static);
        var text = "The introduction to physics and calculus is in the course and we are learning";
        var json = (string)m!.Invoke(null, new object[] { text, Array.Empty<string>(), 10 })!;
        var el = JsonSerializer.Deserialize<JsonElement>(json);
        var tags = el.GetProperty("tags");
        foreach (var item in tags.EnumerateArray())
        {
            var label = item.GetProperty("label").GetString()!.ToLowerInvariant();
            new[] { "the","and","or","for","with","without","a","an","of","in","on","to","from","by","at","as","is","are","was","were","be","been","being" }
                .Should().NotContain(label);
        }
    }

    [Fact]
    public void BuildFallbackTagsJson_ReturnsEmpty_OnWhitespace()
    {
        var m = typeof(FileTagSuggestionPlugin).GetMethod("BuildFallbackTagsJson", BindingFlags.NonPublic | BindingFlags.Static);
        var json = (string)m!.Invoke(null, new object[] { "   \n\t  ", Array.Empty<string>(), 10 })!;
        var el = JsonSerializer.Deserialize<JsonElement>(json);
        el.GetProperty("tags").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void ExtractPlainText_ReturnsContent_ForTextTypes()
    {
        var m = typeof(FileTagSuggestionPlugin).GetMethod("ExtractPlainText", BindingFlags.NonPublic | BindingFlags.Static);
        var text = new AiFileAttachment { ContentType = "text/plain", Bytes = System.Text.Encoding.UTF8.GetBytes("hello tags") };
        var s = (string)m!.Invoke(null, new object[] { text })!;
        s.Should().Contain("hello");
    }

    [Fact]
    public void ExtractPlainText_ReturnsContent_FromDocx()
    {
        using var ms = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = wordDoc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            var body = main.Document.Body!;
            var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            var r = new DocumentFormat.OpenXml.Wordprocessing.Run();
            r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("docx content"));
            p.AppendChild(r);
            body.AppendChild(p);
            main.Document.Save();
        }
        ms.Position = 0;

        var m = typeof(FileTagSuggestionPlugin).GetMethod("ExtractPlainText", BindingFlags.NonPublic | BindingFlags.Static);
        var att = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Stream = ms };
        var s = (string)m!.Invoke(null, new object[] { att })!;
        s.Should().Contain("docx content");
    }

    [Fact]
    public void ExtractPlainText_ReturnsContent_FromPptx()
    {
        using var ms = new MemoryStream();
        using (var pres = DocumentFormat.OpenXml.Packaging.PresentationDocument.Create(ms, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
        {
            var presentationPart = pres.AddPresentationPart();
            presentationPart.Presentation = new DocumentFormat.OpenXml.Presentation.Presentation();

            var slidePart = presentationPart.AddNewPart<DocumentFormat.OpenXml.Packaging.SlidePart>();
            slidePart.Slide = new DocumentFormat.OpenXml.Presentation.Slide(new DocumentFormat.OpenXml.Presentation.CommonSlideData(new DocumentFormat.OpenXml.Presentation.ShapeTree()));

            var shapeTree = slidePart.Slide.CommonSlideData!.ShapeTree!;
            var shape = new DocumentFormat.OpenXml.Presentation.Shape();
            var textBody = new DocumentFormat.OpenXml.Presentation.TextBody();
            var bodyProps = new DocumentFormat.OpenXml.Drawing.BodyProperties();
            var listStyle = new DocumentFormat.OpenXml.Drawing.ListStyle();
            var paragraph = new DocumentFormat.OpenXml.Drawing.Paragraph();
            var run = new DocumentFormat.OpenXml.Drawing.Run();
            run.Append(new DocumentFormat.OpenXml.Drawing.Text("pptx text"));
            paragraph.Append(run);
            textBody.Append(bodyProps);
            textBody.Append(listStyle);
            textBody.Append(paragraph);
            shape.Append(textBody);
            shapeTree.Append(shape);

            presentationPart.Presentation.SlideIdList = new DocumentFormat.OpenXml.Presentation.SlideIdList();
            var slideId = new DocumentFormat.OpenXml.Presentation.SlideId() { RelationshipId = presentationPart.GetIdOfPart(slidePart), Id = (uint)256U };
            presentationPart.Presentation.SlideIdList.Append(slideId);
            presentationPart.Presentation.Save();
        }
        ms.Position = 0;

        var m = typeof(FileTagSuggestionPlugin).GetMethod("ExtractPlainText", BindingFlags.NonPublic | BindingFlags.Static);
        var att = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation", Stream = ms };
        var s = (string)m!.Invoke(null, new object[] { att })!;
        s.Should().Contain("pptx text");
    }

    [Fact]
    public void ProcessWordDocument_ProducesTextItems()
    {
        using var ms = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = wordDoc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            var body = main.Document.Body!;
            var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            var r = new DocumentFormat.OpenXml.Wordprocessing.Run();
            r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("file tag docx"));
            p.AppendChild(r);
            body.AppendChild(p);
            main.Document.Save();
        }
        ms.Position = 0;

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);
        var m = typeof(FileTagSuggestionPlugin).GetMethod("ProcessWordDocument", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (Microsoft.SemanticKernel.ChatCompletion.ChatMessageContentItemCollection)m!.Invoke(sut, new object[] { ms })!;
        items.Should().NotBeNull();
        items.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ProcessPowerPoint_ProducesTextItems()
    {
        using var ms = new MemoryStream();
        using (var pres = DocumentFormat.OpenXml.Packaging.PresentationDocument.Create(ms, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
        {
            var presentationPart = pres.AddPresentationPart();
            presentationPart.Presentation = new DocumentFormat.OpenXml.Presentation.Presentation();

            var slidePart = presentationPart.AddNewPart<DocumentFormat.OpenXml.Packaging.SlidePart>();
            slidePart.Slide = new DocumentFormat.OpenXml.Presentation.Slide(new DocumentFormat.OpenXml.Presentation.CommonSlideData(new DocumentFormat.OpenXml.Presentation.ShapeTree()));

            var shapeTree = slidePart.Slide.CommonSlideData!.ShapeTree!;
            var shape = new DocumentFormat.OpenXml.Presentation.Shape();
            var textBody = new DocumentFormat.OpenXml.Presentation.TextBody();
            var bodyProps = new DocumentFormat.OpenXml.Drawing.BodyProperties();
            var listStyle = new DocumentFormat.OpenXml.Drawing.ListStyle();
            var paragraph = new DocumentFormat.OpenXml.Drawing.Paragraph();
            var run = new DocumentFormat.OpenXml.Drawing.Run();
            run.Append(new DocumentFormat.OpenXml.Drawing.Text("file tag pptx"));
            paragraph.Append(run);
            textBody.Append(bodyProps);
            textBody.Append(listStyle);
            textBody.Append(paragraph);
            shape.Append(textBody);
            shapeTree.Append(shape);

            presentationPart.Presentation.SlideIdList = new DocumentFormat.OpenXml.Presentation.SlideIdList();
            var slideId = new DocumentFormat.OpenXml.Presentation.SlideId() { RelationshipId = presentationPart.GetIdOfPart(slidePart), Id = (uint)256U };
            presentationPart.Presentation.SlideIdList.Append(slideId);
            presentationPart.Presentation.Save();
        }
        ms.Position = 0;

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);
        var m = typeof(FileTagSuggestionPlugin).GetMethod("ProcessPowerPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (Microsoft.SemanticKernel.ChatCompletion.ChatMessageContentItemCollection)m!.Invoke(sut, new object[] { ms })!;
        items.Should().NotBeNull();
        items.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_DocxNullStream_ReturnsEmptyStructure()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment
        {
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            FileName = "doc.docx",
            Bytes = null,
            Stream = null
        };

        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 3, CancellationToken.None);
        json.Should().Contain("\"tags\"");
        var el = JsonSerializer.Deserialize<JsonElement>(json);
        el.GetProperty("tags").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_PdfBytes_ReturnsFallbackJson()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment
        {
            ContentType = "application/pdf",
            FileName = "doc.pdf",
            Bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 } // %PDF
        };

        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, maxTags: 3, CancellationToken.None);
        json.Should().Contain("\"tags\":");
    }

    [Fact]
    public async Task GenerateTagSuggestionsJsonAsync_WithKnownTags_PdfBytes_ReturnsEmptyStructure()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);

        var attachment = new AiFileAttachment
        {
            ContentType = "application/pdf",
            FileName = "doc.pdf",
            Bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }
        };

        var json = await sut.GenerateTagSuggestionsJsonAsync(attachment, new[] { "c#" }, maxTags: 3, CancellationToken.None);
        json.Should().Contain("\"tags\":");
    }

    [Fact]
    public void ProcessWordDocument_AddsImageItems_FromDocxWithImage()
    {
        using var ms = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = wordDoc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            var body = main.Document.Body!;
            var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            var r = new DocumentFormat.OpenXml.Wordprocessing.Run();
            r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("docx text"));
            p.AppendChild(r);
            body.AppendChild(p);

            var imagePart = main.AddNewPart<DocumentFormat.OpenXml.Packaging.ImagePart>("image/png");
            using (var imgStream = imagePart.GetStream())
            {
                var pngBytes = new byte[] {
                    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
                    0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                    0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
                    0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE,
                    0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C,
                    0x63, 0xF8, 0xCF, 0x00, 0x00, 0x04, 0x00, 0x01, 0xE2, 0x26,
                    0x05, 0x9B, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
                    0xAE, 0x42, 0x60, 0x82
                };
                imgStream.Write(pngBytes, 0, pngBytes.Length);
            }

            main.Document.Save();
        }
        ms.Position = 0;

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<FileTagSuggestionPlugin>>();
        var sut = new FileTagSuggestionPlugin(kernel, logger);
        var m = typeof(FileTagSuggestionPlugin).GetMethod("ProcessWordDocument", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (Microsoft.SemanticKernel.ChatCompletion.ChatMessageContentItemCollection)m!.Invoke(sut, new object[] { ms })!;
        bool foundImage = false;
        foreach (var it in items)
        {
            if (it != null && it.GetType().Name == "ImageContent")
            {
                foundImage = true;
                break;
            }
        }
        foundImage.Should().BeTrue();
    }
}
