using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Plugins;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumImport.Commands.ImportCurriculum;

public class ImportCurriculumCommandHandlerTests
{
    private static ImportCurriculumCommand CreateCmd() => new ImportCurriculumCommand { RawText = "<p>Program</p>", CreatedBy = Guid.NewGuid() };

    [Fact]
    public async Task Handle_CleaningFailed_Throws()
    {
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var progSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var validator = new CurriculumImportDataValidator();
        var logger = Substitute.For<ILogger<ImportCurriculumCommandHandler>>();
        var plugin = Substitute.For<ICurriculumExtractionPlugin>();
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(string.Empty);

        var sut = new ImportCurriculumCommandHandler(programRepo, subjectRepo, progSubjRepo, storage, validator, logger, plugin, html);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(CreateCmd(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NullCurriculumData_Throws_NoDataExtracted()
    {
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var progSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var validator = new CurriculumImportDataValidator();
        var logger = Substitute.For<ILogger<ImportCurriculumCommandHandler>>();
        var plugin = Substitute.For<ICurriculumExtractionPlugin>();
        var html = Substitute.For<IHtmlCleaningService>();

        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("clean");
        plugin.ExtractCurriculumJsonAsync("clean", Arg.Any<CancellationToken>()).Returns("null");

        var sut = new ImportCurriculumCommandHandler(programRepo, subjectRepo, progSubjRepo, storage, validator, logger, plugin, html);
        var act = () => sut.Handle(CreateCmd(), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>()
            .WithMessage("*No curriculum data was extracted*");
    }

    [Fact]
    public async Task Handle_ValidationErrors_ThrowsValidationException()
    {
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var progSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var validator = new CurriculumImportDataValidator();
        var logger = Substitute.For<ILogger<ImportCurriculumCommandHandler>>();
        var plugin = Substitute.For<ICurriculumExtractionPlugin>();
        var html = Substitute.For<IHtmlCleaningService>();

        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("clean");
        // Missing required fields to trigger validation errors: empty subjects and structure
        var invalid = new CurriculumImportData
        {
            Program = new CurriculumProgramData { ProgramName = "", ProgramCode = "" },
            Version = new CurriculumVersionData { VersionCode = "", EffectiveYear = 1999 },
            Subjects = new List<SubjectData>(),
            Structure = new List<CurriculumStructureData>()
        };
        var json = JsonSerializer.Serialize(invalid);
        plugin.ExtractCurriculumJsonAsync("clean", Arg.Any<CancellationToken>()).Returns(json);

        var sut = new ImportCurriculumCommandHandler(programRepo, subjectRepo, progSubjRepo, storage, validator, logger, plugin, html);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.ValidationException>(() => sut.Handle(CreateCmd(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingProgramAndSubject_UpdatesAndLinksSkipIfExists()
    {
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var progSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var validator = new CurriculumImportDataValidator();
        var logger = Substitute.For<ILogger<ImportCurriculumCommandHandler>>();
        var plugin = Substitute.For<ICurriculumExtractionPlugin>();
        var html = Substitute.For<IHtmlCleaningService>();

        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("clean");
        var data = new CurriculumImportData
        {
            Program = new CurriculumProgramData { ProgramName = "Prog", ProgramCode = "PC", DegreeLevel = DegreeLevel.Master, TotalCredits = 60, DurationYears = 2, Description = "D" },
            Version = new CurriculumVersionData { VersionCode = "v1", EffectiveYear = DateTime.Now.Year },
            Subjects = new List<SubjectData> {
                new SubjectData { SubjectCode = "SC1", SubjectName = "SN1", Credits = 3, Description = "sdesc" },
                new SubjectData { SubjectCode = "SC2", SubjectName = "SN2", Credits = 4 }
            },
            Structure = new List<CurriculumStructureData> {
                new CurriculumStructureData { SubjectCode = "SC1", TermNumber = 1, IsMandatory = true, PrerequisiteSubjectCodes = new List<string> { "SC2" } },
                new CurriculumStructureData { SubjectCode = "SC2", TermNumber = 2, IsMandatory = false }
            }
        };
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
        plugin.ExtractCurriculumJsonAsync("clean", Arg.Any<CancellationToken>()).Returns(json);

        var existingProgram = new CurriculumProgram { Id = Guid.NewGuid(), ProgramCode = "PC" };
        programRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgram, bool>>>(), Arg.Any<CancellationToken>()).Returns(existingProgram);
        programRepo.UpdateAsync(Arg.Any<CurriculumProgram>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<CurriculumProgram>());

        var subject1 = new Subject { Id = Guid.NewGuid(), SubjectCode = "SC1" };
        var subject2 = new Subject { Id = Guid.NewGuid(), SubjectCode = "SC2" };
        subjectRepo.FirstOrDefaultAsync(Arg.Is<System.Linq.Expressions.Expression<Func<Subject, bool>>>(expr => expr.Compile()(subject1)), Arg.Any<CancellationToken>()).Returns(subject1);
        subjectRepo.FirstOrDefaultAsync(Arg.Is<System.Linq.Expressions.Expression<Func<Subject, bool>>>(expr => expr.Compile()(subject2)), Arg.Any<CancellationToken>()).Returns(subject2);
        subjectRepo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());

        progSubjRepo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);

        var sut = new ImportCurriculumCommandHandler(programRepo, subjectRepo, progSubjRepo, storage, validator, logger, plugin, html);
        var res = await sut.Handle(CreateCmd(), CancellationToken.None);

        res.IsSuccess.Should().BeTrue();
        await programRepo.Received(1).UpdateAsync(Arg.Is<CurriculumProgram>(p => p.ProgramName == "Prog" && p.TotalCredits == 60), Arg.Any<CancellationToken>());
        await subjectRepo.Received().UpdateAsync(Arg.Is<Subject>(s => s.SubjectCode == "SC1" && s.Semester == 1), Arg.Any<CancellationToken>());
        await subjectRepo.Received().UpdateAsync(Arg.Is<Subject>(s => s.SubjectCode == "SC2" && s.Semester == 2), Arg.Any<CancellationToken>());
        await progSubjRepo.DidNotReceive().AddAsync(Arg.Any<CurriculumProgramSubject>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_ExtractionEmpty_Throws()
    {
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var progSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var validator = new CurriculumImportDataValidator();
        var logger = Substitute.For<ILogger<ImportCurriculumCommandHandler>>();
        var plugin = Substitute.For<ICurriculumExtractionPlugin>();
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("clean");
        plugin.ExtractCurriculumJsonAsync("clean", Arg.Any<CancellationToken>()).Returns(string.Empty);

        var sut = new ImportCurriculumCommandHandler(programRepo, subjectRepo, progSubjRepo, storage, validator, logger, plugin, html);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(CreateCmd(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidJson_Throws()
    {
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var progSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var validator = new CurriculumImportDataValidator();
        var logger = Substitute.For<ILogger<ImportCurriculumCommandHandler>>();
        var plugin = Substitute.For<ICurriculumExtractionPlugin>();
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("clean");
        plugin.ExtractCurriculumJsonAsync("clean", Arg.Any<CancellationToken>()).Returns("{not-json}");

        var sut = new ImportCurriculumCommandHandler(programRepo, subjectRepo, progSubjRepo, storage, validator, logger, plugin, html);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(CreateCmd(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_PersistsProgramSubjectsAndMappings()
    {
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var progSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var validator = new CurriculumImportDataValidator();
        var logger = Substitute.For<ILogger<ImportCurriculumCommandHandler>>();
        var plugin = Substitute.For<ICurriculumExtractionPlugin>();
        var html = Substitute.For<IHtmlCleaningService>();

        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("clean");
        var data = new CurriculumImportData
        {
            Program = new CurriculumProgramData { ProgramName = "Prog", ProgramCode = "PC", DegreeLevel = DegreeLevel.Bachelor, TotalCredits = 120, DurationYears = 4 },
            Version = new CurriculumVersionData { VersionCode = "v1", EffectiveYear = DateTime.Now.Year },
            Subjects = new List<SubjectData> { new SubjectData { SubjectCode = "SC1", SubjectName = "SN1", Credits = 3 } },
            Structure = new List<CurriculumStructureData> { new CurriculumStructureData { SubjectCode = "SC1", TermNumber = 1, IsMandatory = true } }
        };
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
        plugin.ExtractCurriculumJsonAsync("clean", Arg.Any<CancellationToken>()).Returns(json);
        programRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgram, bool>>>(), Arg.Any<CancellationToken>()).Returns((CurriculumProgram?)null);
        programRepo.AddAsync(Arg.Any<CurriculumProgram>(), Arg.Any<CancellationToken>()).Returns(ci => { var p = ci.Arg<CurriculumProgram>(); p.Id = Guid.NewGuid(); return p; });
        subjectRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Subject, bool>>>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
        subjectRepo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => { var s = ci.Arg<Subject>(); s.Id = Guid.NewGuid(); return s; });
        progSubjRepo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);
        progSubjRepo.AddAsync(Arg.Any<CurriculumProgramSubject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<CurriculumProgramSubject>());

        var sut = new ImportCurriculumCommandHandler(programRepo, subjectRepo, progSubjRepo, storage, validator, logger, plugin, html);
        var res = await sut.Handle(CreateCmd(), CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
        res.Message.Should().Contain("successfully");
        res.SubjectIds.Should().HaveCount(1);
    }
}
