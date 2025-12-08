using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Common;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Commands.ImportSubjectFromText;

public class ImportSubjectFromTextCommandHandlerTests
{
    private static ImportSubjectFromTextCommandHandler CreateSut(
        ISyllabusExtractionPlugin? extract = null,
        IConstructiveQuestionGenerationPlugin? qgen = null,
        ISubjectRepository? subjectRepo = null,
        IMapper? mapper = null,
        ILogger<ImportSubjectFromTextCommandHandler>? logger = null,
        IHtmlCleaningService? html = null,
        ICurriculumImportStorage? storage = null,
        IReadingUrlService? reading = null,
        IAiQueryClassificationService? ai = null)
    {
        extract ??= Substitute.For<ISyllabusExtractionPlugin>();
        qgen ??= Substitute.For<IConstructiveQuestionGenerationPlugin>();
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        mapper ??= Substitute.For<IMapper>();
        logger ??= Substitute.For<ILogger<ImportSubjectFromTextCommandHandler>>();
        html ??= Substitute.For<IHtmlCleaningService>();
        storage ??= Substitute.For<ICurriculumImportStorage>();
        reading ??= Substitute.For<IReadingUrlService>();
        ai ??= Substitute.For<IAiQueryClassificationService>();
        return new ImportSubjectFromTextCommandHandler(extract, qgen, subjectRepo, mapper, logger, html, storage, reading, ai);
    }

    [Fact]
    public async Task Handle_EmptyExtractedText_ThrowsBadRequest()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(string.Empty);
        var sut = CreateSut(html: html);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html/>" }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UsesCache_When_Available()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabusJson = "{\"SubjectCode\":\"CS101\",\"SubjectName\":\"Intro\",\"Credits\":3,\"Content\":{}}";
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(syllabusJson);

        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => (Subject)ci[0]!);

        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse());

        var sut = CreateSut(html: html, storage: storage, subjectRepo: subjectRepo, mapper: mapper);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html/>" }, CancellationToken.None);
        res.Should().NotBeNull();
        await storage.Received(1).SaveSyllabusDataAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<SyllabusData>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Extracts_When_NoCache()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var extract = Substitute.For<ISyllabusExtractionPlugin>();
        extract.ExtractSyllabusJsonAsync("text", Arg.Any<CancellationToken>()).Returns("{\"SubjectCode\":\"CS102\",\"SubjectName\":\"Intro2\",\"Credits\":4,\"Content\":{}}");

        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => (Subject)ci[0]!);
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse());

        var sut = CreateSut(extract: extract, html: html, storage: storage, subjectRepo: subjectRepo, mapper: mapper);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html/>" }, CancellationToken.None);
        res.Should().NotBeNull();
        await extract.Received(1).ExtractSyllabusJsonAsync("text", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateExisting_Subject_Respects_SemesterOverride()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabusJson = "{\"SubjectCode\":\"CS201\",\"SubjectName\":\"Subj\",\"Credits\":3,\"Semester\":2,\"Content\":{}}";
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(syllabusJson);

        var existing = new Subject { Id = Guid.NewGuid(), SubjectCode = "CS201", Semester = 1 };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns(existing);
        subjectRepo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => (Subject)ci[0]!);
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse());

        var sut = CreateSut(html: html, storage: storage, subjectRepo: subjectRepo, mapper: mapper);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html/>", Semester = 5 }, CancellationToken.None);
        res.Should().NotBeNull();
        existing.Semester.Should().Be(5);
        await subjectRepo.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateExisting_UsesApprovedDateForUpdatedAt()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabusJson = "{\"SubjectCode\":\"CS202\",\"SubjectName\":\"Subj\",\"Credits\":3,\"ApprovedDate\":\"2024-05-01\",\"Content\":{}}";
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(syllabusJson);

        var existing = new Subject { Id = Guid.NewGuid(), SubjectCode = "CS202", UpdatedAt = DateTimeOffset.MinValue };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns(existing);
        subjectRepo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => (Subject)ci[0]!);
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse());

        var sut = CreateSut(html: html, storage: storage, subjectRepo: subjectRepo, mapper: mapper);
        _ = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html/>" }, CancellationToken.None);

        existing.UpdatedAt.Year.Should().Be(2024);
        existing.UpdatedAt.Month.Should().Be(5);
        existing.UpdatedAt.Day.Should().Be(1);
        await subjectRepo.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EnrichesUrls_WhenSessionsPresent()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabusJson = "{\"SubjectCode\":\"CS301\",\"SubjectName\":\"Subj\",\"Credits\":3,\"Content\":{\"SessionSchedule\":[{\"SessionNumber\":1,\"Topic\":\"Intro\"},{\"SessionNumber\":2,\"Topic\":\"Basics\"}],\"ConstructiveQuestions\":[{\"Name\":\"Q1\",\"Question\":\"?\"}]}}";
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(syllabusJson);

        var ai = Substitute.For<IAiQueryClassificationService>();
        ai.ClassifySubjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(RogueLearn.User.Application.Services.SubjectCategory.Programming);
        ai.GenerateBatchQueryVariantsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<string>(), Arg.Any<RogueLearn.User.Application.Services.SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new Dictionary<int, List<string>> { { 1, new List<string> { "q1" } }, { 2, new List<string> { "q2" } } });

        var reading = Substitute.For<IReadingUrlService>();
        reading.GetValidUrlForTopicAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<RogueLearn.User.Application.Services.SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<Func<string, bool>>(), Arg.Any<CancellationToken>())
               .Returns("http://example.com/a");

        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        Subject? captured = null;
        subjectRepo.AddAsync(Arg.Do<Subject>(s => captured = s), Arg.Any<CancellationToken>()).Returns(ci => captured!);

        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse());

        var sut = CreateSut(html: html, storage: storage, subjectRepo: subjectRepo, mapper: mapper, reading: reading, ai: ai);
        _ = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html/>" }, CancellationToken.None);

        await storage.Received(1).SaveSyllabusDataAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<SyllabusData>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await reading.Received(2).GetValidUrlForTopicAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<RogueLearn.User.Application.Services.SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<Func<string, bool>>(), Arg.Any<CancellationToken>());
        captured!.Content.Should().NotBeNull();
        captured!.Content!.ContainsKey("SessionSchedule").Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SkipsUrlSearch_WhenNoSessions()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabusJson = "{\"SubjectCode\":\"CS302\",\"SubjectName\":\"Subj\",\"Credits\":3,\"Content\":{\"SessionSchedule\":[],\"ConstructiveQuestions\":[]}}";
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(syllabusJson);

        var reading = Substitute.For<IReadingUrlService>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse());

        var sut = CreateSut(html: html, storage: storage, subjectRepo: subjectRepo, mapper: mapper, reading: reading);
        _ = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html/>" }, CancellationToken.None);

        await reading.DidNotReceive().GetValidUrlForTopicAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<RogueLearn.User.Application.Services.SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<Func<string, bool>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotGenerateQuestions_WhenExistingPresent()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabusJson = "{\"SubjectCode\":\"CS303\",\"SubjectName\":\"Subj\",\"Credits\":3,\"Content\":{\"SessionSchedule\":[],\"ConstructiveQuestions\":[{\"Name\":\"Q\",\"Question\":\"?\"}]}}";
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(syllabusJson);

        var qgen = Substitute.For<IConstructiveQuestionGenerationPlugin>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse());

        var sut = CreateSut(qgen: qgen, html: html, storage: storage, subjectRepo: subjectRepo, mapper: mapper);
        _ = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html/>" }, CancellationToken.None);

        await qgen.DidNotReceive().GenerateQuestionsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidJson_ThrowsBadRequest()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("not-json");
        var sut = CreateSut(html: html, storage: storage);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html/>" }, CancellationToken.None));
    }
}
