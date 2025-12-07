using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Plugins;

public class ContentSummarizationPluginTests
{
    private static IChatCompletionService CreateChatService(Func<ChatHistory, ChatMessageContent> responder)
    {
        var chat = Substitute.For<IChatCompletionService>();
        chat.GetChatMessageContentsAsync(
            Arg.Any<ChatHistory>(),
            Arg.Any<PromptExecutionSettings?>(),
            Arg.Any<Kernel?>(),
            Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult<IReadOnlyList<ChatMessageContent>>(
                new List<ChatMessageContent> { responder((ChatHistory)ci[0]) }));
        return chat;
    }

    private static IChatCompletionService CreateThrowingChatService(Exception ex)
    {
        var chat = Substitute.For<IChatCompletionService>();
        chat.GetChatMessageContentsAsync(
            Arg.Any<ChatHistory>(),
            Arg.Any<PromptExecutionSettings?>(),
            Arg.Any<Kernel?>(),
            Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<IReadOnlyList<ChatMessageContent>>(ex));
        return chat;
    }
    
    [Fact]
    public async Task SummarizeTextAsync_ReturnsNull_OnMissingChatService()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var result = await sut.SummarizeTextAsync("some text", CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public void TryParseBlockNoteJsonToObject_StripsFourBackticksFence()
    {
        var json = "````\n[ 1 ]\n````";
        var parse = typeof(ContentSummarizationPlugin).GetMethod("TryParseBlockNoteJsonToObject", BindingFlags.NonPublic | BindingFlags.Static);
        var res = parse!.Invoke(null, new object[] { json });
        res.Should().NotBeNull();
        var list = (List<object?>)res!;
        list[0].Should().Be(1);
    }

    [Fact]
    public void ExtractPlainTextFromAttachment_ReturnsJsonString()
    {
        var m = typeof(ContentSummarizationPlugin).GetMethod("ExtractPlainTextFromAttachment", BindingFlags.NonPublic | BindingFlags.Static);
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes("[ { \"type\": \"paragraph\" } ]");
        var att = new AiFileAttachment { ContentType = "application/json", Bytes = jsonBytes };
        var text = (string)m!.Invoke(null, new object[] { att })!;
        text.Should().Contain("paragraph");
    }

    [Fact]
    public async Task SummarizeAsync_JsonBytes_ChatThrows_FallbackUsesPlainJson()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateThrowingChatService(new InvalidOperationException("boom"));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var bytes = System.Text.Encoding.UTF8.GetBytes("[ { \"type\": \"paragraph\" } ]");
        var attachment = new AiFileAttachment { ContentType = "application/json", Bytes = bytes };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_Pdf_ByExtension_WithChat_ReturnsBlocksArray()
    {
        using var ms = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[ { \"type\": \"paragraph\" } ]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = null!, FileName = "x.pdf", Stream = ms };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_PdfBytes_NonJsonChat_ReturnsUnavailableFallback()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("nonsense") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/pdf", FileName = "x.pdf", Bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 } };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_OctetStreamBytes_NonJsonChat_ReturnsUnavailableFallback()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("nonsense") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/octet-stream", FileName = "blob.bin", Bytes = new byte[] { 1, 2, 3, 4 } };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public void ProcessPowerPoint_AddsHeaderWithoutText_FromEmptySlide()
    {
        using var ms = new MemoryStream();
        using (var pres = DocumentFormat.OpenXml.Packaging.PresentationDocument.Create(ms, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
        {
            var presentationPart = pres.AddPresentationPart();
            presentationPart.Presentation = new DocumentFormat.OpenXml.Presentation.Presentation();
            var slidePart = presentationPart.AddNewPart<DocumentFormat.OpenXml.Packaging.SlidePart>();
            slidePart.Slide = new DocumentFormat.OpenXml.Presentation.Slide(new DocumentFormat.OpenXml.Presentation.CommonSlideData(new DocumentFormat.OpenXml.Presentation.ShapeTree()));
            presentationPart.Presentation.SlideIdList = new DocumentFormat.OpenXml.Presentation.SlideIdList();
            var slideId = new DocumentFormat.OpenXml.Presentation.SlideId() { RelationshipId = presentationPart.GetIdOfPart(slidePart), Id = (uint)256U };
            presentationPart.Presentation.SlideIdList.Append(slideId);
            presentationPart.Presentation.Save();
        }
        ms.Position = 0;
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var m = typeof(ContentSummarizationPlugin).GetMethod("ProcessPowerPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (Microsoft.SemanticKernel.ChatCompletion.ChatMessageContentItemCollection)m!.Invoke(sut, new object[] { ms })!;
        items.Should().NotBeNull();
        items.Count.Should().Be(1);
    }

    [Fact]
    public async Task SummarizeAsync_Pptx_EmptyArray_UsesLocalTextFallback()
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
            run.Append(new DocumentFormat.OpenXml.Drawing.Text("pptx local text"));
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

        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation", Stream = ms, FileName = "x.pptx" };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_Text_ChatThrows_FallbackPlainText()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateThrowingChatService(new InvalidOperationException("boom"));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "text/plain", Bytes = System.Text.Encoding.UTF8.GetBytes("plain hello") };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_Docx_ByExtension_ReturnsBlocks_WithChat()
    {
        using var ms = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = wordDoc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            var body = main.Document.Body!;
            var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            var r = new DocumentFormat.OpenXml.Wordprocessing.Run();
            r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("docx ext path"));
            p.AppendChild(r);
            body.AppendChild(p);
            main.Document.Save();
        }
        ms.Position = 0;

        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[ { \"type\": \"paragraph\" } ]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = null!, Stream = ms, FileName = "a.docx" };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public void ProcessPowerPoint_BreaksAfter20Slides()
    {
        using var ms = new MemoryStream();
        using (var pres = DocumentFormat.OpenXml.Packaging.PresentationDocument.Create(ms, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
        {
            var presentationPart = pres.AddPresentationPart();
            presentationPart.Presentation = new DocumentFormat.OpenXml.Presentation.Presentation();
            for (int i = 0; i < 21; i++)
            {
                var slidePart = presentationPart.AddNewPart<DocumentFormat.OpenXml.Packaging.SlidePart>();
                slidePart.Slide = new DocumentFormat.OpenXml.Presentation.Slide(new DocumentFormat.OpenXml.Presentation.CommonSlideData(new DocumentFormat.OpenXml.Presentation.ShapeTree()));
                var shapeTree = slidePart.Slide.CommonSlideData!.ShapeTree!;
                var shape = new DocumentFormat.OpenXml.Presentation.Shape();
                var textBody = new DocumentFormat.OpenXml.Presentation.TextBody();
                var bodyProps = new DocumentFormat.OpenXml.Drawing.BodyProperties();
                var listStyle = new DocumentFormat.OpenXml.Drawing.ListStyle();
                var paragraph = new DocumentFormat.OpenXml.Drawing.Paragraph();
                var run = new DocumentFormat.OpenXml.Drawing.Run();
                run.Append(new DocumentFormat.OpenXml.Drawing.Text($"slide {i}"));
                paragraph.Append(run);
                textBody.Append(bodyProps);
                textBody.Append(listStyle);
                textBody.Append(paragraph);
                shape.Append(textBody);
                shapeTree.Append(shape);
                if (presentationPart.Presentation.SlideIdList == null)
                {
                    presentationPart.Presentation.SlideIdList = new DocumentFormat.OpenXml.Presentation.SlideIdList();
                }
                var slideId = new DocumentFormat.OpenXml.Presentation.SlideId() { RelationshipId = presentationPart.GetIdOfPart(slidePart), Id = (uint)(256U + i) };
                presentationPart.Presentation.SlideIdList.Append(slideId);
            }
            presentationPart.Presentation.Save();
        }
        ms.Position = 0;
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var m = typeof(ContentSummarizationPlugin).GetMethod("ProcessPowerPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (Microsoft.SemanticKernel.ChatCompletion.ChatMessageContentItemCollection)m!.Invoke(sut, new object[] { ms })!;
        items.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SummarizeAsync_TextStream_WithChat_ReturnsBlocksArray()
    {
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hello stream text"));
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[ { \"type\": \"paragraph\" } ]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "text/plain", FileName = "t.txt", Stream = ms };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_ParsesObjectWrapper_WhenChatReturnsObject()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("{ \"blocks\": [ { \"type\": \"paragraph\" } ] }") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "text/plain", Bytes = System.Text.Encoding.UTF8.GetBytes("x") };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsNull_WhenEmptyContentItems()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "image/png", FileName = "x.png" };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SummarizeAsync_PdfBytes_WithChat_ReturnsBlocksArray()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[ { \"type\": \"paragraph\" } ]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/pdf", FileName = "x.pdf", Bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 } };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_OctetStream_Bytes_WithChat_ReturnsBlocks()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[ { \"type\": \"paragraph\" } ]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/octet-stream", FileName = "blob.bin", Bytes = new byte[] { 1, 2, 3 } };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeTextAsync_UsesPlainTextFallback_OnEmptyArray()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var res = await sut.SummarizeTextAsync("hello world", CancellationToken.None);
        res.Should().NotBeNull();
    }

    [Fact]
    public void TryParseBlockNoteJsonToObject_ParsesDoubleNumber()
    {
        var arr = "[ 3.14 ]";
        var parse = typeof(ContentSummarizationPlugin).GetMethod("TryParseBlockNoteJsonToObject", BindingFlags.NonPublic | BindingFlags.Static);
        var res = parse!.Invoke(null, new object[] { arr });
        res.Should().NotBeNull();
        var list = (List<object?>)res!;
        list[0].Should().Be(3.14);
    }

    [Fact]
    public void ExtractTextFromPowerPoint_ReturnsEmpty_OnInvalidStream()
    {
        using var ms = new MemoryStream(new byte[] { 9, 9, 9 });
        var m = typeof(ContentSummarizationPlugin).GetMethod("ExtractTextFromPowerPoint", BindingFlags.NonPublic | BindingFlags.Static);
        var text = (string)m!.Invoke(null, new object[] { ms })!;
        text.Should().BeEmpty();
    }

    [Fact]
    public async Task SummarizeAsync_PdfStream_ReturnsNull_OnMissingChatService()
    {
        using var ms = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/pdf", FileName = "x.pdf", Stream = ms };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SummarizeAsync_OctetStream_ImageStream_WithChat_ReturnsBlocks()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[ { \"type\": \"paragraph\" } ]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/octet-stream", FileName = "blob.bin", Stream = ms };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_Pdf_WithChat_ReturnsUnavailableFallback()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("nonsense") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();

        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/pdf", FileName = "x.pdf", Bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 } };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeTextAsync_ReturnsNull_WhenChatThrows()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateThrowingChatService(new InvalidOperationException("boom"));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var res = await sut.SummarizeTextAsync("hello", CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public void ProcessPowerPoint_ReturnsEmpty_OnInvalidStream()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var m = typeof(ContentSummarizationPlugin).GetMethod("ProcessPowerPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (Microsoft.SemanticKernel.ChatCompletion.ChatMessageContentItemCollection)m!.Invoke(sut, new object[] { ms })!;
        items.Count.Should().Be(0);
    }

    [Fact]
    public void ProcessWordDocument_ReturnsEmpty_OnInvalidStream()
    {
        using var ms = new MemoryStream(new byte[] { 5, 6, 7, 8 });
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var m = typeof(ContentSummarizationPlugin).GetMethod("ProcessWordDocument", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (Microsoft.SemanticKernel.ChatCompletion.ChatMessageContentItemCollection)m!.Invoke(sut, new object[] { ms })!;
        items.Count.Should().Be(0);
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsNull_OnMissingChatService()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var attachment = new AiFileAttachment
        {
            FileName = "test.txt",
            ContentType = "text/plain",
            Bytes = System.Text.Encoding.UTF8.GetBytes("Hello")
        };

        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_Docx_WithChat_ReturnsBlocksArray()
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

        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[ { \"type\": \"paragraph\" } ]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();

        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Stream = ms, FileName = "a.docx" };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_Pptx_WithChat_ReturnsBlocksArray()
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

        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[ { \"type\": \"paragraph\" } ]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();

        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation", Stream = ms, FileName = "x.pptx" };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_Text_WithChat_ReturnsPlain_OnNonJson()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("hello, not json") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();

        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "text/plain", Bytes = System.Text.Encoding.UTF8.GetBytes("some text") };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_Docx_EmptyContent_ReturnsNull()
    {
        using var ms = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = wordDoc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            main.Document.Save();
        }
        ms.Position = 0;

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Stream = ms, FileName = "a.docx" };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SummarizeAsync_Docx_ReturnsPlainTextFallback_OnMissingChatService()
    {
        using var ms = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = wordDoc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            var body = main.Document.Body!;
            var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            var r = new DocumentFormat.OpenXml.Wordprocessing.Run();
            r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("docx summarize fallback"));
            p.AppendChild(r);
            body.AppendChild(p);
            main.Document.Save();
        }
        ms.Position = 0;

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Stream = ms, FileName = "a.docx" };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsNull_WhenNoContent()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/octet-stream", FileName = "x.bin" };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SummarizeAsync_UsesPlainTextFallback_WhenChatReturnsEmptyArray()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();

        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "text/plain", Bytes = System.Text.Encoding.UTF8.GetBytes("hello world") };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_ParsesBlocks_WhenChatReturnsArray()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[ { \"type\": \"paragraph\" } ]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();

        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "text/plain", Bytes = System.Text.Encoding.UTF8.GetBytes("some text") };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeTextAsync_ParsesBlocks_WhenChatReturnsArray()
    {
        var builder = Kernel.CreateBuilder();
        var fake = CreateChatService(_ => new ChatMessageContent(AuthorRole.Assistant, new ChatMessageContentItemCollection { new TextContent("[ { \"type\": \"paragraph\" } ]") }));
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();

        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var res = await sut.SummarizeTextAsync("hello", CancellationToken.None);
        res.Should().NotBeNull();
    }

    [Fact]
    public void SanitizeJsonOutput_ExtractsBlocksArray_FromObjectWrapper()
    {
        var raw = "{\"blocks\": [ { \"x\": 1 } ] }";
        var m = typeof(ContentSummarizationPlugin).GetMethod("SanitizeJsonOutput", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().StartWith("[").And.EndWith("]");
        cleaned.Should().Contain("\"x\"");
    }

    [Fact]
    public void SanitizeJsonOutput_ExtractsArray_FromCodeFence()
    {
        var raw = "```json\n[ { \"a\": 1 } ]\n```";
        var m = typeof(ContentSummarizationPlugin).GetMethod("SanitizeJsonOutput", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().Be("[ { \"a\": 1 } ]");
    }

    [Fact]
    public void SanitizeJsonOutput_ReturnsInput_WhenNoArrayOrWrapperFound()
    {
        var raw = "not json";
        var m = typeof(ContentSummarizationPlugin).GetMethod("SanitizeJsonOutput", BindingFlags.NonPublic | BindingFlags.Static);
        var cleaned = (string)m!.Invoke(null, new object[] { raw })!;
        cleaned.Should().Be("not json");
    }

    [Fact]
    public void TryParseBlockNoteJsonToObject_ParsesArrayAndObjectWrapper()
    {
        var parse = typeof(ContentSummarizationPlugin).GetMethod("TryParseBlockNoteJsonToObject", BindingFlags.NonPublic | BindingFlags.Static);
        var arr = "[ { \"type\": \"paragraph\" } ]";
        var res1 = parse!.Invoke(null, new object[] { arr });
        res1.Should().NotBeNull();

        var obj = "{ \"blocks\": [ { \"type\": \"heading\" } ] }";
        var res2 = parse!.Invoke(null, new object[] { obj });
        res2.Should().NotBeNull();
    }

    [Fact]
    public void ExtractPlainTextFromAttachment_ReadsTextAndJson()
    {
        var m = typeof(ContentSummarizationPlugin).GetMethod("ExtractPlainTextFromAttachment", BindingFlags.NonPublic | BindingFlags.Static);
        var text = new AiFileAttachment { ContentType = "text/plain", Bytes = System.Text.Encoding.UTF8.GetBytes("hello world") };
        var s1 = (string)m!.Invoke(null, new object[] { text })!;
        s1.Should().Contain("hello");

        var json = new AiFileAttachment { ContentType = "application/json", Bytes = System.Text.Encoding.UTF8.GetBytes("{\"a\":1}") };
        var s2 = (string)m!.Invoke(null, new object[] { json })!;
        s2.Should().Contain("\"a\"");
    }

    [Fact]
    public async Task SummarizeTextAsync_ReturnsNull_OnEmptyInput()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);
        var res = await sut.SummarizeTextAsync(string.Empty);
        res.Should().BeNull();
    }

    [Fact]
    public void TryParseBlockNoteJsonToObject_ReturnsNull_OnInvalidJson()
    {
        var parse = typeof(ContentSummarizationPlugin).GetMethod("TryParseBlockNoteJsonToObject", BindingFlags.NonPublic | BindingFlags.Static);
        var invalid = "not json";
        var res = parse!.Invoke(null, new object[] { invalid });
        res.Should().BeNull();
    }

    [Fact]
    public void ProcessWordDocument_AddsTextItems_FromDocx()
    {
        using var ms = new MemoryStream();
        using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = wordDoc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            var body = main.Document.Body!;
            var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            var r = new DocumentFormat.OpenXml.Wordprocessing.Run();
            r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("docx summary text"));
            p.AppendChild(r);
            body.AppendChild(p);
            main.Document.Save();
        }
        ms.Position = 0;

        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var m = typeof(ContentSummarizationPlugin).GetMethod("ProcessWordDocument", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (Microsoft.SemanticKernel.ChatCompletion.ChatMessageContentItemCollection)m!.Invoke(sut, new object[] { ms })!;
        items.Should().NotBeNull();
        items.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ProcessPowerPoint_AddsTextItems_FromPptx()
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
            run.Append(new DocumentFormat.OpenXml.Drawing.Text("pptx summary"));
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
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var m = typeof(ContentSummarizationPlugin).GetMethod("ProcessPowerPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (Microsoft.SemanticKernel.ChatCompletion.ChatMessageContentItemCollection)m!.Invoke(sut, new object[] { ms })!;
        items.Should().NotBeNull();
        items.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ExtractTextFromPowerPoint_ParsesMinimalPptx()
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
            run.Append(new DocumentFormat.OpenXml.Drawing.Text("pptx text for extract"));
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

        var m = typeof(ContentSummarizationPlugin).GetMethod("ExtractTextFromPowerPoint", BindingFlags.NonPublic | BindingFlags.Static);
        var text = (string)m!.Invoke(null, new object[] { ms })!;
        text.Should().Contain("pptx text for extract");
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsPlainTextFallback_OnPptx_WithMissingChatService()
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
            run.Append(new DocumentFormat.OpenXml.Drawing.Text("pptx summary fallback"));
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
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var attachment = new AiFileAttachment { ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation", Stream = ms, FileName = "x.pptx" };
        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_PdfBytes_ReturnsNull_OnMissingChatService()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var attachment = new AiFileAttachment
        {
            ContentType = "application/pdf",
            FileName = "x.pdf",
            Bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }
        };

        var result = await sut.SummarizeAsync(attachment, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public void TryParseBlockNoteJsonToObject_ParsesMultipleTypes()
    {
        var json = "[ \"text\", 5, true, false, null, { \"k\": 2 } ]";
        var parse = typeof(ContentSummarizationPlugin).GetMethod("TryParseBlockNoteJsonToObject", BindingFlags.NonPublic | BindingFlags.Static);
        var res = parse!.Invoke(null, new object[] { json });
        res.Should().NotBeNull();
        var list = (List<object?>)res!;
        list[0].Should().Be("text");
        list[1].Should().Be(5);
        list[2].Should().Be(true);
        list[3].Should().Be(false);
        list[4].Should().BeNull();
        var dict = (Dictionary<string, object>)list[5]!;
        dict["k"].Should().Be(2);
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
        var logger = Substitute.For<ILogger<ContentSummarizationPlugin>>();
        var sut = new ContentSummarizationPlugin(kernel, logger);

        var m = typeof(ContentSummarizationPlugin).GetMethod("ProcessWordDocument", BindingFlags.NonPublic | BindingFlags.Instance);
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
