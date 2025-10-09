using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using FluentAssertions;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportSyllabus;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Entities;
using System.Linq.Expressions;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

namespace RogueLearn.User.Application.Tests.Features.CurriculumImport.Commands;

public class ImportSyllabusCommandHandlerTests
{
    private readonly Mock<Kernel> _mockKernel;
    private readonly Mock<ISubjectRepository> _mockSubjectRepository;
    private readonly Mock<ISyllabusVersionRepository> _mockSyllabusVersionRepository;
    private readonly Mock<SyllabusDataValidator> _mockValidator;
    private readonly Mock<ILogger<ImportSyllabusCommandHandler>> _mockLogger;
    private readonly ImportSyllabusCommandHandler _handler;

    public ImportSyllabusCommandHandlerTests()
    {
        _mockKernel = new Mock<Kernel>();
        _mockSubjectRepository = new Mock<ISubjectRepository>();
        _mockSyllabusVersionRepository = new Mock<ISyllabusVersionRepository>();
        _mockValidator = new Mock<SyllabusDataValidator>();
        _mockLogger = new Mock<ILogger<ImportSyllabusCommandHandler>>();

        _handler = new ImportSyllabusCommandHandler(
            _mockKernel.Object,
            _mockSubjectRepository.Object,
            _mockSyllabusVersionRepository.Object,
            _mockValidator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new ImportSyllabusCommand
        {
            RawText = "Sample syllabus text",
            CreatedBy = Guid.NewGuid()
        };

        var existingSubject = new Subject
        {
            Id = Guid.NewGuid(),
            SubjectCode = "CS101",
            SubjectName = "Introduction to Computer Science",
            Credits = 3
        };

        var mockValidationResult = new FluentValidation.Results.ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<SyllabusData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(mockValidationResult);

        _mockSubjectRepository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Subject, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubject);

        _mockSyllabusVersionRepository
            .Setup(r => r.AddAsync(It.IsAny<SyllabusVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyllabusVersion());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.SubjectId.Should().Be(existingSubject.Id);
        result.SubjectCode.Should().Be(existingSubject.SubjectCode);
    }

    [Fact]
    public async Task Handle_WithEmptyRawText_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new ImportSyllabusCommand
        {
            RawText = "",
            CreatedBy = Guid.NewGuid()
        };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Failed to extract syllabus data");
    }

    [Fact]
    public async Task Handle_WithValidationErrors_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new ImportSyllabusCommand
        {
            RawText = "Sample syllabus text",
            CreatedBy = Guid.NewGuid()
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("Property", "Error message"));

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<SyllabusData>(), It.IsAny<CancellationToken>()))
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
    public async Task Handle_WithNonExistentSubject_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = new ImportSyllabusCommand
        {
            RawText = "Sample syllabus text",
            CreatedBy = Guid.NewGuid()
        };

        var mockValidationResult = new FluentValidation.Results.ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<SyllabusData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(mockValidationResult);

        _mockSubjectRepository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Subject, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subject?)null);

        // Act & Assert
        var result = await _handler.Handle(request, CancellationToken.None);
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("An error occurred during syllabus import");
    }

    [Fact]
    public async Task Handle_WithException_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new ImportSyllabusCommand
        {
            RawText = "Sample syllabus text",
            CreatedBy = Guid.NewGuid()
        };

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<SyllabusData>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("An error occurred during syllabus import");
    }

    [Fact]
    public async Task Handle_ShouldCreateSyllabusVersionWithCorrectProperties()
    {
        // Arrange
        var request = new ImportSyllabusCommand
        {
            RawText = "Sample syllabus text",
            CreatedBy = Guid.NewGuid()
        };

        var existingSubject = new Subject
        {
            Id = Guid.NewGuid(),
            SubjectCode = "CS101",
            SubjectName = "Introduction to Computer Science",
            Credits = 3
        };

        var mockValidationResult = new FluentValidation.Results.ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<SyllabusData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(mockValidationResult);

        _mockSubjectRepository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Subject, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubject);

        SyllabusVersion? capturedSyllabusVersion = null;
        _mockSyllabusVersionRepository
            .Setup(r => r.AddAsync(It.IsAny<SyllabusVersion>(), It.IsAny<CancellationToken>()))
            .Callback<SyllabusVersion, CancellationToken>((sv, ct) => capturedSyllabusVersion = sv)
            .ReturnsAsync(new SyllabusVersion());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        capturedSyllabusVersion.Should().NotBeNull();
        capturedSyllabusVersion!.SubjectId.Should().Be(existingSubject.Id);
        capturedSyllabusVersion.CreatedBy.Should().Be(request.CreatedBy);
        capturedSyllabusVersion.IsActive.Should().BeTrue();
    }
}