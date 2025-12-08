using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.SemanticKernel;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Linq;
using RogueLearn.User.Application.Features.GameSessions.Commands.CompleteGameSession;
using RogueLearn.User.Application.Features.UnityMatches.Commands.SubmitUnityMatchResult;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Api.Controllers
{
    [ApiController]
    [Route("api/quests/game/sessions")]
    // For demo flow, allow anonymous posts with optional API key; tighten later if needed.
    [AllowAnonymous]
    public class GameSessionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly Microsoft.SemanticKernel.Kernel _kernel;
        private readonly RogueLearn.User.Domain.Interfaces.ISubjectRepository _subjectRepository;
        private readonly RogueLearn.User.Domain.Interfaces.IMatchResultRepository _matchResultRepository;
        private readonly RogueLearn.User.Domain.Interfaces.IGameSessionRepository _gameSessionRepository;
        private readonly RogueLearn.User.Domain.Interfaces.IMatchPlayerSummaryRepository _matchPlayerSummaryRepository;
        private readonly RogueLearn.User.Domain.Interfaces.IQuestStepRepository _questStepRepository;

        // MVP: All session data now stored in database for production scalability
        private static readonly Regex JoinCodeRegex = new Regex(@"Relay\s+Join\s+Code\s*:\s*([A-Z0-9]{6,12})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public GameSessionsController(
            IMediator mediator,
            Microsoft.SemanticKernel.Kernel kernel,
            RogueLearn.User.Domain.Interfaces.ISubjectRepository subjectRepository,
            RogueLearn.User.Domain.Interfaces.IMatchResultRepository matchResultRepository,
            RogueLearn.User.Domain.Interfaces.IGameSessionRepository gameSessionRepository,
            RogueLearn.User.Domain.Interfaces.IMatchPlayerSummaryRepository matchPlayerSummaryRepository,
            RogueLearn.User.Domain.Interfaces.IQuestStepRepository questStepRepository)
        {
            _mediator = mediator;
            _kernel = kernel;
            _subjectRepository = subjectRepository;
            _matchResultRepository = matchResultRepository;
            _gameSessionRepository = gameSessionRepository;
            _matchPlayerSummaryRepository = matchPlayerSummaryRepository;
            _questStepRepository = questStepRepository;
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

        // MVP: Get player summaries from database
        // GET /api/quests/game/sessions/{sessionId}/players
        [HttpGet("{sessionId:guid}/players")]
        public async Task<IActionResult> GetPlayers(Guid sessionId)
        {
            try
            {
                var summaries = await _matchPlayerSummaryRepository.GetBySessionIdAsync(sessionId);

                if (summaries.Count == 0)
                {
                    var session = await _gameSessionRepository.GetBySessionIdAsync(sessionId);
                    if (session?.MatchResultId != null)
                    {
                        summaries = await _matchPlayerSummaryRepository.GetByMatchResultIdAsync(session.MatchResultId.Value);
                    }
                }

                var list = summaries.Select(s =>
                {
                    object? topics = null;
                    if (!string.IsNullOrWhiteSpace(s.TopicBreakdownJson))
                    {
                        try { topics = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(s.TopicBreakdownJson!); }
                        catch { topics = s.TopicBreakdownJson; }
                    }

                    return new
                    {
                        id = s.Id,
                        user_id = s.UserId,
                        client_id = s.ClientId,
                        total_questions = s.TotalQuestions,
                        correct_answers = s.CorrectAnswers,
                        average_time = s.AverageTime,
                        topic_breakdown = topics,
                        created_at = s.CreatedAt
                    };
                });

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

        // MVP: Generate pack for session (using optional PackSpec)
        // POST /api/quests/game/sessions/{sessionId}/pack
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
            // Fallback 1: try to pull from quest steps (knowledge check / quiz)
            questions.AddRange(await TryBuildPackFromQuestStepsAsync(count));
            if (questions.Count == 0)
            {
                Console.WriteLine($"[GameSession] ⚠️ No quest steps found for subject: {subject}, topic: {topic}, difficulty: {difficulty}");
            }
            // Fallback 2: if still short, use simple arithmetic
            var remaining = count - questions.Count;
            for (int i = 1; i <= remaining; i++)
            {
                var a = i;
                var b = i;
                var correct = a + b;
                questions.Add(new
                {
                    id = $"fallback_q{i}",
                    prompt = $"{a}+{b}=?",
                    options = new[] { (a).ToString(), (correct).ToString(), (a + 1).ToString(), (correct + 2).ToString() },
                    answerIndex = 1,
                    topic = "basic arithmetic"
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

        // Prompt template for question pack generation
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

        // Fallback: build questions from quest steps (knowledge_check / quiz activities)
        private async Task<List<object>> TryBuildPackFromQuestStepsAsync(int count)
        {
            var results = new List<object>();
            try
            {
                // Grab recent quest steps; small page to avoid large scans
                var steps = await _questStepRepository.GetPagedAsync(1, 25);
                foreach (var step in steps.OrderByDescending(s => s.CreatedAt))
                {
                    if (step.Content == null) continue;
                    var questionsFromStep = ExtractQuestionsFromStepContent(step.Content);
                    foreach (var q in questionsFromStep)
                    {
                        if (results.Count >= count) break;
                        results.Add(q);
                    }
                    if (results.Count >= count) break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GameSession] Fallback quest step extraction failed: {ex.Message}");
            }

            return results;
        }

        private static IEnumerable<object> ExtractQuestionsFromStepContent(object content)
        {
            var list = new List<object>();
            try
            {
                string jsonString;
                if (content is string s)
                {
                    jsonString = s;
                }
                else
                {
                    jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(content);
                }

                using var doc = JsonDocument.Parse(jsonString);
                if (doc.RootElement.TryGetProperty("activities", out var activities) && activities.ValueKind == JsonValueKind.Array)
                {
                    foreach (var act in activities.EnumerateArray())
                    {
                        if (act.ValueKind != JsonValueKind.Object) continue;
                        var type = act.TryGetProperty("type", out var t) ? t.GetString() : null;
                        if (string.IsNullOrWhiteSpace(type)) continue;
                        var normalizedType = type.ToLowerInvariant();
                        if (normalizedType != "knowledgecheck" && normalizedType != "quiz") continue;

                        if (act.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Object)
                        {
                            // If payload has an inner "questions" array, expand it
                            if (payload.TryGetProperty("questions", out var qArr) && qArr.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var q in qArr.EnumerateArray())
                                {
                                    var question = BuildQuestionFromPayload(q, payload);
                                    if (question != null) list.Add(question);
                                }
                            }
                            else
                            {
                                var question = BuildQuestionFromPayload(payload, payload);
                                if (question != null) list.Add(question);
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore bad content
            }

            return list;
        }

        private static object? BuildQuestionFromPayload(JsonElement payload, JsonElement parentPayload)
        {
            try
            {
                string? prompt = null;
                if (payload.TryGetProperty("prompt", out var p)) prompt = p.GetString();
                else if (payload.TryGetProperty("question", out var q)) prompt = q.GetString();
                else if (payload.TryGetProperty("text", out var t)) prompt = t.GetString();

                if (string.IsNullOrWhiteSpace(prompt)) return null;

                string[]? options = null;
                if (payload.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                {
                    options = opts.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString() ?? string.Empty)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();
                }

                if (options == null || options.Length < 2) return null;

                int answerIndex = -1;
                if (payload.TryGetProperty("answerIndex", out var ai) && ai.TryGetInt32(out var aii))
                {
                    answerIndex = aii;
                }
                else if (payload.TryGetProperty("correctOption", out var co) && co.ValueKind == JsonValueKind.Number && co.TryGetInt32(out var coi))
                {
                    answerIndex = coi;
                }
                else if (payload.TryGetProperty("answer", out var ans))
                {
                    var ansStr = ans.ToString();
                    if (!string.IsNullOrWhiteSpace(ansStr))
                    {
                        var idx = Array.FindIndex(options, o => string.Equals(o, ansStr, StringComparison.OrdinalIgnoreCase));
                        if (idx >= 0) answerIndex = idx;
                    }
                }

                if (answerIndex < 0 || answerIndex >= options.Length) answerIndex = 0;

                string? topic = null;
                if (payload.TryGetProperty("topic", out var tp)) topic = tp.GetString();
                if (string.IsNullOrWhiteSpace(topic) && parentPayload.TryGetProperty("topic", out var tp2)) topic = tp2.GetString();

                return new
                {
                    id = payload.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                    prompt,
                    options,
                    answerIndex,
                    topic = string.IsNullOrWhiteSpace(topic) ? "knowledge check" : topic
                };
            }
            catch
            {
                return null;
            }
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

        // MVP: Get Unity match results from database
        // GET /api/quests/game/sessions/unity-matches?limit=10&userId=xxx
        [HttpGet("unity-matches")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
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

                List<MatchResult> matchResults;
                if (userIdGuid.HasValue)
                {
                    // Prefer per-player linkage (match_player_summaries) so non-host players get their matches
                    var matchResultIds = await _matchPlayerSummaryRepository.GetRecentMatchResultIdsByUserAsync(userIdGuid.Value, limit);
                    var list = new List<MatchResult>();
                    foreach (var id in matchResultIds)
                    {
                        var mr = await _matchResultRepository.GetByIdAsync(id);
                        if (mr != null) list.Add(mr);
                    }

                    // Fallback to host-linked matches if no per-player records exist
                    if (list.Count == 0)
                    {
                        list = await _matchResultRepository.GetMatchesByUserAsync(userIdGuid.Value, limit);
                    }

                    matchResults = list
                        .OrderByDescending(m => m.StartUtc)
                        .Take(limit)
                        .ToList();
                    Console.WriteLine($"[GameSession] ℹ️ Found {list.Count} matches for user {userIdGuid}");
                }
                else
                {
                    matchResults = await _matchResultRepository.GetRecentMatchesAsync(limit);
                    Console.WriteLine($"[GameSession] ℹ️ Found {matchResults.Count} recent matches");
                }

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
                Console.WriteLine($"[GameSession] ℹ️ Returning {matchesJson.Count} matches for user {userIdGuid}");
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

        // Host: start Unity headless via Docker and return join code
        // POST /api/quests/game/sessions/host
        [HttpPost("host")]
        [Consumes("application/json")]
        [AllowAnonymous] // tighten with auth when ready
        public async Task<IActionResult> StartHost([FromBody] HostRequest? request, CancellationToken cancellationToken)
        {
            var image = Env("RL_DOCKER_IMAGE", "roguelearn-server:latest");
            var baseName = Env("RL_DOCKER_CONTAINER", string.Empty);
            var name = string.IsNullOrWhiteSpace(baseName)
                ? $"roguelearn-server-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
                : $"{baseName}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            var envs = new List<string>
            {
                $"UNITY_SERVER_SCENE={Env("UNITY_SERVER_SCENE", "HostUI")}",
                $"RELAY_REGION={Env("RELAY_REGION", "us-central")}",
                $"RL_MAX_CONNECTIONS={Env("RL_MAX_CONNECTIONS", "20")}"
            };

            var userApiBase = Env("USER_API_BASE", Env("RL_DOCKER_USER_API_BASE", string.Empty));
            if (!string.IsNullOrWhiteSpace(userApiBase))
            {
                envs.Add($"USER_API_BASE={userApiBase.TrimEnd('/')}");
            }
            envs.Add($"INSECURE_TLS={Env("INSECURE_TLS", "0")}");

            if (!string.IsNullOrWhiteSpace(request?.UserId))
            {
                envs.Add($"USER_ID={request.UserId}");
            }

            var runArgs = new List<string> { "run", "--rm", "--name", name, "-d" };
            var portHost = Env("RL_DOCKER_PORT_HOST", string.Empty);
            var portContainer = Env("RL_DOCKER_PORT_CONTAINER", "8080");
            if (!string.IsNullOrWhiteSpace(portHost))
            {
                runArgs.AddRange(new[] { "-p", $"{portHost}:{portContainer}" });
            }

            AddIfSet(runArgs, "--cpus", Env("RL_DOCKER_CPUS", string.Empty));
            AddIfSet(runArgs, "--cpuset-cpus", Env("RL_DOCKER_CPUSET", string.Empty));
            AddIfSet(runArgs, "-m", Env("RL_DOCKER_MEMORY", string.Empty));
            AddIfSet(runArgs, "--cpu-shares", Env("RL_DOCKER_CPU_SHARES", string.Empty));

            var extraArgs = Env("RL_DOCKER_EXTRA_ARGS", string.Empty);
            if (!string.IsNullOrWhiteSpace(extraArgs))
            {
                runArgs.AddRange(extraArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            var envPort = Env("RL_DOCKER_ENV_PORT", portContainer);
            if (!string.IsNullOrWhiteSpace(envPort))
            {
                envs.Add($"PORT={envPort}");
            }

            var extraEnvs = Env("RL_DOCKER_EXTRA_ENVS", string.Empty);
            if (!string.IsNullOrWhiteSpace(extraEnvs))
            {
                envs.AddRange(extraEnvs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            foreach (var e in envs)
            {
                runArgs.AddRange(new[] { "-e", e });
            }

            if (OperatingSystem.IsLinux())
            {
                runArgs.Add("--add-host=host.docker.internal:host-gateway");
            }

            runArgs.Add(image!);

            try
            {
                var runResult = await RunProcessAsync("docker", runArgs, TimeSpan.FromSeconds(30), cancellationToken);
                if (runResult.exitCode != 0)
                {
                    Console.Error.WriteLine($"[Host] docker run failed: {runResult.stderr}");
                    return StatusCode(500, new { ok = false, error = $"docker run failed: {runResult.stderr}" });
                }

                var timeoutMs = int.TryParse(Env("RL_LOG_TIMEOUT_MS", "20000"), out var ms) ? ms : 20000;
                var joinData = await ReadJoinCodeFromLogs(name, TimeSpan.FromMilliseconds(timeoutMs), cancellationToken);

                if (string.IsNullOrWhiteSpace(joinData.joinCode))
                {
                    await ForceRemoveContainer(name);
                    return StatusCode(500, new { ok = false, error = "Failed to obtain join code from container logs" });
                }

                return Ok(new
                {
                    ok = true,
                    joinCode = joinData.joinCode,
                    hostId = name,
                    message = $"Unity headless server started in Docker ({image}).",
                    raw = joinData.rawLine,
                    wsUrl = Env("NEXT_PUBLIC_GAME_WS_URL", null)
                });
            }
            catch (OperationCanceledException)
            {
                await ForceRemoveContainer(name);
                return StatusCode(504, new { ok = false, error = "Timed out starting host" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Host] Failed to start Unity host container: {ex}");
                await ForceRemoveContainer(name);
                return StatusCode(500, new { ok = false, error = ex.Message });
            }
        }

        public class HostRequest
        {
            [JsonPropertyName("userId"), JsonProperty("userId")] public string? UserId { get; set; }
        }

        private static string? Env(string key, string? defaultValue) =>
            Environment.GetEnvironmentVariable(key) ?? defaultValue;

        private static void AddIfSet(List<string> args, string flag, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                args.AddRange(new[] { flag, value });
            }
        }

        private static async Task<(string? joinCode, string? rawLine)> ReadJoinCodeFromLogs(
            string containerName,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var tcs = new TaskCompletionSource<(string?, string?)>(TaskCreationOptions.RunContinuationsAsynchronously);

            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"logs -f {containerName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            proc.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                TryParseLine(e.Data);
            };
            proc.ErrorDataReceived += (_, _) => { };
            proc.Exited += (_, __) =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetException(new InvalidOperationException("docker logs exited before join code was found"));
                }
            };

            void TryParseLine(string line)
            {
                try
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("{"))
                    {
                        using var doc = JsonDocument.Parse(trimmed);
                        if (doc.RootElement.TryGetProperty("event", out var ev) &&
                            ev.GetString()?.Equals("relay_join_code", StringComparison.OrdinalIgnoreCase) == true &&
                            doc.RootElement.TryGetProperty("joinCode", out var jc))
                        {
                            tcs.TrySetResult((jc.GetString(), line));
                            return;
                        }
                    }

                    var m = JoinCodeRegex.Match(line);
                    if (m.Success)
                    {
                        tcs.TrySetResult((m.Groups[1].Value, line));
                    }
                }
                catch
                {
                    // ignore parse errors
                }
            }

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            try
            {
                var result = await tcs.Task.WaitAsync(timeout, cts.Token);
                return result;
            }
            finally
            {
                try { if (!proc.HasExited) proc.Kill(true); } catch { }
                proc.Dispose();
            }
        }

        private static async Task<(int exitCode, string stdout, string stderr)> RunProcessAsync(
            string fileName,
            IEnumerable<string> args,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            // Resolve a working directory that actually exists to avoid Process.Start failures.
            var cwd = AppContext.BaseDirectory;
            if (string.IsNullOrWhiteSpace(cwd) || !Directory.Exists(cwd))
            {
                cwd = "/home/ubuntu/roguelearn";
                if (!Directory.Exists(cwd))
                {
                    cwd = "/";
                }
            }

            // If docker isn't on PATH for the service user, fall back to the usual Linux location.
            if (fileName.Equals("docker", StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists("/usr/bin/docker"))
            {
                fileName = "/usr/bin/docker";
            }

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = cwd
            };

            using var proc = new Process { StartInfo = psi };
            var stdout = new List<string>();
            var stderr = new List<string>();

            proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.Add(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.Add(e.Data); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            await proc.WaitForExitAsync(cts.Token);

            return (proc.ExitCode, string.Join("\n", stdout), string.Join("\n", stderr));
        }

        private static async Task ForceRemoveContainer(string name)
        {
            try
            {
                await RunProcessAsync("docker", new[] { "rm", "-f", name }, TimeSpan.FromSeconds(10), CancellationToken.None);
            }
            catch
            {
                // best effort
            }
        }
    }
}

