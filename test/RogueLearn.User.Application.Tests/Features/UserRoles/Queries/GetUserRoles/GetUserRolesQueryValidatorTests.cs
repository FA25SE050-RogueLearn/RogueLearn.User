using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;

namespace RogueLearn.User.Application.Tests.Features.UserRoles.Queries.GetUserRoles;

public class GetUserRolesQueryValidatorTests
{
    [Fact]
    public async Task Valid_Passes()
    {
        var v = new GetUserRolesQueryValidator();
        var q = new GetUserRolesQuery { AuthUserId = System.Guid.NewGuid() };
        var res = await v.ValidateAsync(q, CancellationToken.None);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_AuthUserId_Fails()
    {
        var v = new GetUserRolesQueryValidator();
        var q = new GetUserRolesQuery { AuthUserId = default };
        var res = await v.ValidateAsync(q, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(GetUserRolesQuery.AuthUserId));
    }
}