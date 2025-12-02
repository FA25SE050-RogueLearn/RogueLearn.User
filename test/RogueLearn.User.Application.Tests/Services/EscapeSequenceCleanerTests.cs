using FluentAssertions;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class EscapeSequenceCleanerTests
{
    [Fact]
    public void CleanEscapeSequences_Returns_String_And_Is_Not_Empty()
    {
        var input = "{\"text\": \"Line\\nTab\\tBackslash\\\\\"}";
        var cleaned = EscapeSequenceCleaner.CleanEscapeSequences(input);
        cleaned.Should().NotBeNullOrEmpty();
        cleaned.Should().Contain("Line");
    }

    [Fact]
    public void CleanAndValidate_Succeeds_On_Minimal_Activities_Json()
    {
        var json = "{ \"activities\": [] }";
        var (success, cleaned, error) = EscapeSequenceCleaner.CleanAndValidate(json);
        success.Should().BeTrue();
        cleaned.Should().NotBeNull();
        error.Should().BeNull();
    }
}