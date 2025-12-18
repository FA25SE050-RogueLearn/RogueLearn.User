using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using RogueLearn.User.Application.Features.GameSessions.Commands.CompleteGameSession;
using RogueLearn.User.Application.Features.UnityMatches.Commands.SubmitUnityMatchResult;
using RogueLearn.User.Application.Features.UnityMatches.Queries.GetUnityMatches;
using RogueLearn.User.Application.Features.UnityMatches.Queries.GetUnityMatchFile;
using RogueLearn.User.Application.Features.UnityMatches.Queries.GetLastPlayerSummary;
using RogueLearn.User.Application.Features.GameSessions.Commands.CreateGameSession;
using RogueLearn.User.Application.Features.GameSessions.Commands.GenerateQuestionPack;
using RogueLearn.User.Application.Features.GameSessions.Commands.StartHost;
using RogueLearn.User.Application.Features.GameSessions.Queries.ResolveGameSession;
using RogueLearn.User.Application.Features.GameSessions.Queries.GetGameSessionPack;
using RogueLearn.User.Application.Features.GameSessions.Queries.GetGameSessionPlayers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RogueLearn.User.Api.Controllers
{
    [ApiController]
    [Route("api/quests/game/sessions")]
    public class GameSessionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GameSessionsController> _logger;

        public GameSessionsController(
            IMediator mediator,
            ILogger<GameSessionsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // DTOs for strong and reliable model binding from JSON bodies
        public class PackSpecDto
        {
            [JsonPropertyName("subject"), JsonProperty("subject")] public string? Subject { get; set; }
            [JsonPropertyName("topic"), JsonProperty("topic")] public string? Topic { get; set; }
            [JsonPropertyName("difficulty"), JsonProperty("difficulty")] public string? Difficulty { get; set; }
            [JsonPropertyName("count"), JsonProperty("count")] public int? Count { get; set; }
        }

        public class CreateSessionRequest
        {
            [JsonPropertyName("relay_join_code"), JsonProperty("relay_join_code")] public string? RelayJoinCode { get; set; }
            [JsonPropertyName("pack_spec"), JsonProperty("pack_spec")] public PackSpecDto? PackSpec { get; set; }
            [JsonPropertyName("user_id"), JsonProperty("user_id")] public string? UserId { get; set; }
        }

        // MVP: Create game session with question pack (saved to database)
        // POST /api/quests/game/sessions/create
        [HttpPost("create")]
        [Consumes("application/json")]
        [Authorize]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            var sessionId = Guid.NewGuid();
            try
            {
                var packResult = await _mediator.Send(new GenerateQuestionPackCommand(
                    sessionId,
                    request?.PackSpec?.Subject,
                    request?.PackSpec?.Topic,
                    request?.PackSpec?.Difficulty,
                    request?.PackSpec?.Count));

                Guid? userId = null;
                if (!string.IsNullOrEmpty(request?.UserId) && Guid.TryParse(request.UserId, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var cmd = new CreateGameSessionCommand
                {
                    SessionId = sessionId,
                    UserId = userId,
                    RelayJoinCode = request?.RelayJoinCode?.Trim(),
                    PackId = packResult.PackId,
                    Subject = packResult.Subject,
                    Topic = packResult.Topic,
                    Difficulty = packResult.Difficulty,
                    QuestionPackJson = packResult.QuestionPackJson
                };

                await _mediator.Send(cmd);

                var response = new
                {
                    match_id = sessionId.ToString(),
                    pack_url = $"/api/quests/game/sessions/{sessionId}/pack"
                };
                return Created($"/api/quests/game/sessions/{sessionId}", response);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GameSession] Failed to create session: {ex.Message}");
                Console.Error.WriteLine($"[GameSession] Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to create game session", details = ex.Message });
            }
        }

        private static string ExtractPackId(JsonElement packJson, string fallback)
        {
            try
            {
                if (packJson.ValueKind == JsonValueKind.Object && packJson.TryGetProperty("packId", out var idProp))
                {
                    var id = idProp.GetString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        return id;
                    }
                }
            }
            catch
            {
                // ignore
            }
            return fallback;
        }

        //[HttpPost("{sessionId:guid}/events")]
        //public IActionResult CompleteSession(Guid sessionId, [FromBody] JsonElement body)
        //{
        //}

        [HttpPost("{sessionId:guid}/complete")]
        [Consumes("application/json")]
        [Authorize(Policy = "GameApiKey")]
        public async Task<IActionResult> CompleteSession(Guid sessionId, [FromBody] JsonElement body)
        {
            try
            {
                var result = await _mediator.Send(new CompleteGameSessionCommand(sessionId));
                if (result.AlreadyCompleted)
                {
                    return Ok(new { match_id = result.MatchId, status = "already_completed" });
                }

                var response = new { match_id = result.MatchId };
                return Created($"/api/quests/game/sessions/{sessionId}/complete", response);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DEMO] Failed to persist completion for session {sessionId}: {ex.Message}");
                return StatusCode(500, new { error = "Failed to complete session", details = ex.Message });
            }
        }

        // MVP: Unity posts match results here (from ServerMatchRecorder)
        // POST /api/quests/game/sessions/unity-match-result
        [HttpPost("unity-match-result")]
        [Consumes("application/json")]
        [Authorize(Policy = "GameApiKey")]
        public async Task<IActionResult> SubmitUnityMatchResult()
        {
            try
            {
                var effectivePayload = await ReadPayloadAsync();
                var command = SubmitUnityMatchResultCommand.FromPayload(effectivePayload);
                var result = await _mediator.Send(command);
                return Ok(new { success = result.Success, matchId = result.MatchId, sessionId = result.SessionId });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Unity Match] Failed to save match result: {ex.Message}");
                Console.Error.WriteLine($"[Unity Match] Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to save match result", details = ex.Message });
            }
        }

        private async Task<JsonElement> ReadPayloadAsync()
        {
            // Read raw body manually to avoid formatter issues (Newtonsoft vs System.Text.Json)
            try
            {
                Request.EnableBuffering();
                using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                Request.Body.Position = 0;

                if (string.IsNullOrWhiteSpace(body))
                {
                    return JsonDocument.Parse("{}").RootElement.Clone();
                }

                var doc = JsonDocument.Parse(body);
                return doc.RootElement.Clone();
            }
            catch
            {
                return JsonDocument.Parse("{}").RootElement.Clone();
            }
        }

        [HttpGet("/api/player/{userId}/last-summary")]
        [Authorize]
        public async Task<IActionResult> GetLastSummary(string userId)
        {
            var json = await _mediator.Send(new GetLastPlayerSummaryQuery(userId));
            if (string.IsNullOrWhiteSpace(json))
            {
                return NotFound(new { error = "No summary for player" });
            }
            return Content(json, "application/json");
        }

        // MVP: Get question pack from database
        // GET /api/quests/game/sessions/{sessionId}/pack
        [HttpGet("{sessionId:guid}/pack")]
        [Authorize(Policy = "GameApiKey")]
        public async Task<IActionResult> GetPack(Guid sessionId)
        {
            try
            {
                var packJson = await _mediator.Send(new GetGameSessionPackQuery(sessionId));
                if (string.IsNullOrWhiteSpace(packJson))
                    return NotFound(new { error = "Pack not found for session" });
                return Content(packJson, "application/json");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GameSession] Failed to read pack for session {sessionId}: {ex.Message}");
                return StatusCode(500, new { error = "Failed to read pack" });
            }
        }

        // MVP: Get player summaries from database
        // GET /api/quests/game/sessions/{sessionId}/players
        [HttpGet("{sessionId:guid}/players")]
        [Authorize]
        public async Task<IActionResult> GetPlayers(Guid sessionId)
        {
            var players = await _mediator.Send(new GetGameSessionPlayersQuery(sessionId));
            return Ok(players);
        }

        // MVP: Resolve join code to session (from database)
        // GET /api/quests/game/sessions/resolve?code=ABCDEF
        [HttpGet("resolve")]
        [Authorize(Policy = "GameApiKey")]
        public async Task<IActionResult> ResolveByJoinCode([FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { error = "Missing join code" });
            }

            try
            {
                var gameSession = await _mediator.Send(new ResolveGameSessionQuery(code, null));
                if (gameSession == null) return NotFound(new { error = "Session not found for join code" });
                var result = new
                {
                    match_id = gameSession.SessionId.ToString(),
                    pack_url = $"/api/quests/game/sessions/{gameSession.SessionId}/pack"
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GameSession] Failed to resolve join code '{code}': {ex.Message}");
                return StatusCode(500, new { error = "Failed to resolve join code" });
            }
        }

        // MVP: Generate pack for session (using optional PackSpec)
        // POST /api/quests/game/sessions/{sessionId}/pack
        // MVP: Get Unity match results from database
        // GET /api/quests/game/sessions/unity-matches?limit=10&userId=xxx
        [HttpGet("unity-matches")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [Authorize]
        public async Task<IActionResult> GetUnityMatches([FromQuery] int limit = 10, [FromQuery] string? userId = null)
        {
            var json = await _mediator.Send(new GetUnityMatchesQuery(limit, userId));
            return Content(json, "application/json");
        }


        // MVP: Get specific Unity match by ID
        // GET /api/quests/game/sessions/unity-matches/{matchId}
        [HttpGet("unity-matches/{matchId}")]
        [Authorize]
        public async Task<IActionResult> GetUnityMatch(string matchId)
        {
            var json = await _mediator.Send(new GetUnityMatchFileQuery(matchId));
            if (string.IsNullOrWhiteSpace(json)) return NotFound(new { error = "Match not found" });
            return Content(json, "application/json");
        }

        // Host: start Unity headless via Docker and return join code
        // POST /api/quests/game/sessions/host
        [HttpPost("host")]
        [Consumes("application/json")]
        [Authorize(Policy = "GameApiKey")]
        public async Task<IActionResult> StartHost([FromBody] HostRequest? request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new StartHostCommand(request?.UserId), cancellationToken);
            if (!result.Ok)
            {
                return StatusCode(500, new { ok = false, error = result.Error });
            }

            return Ok(new
            {
                ok = true,
                joinCode = result.JoinCode,
                hostId = result.HostId,
                message = result.Message,
                raw = result.RawLog,
                wsUrl = result.WsUrl
            });
        }

        [HttpDelete("host/{hostId}")]
        [Authorize(Policy = "GameApiKey")]
        public async Task<IActionResult> StopHost([FromRoute] string hostId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(hostId))
            {
                return BadRequest(new { ok = false, error = "Missing hostId" });
            }

            if (!Regex.IsMatch(hostId, @"^[a-zA-Z0-9_.-]+$"))
            {
                return BadRequest(new { ok = false, error = "Invalid hostId" });
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"rm -f {hostId}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    return StatusCode(500, new { ok = false, error = "Failed to start docker process" });
                }

                var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                var stderrTask = proc.StandardError.ReadToEndAsync();

                await proc.WaitForExitAsync(cancellationToken);
                var stdout = await stdoutTask;
                var stderr = await stderrTask;

                if (proc.ExitCode != 0)
                {
                    _logger.LogWarning("[Host] docker rm -f failed for {HostId}: {Error}", hostId, stderr);
                    return StatusCode(500, new { ok = false, hostId, error = stderr });
                }

                return Ok(new { ok = true, hostId, message = stdout });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(504, new { ok = false, hostId, error = "StopHost timed out" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Host] Failed to stop host {HostId}", hostId);
                return StatusCode(500, new { ok = false, hostId, error = ex.Message });
            }
        }

        public class HostRequest
        {
            [JsonPropertyName("userId"), JsonProperty("userId")] public string? UserId { get; set; }
        }
    }
}
