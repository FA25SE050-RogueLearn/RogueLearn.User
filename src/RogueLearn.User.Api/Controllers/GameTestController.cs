using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingBlocks.Shared.Authentication;
using System.Security.Claims;

namespace RogueLearn.User.Api.Controllers;

/// <summary>
/// Provides simple, authenticated endpoints for game clients to test connectivity and authentication.
/// </summary>
[ApiController]
[Route("api/game-test")]
[Authorize] // IMPORTANT: This endpoint requires a valid JWT, making it a perfect test for the game client's auth flow.
public class GameTestController : ControllerBase
{
    /// <summary>
    /// An authenticated "echo" endpoint.
    /// If the game client sends a valid JWT, this endpoint will return the user's ID and username.
    /// </summary>
    /// <returns>A JSON object with the authenticated user's details.</returns>
    [HttpGet("echo")]
    [ProducesResponseType(typeof(GameTestEchoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetAuthenticatedEcho()
    {
        // Use the same trusted extension method to get the user's ID from the token.
        var authUserId = User.GetAuthUserId();

        // You can also extract other claims for the game developer to verify.
        var username = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value ?? "Username not found in token";
        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "Email not found in token";

        var response = new GameTestEchoResponse
        {
            Message = "Authentication successful. Welcome to RogueLearn Backend!",
            AuthenticatedUserId = authUserId,
            Username = username,
            Email = email,
            ServerTimestamp = DateTimeOffset.UtcNow
        };

        return Ok(response);
    }
}

/// <summary>
/// The response object for the echo test endpoint.
/// </summary>
public class GameTestEchoResponse
{
    public string Message { get; set; } = string.Empty;
    public Guid AuthenticatedUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset ServerTimestamp { get; set; }
}