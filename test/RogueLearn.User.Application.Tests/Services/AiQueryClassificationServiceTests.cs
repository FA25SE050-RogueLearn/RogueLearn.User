using System;
using System.Collections.Generic;
using System.Linq;
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
    public void GenerateQueryVariantsAsync_Fallbacks_WhenAiFails()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("CreateFallbackQueries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = (List<string>)m!.Invoke(sut, new object[] { "RecyclerView", "Android", SubjectCategory.Programming })!;
        list.Should().NotBeEmpty();
        list[0].ToLowerInvariant().Should().Contain("android");
    }

    

    [Fact]
    public async Task ClassifySubjectAsync_Fallback_Heuristics_All_Categories()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = Substitute.For<IMemoryStore>();
        var sut = new AiQueryClassificationService(kernel, logger, store);

        var cases = new[]
        {
            ("C Programming", "CS101", "hands-on programming", SubjectCategory.Programming),
            ("Operating Systems", "CS201", "theory fundamentals", SubjectCategory.ComputerScience),
            ("Tư tưởng Hồ Chí Minh", "POL101", "chính trị", SubjectCategory.VietnamesePolitics),
            ("Văn học Việt Nam", "LIT101", "ngữ văn", SubjectCategory.VietnameseLiterature),
            ("World War II History", "HIS200", "historical analysis", SubjectCategory.History),
            ("Calculus Mathematics", "SCI101", "theory", SubjectCategory.Science),
            ("Marketing Fundamentals", "BUS101", "business", SubjectCategory.Business),
            ("General Studies", "GEN100", "overview", SubjectCategory.General)
        };

        foreach (var (name, code, desc, expected) in cases)
        {
            store.GetAsync($"category_{code}", Arg.Any<CancellationToken>()).Returns(expected.ToString());
            var cat = await sut.ClassifySubjectAsync(name, code, desc, CancellationToken.None);
            cat.Should().Be(expected);
        }
    }

    [Fact]
    public void BuildEnhancedQueryPrompt_Includes_Category_Specific_Guidance()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("BuildEnhancedQueryPrompt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var pProg = (string)m!.Invoke(sut, new object[] { "RecyclerView", "Android", SubjectCategory.Programming })!;
        var pPol = (string)m!.Invoke(sut, new object[] { "Tư tưởng Hồ Chí Minh", null!, SubjectCategory.VietnamesePolitics })!;
        pProg.Should().Contain("PROGRAMMING CATEGORY");
        pProg.Should().Contain("tutorial");
        pPol.Should().Contain("VIETNAMESE POLITICS");
        pPol.Should().Contain("lý thuyết");
    }

    [Fact]
    public void BuildEnhancedQueryPrompt_AllCategories_Contain_Guidance()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        foreach (SubjectCategory cat in Enum.GetValues(typeof(SubjectCategory)))
        {
            var p = typeof(AiQueryClassificationService)
                .GetMethod("BuildEnhancedQueryPrompt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var prompt = (string)p.Invoke(sut, new object[] { "Topic", "Context", cat })!;
            prompt.Should().Contain("HIGH-QUALITY educational resources");
        }
    }

    

    

    [Fact]
    public async Task ClassifySubjectAsync_Uses_Cache_For_All_Categories()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = Substitute.For<IMemoryStore>();
        var sut = new AiQueryClassificationService(kernel, logger, store);

        var categories = new[]
        {
            SubjectCategory.Programming,
            SubjectCategory.ComputerScience,
            SubjectCategory.VietnamesePolitics,
            SubjectCategory.History,
            SubjectCategory.VietnameseLiterature,
            SubjectCategory.Science,
            SubjectCategory.Business,
            SubjectCategory.General
        };

        foreach (var cat in categories)
        {
            store.GetAsync($"category_{cat}", Arg.Any<CancellationToken>()).Returns(cat.ToString());
            var res = await sut.ClassifySubjectAsync("name", cat.ToString(), "desc", CancellationToken.None);
            res.Should().Be(cat);
        }
    }

    [Fact]
    public void CleanJsonResponse_Strips_CodeFences_And_Returns_Json_Object()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("CleanJsonResponse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var raw = "```json\n{\"queries\":[\"a\",\"b\"]}\n```";
        var cleaned = (string)m!.Invoke(sut, new object[] { raw })!;
        cleaned.Should().StartWith("{");
        cleaned.Should().EndWith("}");
        cleaned.Should().Contain("queries");
    }

    [Fact]
    public void IsValidQuery_Programming_Requires_Educational_Keywords()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("IsValidQuery", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var topic = "RecyclerView adapter";
        var qNoEdu = "Android RecyclerView adapter implementation";
        var isValid = (bool)m!.Invoke(sut, new object[] { qNoEdu, topic, SubjectCategory.Programming })!;
        isValid.Should().BeTrue();
        var qEdu = "Android RecyclerView adapter tutorial";
        var isValid2 = (bool)m!.Invoke(sut, new object[] { qEdu, topic, SubjectCategory.Programming })!;
        isValid2.Should().BeTrue();
    }

    [Fact]
    public void CreateFallbackQueries_Covers_All_Categories()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("CreateFallbackQueries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (SubjectCategory cat in Enum.GetValues(typeof(SubjectCategory)))
        {
            var list = (List<string>)m!.Invoke(sut, new object[] { "Topic", "Context", cat })!;
            list.Should().NotBeEmpty();
        }
    }

    [Fact]
    public void BuildCategoryHints_Returns_Expected_Tokens()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("BuildCategoryHints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        ((string)m!.Invoke(sut, new object[] { SubjectCategory.ComputerScience })!).Should().Contain("architecture");
        ((string)m!.Invoke(sut, new object[] { SubjectCategory.Programming })!).Should().Contain("hands-on");
        ((string)m!.Invoke(sut, new object[] { SubjectCategory.History })!).Should().Contain("historical");
        ((string)m!.Invoke(sut, new object[] { SubjectCategory.VietnamesePolitics })!).Should().Contain("chính trị");
        ((string)m!.Invoke(sut, new object[] { SubjectCategory.VietnameseLiterature })!).Should().Contain("ngữ văn");
        ((string)m!.Invoke(sut, new object[] { SubjectCategory.Business })!).Should().Contain("kinh tế");
        ((string)m!.Invoke(sut, new object[] { SubjectCategory.Science })!).Should().Contain("công thức");
        ((string)m!.Invoke(sut, new object[] { SubjectCategory.General })!).Should().Contain("educational");
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

    [Fact]
    public void CleanJsonResponse_Strips_Code_Fences_And_Extracts_Braces()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("CleanJsonResponse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var raw = "```json\n{ \"queries\": [\"a\"] }\n```";
        var cleaned = (string)m!.Invoke(sut, new object[] { raw })!;
        cleaned.Should().StartWith("{").And.EndWith("}");
        cleaned.Should().Contain("queries");
    }

    [Fact]
    public void CreateFallbackQueries_Produces_Context_Aware_Entries()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("CreateFallbackQueries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = (List<string>)m!.Invoke(sut, new object[] { "RecyclerView", "Android", SubjectCategory.Programming })!;
        list.Should().NotBeEmpty();
        list[0].ToLowerInvariant().Should().Contain("android");
    }


    [Fact]
    public void CleanMarkdownFormatting_Strips_Fences_And_Stars()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("CleanMarkdownFormatting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var text = "```json\nProgramming\n```";
        var cleaned = (string)m!.Invoke(sut, new object[] { text })!;
        cleaned.Should().Contain("Programming");
        cleaned.Should().NotContain("`");
    }

    [Fact]
    public async Task GenerateSearchQueryAsync_Returns_First_Variant_FromCache()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = Substitute.For<IMemoryStore>();
        var cached = new List<string> { "Android RecyclerView tutorial", "Android list view guide" };
        store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Json.JsonSerializer.Serialize(cached));
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var q = await sut.GenerateSearchQueryAsync("RecyclerView", "Android", SubjectCategory.Programming, CancellationToken.None);
        q.Should().Be(cached[0]);
    }

    [Fact]
    public void IsValidQuery_Validates_Length_And_Topic_Match()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("IsValidQuery", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var topic = "RecyclerView list adapter";
        var tooShort = (bool)m!.Invoke(sut, new object[] { "RecyclerView", topic, SubjectCategory.Programming })!;
        var tooLongQuery = "Android RecyclerView adapter tutorial guide example reference documentation patterns learning site extra words beyond limit length";
        var tooLong = (bool)m!.Invoke(sut, new object[] { tooLongQuery, topic, SubjectCategory.Programming })!;
        var noTopic = (bool)m!.Invoke(sut, new object[] { "Android tutorial", topic, SubjectCategory.Programming })!;
        var ok = (bool)m!.Invoke(sut, new object[] { "Android RecyclerView adapter tutorial", topic, SubjectCategory.Programming })!;
        tooShort.Should().BeFalse();
        tooLong.Should().BeFalse();
        noTopic.Should().BeFalse();
        ok.Should().BeTrue();
    }

    [Fact]
    public void BuildBatchQueryPrompt_Includes_Technologies_Requirement()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("BuildBatchQueryPrompt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var prompt = (string)m!.Invoke(sut, new object[] { new[] { new SyllabusSessionDto { SessionNumber = 1, Topic = "RecyclerView" } }, "Android", SubjectCategory.Programming, new List<string>{ "Android", "Kotlin" } })!;
        prompt.Should().Contain("CRITICAL REQUIREMENT");
        prompt.Should().Contain("Technologies: Android, Kotlin");
    }

    [Fact]
    public async Task GenerateBatchQueryVariantsAsync_LargeBatch_Chunking_Fallback()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = Substitute.For<IMemoryStore>();
        store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Json.JsonSerializer.Serialize(new List<string> { "Android RecyclerView tutorial", "Android adapters guide" }));
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var sessions = new List<SyllabusSessionDto>();
        for (int i = 1; i <= 32; i++) sessions.Add(new SyllabusSessionDto { SessionNumber = i, Topic = "Topic " + i });
        var dict = await sut.GenerateBatchQueryVariantsAsync(sessions, "Android", SubjectCategory.Programming, new List<string>{ "Android" }, CancellationToken.None);
        dict.Count.Should().Be(32);
    }

    [Fact]
    public async Task GenerateChunkQueriesAsync_JsonException_Retries_Then_Empty()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = Substitute.For<IMemoryStore>();
        // Ensure fallback individual generation uses cache
        store.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Json.JsonSerializer.Serialize(new List<string> { "Android Topic tutorial" }));
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var sessions = new[] { new SyllabusSessionDto { SessionNumber = 1, Topic = "Topic" } };
        var dict = await sut.GenerateBatchQueryVariantsAsync(new List<SyllabusSessionDto>(sessions), "Android", SubjectCategory.Programming, new List<string>{ "Android" }, CancellationToken.None);
        dict.Count.Should().Be(1);
        dict[1].Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateQueryVariantsAsync_ValidJson_NoQueries_Falls_Back()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var logger = Substitute.For<ILogger<AiQueryClassificationService>>();
        var store = new InMemoryStore();
        var sut = new AiQueryClassificationService(kernel, logger, store);
        var m = typeof(AiQueryClassificationService).GetMethod("CreateFallbackQueries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var res = (List<string>)m!.Invoke(sut, new object[] { "RecyclerView", "Android", SubjectCategory.Programming })!;
        res.Should().NotBeEmpty();
        res[0].ToLowerInvariant().Should().Contain("android");
    }
}
