using System.Linq;
using FluentAssertions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Models;

public class BlockNoteDocumentFactoryTests
{
    [Fact]
    public void FromPlainText_ReturnsHeadingAndParagraphs()
    {
        var text = "Summary\nLine one\nLine two";
        var blocks = BlockNoteDocumentFactory.FromPlainText(text);
        blocks.Should().NotBeNull();
        blocks.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FromPlainText_ReturnsEmpty_ForEmptyText()
    {
        var blocks = BlockNoteDocumentFactory.FromPlainText("");
        blocks.Should().NotBeNull();
        blocks.Should().BeEmpty();
    }
}