using MediatR;
using Microsoft.SemanticKernel;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;

namespace RogueLearn.User.Application.Features.GameSessions.Commands.GenerateQuestionPack
{
    public record GenerateQuestionPackCommand(
        Guid SessionId,
        string? Subject,
        string? Topic,
        string? Difficulty,
        int? Count) : IRequest<GenerateQuestionPackResult>;

    public record GenerateQuestionPackResult(
        string PackId,
        string Subject,
        string Topic,
        string Difficulty,
        string QuestionPackJson);

    public class GenerateQuestionPackCommandHandler : IRequestHandler<GenerateQuestionPackCommand, GenerateQuestionPackResult>
    {
        private readonly Kernel _kernel;
        private readonly ISubjectRepository _subjectRepository;
        private readonly IQuestStepRepository _questStepRepository;

        public GenerateQuestionPackCommandHandler(
            Kernel kernel,
            ISubjectRepository subjectRepository,
            IQuestStepRepository questStepRepository)
        {
            _kernel = kernel;
            _subjectRepository = subjectRepository;
            _questStepRepository = questStepRepository;
        }

        public async Task<GenerateQuestionPackResult> Handle(GenerateQuestionPackCommand request, CancellationToken cancellationToken)
        {
            var subject = string.IsNullOrWhiteSpace(request.Subject) ? "demo" : request.Subject!;
            var topic = string.IsNullOrWhiteSpace(request.Topic) ? "basics" : request.Topic!;
            var difficulty = string.IsNullOrWhiteSpace(request.Difficulty) ? "easy" : request.Difficulty!;
            var count = request.Count.HasValue ? Math.Clamp(request.Count.Value, 1, 20) : 6;

            var syllabusJson = await TryReadSyllabusFromDbAsync(subject);

            var packJson = await TryGenerateWithKernelAsync(syllabusJson, subject, topic, difficulty, count, cancellationToken)
                            ?? await BuildPackFromQuestStepsAsync(request.SessionId, subject, topic, difficulty, count, cancellationToken);

            var packId = ExtractPackId(packJson, request.SessionId.ToString());

            return new GenerateQuestionPackResult(
                packId,
                subject,
                topic,
                difficulty,
                packJson);
        }

        private async Task<string?> TryReadSyllabusFromDbAsync(string? subjectCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subjectCode)) return null;
                var subj = await _subjectRepository.GetByCodeAsync(subjectCode.Trim().ToUpperInvariant());
                if (subj?.Content == null) return null;
                var token = Newtonsoft.Json.Linq.JToken.FromObject(subj.Content);
                var json = token.ToString(Newtonsoft.Json.Formatting.None);
                return string.IsNullOrWhiteSpace(json) ? null : json;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> TryGenerateWithKernelAsync(string? syllabusJson, string subject, string topic, string difficulty, int count, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(syllabusJson)) return null;

            int attempts = 0;
            int maxAttempts = 3;
            int delayMs = 1000;
            while (attempts < maxAttempts)
            {
                try
                {
                    var prompt = BuildQuestionPackPrompt(syllabusJson!, subject, topic, difficulty, count);
                    var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
                    var raw = result.GetValue<string>() ?? string.Empty;
                    var cleaned = CleanToJson(raw);
                    using var doc = JsonDocument.Parse(cleaned);
                    var root = doc.RootElement.Clone();
                    if (ValidatePackJson(root))
                    {
                        return root.GetRawText();
                    }
                }
                catch
                {
                    // ignore and retry
                }

                attempts++;
                if (attempts < maxAttempts)
                {
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs = Math.Min(delayMs * 2, 8000);
                }
            }

            return null;
        }

        private async Task<string> BuildPackFromQuestStepsAsync(Guid sessionId, string subject, string topic, string difficulty, int count, CancellationToken cancellationToken)
        {
            var questions = new List<object>();
            try
            {
                var steps = await _questStepRepository.GetPagedAsync(1, 25);
                foreach (var step in steps.OrderByDescending(s => s.CreatedAt))
                {
                    if (step.Content == null) continue;
                    var questionsFromStep = ExtractQuestionsFromStepContent(step.Content);
                    foreach (var q in questionsFromStep)
                    {
                        if (questions.Count >= count) break;
                        questions.Add(q);
                    }
                    if (questions.Count >= count) break;
                }
            }
            catch
            {
                // ignore fallback errors
            }

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

            return JsonSerializer.Serialize(packObj, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }

        private static IEnumerable<object> ExtractQuestionsFromStepContent(object content)
        {
            var list = new List<object>();
            try
            {
                string jsonString = content is string s ? s : Newtonsoft.Json.JsonConvert.SerializeObject(content);

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

        private static string ExtractPackId(JsonElement packJson, string fallback)
        {
            try
            {
                if (packJson.ValueKind == JsonValueKind.Object && packJson.TryGetProperty("packId", out var idProp))
                {
                    var id = idProp.GetString();
                    if (!string.IsNullOrWhiteSpace(id)) return id!;
                }
            }
            catch { }
            return fallback;
        }

        private static string ExtractPackId(string packJson, string fallback)
        {
            try
            {
                using var doc = JsonDocument.Parse(packJson);
                return ExtractPackId(doc.RootElement, fallback);
            }
            catch
            {
                return fallback;
            }
        }

        private static string BuildQuestionPackPrompt(string syllabusJson, string subject, string topic, string difficulty, int count)
        {
            var sb = new StringBuilder();
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
            sb.Append("Subject: ").Append(subject).Append('\n');
            sb.Append("Topic: ").Append(topic).Append('\n');
            sb.Append("Difficulty: ").Append(difficulty).Append('\n');
            sb.Append("Count: ").Append(count.ToString()).Append('\n');
            sb.Append("Syllabus JSON:\n").Append(syllabusJson);
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

        private static bool ValidatePackJson(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object) return false;
            if (!root.TryGetProperty("packId", out var _) || !root.TryGetProperty("questions", out var qs)) return false;
            if (qs.ValueKind != JsonValueKind.Array) return false;
            return true;
        }
    }
}
