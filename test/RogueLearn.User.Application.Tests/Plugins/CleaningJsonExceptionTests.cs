using System;
using System.Reflection;
using FluentAssertions;

namespace RogueLearn.User.Application.Tests.Plugins;

public class CleaningJsonExceptionTests
{
    [Fact]
    public void Can_Create_CleaningJsonException_Via_Reflection()
    {
        var type = Type.GetType("RogueLearn.User.Application.Plugins.CleaningJsonException, RogueLearn.User.Application");
        type.Should().NotBeNull();

        var ctor = type!.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(string), typeof(Exception) }, null);
        ctor.Should().NotBeNull();

        var ex = new Exception("inner");
        var inst = ctor!.Invoke(new object[] { "msg", "clean", ex });
        inst.Should().NotBeNull();

        var prop = type.GetProperty("CleanedContent", BindingFlags.Public | BindingFlags.Instance);
        prop.Should().NotBeNull();
        var val = prop!.GetValue(inst) as string;
        val.Should().Be("clean");
    }
}