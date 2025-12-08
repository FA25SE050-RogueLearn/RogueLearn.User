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
        res.Message.Should().Be("Curriculum imported successfully");
        res.SubjectIds.Should().HaveCount(1);
    }
}