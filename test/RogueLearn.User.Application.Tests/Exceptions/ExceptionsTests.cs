using FluentAssertions;
using RogueLearn.User.Application.Exceptions;

namespace RogueLearn.User.Application.Tests.Exceptions;

public class ExceptionsTests
{
    [Fact]
    public void BadRequestException_Constructors_Work()
    {
        new BadRequestException().Message.Should().Be("Bad request");
        new BadRequestException("m").Message.Should().Be("m");
        var inner = new Exception("i");
        new BadRequestException("m", inner).InnerException.Should().Be(inner);
    }

    [Fact]
    public void ConflictException_Constructors_Work()
    {
        new ConflictException().Message.Should().NotBeNull();
        new ConflictException("m").Message.Should().Be("m");
        var inner = new Exception("i");
        new ConflictException("m", inner).InnerException.Should().Be(inner);
    }

    [Fact]
    public void ForbiddenException_Constructors_Work()
    {
        new ForbiddenException().Message.Should().Be("Access forbidden");
        new ForbiddenException("m").Message.Should().Be("m");
        var inner = new Exception("i");
        new ForbiddenException("m", inner).InnerException.Should().Be(inner);
    }

    [Fact]
    public void MethodNotAllowedException_Constructors_Work()
    {
        new MethodNotAllowedException().Message.Should().Be("Method not allowed");
        new MethodNotAllowedException("m").Message.Should().Be("m");
        var inner = new Exception("i");
        new MethodNotAllowedException("m", inner).InnerException.Should().Be(inner);
    }

    [Fact]
    public void NotFoundException_Constructors_Work()
    {
        new NotFoundException().Message.Should().NotBeNullOrEmpty();
        new NotFoundException("m").Message.Should().Be("m");
        var inner = new Exception("i");
        new NotFoundException("m", inner).InnerException.Should().Be(inner);
        new NotFoundException("Entity", 42).Message.Should().Contain("Entity \"Entity\" (42) was not found.");
    }

    [Fact]
    public void TooManyRequestsException_Constructors_Work()
    {
        new TooManyRequestsException().Message.Should().Be("Too many requests");
        new TooManyRequestsException("m").Message.Should().Be("m");
        var inner = new Exception("i");
        new TooManyRequestsException("m", inner).InnerException.Should().Be(inner);
    }

    [Fact]
    public void UnauthorizedException_Constructors_Work()
    {
        new UnauthorizedException().Message.Should().Be("Unauthorized access");
        new UnauthorizedException("m").Message.Should().Be("m");
        var inner = new Exception("i");
        new UnauthorizedException("m", inner).InnerException.Should().Be(inner);
    }

    [Fact]
    public void UnprocessableEntityException_Constructors_Work()
    {
        new UnprocessableEntityException().Message.Should().Be("Unprocessable entity");
        new UnprocessableEntityException("m").Message.Should().Be("m");
        var inner = new Exception("i");
        new UnprocessableEntityException("m", inner).InnerException.Should().Be(inner);
    }

    [Fact]
    public void ValidationException_Constructors_Work()
    {
        var exDefault = new ValidationException();
        exDefault.Message.Should().Be("One or more validation failures have occurred.");
        exDefault.Errors.Should().NotBeNull();

        var exWithMessage = new ValidationException("m");
        exWithMessage.Message.Should().Be("m");
        exWithMessage.Errors.Should().NotBeNull();
    }
}