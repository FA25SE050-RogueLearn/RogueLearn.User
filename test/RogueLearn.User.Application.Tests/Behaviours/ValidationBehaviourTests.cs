using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using RogueLearn.User.Application.Behaviours;

namespace RogueLearn.User.Application.Tests.Behaviours;

public class ValidationBehaviourTests
{
    public record DummyRequest(string Payload) : IRequest<string>;
    private static Task<string> NextOk(CancellationToken ct) => Task.FromResult("ok");
    private static Task<string> NextResult(CancellationToken ct) => Task.FromResult("result");
    private static Task<string> NextShouldNotBeCalled(CancellationToken ct) => Task.FromResult("should not be called");

    [Fact]
    public async Task Handle_No_Validators_Calls_Next()
    {
        var sut = new ValidationBehaviour<DummyRequest, string>(Array.Empty<IValidator<DummyRequest>>());
        var request = new DummyRequest("data");
        var res = await sut.Handle(request, new RequestHandlerDelegate<string>(NextOk), CancellationToken.None);
        Assert.Equal("ok", res);
        
    }

    [Fact]
    public async Task Handle_With_Validations_Success_Calls_Next()
    {
        var validator = Substitute.For<IValidator<DummyRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<DummyRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var sut = new ValidationBehaviour<DummyRequest, string>(new[] { validator });
        var request = new DummyRequest("ok");
        var res = await sut.Handle(request, new RequestHandlerDelegate<string>(NextResult), CancellationToken.None);
        Assert.Equal("result", res);
        
    }

    [Fact]
    public async Task Handle_With_Failures_Throws_ValidationException()
    {
        var validator = Substitute.For<IValidator<DummyRequest>>();
        var failure = new ValidationFailure("Payload", "must not be empty");
        validator.ValidateAsync(Arg.Any<ValidationContext<DummyRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { failure }));

        var sut = new ValidationBehaviour<DummyRequest, string>(new[] { validator });
        var request = new DummyRequest("");
        var ex = await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.ValidationException>(() => sut.Handle(request, new RequestHandlerDelegate<string>(NextShouldNotBeCalled), CancellationToken.None));
        Assert.True(ex.Errors.ContainsKey("Payload"));
        
    }
}