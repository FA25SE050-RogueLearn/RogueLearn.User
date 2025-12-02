using FluentAssertions;
using FluentValidation.Results;
using RogueLearn.User.Application.Exceptions;

namespace RogueLearn.User.Application.Tests.Exceptions;

public class ValidationExceptionFailureGroupingTests
{
    [Fact]
    public void GroupsFailuresByPropertyName()
    {
        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("A", "a1"),
            new ValidationFailure("A", "a2"),
            new ValidationFailure("B", "b1")
        };

        var ex = new ValidationException(failures);
        ex.Errors.Should().ContainKey("A");
        ex.Errors.Should().ContainKey("B");
        ex.Errors["A"].Should().Equal("a1", "a2");
        ex.Errors["B"].Should().Equal("b1");
    }
}