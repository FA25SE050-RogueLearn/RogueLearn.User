// RogueLearn.User/test/RogueLearn.User.Application.Tests/Features/CurriculumImport/Commands/ImportCurriculumCommandHandlerTests.cs
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;
using System.Linq.Expressions;
using System.Text.Json;

namespace RogueLearn.User.Application.Tests.Features.CurriculumImport.Commands;

public class ImportCurriculumCommandHandlerTests
{
    private readonly Mock<ICurriculumProgramRepository> _mockCurriculumProgramRepository;
    private readonly Mock<ISubjectRepository> _mockSubjectRepository;
    private readonly Mock<ICurriculumProgramSubjectRepository> _mockProgramSubjectRepository;
    private readonly Mock<ICurriculumImportStorage> _mockCurriculumImportStorage;
    private readonly Mock<CurriculumImportDataValidator> _mockValidator;
    private readonly Mock<ILogger<ImportCurriculumCommandHandler>> _mockLogger;
    private readonly Mock<IFlmExtractionPlugin> _mockFlmPlugin;
    private readonly Mock<IHtmlCleaningService> _mockHtmlCleaningService; // FIX: Mock for the new dependency
    private readonly ImportCurriculumCommandHandler _handler;


    public ImportCurriculumCommandHandlerTests()
    {
        _mockCurriculumProgramRepository = new Mock<ICurriculumProgramRepository>();
        _mockSubjectRepository = new Mock<ISubjectRepository>();
        _mockProgramSubjectRepository = new Mock<ICurriculumProgramSubjectRepository>();
        _mockCurriculumImportStorage = new Mock<ICurriculumImportStorage>();
        _mockValidator = new Mock<CurriculumImportDataValidator>();
        _mockLogger = new Mock<ILogger<ImportCurriculumCommandHandler>>();
        _mockFlmPlugin = new Mock<IFlmExtractionPlugin>();
        _mockHtmlCleaningService = new Mock<IHtmlCleaningService>(); // FIX: Initialize the new mock

        _handler = new ImportCurriculumCommandHandler(
             _mockCurriculumProgramRepository.Object,
             _mockSubjectRepository.Object,
             _mockProgramSubjectRepository.Object,
             _mockCurriculumImportStorage.Object,
             _mockValidator.Object,
             _mockLogger.Object,
             _mockFlmPlugin.Object,
             _mockHtmlCleaningService.Object); // FIX: Pass the new mocked dependency
    }

    private string GetValidExtractedJson()
    {
        var data = new
        {
            program = new { programName = "Test Program", programCode = "TEST", degreeLevel = "Bachelor" },
            version = new { versionCode = "2024", effectiveYear = 2024 },
            subjects = new[] { new { subjectCode = "SUB101", subjectName = "Test Subject", credits = 3 } },
            structure = new[] { new { subjectCode = "SUB101", termNumber = 1 } }
        };
        return JsonSerializer.Serialize(data);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new ImportCurriculumCommand
        {
            RawText = "<table><tr><td>SubjectCode</td><td>SUB101</td></tr></table>",
            CreatedBy = Guid.NewGuid()
        };

        // Mock the cleaning service to return some text
        _mockHtmlCleaningService.Setup(s => s.ExtractCleanTextFromHtml(It.IsAny<string>())).Returns("Cleaned text");

        _mockFlmPlugin.Setup(p => p.ExtractCurriculumJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GetValidExtractedJson());

        var mockValidationResult = new FluentValidation.Results.ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<CurriculumImportData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(mockValidationResult);

        _mockCurriculumProgramRepository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<CurriculumProgram, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurriculumProgram?)null);

        _mockCurriculumProgramRepository
            .Setup(r => r.AddAsync(It.IsAny<CurriculumProgram>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurriculumProgram { Id = Guid.NewGuid() });

        _mockSubjectRepository
            .Setup(r => r.AddAsync(It.IsAny<Subject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subject { Id = Guid.NewGuid() });

        _mockProgramSubjectRepository
            .Setup(r => r.AddAsync(It.IsAny<CurriculumProgramSubject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurriculumProgramSubject());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _mockCurriculumProgramRepository.Verify(x => x.AddAsync(It.IsAny<CurriculumProgram>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSubjectRepository.Verify(x => x.AddAsync(It.IsAny<Subject>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockProgramSubjectRepository.Verify(x => x.AddAsync(It.IsAny<CurriculumProgramSubject>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}