using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using FluentAssertions;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Features.CurriculumImport.Queries;

public class ValidateSyllabusQueryHandlerTests
{
    private readonly Mock<Kernel> _mockKernel;
    private readonly Mock<SyllabusDataValidator> _mockValidator;
    private readonly Mock<ILogger<ValidateSyllabusQueryHandler>> _mockLogger;
    private readonly ValidateSyllabusQueryHandler _handler;

    public ValidateSyllabusQueryHandlerTests()
    {
        _mockKernel = new Mock<Kernel>();
        _mockValidator = new Mock<SyllabusDataValidator>();
        _mockLogger = new Mock<ILogger<ValidateSyllabusQueryHandler>>();

        _handler = new ValidateSyllabusQueryHandler(
            _mockKernel.Object,
            _mockValidator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new ValidateSyllabusQuery
        {
            RawText = "Sample syllabus text for validation"
        };

        var mockValidationResult = new FluentValidation.Results.ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<SyllabusData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(mockValidationResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ValidationErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithEmptyRawText_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new ValidateSyllabusQuery
        {
            RawText = ""
        };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("Failed to extract syllabus data from the provided text");
    }

    [Fact]
    public async Task Handle_WithValidationErrors_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new ValidateSyllabusQuery
        {
            RawText = "Sample syllabus text for validation"
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("SubjectCode", "Subject code is required"));
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("VersionNumber", "Version number must be greater than 0"));

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<SyllabusData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(validationResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().HaveCount(2);
        result.ValidationErrors.Should().Contain("Subject code is required");
        result.ValidationErrors.Should().Contain("Version number must be greater than 0");
    }

    [Fact]
    public async Task Handle_WithException_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new ValidateSyllabusQuery
        {
            RawText = "Sample syllabus text for validation"
        };

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<SyllabusData>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("An error occurred during validation");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithInvalidRawText_ShouldReturnFailureResponse(string? rawText)
    {
        // Arrange
        var request = new ValidateSyllabusQuery
        {
            RawText = rawText!
        };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldLogInformationMessages()
    {
        // Arrange
        var request = new ValidateSyllabusQuery
        {
            RawText = "Sample syllabus text for validation"
        };

        var mockValidationResult = new FluentValidation.Results.ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<SyllabusData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(mockValidationResult);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting syllabus validation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Syllabus validation completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidSyllabusData_ShouldReturnExtractedData()
    {
        // Arrange
        var request = new ValidateSyllabusQuery
        {
            RawText = "Sample syllabus text with valid structure"
        };

        var mockValidationResult = new FluentValidation.Results.ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<SyllabusData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(mockValidationResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ExtractedData.Should().NotBeNull();
    }
}