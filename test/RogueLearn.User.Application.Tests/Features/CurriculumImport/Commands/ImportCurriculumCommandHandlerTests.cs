using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;
using System.Linq.Expressions;

namespace RogueLearn.User.Application.Tests.Features.CurriculumImport.Commands;

public class ImportCurriculumCommandHandlerTests
{
    private readonly Mock<Kernel> _mockKernel;
    private readonly Mock<ICurriculumProgramRepository> _mockCurriculumProgramRepository;
    private readonly Mock<ICurriculumVersionRepository> _mockCurriculumVersionRepository;
    private readonly Mock<ISubjectRepository> _mockSubjectRepository;
    private readonly Mock<ICurriculumStructureRepository> _mockCurriculumStructureRepository;
    private readonly Mock<CurriculumImportDataValidator> _mockValidator;
    private readonly Mock<ILogger<ImportCurriculumCommandHandler>> _mockLogger;
    private readonly ImportCurriculumCommandHandler _handler;

    public ImportCurriculumCommandHandlerTests()
    {
        _mockKernel = new Mock<Kernel>();
        _mockCurriculumProgramRepository = new Mock<ICurriculumProgramRepository>();
        _mockCurriculumVersionRepository = new Mock<ICurriculumVersionRepository>();
        _mockSubjectRepository = new Mock<ISubjectRepository>();
        _mockCurriculumStructureRepository = new Mock<ICurriculumStructureRepository>();
        _mockValidator = new Mock<CurriculumImportDataValidator>();
        _mockLogger = new Mock<ILogger<ImportCurriculumCommandHandler>>();

        _handler = new ImportCurriculumCommandHandler(
            _mockKernel.Object,
            _mockCurriculumProgramRepository.Object,
            _mockCurriculumVersionRepository.Object,
            _mockSubjectRepository.Object,
            _mockCurriculumStructureRepository.Object,
            _mockValidator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new ImportCurriculumCommand
        {
            RawText = "Sample curriculum text",
            CreatedBy = Guid.NewGuid()
        };

        var mockValidationResult = new FluentValidation.Results.ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<CurriculumImportData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(mockValidationResult);

        _mockCurriculumProgramRepository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<CurriculumProgram, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurriculumProgram?)null);

        _mockCurriculumProgramRepository
            .Setup(r => r.AddAsync(It.IsAny<CurriculumProgram>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurriculumProgram());

        _mockCurriculumVersionRepository
            .Setup(r => r.AddAsync(It.IsAny<CurriculumVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurriculumVersion());

        _mockSubjectRepository
            .Setup(r => r.AddAsync(It.IsAny<Subject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subject());

        _mockCurriculumStructureRepository
            .Setup(r => r.AddAsync(It.IsAny<CurriculumStructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurriculumStructure());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithEmptyRawText_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new ImportCurriculumCommand
        {
            RawText = "",
            CreatedBy = Guid.NewGuid()
        };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Failed to extract curriculum data");
    }

    [Fact]
    public async Task Handle_WithValidationErrors_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new ImportCurriculumCommand
        {
            RawText = "Sample curriculum text",
            CreatedBy = Guid.NewGuid()
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("Property", "Error message"));

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<CurriculumImportData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(validationResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Validation failed");
        result.ValidationErrors.Should().Contain("Error message");
    }

    [Fact]
    public async Task Handle_WithExistingProgram_ShouldReuseProgram()
    {
        // Arrange
        var request = new ImportCurriculumCommand
        {
            RawText = "Sample curriculum text",
            CreatedBy = Guid.NewGuid()
        };

        var existingProgram = new CurriculumProgram
        {
            Id = Guid.NewGuid(),
            ProgramCode = "CS101",
            ProgramName = "Computer Science",
            DegreeLevel = DegreeLevel.Bachelor
        };

        var mockValidationResult = new FluentValidation.Results.ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<CurriculumImportData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(mockValidationResult);

        _mockCurriculumProgramRepository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<CurriculumProgram, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProgram);

        _mockCurriculumVersionRepository
            .Setup(r => r.AddAsync(It.IsAny<CurriculumVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurriculumVersion());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CurriculumProgramId.Should().Be(existingProgram.Id);
        _mockCurriculumProgramRepository.Verify(r => r.AddAsync(It.IsAny<CurriculumProgram>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithException_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new ImportCurriculumCommand
        {
            RawText = "Sample curriculum text",
            CreatedBy = Guid.NewGuid()
        };

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<CurriculumImportData>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("An error occurred during curriculum import");
    }
}