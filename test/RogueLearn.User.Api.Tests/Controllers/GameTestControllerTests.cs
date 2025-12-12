using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Controllers;

namespace RogueLearn.User.Api.Tests.Controllers;

public class GameTestControllerTests
{
    [Fact]
    public void GetAuthenticatedEcho_Returns_UserDetails()
    {
        var userId = Guid.NewGuid();
        var controller = new GameTestController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim("username", "testuser"),
                    new Claim(ClaimTypes.Email, "user@example.com"),
                }, "Test"))
            }
        };

        var result = controller.GetAuthenticatedEcho();
        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeOfType<GameTestEchoResponse>();
        var payload = (GameTestEchoResponse)ok.Value!;
        payload.AuthenticatedUserId.Should().Be(userId);
        payload.Username.Should().Be("testuser");
        payload.Email.Should().Be("user@example.com");
        payload.Message.Should().NotBeNullOrEmpty();
    }
}

