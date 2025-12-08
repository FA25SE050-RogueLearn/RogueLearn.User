using FluentAssertions;
using RogueLearn.User.Application.Mappings;
using System.Text.Json;
using System.Reflection;

namespace RogueLearn.User.Application.Tests.Mappings;

public class MappingProfileTests
{
    [Fact]
    public void Can_Instantiate_MappingProfile()
    {
        var profile = new MappingProfile();
        profile.Should().NotBeNull();
    }

    [Fact]
    public void ParseJsonContent_With_JsonString_Parses_To_Object()
    {
        var method = typeof(MappingProfile).GetMethod("ParseJsonContent", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = method.Invoke(null, new object?[] { "{\"a\":1}" });
        result.Should().BeOfType<Dictionary<string, object>>();
        ((Dictionary<string, object>)result!)["a"].Should().Be(1);
    }

    [Fact]
    public void ParseJsonContent_With_JsonElement_Number_Parses_To_Int()
    {
        var el = JsonSerializer.Deserialize<JsonElement>("42");
        var method = typeof(MappingProfile).GetMethod("ParseJsonContent", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = method.Invoke(null, new object?[] { el });
        result.Should().Be(42);
    }

    [Fact]
    public void ParseJsonContent_With_NonJson_String_Returns_String()
    {
        var method = typeof(MappingProfile).GetMethod("ParseJsonContent", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = method.Invoke(null, new object?[] { "hello" });
        result.Should().Be("hello");
    }

    [Fact]
    public void ParseJsonContent_With_Null_Returns_Null()
    {
        var method = typeof(MappingProfile).GetMethod("ParseJsonContent", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = method.Invoke(null, new object?[] { null });
        result.Should().BeNull();
    }

    [Fact]
    public void ParseJsonContent_ArrayString_Parses_To_List()
    {
        var json = "[{}]";
        var method = typeof(MappingProfile).GetMethod("ParseJsonContent", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = method.Invoke(null, new object?[] { json });
        result.Should().BeOfType<List<object>>();
        ((List<object>)result!).Count.Should().Be(1);
    }

    [Fact]
    public void ConvertJsonElement_False_Parses_To_Boolean()
    {
        var method = typeof(MappingProfile).GetMethod("ParseJsonContent", BindingFlags.NonPublic | BindingFlags.Static)!;
        var el = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("false");
        var result = method.Invoke(null, new object?[] { el });
        result.Should().Be(false);
    }

    [Fact]
    public void ConvertJsonElement_Null_Parses_To_Null()
    {
        var method = typeof(MappingProfile).GetMethod("ParseJsonContent", BindingFlags.NonPublic | BindingFlags.Static)!;
        var el = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("null");
        var result = method.Invoke(null, new object?[] { el });
        result.Should().BeNull();
    }
}
