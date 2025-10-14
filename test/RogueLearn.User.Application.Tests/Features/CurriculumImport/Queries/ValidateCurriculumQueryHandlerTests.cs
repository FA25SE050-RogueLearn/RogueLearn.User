using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using FluentAssertions;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;

namespace RogueLearn.User.Application.Tests.Features.CurriculumImport.Queries;

public class ValidateCurriculumQueryHandlerTests
{
    private readonly Mock<Kernel> _mockKernel;
    private readonly Mock<CurriculumImportDataValidator> _mockValidator;
    private readonly Mock<ILogger<ValidateCurriculumQueryHandler>> _mockLogger;
    private readonly Mock<RogueLearn.User.Application.Interfaces.ICurriculumImportStorage> _mockStorage;
    private readonly Mock<IFlmExtractionPlugin> _mockFlmPlugin;
    private readonly ValidateCurriculumQueryHandler _handler;

    public ValidateCurriculumQueryHandlerTests()
    {
        _mockKernel = new Mock<Kernel>();
        _mockValidator = new Mock<CurriculumImportDataValidator>();
        _mockLogger = new Mock<ILogger<ValidateCurriculumQueryHandler>>();
        _mockStorage = new Mock<RogueLearn.User.Application.Interfaces.ICurriculumImportStorage>();
        _mockFlmPlugin = new Mock<IFlmExtractionPlugin>();

        _handler = new ValidateCurriculumQueryHandler(
            _mockKernel.Object,
            _mockStorage.Object,
            _mockValidator.Object,
            _mockLogger.Object,
            _mockFlmPlugin.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new ValidateCurriculumQuery
        {
            RawText = "Sample curriculum text for validation"
        };

        var mockValidationResult = new FluentValidation.Results.ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<CurriculumImportData>(), It.IsAny<CancellationToken>()))
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
        var request = new ValidateCurriculumQuery
        {
            RawText = ""
        };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("Failed to extract curriculum data from the provided text");
    }

    [Fact]
    public async Task Handle_WithValidationErrors_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new ValidateCurriculumQuery
        {
            RawText = "Sample curriculum text for validation"
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("ProgramName", "Program name is required"));
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("ProgramCode", "Program code is required"));

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<CurriculumImportData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(validationResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().HaveCount(2);
        result.ValidationErrors.Should().Contain("Program name is required");
        result.ValidationErrors.Should().Contain("Program code is required");
    }

    [Fact]
    public async Task Handle_WithException_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new ValidateCurriculumQuery
        {
            RawText = "Sample curriculum text for validation"
        };

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<CurriculumImportData>(), It.IsAny<CancellationToken>()))
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
        var request = new ValidateCurriculumQuery
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
        var request = new ValidateCurriculumQuery
        {
            RawText = "Sample curriculum text for validation"
        };

        var mockValidationResult = new FluentValidation.Results.ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<CurriculumImportData>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(mockValidationResult);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting curriculum validation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Curriculum validation completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}