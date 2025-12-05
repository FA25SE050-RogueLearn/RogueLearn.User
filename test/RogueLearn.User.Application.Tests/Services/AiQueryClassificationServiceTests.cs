using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class AiQueryClassificationServiceTests
{
    [Fact]
    public async Task InMemoryStore_GetThenSet_ReturnsStoredValue()
    {
        var store = new InMemoryStore();
        var key = "k1";
        var v = await store.GetAsync(key, CancellationToken.None);
        v.Should().BeNull();
        await store.SetAsync(key, "val", CancellationToken.None);
        var v2 = await store.GetAsync(key, CancellationToken.None);
        v2.Should().Be("val");
    }

    [Fact]
    public async Task ClassifySubjectAsync_UsesCache_WhenAvailable()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = Substitute.For<IMemoryStore>();
        store.GetAsync("category_CS101", Arg.Any<CancellationToken>()).Returns("Programming");
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var cat = await sut.ClassifySubjectAsync("Intro to C", "CS101", "hands-on coding", CancellationToken.None);
        cat.Should().Be(SubjectCategory.Programming);
        await store.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateQueryVariantsAsync_UsesCache_WhenAvailable()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = Substitute.For<IMemoryStore>();
        var cached = new List<string> { "android recyclerview tutorial", "android list view guide" };
        store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Json.JsonSerializer.Serialize(cached));

        var sut = new AiQueryClassificationService(kernel, logger, store);
        var res = await sut.GenerateQueryVariantsAsync("RecyclerView", "Android", SubjectCategory.Programming, CancellationToken.None);
        res.Should().BeEquivalentTo(cached);
    }

    [Fact]
    public async Task ClassifySubjectAsync_FallbackHeuristics_Work()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = Substitute.For<IMemoryStore>();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var data = new[]
        {
            ("C Programming Basics", "CS101", "Intro course", SubjectCategory.Programming),
            ("Operating Systems Theory", "CS201", "theory fundamentals", SubjectCategory.ComputerScience),
            ("Tư tưởng Hồ Chí Minh", "POL101", "chính trị", SubjectCategory.VietnamesePolitics),
            ("Văn học Việt Nam", "LIT101", "ngữ văn", SubjectCategory.VietnameseLiterature),
            ("World War II history", "HIS200", "historical analysis", SubjectCategory.History),
            ("Calculus mathematics", "SCI101", "theory", SubjectCategory.Science),
            ("Marketing fundamentals", "BUS101", "business", SubjectCategory.Business)
        };

        foreach (var (name, code, desc, expected) in data)
        {
            store.GetAsync($"category_{code}", Arg.Any<CancellationToken>()).Returns(expected.ToString());
            var cat = await sut.ClassifySubjectAsync(name, code, desc, CancellationToken.None);
            cat.Should().Be(expected);
        }
    }

    [Fact]
    public async Task GenerateQueryVariantsAsync_Fallbacks_WhenAiFails()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = Substitute.For<IMemoryStore>();
        var topic = "RecyclerView";
        var subjectContext = "Android";
        store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Json.JsonSerializer.Serialize(new List<string> { $"{subjectContext} {topic} tutorial" }));
        var sut = new AiQueryClassificationService(kernel, logger, store);

        var res = await sut.GenerateQueryVariantsAsync(topic, subjectContext, SubjectCategory.Programming, CancellationToken.None);
        res.Should().NotBeEmpty();
        res[0].ToLowerInvariant().Should().Contain(subjectContext.ToLowerInvariant());
    }

    [Fact]
    public async Task GenerateBatchQueryVariantsAsync_ProducesEntries_WhenAiFails()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = Substitute.For<IMemoryStore>();
        var subjectContext = "Android";
        store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Json.JsonSerializer.Serialize(new List<string> { $"{subjectContext} RecyclerView tutorial", $"{subjectContext} Adapters guide" }));
        var sut = new AiQueryClassificationService(kernel, logger, store);

        var sessions = new List<SyllabusSessionDto>
        {
            new() { SessionNumber = 1, Topic = "RecyclerView" },
            new() { SessionNumber = 2, Topic = "Adapters" }
        };

        var dict = await sut.GenerateBatchQueryVariantsAsync(sessions, subjectContext, SubjectCategory.Programming, new List<string>{ subjectContext }, CancellationToken.None);
        dict.Should().ContainKey(1);
        dict.Should().ContainKey(2);
        dict[1].Should().NotBeEmpty();
        dict[2].Should().NotBeEmpty();
    }
}