using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.SemanticKernel;
using Microsoft.AspNetCore.Http;

namespace RogueLearn.User.Api.Controllers
{
    [ApiController]
    [Route("api/quests/game/sessions")]
    // For demo flow, allow anonymous posts with optional API key; tighten later if needed.
    [AllowAnonymous]
    public class GameSessionsController : ControllerBase
    {
        private readonly Microsoft.SemanticKernel.Kernel _kernel;
        private readonly RogueLearn.User.Domain.Interfaces.ISubjectRepository _subjectRepository;
        private readonly RogueLearn.User.Domain.Interfaces.IMatchResultRepository _matchResultRepository;
        private readonly RogueLearn.User.Domain.Interfaces.IGameSessionRepository _gameSessionRepository;

        // MVP: All session data now stored in database for production scalability

        public GameSessionsController(
            Microsoft.SemanticKernel.Kernel kernel,
            RogueLearn.User.Domain.Interfaces.ISubjectRepository subjectRepository,
            RogueLearn.User.Domain.Interfaces.IMatchResultRepository matchResultRepository,
            RogueLearn.User.Domain.Interfaces.IGameSessionRepository gameSessionRepository)
        {
            _kernel = kernel;
            _subjectRepository = subjectRepository;
            _matchResultRepository = matchResultRepository;
            _gameSessionRepository = gameSessionRepository;
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

        public class CompletionRequest
        {
            [JsonPropertyName("result"), JsonProperty("result")] public string? Result { get; set; }
            [JsonPropertyName("timestamp"), JsonProperty("timestamp")] public string? Timestamp { get; set; }
            [JsonPropertyName("summary"), JsonProperty("summary")] public JsonElement Summary { get; set; }
        }

        private static string NormalizeCompletionToV2(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                string? result = null;
                string? ts = null;
                if (root.TryGetProperty("result", out var r)) result = r.ValueKind == JsonValueKind.String ? r.GetString() : r.ToString();
                if (root.TryGetProperty("timestamp", out var t)) ts = t.ValueKind == JsonValueKind.String ? t.GetString() : DateTime.UtcNow.ToString("o");
                var topicsArray = new List<object>();
                if (root.TryGetProperty("summary", out var summaryEl))
                {
                    if (summaryEl.ValueKind == JsonValueKind.Object && summaryEl.TryGetProperty("topics", out var topicsEl))
                    {
                        if (topicsEl.ValueKind == JsonValueKind.Array)
                        {
                            bool hasObjects = false;
                            foreach (var item in topicsEl.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.Object)
                                {
                                    hasObjects = true;
                                    string? topicName = item.TryGetProperty("topic", out var tn) && tn.ValueKind == JsonValueKind.String ? tn.GetString() : null;
                                    int totalVal = item.TryGetProperty("total", out var tt) && tt.ValueKind == JsonValueKind.Number ? tt.GetInt32() : 0;
                                    int correctVal = item.TryGetProperty("correct", out var cc) && cc.ValueKind == JsonValueKind.Number ? cc.GetInt32() : 0;
                                    topicsArray.Add(new { topic = topicName ?? string.Empty, total = totalVal, correct = correctVal });
                                }
                            }
                            if (!hasObjects)
                            {
                                topicsArray = new List<object>();
                            }
                        }
                        else if (topicsEl.ValueKind == JsonValueKind.String)
                        {
                            var s = topicsEl.GetString() ?? string.Empty;
                            try
                            {
                                using var inner = JsonDocument.Parse(s);
                                var innerRoot = inner.RootElement;
                                if (innerRoot.ValueKind == JsonValueKind.Array)
                                {
                                    bool hasObjects = false;
                                    foreach (var item in innerRoot.EnumerateArray())
                                    {
                                        if (item.ValueKind == JsonValueKind.Object)
                                        {
                                            hasObjects = true;
                                            string? topicName = item.TryGetProperty("topic", out var tn) && tn.ValueKind == JsonValueKind.String ? tn.GetString() : null;
                                            int totalVal = item.TryGetProperty("total", out var tt) && tt.ValueKind == JsonValueKind.Number ? tt.GetInt32() : 0;
                                            int correctVal = item.TryGetProperty("correct", out var cc) && cc.ValueKind == JsonValueKind.Number ? cc.GetInt32() : 0;
                                            topicsArray.Add(new { topic = topicName ?? string.Empty, total = totalVal, correct = correctVal });
                                        }
                                    }
                                    if (!hasObjects) topicsArray = new List<object>();
                                }
                            }
                            catch { topicsArray = new List<object>(); }
                        }
                    }
                }
                var obj = new
                {
                    format = "v2",
                    result = result ?? string.Empty,
                    timestamp = ts ?? DateTime.UtcNow.ToString("o"),
                    summary = new { topics = topicsArray }
                };
                return System.Text.Json.JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
            }
            catch
            {
                var obj = new
                {
                    format = "v2",
                    result = string.Empty,
                    timestamp = DateTime.UtcNow.ToString("o"),
                    summary = new { topics = Array.Empty<object>() }
                };
                return System.Text.Json.JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
            }
        }

        private static string BuildRawFromModel(CompletionRequest summary)
        {
            var obj = new
            {
                result = summary?.Result,
                timestamp = summary?.Timestamp,
                summary = HasValue(summary!.Summary) ? summary!.Summary : default(JsonElement)
            };
            return System.Text.Json.JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
        }

        private static bool HasValue(JsonElement el) => el.ValueKind != JsonValueKind.Undefined && el.ValueKind != JsonValueKind.Null;

        private static string NormalizeCompletionFromModel(CompletionRequest model)
        {
            var topicsArray = new List<object>();
            try
            {
                var summaryEl = model.Summary;
                if (HasValue(summaryEl) && summaryEl.ValueKind == JsonValueKind.Object)
                {
                    if (summaryEl.TryGetProperty("topics", out var topicsEl) && topicsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in topicsEl.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object)
                            {
                                string? topicName = item.TryGetProperty("topic", out var tn) && tn.ValueKind == JsonValueKind.String ? tn.GetString() : null;
                                int totalVal = item.TryGetProperty("total", out var tt) && tt.ValueKind == JsonValueKind.Number ? tt.GetInt32() : 0;
                                int correctVal = item.TryGetProperty("correct", out var cc) && cc.ValueKind == JsonValueKind.Number ? cc.GetInt32() : 0;
                                topicsArray.Add(new { topic = topicName ?? string.Empty, total = totalVal, correct = correctVal });
                            }
                        }
                    }
                }
            }
            catch { topicsArray = new List<object>(); }

            var obj = new
            {
                format = "v2",
                result = model?.Result ?? string.Empty,
                timestamp = model?.Timestamp ?? DateTime.UtcNow.ToString("o"),
                summary = new { topics = topicsArray }
            };
            return System.Text.Json.JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
        }

        // MVP: Create game session with question pack (saved to database)
        // POST /api/quests/game/sessions/create
        [HttpPost("create")]
        [Consumes("application/json")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            var sessionId = Guid.NewGuid();
            try
            {
                // Generate question pack for this session
                var packJson = await GeneratePackAsync(sessionId, request);
                var packJsonString = packJson.GetRawText();

                // Extract pack metadata from JSON
                var packId = sessionId.ToString();
                var subject = request?.PackSpec?.Subject ?? "demo";
                var topic = request?.PackSpec?.Topic ?? "basics";
                var difficulty = request?.PackSpec?.Difficulty ?? "easy";

                try
                {
                    if (packJson.TryGetProperty("packId", out var packIdEl))
                        packId = packIdEl.GetString() ?? packId;
                }
                catch { }

                // Parse user_id if provided
                Guid? userId = null;
                if (!string.IsNullOrEmpty(request?.UserId) && Guid.TryParse(request.UserId, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                // Create game session entity
                var gameSession = new RogueLearn.User.Domain.Entities.GameSession
                {
                    SessionId = sessionId,
                    UserId = userId,
                    RelayJoinCode = request?.RelayJoinCode?.Trim(),
                    PackId = packId,
                    Subject = subject,
                    Topic = topic,
                    Difficulty = difficulty,
                    QuestionPackJson = packJsonString, // Store as JSON string
                    Status = "created"
                };

                // Save to database
                await _gameSessionRepository.AddAsync(gameSession);

                Console.WriteLine($"[GameSession] Created session {sessionId} with pack (subject: {subject}, topic: {topic}, join code: {request?.RelayJoinCode ?? "none"})");

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

        //[HttpPost("{sessionId:guid}/events")]
        //public IActionResult CompleteSession(Guid sessionId, [FromBody] JsonElement body)
        //{
        //}

        [HttpPost("{sessionId:guid}/complete")]
        [Consumes("application/json")]
        public async Task<IActionResult> CompleteSession(Guid sessionId, [FromBody] JsonElement body)
        {
            // MVP FIX: Enable request body buffering to allow multiple reads
            Request.EnableBuffering();

            // Check if session already completed (from database)
            var gameSession = await _gameSessionRepository.GetBySessionIdAsync(sessionId);
            var alreadyCompleted = gameSession?.Status == "completed";

            try
            {
                var raw = body.ValueKind == JsonValueKind.Undefined ? string.Empty : body.GetRawText();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        Request.Body.Position = 0;
                        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                        var altRaw = await reader.ReadToEndAsync();
                        if (!string.IsNullOrWhiteSpace(altRaw)) raw = altRaw;
                    }
                    catch { /* fallback ignored */ }
                }
                var preview = raw.Length > 200 ? raw.Substring(0, 200) + "..." : raw;
                var sender = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
                var ua = Request.Headers.ContainsKey("User-Agent") ? Request.Headers["User-Agent"].ToString() : "unknown";
                var fmtHeader = Request.Headers.ContainsKey("X-Rogue-Format") ? Request.Headers["X-Rogue-Format"].ToString() : null;
                var senderHeader = Request.Headers.ContainsKey("X-Rogue-Sender") ? Request.Headers["X-Rogue-Sender"].ToString() : null;
                Console.WriteLine($"[DEMO] Completion headers: remote={sender}, ua={ua}, X-Rogue-Format={fmtHeader}, X-Rogue-Sender={senderHeader}");
                Console.WriteLine($"[DEMO] Completion raw preview (len={raw.Length}): {preview}");

                string normalized;
                try
                {
                    var model = string.IsNullOrWhiteSpace(raw)
                        ? new CompletionRequest()
                        : System.Text.Json.JsonSerializer.Deserialize<CompletionRequest>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    normalized = string.IsNullOrWhiteSpace(raw)
                        ? NormalizeCompletionFromModel(model ?? new CompletionRequest())
                        : NormalizeCompletionToV2(raw);
                }
                catch
                {
                    normalized = NormalizeCompletionToV2(raw);
                }

                if (!alreadyCompleted && gameSession != null)
                {
                    gameSession.Status = "completed";
                    gameSession.CompletedAt = DateTimeOffset.UtcNow;

                    var existingMatch = await _matchResultRepository.GetByMatchIdAsync(sessionId.ToString());
                    if (existingMatch != null)
                    {
                        gameSession.MatchResultId = existingMatch.Id;
                        if (existingMatch.UserId.HasValue && !gameSession.UserId.HasValue)
                        {
                            gameSession.UserId = existingMatch.UserId;
                        }
                    }

                    await _gameSessionRepository.UpdateAsync(gameSession);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DEMO] Failed to persist completion for session {sessionId}: {ex.Message}");
            }

            if (alreadyCompleted)
            {
                return Ok(new { match_id = sessionId.ToString(), status = "already_completed" });
            }

            var response = new { match_id = sessionId.ToString() };
            return Created($"/api/quests/game/sessions/{sessionId}/complete", response);
        }

        // MVP: Unity posts match results here (from ServerMatchRecorder)
        // POST /api/quests/game/sessions/unity-match-result
        [HttpPost("unity-match-result")]
        [Consumes("application/json")]
        public async Task<IActionResult> SubmitUnityMatchResult()
        {
            try
            {
                // Read raw JSON from request body
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var jsonString = await reader.ReadToEndAsync();

                // Parse JSON document
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                // Extract key fields
                var matchId = root.TryGetProperty("matchId", out var matchIdEl) && matchIdEl.ValueKind == JsonValueKind.String
                    ? matchIdEl.GetString()
                    : Guid.NewGuid().ToString();

                var result = root.TryGetProperty("result", out var resultEl) && resultEl.ValueKind == JsonValueKind.String
                    ? resultEl.GetString()
                    : "unknown";

                var joinCode = root.TryGetProperty("joinCode", out var joinEl) && joinEl.ValueKind == JsonValueKind.String
                    ? joinEl.GetString()
                    : null;

                var scene = root.TryGetProperty("scene", out var sceneEl) && sceneEl.ValueKind == JsonValueKind.String
                    ? sceneEl.GetString()
                    : "unknown";

                var startUtc = root.TryGetProperty("startUtc", out var startEl) && startEl.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(startEl.GetString(), out var start)
                    ? start
                    : DateTime.UtcNow.AddMinutes(-5);

                var endUtc = root.TryGetProperty("endUtc", out var endEl) && endEl.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(endEl.GetString(), out var end)
                    ? end
                    : DateTime.UtcNow;

                var totalPlayers = root.TryGetProperty("totalPlayers", out var totalPlayersEl) && totalPlayersEl.ValueKind == JsonValueKind.Number
                    ? totalPlayersEl.GetInt32()
                    : 0;

                // Extract user_id if provided (from frontend)
                Guid? userId = null;
                if (root.TryGetProperty("userId", out var userIdEl) && userIdEl.ValueKind == JsonValueKind.String)
                {
                    var userIdStr = userIdEl.GetString();
                    if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                // Try to resolve the originating game session so we can attach the question pack
                var resolvedSession = await ResolveSessionForMatchAsync(matchId, userId, joinCode, endUtc);
                if (resolvedSession == null)
                {
                    Console.WriteLine($"[Unity Match] ⚠️ No game session resolved for match {matchId} (join code: {joinCode ?? "none"})");
                }

                // Create match result entity
                var matchResult = new RogueLearn.User.Domain.Entities.MatchResult
                {
                    MatchId = matchId ?? Guid.NewGuid().ToString(),
                    StartUtc = startUtc,
                    EndUtc = endUtc,
                    Result = result ?? "unknown",
                    Scene = scene ?? "unknown",
                    TotalPlayers = totalPlayers,
                    UserId = userId,
                    MatchDataJson = resolvedSession != null
                        ? MergeQuestionPackIntoMatchData(jsonString, resolvedSession.QuestionPackJson, resolvedSession.SessionId)
                        : jsonString // Store full JSON as string
                };

                var savedMatch = await _matchResultRepository.AddAsync(matchResult);

                Console.WriteLine($"[Unity Match] Attempting to link match result {savedMatch.Id} to game session");
                Console.WriteLine($"[Unity Match] MatchId: {savedMatch.MatchId}");

                // First try: Look up by exact matchId (if it's a valid GUID)
                RogueLearn.User.Domain.Entities.GameSession? session = resolvedSession;
                Guid sessionGuid;
                if (session == null && Guid.TryParse(savedMatch.MatchId, out sessionGuid))
                {
                    Console.WriteLine($"[Unity Match] Parsed sessionGuid: {sessionGuid}");
                    session = await _gameSessionRepository.GetBySessionIdAsync(sessionGuid);
                    if (session != null)
                    {
                        Console.WriteLine($"[Unity Match] Found game session by exact GUID");
                    }
                }
                else
                {
                    Console.WriteLine($"[Unity Match] ⚠️ Failed to parse MatchId '{matchResult.MatchId}' as GUID");
                }

                // Second try: If not found and we have a userId, find the most recent uncompleted session
                if (session == null && matchResult.UserId.HasValue)
                {
                    Console.WriteLine($"[Unity Match] Looking for most recent uncompleted session for user {matchResult.UserId.Value}");
                    var recentSessions = await _gameSessionRepository.GetRecentSessionsByUserAsync(matchResult.UserId.Value, 20);
                    session = recentSessions
                        .Where(s => s.Status != "completed" || s.MatchResultId == null)
                        .OrderByDescending(s => s.CreatedAt)
                        .FirstOrDefault();

                    if (session != null)
                    {
                        Console.WriteLine($"[Unity Match] Found uncompleted session {session.Id} for user");
                    }
                }

                // Third try: reuse the resolved session (even if completed) and keep the linkage
                if (session == null && resolvedSession != null)
                {
                    session = resolvedSession;
                }

                if (session != null)
                {
                    Console.WriteLine($"[Unity Match] Updating game session {session.Id} with match_result_id");
                    session.MatchResultId = savedMatch.Id;
                    session.CompletedAt = session.CompletedAt ?? DateTimeOffset.UtcNow;
                    session.Status = "completed";
                    if (savedMatch.UserId.HasValue && !session.UserId.HasValue)
                    {
                        session.UserId = savedMatch.UserId;
                    }
                    await _gameSessionRepository.UpdateAsync(session);
                    Console.WriteLine($"[Unity Match] ✓ Successfully linked match result to game session");
                }
                else
                {
                    Console.WriteLine($"[Unity Match] ⚠️ No game session found to link - neither by GUID nor by user");
                }

                return Ok(new { success = true, matchId = savedMatch.MatchId, sessionId = session?.SessionId });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Unity Match] Failed to save match result: {ex.Message}");
                Console.Error.WriteLine($"[Unity Match] Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to save match result", details = ex.Message });
            }
        }

        [HttpGet("/api/player/{userId}/last-summary")]
        public IActionResult GetLastSummary(string userId)
        {
            try
            {
                var overrideDir = Environment.GetEnvironmentVariable("RESULTS_DIR");
                string baseDir = string.IsNullOrWhiteSpace(overrideDir)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "tmp", "match-results", "players")
                    : Path.Combine(overrideDir, "players");
                if (!Directory.Exists(baseDir)) return NotFound(new { error = "No player summaries" });
                var files = Directory.GetFiles(baseDir, $"player_{userId}_*.json");
                if (files == null || files.Length == 0) return NotFound(new { error = "No summary for player" });
                Array.Sort(files, (a, b) => System.IO.File.GetLastWriteTimeUtc(b).CompareTo(System.IO.File.GetLastWriteTimeUtc(a)));
                var path = files[0];
                var json = System.IO.File.ReadAllText(path, Encoding.UTF8);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DEMO] Failed to read player summary: {ex.Message}");
                return StatusCode(500, new { error = "Failed to read summary" });
            }
        }

        // MVP: Get question pack from database
        // GET /api/quests/game/sessions/{sessionId}/pack
        [HttpGet("{sessionId:guid}/pack")]
        public async Task<IActionResult> GetPack(Guid sessionId)
        {
            try
            {
                var gameSession = await _gameSessionRepository.GetBySessionIdAsync(sessionId);

                if (gameSession == null || string.IsNullOrEmpty(gameSession.QuestionPackJson))
                {
                    return NotFound(new { error = "Pack not found for session" });
                }

                // Return question pack JSON directly
                return Content(gameSession.QuestionPackJson, "application/json");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GameSession] Failed to read pack for session {sessionId}: {ex.Message}");
                return StatusCode(500, new { error = "Failed to read pack" });
            }
        }

        [HttpGet("{sessionId:guid}/result")]
        public IActionResult GetResult(Guid sessionId)
        {
            try
            {
                var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "tmp", "match-results", "results");
                var path = Path.Combine(baseDir, $"result_{sessionId}.json");
                if (System.IO.File.Exists(path))
                {
                    var json = System.IO.File.ReadAllText(path, Encoding.UTF8);
                    return Content(json, "application/json");
                }
                return NotFound(new { error = "Result not found for session" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DEMO] Failed to read result for session {sessionId}: {ex.Message}");
                return StatusCode(500, new { error = "Failed to read result" });
            }
        }

        [HttpGet("{sessionId:guid}/players")]
        public IActionResult GetPlayers(Guid sessionId)
        {
            try
            {
                var overrideDir = Environment.GetEnvironmentVariable("RESULTS_DIR");
                string baseDir = string.IsNullOrWhiteSpace(overrideDir)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "tmp", "match-results", "players")
                    : Path.Combine(overrideDir, "players");
                if (!Directory.Exists(baseDir)) return Ok(Array.Empty<object>());
                var files = Directory.GetFiles(baseDir, $"player_*_{sessionId}.json");
                var list = new List<object>();
                foreach (var f in files)
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    var parts = name.Split('_');
                    if (parts.Length >= 3)
                    {
                        var uid = parts[1];
                        var json = System.IO.File.ReadAllText(f, Encoding.UTF8);
                        list.Add(new { user_id = uid, summary = Newtonsoft.Json.JsonConvert.DeserializeObject(json) });
                    }
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DEMO] Failed to list players for session {sessionId}: {ex.Message}");
                return StatusCode(500, new { error = "Failed to list players" });
            }
        }

        // MVP: Resolve join code to session (from database)
        // GET /api/quests/game/sessions/resolve?code=ABCDEF
        [HttpGet("resolve")]
        public async Task<IActionResult> ResolveByJoinCode([FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { error = "Missing join code" });
            }

            try
            {
                var gameSession = await _gameSessionRepository.GetByJoinCodeAsync(code.Trim());

                if (gameSession != null)
                {
                    var result = new
                    {
                        match_id = gameSession.SessionId.ToString(),
                        pack_url = $"/api/quests/game/sessions/{gameSession.SessionId}/pack"
                    };
                    return Ok(result);
                }

                return NotFound(new { error = "Session not found for join code" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GameSession] Failed to resolve join code '{code}': {ex.Message}");
                return StatusCode(500, new { error = "Failed to resolve join code" });
            }
        }

        // Simple demo pack generator. Uses optional payload.pack_spec to shape the pack.
        // Structure:
        // {
        //   packId: string,
        //   subject: string,
        //   topic: string,
        //   difficulty: string,
        //   questions: [{ id, prompt, options: [..], answerIndex }]
        // }
        private async Task<JsonElement> GeneratePackAsync(Guid sessionId, CreateSessionRequest? request)
        {
            string subject = "demo";
            string topic = "basics";
            string difficulty = "easy";
            int count = 6;

            string? syllabusJson = null;

            try
            {
                var spec = request?.PackSpec;
                if (spec != null)
                {
                    Console.WriteLine($"[GameSession] PackSpec: Subject={spec.Subject}, Topic={spec.Topic}, Difficulty={spec.Difficulty}, Count={spec.Count}");
                    if (!string.IsNullOrWhiteSpace(spec.Subject)) subject = spec.Subject!;
                    if (!string.IsNullOrWhiteSpace(spec.Topic)) topic = spec.Topic!;
                    if (!string.IsNullOrWhiteSpace(spec.Difficulty)) difficulty = spec.Difficulty!;
                    if (spec.Count.HasValue)
                    {
                        try { count = Math.Clamp(spec.Count.Value, 1, 20); } catch { }
                    }

                    Console.WriteLine($"[GameSession] Attempting to load syllabus from database for subject: {spec.Subject}");
                    syllabusJson = await TryReadSyllabusFromDbAsync(spec.Subject);
                    if (string.IsNullOrWhiteSpace(syllabusJson))
                    {
                        Console.WriteLine($"[GameSession] No syllabus found in database for subject: {spec.Subject}");
                    }
                    else
                    {
                        Console.WriteLine($"[GameSession] ✅ Loaded syllabus from database, length: {syllabusJson.Length} chars");
                    }
                }
                else
                {
                    Console.WriteLine($"[GameSession] ⚠️ PackSpec is null, using default subject/topic");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GameSession] ❌ Error loading syllabus: {ex.Message}");
            }

            if (!string.IsNullOrWhiteSpace(syllabusJson))
            {
                int attempts = 0;
                int maxAttempts = 3;
                int delayMs = 1000;
                while (attempts < maxAttempts)
                {
                    try
                    {
                        Console.WriteLine($"[GameSession] Attempting to generate question pack with Gemini (attempt {attempts + 1}/{maxAttempts})...");
                        var prompt = BuildQuestionPackPrompt(syllabusJson!, subject, topic, difficulty, count);
                        var result = await _kernel.InvokePromptAsync(prompt);
                        var raw = result.GetValue<string>() ?? string.Empty;
                        Console.WriteLine($"[GameSession] Gemini response length: {raw.Length} characters");
                        var cleaned = CleanToJson(raw);
                        using var doc = JsonDocument.Parse(cleaned);
                        var root = doc.RootElement.Clone();
                        if (ValidatePackJson(root))
                        {
                            Console.WriteLine($"[GameSession] ✅ Successfully generated question pack with Gemini");
                            return root;
                        }
                        Console.WriteLine($"[GameSession] ⚠️ Pack validation failed, retrying...");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[GameSession] ❌ Gemini API error (attempt {attempts + 1}/{maxAttempts}): {ex.GetType().Name} - {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.Error.WriteLine($"[GameSession] Inner exception: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                        }
                    }

                    attempts++;
                    if (attempts < maxAttempts)
                    {
                        Console.WriteLine($"[GameSession] Waiting {delayMs}ms before retry...");
                        await Task.Delay(delayMs);
                        delayMs = Math.Min(delayMs * 2, 8000);
                    }
                }
                Console.WriteLine($"[GameSession] ⚠️ All {maxAttempts} attempts failed, falling back to simple questions");
            }

            var questions = new List<object>();
            for (int i = 1; i <= count; i++)
            {
                var a = i;
                var b = i;
                var correct = a + b;
                questions.Add(new
                {
                    id = $"q{i}",
                    prompt = $"{a}+{b}=?",
                    options = new[] { (a).ToString(), (correct).ToString(), (a + 1).ToString(), (correct + 2).ToString() },
                    answerIndex = 1
                });
            }

            var packObj = new
            {
                packId = $"pack_{sessionId}",
                subject,
                topic,
                difficulty,
                questions = questions
            };

            var jsonString = System.Text.Json.JsonSerializer.Serialize(packObj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            using var fallbackDoc = JsonDocument.Parse(jsonString);
            return fallbackDoc.RootElement.Clone();
        }

        private static string BuildQuestionPackPrompt(string syllabusJson, string subject, string topic, string difficulty, int count)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Generate a JSON object for a boss-fight question pack.\n");
            sb.Append("Use the syllabus to craft questions that test SUBJECT KNOWLEDGE, CONCEPTS, and PRACTICAL APPLICATION only.\n");
            sb.Append("Focus on: core theories, fundamental concepts, technical skills, problem-solving, analysis, and application of learned material.\n");
            sb.Append("EXCLUDE: administrative tasks (group formation, topic selection, requirement gathering), project logistics, organizational activities, and non-academic processes.\n");
            sb.Append("Base questions on the MappedSkills, learning outcomes, and academic content from the syllabus.\n");
            sb.Append("Questions should assess understanding, comprehension, application, and analysis of the subject matter.\n");
            sb.Append("Return only valid JSON with no markdown formatting or code blocks.\n");
            sb.Append("Schema: { packId: string, subject: string, topic: string, difficulty: string, questions: [ { id: string, prompt: string, options: [string], answerIndex: number, timeLimitSec?: number, explanation?: string, topic?: string, difficulty?: string } ] }\n");
            sb.Append("Constraints: questions must be unambiguous, options 4-5 items, one correct answerIndex, mix of difficulties if provided, avoid code or content that cannot be rendered as text.\n");
            sb.Append("\nIMPORTANT: Each question MUST have a 'topic' field with a detailed, specific topic name from the syllabus.\n");
            sb.Append("The topic should be descriptive and specific (e.g., 'Object-Oriented Programming Principles', 'Database Normalization Forms', 'Network Protocol Layers').\n");
            sb.Append("DO NOT use generic topics like 'basics' or just the subject code. Use the actual curriculum topics from the syllabus.\n");
            sb.Append("Different questions should cover different specific topics to provide detailed learning analytics.\n");
            sb.Append("Subject: "); sb.Append(subject); sb.Append("\n");
            sb.Append("Topic: "); sb.Append(topic); sb.Append("\n");
            sb.Append("Difficulty: "); sb.Append(difficulty); sb.Append("\n");
            sb.Append("Count: "); sb.Append(count.ToString()); sb.Append("\n");
            sb.Append("Syllabus JSON:\n");
            sb.Append(syllabusJson);
            return sb.ToString();
        }

        private static string CleanToJson(string raw)
        {
            var s = raw.Trim();
            if (s.StartsWith("```"))
            {
                var idx = s.IndexOf('\n');
                if (idx > -1) s = s[(idx + 1)..];
            }
            if (s.EndsWith("```") && s.Length >= 3)
            {
                var last = s.LastIndexOf("```", StringComparison.Ordinal);
                if (last > -1) s = s[..last];
            }
            var start = s.IndexOf('{');
            var end = s.LastIndexOf('}');
            if (start >= 0 && end > start) s = s.Substring(start, end - start + 1);
            return s.Trim();
        }

        private async Task<string?> TryReadSyllabusFromDbAsync(string? subjectCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subjectCode)) return null;
                var subj = await _subjectRepository.GetByCodeAsync(subjectCode.Trim().ToUpperInvariant());
                if (subj == null) return null;
                var contentVal = subj.Content;
                if (contentVal == null) return null;
                var token = Newtonsoft.Json.Linq.JToken.FromObject(contentVal);
                var json = token.ToString(Newtonsoft.Json.Formatting.None);
                return string.IsNullOrWhiteSpace(json) ? null : json;
            }
            catch { return null; }
        }

        private static bool ValidatePackJson(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object) return false;
            if (!root.TryGetProperty("packId", out var _) || !root.TryGetProperty("questions", out var qs)) return false;
            if (qs.ValueKind != JsonValueKind.Array) return false;
            return true;
        }

        private static bool QuestionsMissing(JsonObject matchNode)
        {
            if (!matchNode.TryGetPropertyValue("questions", out var questionsNode)) return true;
            if (questionsNode is JsonArray arr) return arr.Count == 0;
            return true;
        }

        private static string MergeQuestionPackIntoMatchData(string? matchDataJson, string? questionPackJson, Guid sessionId)
        {
            var matchNode = JsonNode.Parse(string.IsNullOrWhiteSpace(matchDataJson) ? "{}" : matchDataJson) as JsonObject ?? new JsonObject();
            matchNode["sessionId"] = sessionId.ToString();

            if (!string.IsNullOrWhiteSpace(questionPackJson))
            {
                var packNode = JsonNode.Parse(questionPackJson);
                if (packNode is JsonObject packObj)
                {
                    if (matchNode["questionPack"] == null)
                    {
                        matchNode["questionPack"] = packObj.DeepClone();
                    }

                    if (QuestionsMissing(matchNode) && packObj["questions"] is JsonArray qArr)
                    {
                        matchNode["questions"] = qArr.DeepClone();
                    }
                }
            }

            return matchNode.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }

        private async Task<RogueLearn.User.Domain.Entities.GameSession?> ResolveSessionForMatchAsync(string? matchId, Guid? userId, string? joinCode, DateTime endUtc)
        {
            RogueLearn.User.Domain.Entities.GameSession? session = null;

            if (Guid.TryParse(matchId, out var sessionGuid))
            {
                session = await _gameSessionRepository.GetBySessionIdAsync(sessionGuid);
                if (session != null) return session;
            }

            if (!string.IsNullOrWhiteSpace(joinCode))
            {
                session = await _gameSessionRepository.GetByJoinCodeAsync(joinCode.Trim());
                if (session != null) return session;
            }

            if (userId.HasValue)
            {
                var recentSessions = await _gameSessionRepository.GetRecentSessionsByUserAsync(userId.Value, 20);
                var targetTime = endUtc == default ? DateTime.UtcNow : endUtc.ToUniversalTime();
                session = recentSessions
                    .OrderBy(s =>
                    {
                        var pivot = (s.CompletedAt ?? s.CreatedAt).UtcDateTime;
                        return Math.Abs((pivot - targetTime).TotalMinutes);
                    })
                    .FirstOrDefault();
            }

            return session;
        }

        // MVP: Get Unity match results from database
        // GET /api/quests/game/sessions/unity-matches?limit=10&userId=xxx
        [HttpGet("unity-matches")]
        public async Task<IActionResult> GetUnityMatches([FromQuery] int limit = 10, [FromQuery] string? userId = null)
        {
            try
            {
                // Parse userId if provided
                Guid? userIdGuid = null;
                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var parsedUserId))
                {
                    userIdGuid = parsedUserId;
                }

                // Get matches (filtered by user if userId provided)
                var matchResults = userIdGuid.HasValue
                    ? await _matchResultRepository.GetMatchesByUserAsync(userIdGuid.Value, limit)
                    : await _matchResultRepository.GetRecentMatchesAsync(limit);

                var matchesJson = new List<string>();
                foreach (var m in matchResults.Where(m => !string.IsNullOrWhiteSpace(m.MatchDataJson)))
                {
                    var matchData = m.MatchDataJson!;
                    bool needsQuestions = false;

                    try
                    {
                        var node = JsonNode.Parse(matchData) as JsonObject;
                        needsQuestions = node == null || QuestionsMissing(node) || node["questionPack"] == null;
                    }
                    catch
                    {
                        needsQuestions = true;
                    }

                    if (needsQuestions)
                    {
                        RogueLearn.User.Domain.Entities.GameSession? session = null;

                        // Prefer explicit linkage
                        if (m.Id != Guid.Empty)
                        {
                            session = await _gameSessionRepository.GetByMatchResultIdAsync(m.Id);
                        }

                        // Fallback: matchId as sessionId
                        if (session == null && Guid.TryParse(m.MatchId, out var sessionGuid))
                        {
                            session = await _gameSessionRepository.GetBySessionIdAsync(sessionGuid);
                        }

                        // Fallback: most recent session for this user
                        if (session == null && m.UserId.HasValue)
                        {
                            var recentSessions = await _gameSessionRepository.GetRecentSessionsByUserAsync(m.UserId.Value, 10);
                            session = recentSessions.FirstOrDefault();
                        }

                        if (session != null && !string.IsNullOrWhiteSpace(session.QuestionPackJson))
                        {
                            matchData = MergeQuestionPackIntoMatchData(matchData, session.QuestionPackJson, session.SessionId);
                        }
                    }

                    matchesJson.Add(matchData);
                }

                // Build response JSON manually
                var responseJson = "{\"matches\":[" + string.Join(",", matchesJson) + "]}";

                return Content(responseJson, "application/json");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DEMO] Failed to read Unity matches from database: {ex.Message}");
                return StatusCode(500, new { error = "Failed to read matches" });
            }
        }

        // MVP: Get specific Unity match by ID
        // GET /api/quests/game/sessions/unity-matches/{matchId}
        [HttpGet("unity-matches/{matchId}")]
        public IActionResult GetUnityMatch(string matchId)
        {
            try
            {
                var resultsRoot = Environment.GetEnvironmentVariable("RESULTS_LOG_ROOT") ?? "/var/log/unity/matches";
                if (!Directory.Exists(resultsRoot))
                {
                    return NotFound(new { error = "Match not found" });
                }

                // Search all date directories for the match file
                foreach (var dateDir in Directory.GetDirectories(resultsRoot))
                {
                    var matchFiles = Directory.GetFiles(dateDir, $"match_*_{matchId}.json");
                    if (matchFiles.Length > 0)
                    {
                        var json = System.IO.File.ReadAllText(matchFiles[0], Encoding.UTF8);
                        return Content(json, "application/json");
                    }
                }

                return NotFound(new { error = "Match not found" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DEMO] Failed to read Unity match {matchId}: {ex.Message}");
                return StatusCode(500, new { error = "Failed to read match" });
            }
        }
    }
}
