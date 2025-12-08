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

    [Fact]
    public void ValidateEscapeSequences_Finds_OverEscaped_Issues()
    {
        var input = "{ \"text\": \"\\\\\\\\n\\\\\\\\0\" }"; // 8 backslashes patterns
        var (isValid, issues) = EscapeSequenceCleaner.ValidateEscapeSequences(input);
        isValid.Should().BeFalse();
        issues.Should().NotBeEmpty();
    }

    [Fact]
    public void CleanAndValidate_Fails_On_Empty_Input()
    {
        var (success, cleaned, error) = EscapeSequenceCleaner.CleanAndValidate("");
        success.Should().BeFalse();
        cleaned.Should().BeNull();
        error.Should().NotBeNull();
    }

    [Fact]
    public void NormalizeEscapeSequences_Reduces_Backslashes()
    {
        var input = "{ \"text\": \"\\\\\\\\t\\\\\\\\n\" }";
        var normalized = EscapeSequenceCleaner.NormalizeEscapeSequences(input);
        normalized.Should().Contain("newline");
        normalized.Should().NotMatchRegex("\\\\{4,}");
    }
}