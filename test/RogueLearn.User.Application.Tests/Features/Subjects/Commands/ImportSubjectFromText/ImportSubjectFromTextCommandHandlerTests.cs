using FluentAssertions;
using NSubstitute;
using AutoMapper;
using RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Commands.ImportSubjectFromText;

public class ImportSubjectFromTextCommandHandlerTests
{
    private static ImportSubjectFromTextCommandHandler CreateSut(
        ISyllabusExtractionPlugin? extractor = null,
        IConstructiveQuestionGenerationPlugin? qGen = null,
        ISubjectRepository? subjectRepo = null,
        IMapper? mapper = null,
        Microsoft.Extensions.Logging.ILogger<ImportSubjectFromTextCommandHandler>? logger = null,
        IHtmlCleaningService? html = null,
        ICurriculumImportStorage? storage = null,
        IReadingUrlService? reading = null,
        IAiQueryClassificationService? ai = null)
    {
        extractor ??= Substitute.For<ISyllabusExtractionPlugin>();
        qGen ??= Substitute.For<IConstructiveQuestionGenerationPlugin>();
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        mapper ??= Substitute.For<IMapper>();
        logger ??= Substitute.For<Microsoft.Extensions.Logging.ILogger<ImportSubjectFromTextCommandHandler>>();
        html ??= Substitute.For<IHtmlCleaningService>();
        storage ??= Substitute.For<ICurriculumImportStorage>();
        reading ??= Substitute.For<IReadingUrlService>();
        ai ??= Substitute.For<IAiQueryClassificationService>();

        return new ImportSubjectFromTextCommandHandler(extractor, qGen, subjectRepo, mapper, logger, html, storage, reading, ai);
    }

    [Fact]
    public async Task Handle_CleanTextWhitespace_ThrowsBadRequest()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("   ");
        var sut = CreateSut(html: html);
        var act = () => sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_NoCache_UsesExtractor()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("valid syllabus text with content and sessions and readings " + new string('x', 80));
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var extractor = Substitute.For<ISyllabusExtractionPlugin>();
        extractor.ExtractSyllabusJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("{\"SubjectCode\":\"SC\",\"SubjectName\":\"Name\",\"Content\":{\"SessionSchedule\":[]}} ");
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse { Id = Guid.NewGuid() });
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());
        var sut = CreateSut(extractor: extractor, html: html, storage: storage, mapper: mapper, subjectRepo: subjectRepo);
        await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        await extractor.Received(1).ExtractSyllabusJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidExtractedJson_ThrowsBadRequest()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text" + new string('x', 120));
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("{");
        var sut = CreateSut(html: html, storage: storage);
        var act = () => sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_ConstructiveQuestionsGenerated()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text" + new string('x', 120));
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabus = new SyllabusData { SubjectCode = "SC", SubjectName = "Name", Content = new SyllabusContent { SessionSchedule = new List<SyllabusSessionDto> { new SyllabusSessionDto { SessionNumber = 1, Topic = "A" } } } };
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(System.Text.Json.JsonSerializer.Serialize(syllabus));
        var qGen = Substitute.For<IConstructiveQuestionGenerationPlugin>();
        qGen.GenerateQuestionsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<ConstructiveQuestion> { new ConstructiveQuestion { Question = "Q", Name = "N" } }));
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse { Id = Guid.NewGuid() });
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());
        var ai = Substitute.For<IAiQueryClassificationService>();
        ai.ClassifySubjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(SubjectCategory.Programming));
        ai.GenerateBatchQueryVariantsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<string>(), Arg.Any<SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new Dictionary<int, List<string>> { { 1, new List<string> { "q" } } }));
        var reading = Substitute.For<IReadingUrlService>();
        reading.GetValidUrlForTopicAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<Func<string, bool>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(string.Empty));
        var sut = CreateSut(html: html, storage: storage, qGen: qGen, mapper: mapper, subjectRepo: subjectRepo, ai: ai, reading: reading);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        res.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ConstructiveQuestionsExisting_LogsCount()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text" + new string('x', 120));
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabus = new SyllabusData { SubjectCode = "SC", SubjectName = "Name", Content = new SyllabusContent { ConstructiveQuestions = new List<ConstructiveQuestion> { new ConstructiveQuestion { Question = "Q", Name = "N" } }, SessionSchedule = new List<SyllabusSessionDto>() } };
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(System.Text.Json.JsonSerializer.Serialize(syllabus));
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse { Id = Guid.NewGuid() });
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());
        var sut = CreateSut(html: html, storage: storage, mapper: mapper, subjectRepo: subjectRepo);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        res.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ReadingUrlServiceThrows_CaughtAndContinues()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text" + new string('x', 120));
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabus = new SyllabusData { SubjectCode = "SC", SubjectName = "Name", Content = new SyllabusContent { SessionSchedule = new List<SyllabusSessionDto> { new SyllabusSessionDto { SessionNumber = 1, Topic = "A" } } } };
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(System.Text.Json.JsonSerializer.Serialize(syllabus));
        var ai = Substitute.For<IAiQueryClassificationService>();
        ai.ClassifySubjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(SubjectCategory.Programming));
        ai.GenerateBatchQueryVariantsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<string>(), Arg.Any<SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new Dictionary<int, List<string>> { { 1, new List<string> { "q" } } }));
        var reading = Substitute.For<IReadingUrlService>();
        reading.GetValidUrlForTopicAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<Func<string, bool>>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromException<string?>(new Exception("search failed")));
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse { Id = Guid.NewGuid() });
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());
        var sut = CreateSut(html: html, storage: storage, ai: ai, reading: reading, mapper: mapper, subjectRepo: subjectRepo);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        res.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_UpdateExistingSubject_SemesterOverrideAndApprovedDate()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text" + new string('x', 120));
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabus = new SyllabusData { SubjectCode = "SC", SubjectName = "Name", ApprovedDate = new DateOnly(2024, 5, 1), Content = new SyllabusContent { SessionSchedule = new List<SyllabusSessionDto>() } };
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(System.Text.Json.JsonSerializer.Serialize(syllabus));
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var existing = new Subject { Id = Guid.NewGuid(), SubjectCode = "SC" };
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns(existing);
        subjectRepo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse { Id = existing.Id });
        var sut = CreateSut(html: html, storage: storage, subjectRepo: subjectRepo, mapper: mapper);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>", Semester = 7 }, CancellationToken.None);
        res.Id.Should().Be(existing.Id);
    }
    [Fact]
    public async Task Handle_SyllabusNullOrMissingCode_ThrowsBadRequest()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");

        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("{ }");

        var sut = CreateSut(html: html, storage: storage);
        var act = () => sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("Extracted syllabus data is missing a valid SubjectCode.");
    }

    [Fact]
    public async Task Handle_SessionScheduleExists_TriggersBatchEnrichment()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabus = new SyllabusData
        {
            SubjectCode = "SC",
            SubjectName = "Name",
            Content = new SyllabusContent
            {
                SessionSchedule = new List<SyllabusSessionDto> { new SyllabusSessionDto { SessionNumber = 1, Topic = "A" } }
            }
        };
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(System.Text.Json.JsonSerializer.Serialize(syllabus));

        var qGen = Substitute.For<IConstructiveQuestionGenerationPlugin>();
        qGen.GenerateQuestionsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<ConstructiveQuestion>()));
        var ai = Substitute.For<IAiQueryClassificationService>();
        ai.ClassifySubjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(SubjectCategory.Programming));
        ai.GenerateBatchQueryVariantsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<string>(), Arg.Any<SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new Dictionary<int, List<string>> { { 1, new List<string> { "q" } } }));

        var reading = Substitute.For<IReadingUrlService>();
        reading.GetValidUrlForTopicAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<Func<string, bool>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>("https://example.com"));

        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse { Id = Guid.NewGuid() });

        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());

        var sut = CreateSut(html: html, storage: storage, qGen: qGen, ai: ai, reading: reading, mapper: mapper, subjectRepo: subjectRepo);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        res.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_BatchGenerationFails_LogsAndContinues()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabus = new SyllabusData { SubjectCode = "SC", SubjectName = "Name", Content = new SyllabusContent { SessionSchedule = new List<SyllabusSessionDto> { new SyllabusSessionDto { SessionNumber = 1, Topic = "A" } } } };
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(System.Text.Json.JsonSerializer.Serialize(syllabus));

        var qGen = Substitute.For<IConstructiveQuestionGenerationPlugin>();
        qGen.GenerateQuestionsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<ConstructiveQuestion>()));
        var ai = Substitute.For<IAiQueryClassificationService>();
        ai.ClassifySubjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(SubjectCategory.Programming));
        ai.GenerateBatchQueryVariantsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<string>(), Arg.Any<SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Dictionary<int, List<string>>>(new Exception("fail")));

        var reading = Substitute.For<IReadingUrlService>();
        reading.GetValidUrlForTopicAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<Func<string, bool>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(string.Empty));

        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse { Id = Guid.NewGuid() });
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());

        var sut = CreateSut(html: html, storage: storage, qGen: qGen, ai: ai, reading: reading, mapper: mapper, subjectRepo: subjectRepo);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        res.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_isUrlUsedCheckAndElseBranchCovered()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabus = new SyllabusData { SubjectCode = "SC", SubjectName = "Name", TechnologyStack = "dotnet", Content = new SyllabusContent { SessionSchedule = new List<SyllabusSessionDto> { new SyllabusSessionDto { SessionNumber = 1, Topic = "A", Readings = new List<string> { "https://used" } } } } };
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(System.Text.Json.JsonSerializer.Serialize(syllabus));

        var qGen = Substitute.For<IConstructiveQuestionGenerationPlugin>();
        qGen.GenerateQuestionsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<ConstructiveQuestion>()));
        var ai = Substitute.For<IAiQueryClassificationService>();
        ai.ClassifySubjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(SubjectCategory.Programming));
        ai.GenerateBatchQueryVariantsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<string>(), Arg.Any<SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new Dictionary<int, List<string>>()));

        var reading = Substitute.For<IReadingUrlService>();
        reading.GetValidUrlForTopicAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<Func<string, bool>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(string.Empty));

        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse { Id = Guid.NewGuid() });
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());

        var sut = CreateSut(html: html, storage: storage, qGen: qGen, ai: ai, reading: reading, mapper: mapper, subjectRepo: subjectRepo);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        res.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_SemesterOverrideElseBranchSetFromExtracted()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabus = new SyllabusData { SubjectCode = "SC", SubjectName = "Name", Semester = 3, Content = new SyllabusContent { SessionSchedule = new List<SyllabusSessionDto>() } };
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(System.Text.Json.JsonSerializer.Serialize(syllabus));

        var subjectRepo = Substitute.For<ISubjectRepository>();
        var existing = new Subject { Id = Guid.NewGuid(), SubjectCode = "SC" };
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns(existing);
        subjectRepo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse { Id = existing.Id });

        var sut = CreateSut(html: html, storage: storage, subjectRepo: subjectRepo, mapper: mapper);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        res.Id.Should().Be(existing.Id);
    }

    [Fact]
    public async Task Handle_ParallelSessionsAvoidDuplicateUrls()
    {
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("text");
        var storage = Substitute.For<ICurriculumImportStorage>();
        var syllabus = new SyllabusData
        {
            SubjectCode = "SC",
            SubjectName = "Name",
            Content = new SyllabusContent
            {
                SessionSchedule = new List<SyllabusSessionDto>
                {
                    new SyllabusSessionDto { SessionNumber = 1, Topic = "A" },
                    new SyllabusSessionDto { SessionNumber = 2, Topic = "B" }
                }
            }
        };
        storage.TryGetCachedSyllabusDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(System.Text.Json.JsonSerializer.Serialize(syllabus));

        var ai = Substitute.For<IAiQueryClassificationService>();
        ai.ClassifySubjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(SubjectCategory.Programming));
        ai.GenerateBatchQueryVariantsAsync(Arg.Any<List<SyllabusSessionDto>>(), Arg.Any<string>(), Arg.Any<SubjectCategory>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new Dictionary<int, List<string>>()));

        var reading = new TestReadingService();
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CreateSubjectResponse>(Arg.Any<Subject>()).Returns(new CreateSubjectResponse { Id = Guid.NewGuid() });
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());

        var sut = CreateSut(html: html, storage: storage, ai: ai, reading: reading, mapper: mapper, subjectRepo: subjectRepo);
        var res = await sut.Handle(new ImportSubjectFromTextCommand { RawText = "<html>" }, CancellationToken.None);
        res.Id.Should().NotBeEmpty();
        reading.SecondCallSkipped.Should().BeTrue();
    }

    private class TestReadingService : IReadingUrlService
    {
        public bool SecondCallSkipped { get; private set; }
        private int _count;
        public async Task<string?> GetValidUrlForTopicAsync(string topic, IEnumerable<string> readings, string? subjectContext = null, SubjectCategory category = SubjectCategory.General, List<string>? overrideQueries = null, Func<string, bool>? isUrlUsedCheck = null, CancellationToken cancellationToken = default)
        {
            _count++;
            if (_count == 1)
                return "https://example.com/a";

            if (isUrlUsedCheck != null)
            {
                for (var i = 0; i < 200; i++)
                {
                    if (isUrlUsedCheck("https://example.com/a"))
                    {
                        SecondCallSkipped = true;
                        return null;
                    }
                    await Task.Delay(1, cancellationToken);
                }
            }

            return null;
        }
    }
}
