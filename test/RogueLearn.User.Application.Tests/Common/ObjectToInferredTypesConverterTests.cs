using System.Text.Json;
using FluentAssertions;
using RogueLearn.User.Application.Common;

namespace RogueLearn.User.Application.Tests.Common;

public class ObjectToInferredTypesConverterTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ObjectToInferredTypesConverter());
        return options;
    }

    [Fact]
    public void CanDeserializePrimitiveValues()
    {
        var options = CreateOptions();

        JsonSerializer.Deserialize<object>("true", options).Should().Be(true);
        JsonSerializer.Deserialize<object>("false", options).Should().Be(false);
        JsonSerializer.Deserialize<object>("123", options).Should().Be(123L);
        JsonSerializer.Deserialize<object>("123.5", options).Should().Be(123.5);
        JsonSerializer.Deserialize<object>("\"hello\"", options).Should().Be("hello");
        JsonSerializer.Deserialize<object>("\"2024-10-01T12:34:56Z\"", options).Should().BeOfType<DateTime>();
    }

    [Fact]
    public void CanDeserializeObjectAndArray()
    {
        var options = CreateOptions();
        var obj = JsonSerializer.Deserialize<object>("{\"a\":1,\"b\":[true,\"x\"]}", options);

        obj.Should().BeOfType<Dictionary<string, object>>();
        var dict = (Dictionary<string, object>)obj!;
        dict["a"].Should().Be(1L);
        dict["b"].Should().BeOfType<List<object>>();
        var list = (List<object>)dict["b"];
        list[0].Should().Be(true);
        list[1].Should().Be("x");
    }

    [Fact]
    public void CanSerializeArbitraryObject()
    {
        var options = CreateOptions();
        var payload = new { a = 1, b = "x" };
        var json = JsonSerializer.Serialize<object>(payload, options);
        json.Should().Contain("\"a\":1");
        json.Should().Contain("\"b\":\"x\"");
    }

    [Fact]
    public void InvalidObjectToken_ThrowsJsonException()
    {
        var options = CreateOptions();
        var badJson = "{ 123 }"; // non-PropertyName inside object
        Action act = () => JsonSerializer.Deserialize<object>(badJson, options);
        act.Should().Throw<JsonException>();
    }
}