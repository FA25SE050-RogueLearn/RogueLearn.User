using System.Text.Json;
using FluentAssertions;
using RogueLearn.User.Application.Common;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Common;

public class SyllabusSessionDtoConverterTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SyllabusSessionDtoConverter());
        return options;
    }

    [Fact]
    public void DeserializesWithFlexibleCasingAndNullFiltering()
    {
        var options = CreateOptions();
        var json = "{\"sessionnumber\":3,\"TOPIC\":\"Intro\",\"Activities\":[\"A\",null],\"readings\":[null,\"R\"],\"SuggestedUrl\":\" `http://example` \"}";
        var dto = JsonSerializer.Deserialize<SyllabusSessionDto>(json, options)!;

        dto.SessionNumber.Should().Be(3);
        dto.Topic.Should().Be("Intro");
        dto.Activities.Should().Equal("A");
        dto.Readings.Should().Equal("R");
        dto.SuggestedUrl.Should().Be("http://example");
    }

    [Fact]
    public void SerializesUsingModelAttributes()
    {
        var dto = new SyllabusSessionDto { SessionNumber = 1, SuggestedUrl = "http://x" };
        var json = JsonSerializer.Serialize(dto);
        json.Should().Contain("\"suggestedUrl\":\"http://x\"");
    }
}