using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RogueLearn.User.Application.Models;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RogueLearn.User.Application.Services;

public interface IAiQueryClassificationService
{
    Task<SubjectCategory> ClassifySubjectAsync(string subjectName, string subjectCode, string description, CancellationToken cancellationToken);
    Task<string> GenerateSearchQueryAsync(string topic, string subjectContext, SubjectCategory category, CancellationToken cancellationToken);
    Task<List<string>> GenerateQueryVariantsAsync(string topic, string subjectContext, SubjectCategory category, CancellationToken cancellationToken);
    Task<Dictionary<int, List<string>>> GenerateBatchQueryVariantsAsync(
        List<SyllabusSessionDto> sessions,
        string subjectContext,
        SubjectCategory category,
        List<string> technologyKeywords,
        CancellationToken cancellationToken);
}


public interface IMemoryStore
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);
    Task SetAsync(string key, string value, CancellationToken cancellationToken);
}

public class InMemoryStore : IMemoryStore
{
    private readonly Dictionary<string, (string Value, DateTimeOffset StoredAt)> _store = new();
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        return Task.FromResult(_store.TryGetValue(key, out var v) ? v.Value : null);
    }
    public Task SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        _store[key] = (value, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}

public class AiQueryClassificationService : IAiQueryClassificationService
{
    private readonly Kernel _kernel;
    private readonly ILogger<AiQueryClassificationService> _logger;
    private readonly IMemoryStore _memoryStore;
    private const int MaxRetries = 3;
    private const int BaseDelayMs = 1000;

    public AiQueryClassificationService(Kernel kernel, ILogger<AiQueryClassificationService> logger, IMemoryStore memoryStore)
    {
        _kernel = kernel;
        _logger = logger;
        _memoryStore = memoryStore;
    }

    public async Task<SubjectCategory> ClassifySubjectAsync(string subjectName, string subjectCode, string description, CancellationToken cancellationToken)
    {
        var cacheKey = $"category_{subjectCode}";
        var cached = await _memoryStore.GetAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached) && Enum.TryParse<SubjectCategory>(cached, out var cachedCat))
        {
            _logger.LogInformation("Cache hit for category classification: {SubjectCode}", subjectCode);
            return cachedCat;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Analyze the following subject information and classify it into ONE of these categories:");
        sb.AppendLine();
        sb.AppendLine("Categories:");
        sb.AppendLine("- VietnamesePolitics (Vietnamese political theory, Marxism-Leninism, Ho Chi Minh thought)");
        sb.AppendLine("- History (Historical events, timelines, wars, civilizations)");
        sb.AppendLine("- VietnameseLiterature (Vietnamese language, literature, văn học)");
        sb.AppendLine("- Economics (Economics theory, business economics, kinh tế học)");
        sb.AppendLine("- Science (Mathematics, Physics, Chemistry, Biology - pure theory)");
        sb.AppendLine("- ComputerScience (Computer architecture, operating systems, networks, algorithms, data structures, theory - NOT programming)");
        sb.AppendLine("- Programming (Hands-on coding: Android, web development, .NET, Java, React, Vue - actual coding)");
        sb.AppendLine("- Business (Business management, marketing, MBA topics)");
        sb.AppendLine("- General (Everything else)");
        sb.AppendLine();
        sb.AppendLine("CRITICAL LANGUAGE GUIDELINES:"); 
        sb.AppendLine("- C/C++/Java courses → Programming (hands-on development)");
        sb.AppendLine("- Python/JavaScript/Web courses → Programming (hands-on development)");
        sb.AppendLine("- .NET/ASP.NET courses → Programming (hands-on development)");
        sb.AppendLine("- Operating Systems THEORY → ComputerScience (NOT programming)");
        sb.AppendLine("- Algorithms THEORY → ComputerScience (NOT programming)");
        sb.AppendLine("- Networks THEORY → ComputerScience (NOT programming)");
        sb.AppendLine();
        sb.AppendLine("KEY RULE: If course teaches hands-on CODING (practicum) → Programming");
        sb.AppendLine("KEY RULE: If course teaches CONCEPTS ONLY (no coding) → ComputerScience");
        sb.AppendLine();
        sb.AppendLine("Subject Information:");  
        sb.AppendLine($"- Name: {subjectName}");

        var prompt = sb.ToString();

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
                var category = result.ToString()?.Trim() ?? "General";
                category = CleanMarkdownFormatting(category);

                if (Enum.TryParse<SubjectCategory>(category, out var parsed))
                {
                    await _memoryStore.SetAsync(cacheKey, category, cancellationToken);
                    _logger.LogInformation("AI classified as: {Category}", category);
                    return parsed;
                }
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning("Attempt {Attempt} to classify subject failed: {Message}. Retrying...", attempt, ex.Message);
                await Task.Delay(BaseDelayMs * attempt, cancellationToken);
            }
        }

        // ============================================================================
        // FALLBACK HEURISTICS - Try pattern matching before defaulting to General
        // ============================================================================
        _logger.LogWarning("AI classification failed after retries. Attempting fallback heuristics...");

        var combined = (subjectName + " " + subjectCode + " " + description).ToLowerInvariant();

        // 1. C Language Detection - FIRST PRIORITY
        if ((combined.Contains("c language") ||
             combined.Contains("c programming") ||
             (Regex.IsMatch(combined, @"\bc\b") &&
              !combined.Contains("c#") &&
              !combined.Contains("c++"))) &&
            combined.Contains("programming"))
        {
            _logger.LogInformation("✅ Fallback heuristic matched: C language course → Programming");
            await _memoryStore.SetAsync(cacheKey, "Programming", cancellationToken);
            return SubjectCategory.Programming;
        }

        // 2. Java/Python/C# Hands-On Programming Detection
        if ((combined.Contains("java programming") ||
             combined.Contains("java development") ||
             combined.Contains("python programming") ||
             combined.Contains("python development") ||
             combined.Contains("csharp") ||
             combined.Contains("c# programming") ||
             combined.Contains(".net programming")) &&
            !combined.Contains("theory only"))
        {
            _logger.LogInformation("✅ Fallback heuristic matched: {Name} → Programming", subjectName);
            await _memoryStore.SetAsync(cacheKey, "Programming", cancellationToken);
            return SubjectCategory.Programming;
        }

        // 3. Generic Programming Detection
        if ((combined.Contains("programming") || combined.Contains("software development")) &&
            !combined.Contains("theory only") &&
            !combined.Contains("purely theoretical") &&
            !combined.Contains("fundamentals only"))
        {
            _logger.LogInformation("✅ Fallback heuristic matched: Generic programming → Programming");
            await _memoryStore.SetAsync(cacheKey, "Programming", cancellationToken);
            return SubjectCategory.Programming;
        }

        // 4. ComputerScience Theory Detection
        if ((combined.Contains("operating system") ||
             combined.Contains("algorithm") ||
             combined.Contains("data structure") ||
             combined.Contains("computer architecture") ||
             combined.Contains("network theory")) &&
            (combined.Contains("theory") || combined.Contains("fundamental")))
        {
            _logger.LogInformation("✅ Fallback heuristic matched: CS theory → ComputerScience");
            await _memoryStore.SetAsync(cacheKey, "ComputerScience", cancellationToken);
            return SubjectCategory.ComputerScience;
        }

        // 5. Vietnamese Politics Detection
        if (combined.Contains("chính trị") || combined.Contains("marxism") ||
            combined.Contains("hồ chí minh") || combined.Contains("đảng"))
        {
            _logger.LogInformation("✅ Fallback heuristic matched: Vietnamese politics → VietnamesePolitics");
            await _memoryStore.SetAsync(cacheKey, "VietnamesePolitics", cancellationToken);
            return SubjectCategory.VietnamesePolitics;
        }

        // 6. Vietnamese Literature Detection
        if (combined.Contains("văn học") || combined.Contains("ngữ văn") ||
            combined.Contains("literature") || combined.Contains("tiếng việt"))
        {
            _logger.LogInformation("✅ Fallback heuristic matched: Vietnamese literature → VietnameseLiterature");
            await _memoryStore.SetAsync(cacheKey, "VietnameseLiterature", cancellationToken);
            return SubjectCategory.VietnameseLiterature;
        }

        // 7. History Detection
        if (combined.Contains("history") || combined.Contains("lịch sử") ||
            combined.Contains("historical") || combined.Contains("war"))
        {
            _logger.LogInformation("✅ Fallback heuristic matched: History → History");
            await _memoryStore.SetAsync(cacheKey, "History", cancellationToken);
            return SubjectCategory.History;
        }

        // 8. Science Detection (Math, Physics, Chemistry, Biology)
        if (combined.Contains("mathematics") || combined.Contains("physics") ||
            combined.Contains("chemistry") || combined.Contains("biology") ||
            combined.Contains("toán") || combined.Contains("vật lý"))
        {
            _logger.LogInformation("✅ Fallback heuristic matched: Science → Science");
            await _memoryStore.SetAsync(cacheKey, "Science", cancellationToken);
            return SubjectCategory.Science;
        }

        // 9. Business Detection
        if (combined.Contains("business") || combined.Contains("management") ||
            combined.Contains("marketing") || combined.Contains("kinh doanh"))
        {
            _logger.LogInformation("✅ Fallback heuristic matched: Business → Business");
            await _memoryStore.SetAsync(cacheKey, "Business", cancellationToken);
            return SubjectCategory.Business;
        }

        // Default fallback - all heuristics failed
        _logger.LogWarning("❌ All fallback heuristics failed. Defaulting to General");
        await _memoryStore.SetAsync(cacheKey, "General", cancellationToken);
        return SubjectCategory.General;

    }

    public async Task<string> GenerateSearchQueryAsync(string topic, string subjectContext, SubjectCategory category, CancellationToken cancellationToken)
    {
        var variants = await GenerateQueryVariantsAsync(topic, subjectContext, category, cancellationToken);
        return variants.FirstOrDefault() ?? topic;
    }

    public async Task<List<string>> GenerateQueryVariantsAsync(string topic, string subjectContext, SubjectCategory category, CancellationToken cancellationToken)
    {
        var cacheKey = $"queries_{category}_{topic.GetHashCode()}";
        var cached = await _memoryStore.GetAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            try
            {
                var cachedQueries = JsonSerializer.Deserialize<List<string>>(cached);
                if (cachedQueries != null && cachedQueries.Any())
                {
                    _logger.LogInformation("Cache hit for queries: {Topic}", topic);
                    return cachedQueries;
                }
            }
            catch { /* Ignore cache errors */ }
        }

        var prompt = BuildEnhancedQueryPrompt(topic, subjectContext, category);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("🤖 Generating queries for '{Topic}' (Category: {Category}, Attempt {Attempt}/{Max})",
                    topic, category, attempt, MaxRetries);

                var raw = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
                var json = raw.ToString();
                json = CleanJsonResponse(json);

                _logger.LogDebug("Cleaned JSON response: {Json}", json);

                var doc = JsonDocument.Parse(json);
                var queries = new List<string>();

                if (doc.RootElement.TryGetProperty("queries", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in arr.EnumerateArray())
                    {
                        if (el.ValueKind == JsonValueKind.String)
                        {
                            var q = el.GetString() ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(q))
                            {
                                var cleaned = q.Trim();
                                if (IsValidQuery(cleaned, topic, category))
                                {
                                    queries.Add(cleaned);
                                }
                            }
                        }
                    }
                }

                if (queries.Any())
                {
                    _logger.LogInformation("✅ Generated {Count} validated search queries via AI", queries.Count);

                    try
                    {
                        await _memoryStore.SetAsync(cacheKey, JsonSerializer.Serialize(queries), cancellationToken);
                    }
                    catch { /* Ignore cache errors */ }

                    return queries;
                }

                _logger.LogWarning("AI returned valid JSON but no valid queries after filtering.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attempt {Attempt} failed for topic '{Topic}'", attempt, topic);
                if (attempt < MaxRetries)
                {
                    await Task.Delay(BaseDelayMs * attempt, cancellationToken);
                }
            }
        }

        _logger.LogWarning("❌ All AI attempts failed for '{Topic}'. Using fallback generator.", topic);
        return CreateFallbackQueries(topic, subjectContext, category);
    }

    /// <summary>
    /// Build enhanced prompt with category-specific guidance for academic sources
    /// </summary>
    private string BuildEnhancedQueryPrompt(string topic, string? subjectContext, SubjectCategory category)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert at generating Google Search queries to find HIGH-QUALITY educational resources.");
        sb.AppendLine();
        sb.AppendLine("🎯 GOAL: Generate 3-5 search queries that will return ACADEMIC, TUTORIAL-FOCUSED results.");
        sb.AppendLine();
        sb.AppendLine("📚 Context:");
        sb.AppendLine($"- Topic: {topic}");
        sb.AppendLine($"- Subject Context: {(string.IsNullOrWhiteSpace(subjectContext) ? "General" : subjectContext)}");
        sb.AppendLine($"- Category: {category}");
        sb.AppendLine();

        switch (category)
        {
            case SubjectCategory.Programming:
                sb.AppendLine("🔧 PROGRAMMING CATEGORY - Instructions:");
                sb.AppendLine("- Include framework/language name (e.g., 'Android', 'React', 'ASP.NET', 'Java')");
                sb.AppendLine("- Add keywords: 'tutorial', 'guide', 'example', 'documentation'");
                sb.AppendLine("- Prioritize: Official docs > Tutorial sites (W3Schools, TutorialsPoint, GeeksforGeeks) > Blogs");
                sb.AppendLine("- Example: 'Android RecyclerView tutorial example code'");
                if (!string.IsNullOrWhiteSpace(subjectContext))
                {
                    sb.AppendLine($"- MUST include technology: {subjectContext}");
                }
                break;

            case SubjectCategory.ComputerScience:
                sb.AppendLine("🖥️ COMPUTER SCIENCE (THEORY) - Instructions:");
                sb.AppendLine("- Focus on: concepts, architecture, theory, algorithms");
                sb.AppendLine("- Add keywords: 'tutorial', 'guide', 'explanation', 'theory', 'architecture'");
                sb.AppendLine("- Prioritize: GeeksforGeeks, TutorialsPoint, Wikipedia, university sites");
                sb.AppendLine("- Example: 'Computer architecture cache memory tutorial'");
                sb.AppendLine("- AVOID: Coding tutorials, framework-specific content");
                break;

            case SubjectCategory.VietnamesePolitics:
                sb.AppendLine("🇻🇳 VIETNAMESE POLITICS - Instructions:");
                sb.AppendLine("- Use Vietnamese keywords: 'lý thuyết', 'bài giảng', 'tài liệu'");
                sb.AppendLine("- Prioritize: Official .vn sites, educational institutions");
                sb.AppendLine("- Example: 'Tư tưởng Hồ Chí Minh lý thuyết bài giảng'");
                break;

            case SubjectCategory.History:
                sb.AppendLine("📜 HISTORY - Instructions:");
                sb.AppendLine("- Add keywords: 'lịch sử', 'tài liệu', 'historical analysis'");
                sb.AppendLine("- Prioritize: Wikipedia, educational sites, academic sources");
                sb.AppendLine("- Example: 'Lịch sử Việt Nam chiến tranh tài liệu'");
                break;

            case SubjectCategory.VietnameseLiterature:
                sb.AppendLine("📖 VIETNAMESE LITERATURE - Instructions:");
                sb.AppendLine("- Use: 'văn học', 'ngữ văn', 'bài tập', 'trắc nghiệm'");
                sb.AppendLine("- Prioritize: VietJack, educational sites");
                sb.AppendLine("- Example: 'Văn học Việt Nam bài tập trắc nghiệm'");
                break;

            case SubjectCategory.Science:
                sb.AppendLine("🔬 SCIENCE - Instructions:");
                sb.AppendLine("- Add: 'lý thuyết', 'công thức', 'theory', 'explanation'");
                sb.AppendLine("- Prioritize: Khan Academy, educational platforms");
                sb.AppendLine("- Example: 'Toán học vi tích phân lý thuyết'");
                break;

            case SubjectCategory.Business:
                sb.AppendLine("💼 BUSINESS - Instructions:");
                sb.AppendLine("- Add: 'bài giảng', 'guide', 'tutorial'");
                sb.AppendLine("- Example: 'Kinh tế vi mô bài giảng'");
                break;

            default:
                sb.AppendLine("📚 GENERAL - Instructions:");
                sb.AppendLine("- Add: 'tutorial', 'guide', 'explanation'");
                sb.AppendLine("- Detect language (English/Vietnamese) and use appropriate keywords");
                break;
        }

        sb.AppendLine();
        sb.AppendLine("⚠️ CRITICAL REQUIREMENTS:");
        sb.AppendLine("1. Each query must be 5-12 words (concise but specific)");
        sb.AppendLine("2. Include relevant keywords from the category instructions above");
        sb.AppendLine("3. Mix English and Vietnamese queries ONLY if topic contains Vietnamese");
        sb.AppendLine("4. Avoid generic queries like just the topic name");
        sb.AppendLine("5. Prioritize queries that will return TUTORIAL/EDUCATIONAL sites");
        sb.AppendLine();
        sb.AppendLine("🚫 DO NOT:");
        sb.AppendLine("- Use quotes or special operators (site:, inurl:)");
        sb.AppendLine("- Make queries too long (>12 words)");
        sb.AppendLine("- Generate duplicate or near-duplicate queries");
        sb.AppendLine();
        sb.AppendLine("📤 OUTPUT FORMAT:");
        sb.AppendLine("Return ONLY valid JSON (no markdown, no code fences):");
        sb.AppendLine("{\"queries\": [\"query1\", \"query2\", \"query3\"]}");

        return sb.ToString();
    }

    /// <summary>
    /// Validate query quality before returning
    /// </summary>
    private bool IsValidQuery(string query, string topic, SubjectCategory category)
    {
        // Too short or too long
        var wordCount = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < 2 || wordCount > 15)
        {
            _logger.LogDebug("❌ Query rejected (word count {Count}): {Query}", wordCount, query);
            return false;
        }

        // Must contain at least part of the topic
        var topicTokens = topic.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 3)
            .ToList();

        var queryLower = query.ToLowerInvariant();
        var hasTopicMatch = topicTokens.Any(token => queryLower.Contains(token));

        if (!hasTopicMatch)
        {
            _logger.LogDebug("❌ Query rejected (no topic match): {Query}", query);
            return false;
        }

        // Category-specific validation
        switch (category)
        {
            case SubjectCategory.Programming:
            case SubjectCategory.ComputerScience:
                // Should contain educational keywords
                var eduKeywords = new[] { "tutorial", "guide", "documentation", "example", "docs", "learn" };
                if (!eduKeywords.Any(kw => queryLower.Contains(kw)))
                {
                    _logger.LogDebug("⚠️ Query missing educational keyword: {Query}", query);
                    // Don't reject, just warn
                }
                break;

            case SubjectCategory.VietnamesePolitics:
            case SubjectCategory.History:
            case SubjectCategory.VietnameseLiterature:
                // Vietnamese queries should have Vietnamese keywords
                var vnKeywords = new[] { "lý thuyết", "bài giảng", "tài liệu", "lịch sử", "văn học", "ngữ văn" };
                var isVietnamese = topic.Contains("ế") || topic.Contains("ư") || topic.Contains("ơ");
                if (isVietnamese && !vnKeywords.Any(kw => queryLower.Contains(kw)))
                {
                    _logger.LogDebug("⚠️ Vietnamese query missing Vietnamese educational keywords: {Query}", query);
                }
                break;
        }

        return true;
    }

    public async Task<Dictionary<int, List<string>>> GenerateBatchQueryVariantsAsync(
        List<SyllabusSessionDto> sessions,
        string subjectContext,
        SubjectCategory category,
        List<string> technologyKeywords,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, List<string>>();

        // For large batches (60+ sessions), use smaller chunks to avoid token limits
        int chunkSize = sessions.Count > 30 ? 8 : 10;
        var chunks = sessions.Chunk(chunkSize).ToList();

        _logger.LogInformation("🔄 Processing {SessionCount} sessions in {ChunkCount} chunks of {ChunkSize}",
            sessions.Count, chunks.Count, chunkSize);

        // Process chunks with retry and delay to avoid rate limits
        int chunkIndex = 0;
        foreach (var chunk in chunks)
        {
            chunkIndex++;
            _logger.LogDebug("Processing chunk {Index}/{Total}...", chunkIndex, chunks.Count);

            var chunkResult = await GenerateChunkQueriesAsync(
                chunk.ToArray(),
                subjectContext,
                category,
                technologyKeywords,
                cancellationToken);

            foreach (var kvp in chunkResult)
            {
                result[kvp.Key] = kvp.Value;
            }

            // Add small delay between chunks to avoid overwhelming the LLM API
            if (chunkIndex < chunks.Count)
            {
                await Task.Delay(500, cancellationToken); // 500ms delay between chunks
            }
        }

        _logger.LogInformation("✅ Batch generation complete: {SuccessCount}/{TotalCount} sessions have queries",
            result.Count, sessions.Count);

        // For sessions without queries, generate them individually as fallback
        var missingSessions = sessions.Where(s => !result.ContainsKey(s.SessionNumber)).ToList();
        if (missingSessions.Any())
        {
            _logger.LogWarning("⚠️ {MissingCount} sessions missing queries. Generating individually...",
                missingSessions.Count);

            foreach (var session in missingSessions)
            {
                try
                {
                    var queries = await GenerateQueryVariantsAsync(session.Topic, subjectContext, category, cancellationToken);
                    if (queries.Any())
                    {
                        result[session.SessionNumber] = queries;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate queries for session {SessionNumber}", session.SessionNumber);
                }
            }
        }

        return result;
    }

    private async Task<Dictionary<int, List<string>>> GenerateChunkQueriesAsync(
        SyllabusSessionDto[] sessions,
        string subjectContext,
        SubjectCategory category,
        List<string> technologyKeywords,
        CancellationToken cancellationToken)
    {
        var prompt = BuildBatchQueryPrompt(sessions, subjectContext, category, technologyKeywords);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("🤖 Batch generating queries for {Count} sessions (Attempt {Attempt})", sessions.Length, attempt);

                var raw = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
                var json = CleanJsonResponse(raw.ToString());

                // Log sample of response for debugging
                var previewLength = Math.Min(json.Length, 300);
                _logger.LogDebug("Batch JSON response preview: {Preview}", json.Substring(0, previewLength) + "...");

                var doc = JsonDocument.Parse(json);
                var dict = new Dictionary<int, List<string>>();

                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    if (int.TryParse(property.Name, out int sessionNum) && property.Value.ValueKind == JsonValueKind.Array)
                    {
                        var queries = property.Value.EnumerateArray()
                            .Select(x => x.GetString())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Select(x => x!.Trim())
                            .ToList();

                        if (queries.Any())
                        {
                            dict[sessionNum] = queries;
                            _logger.LogDebug("  Session {SessionNumber}: {QueryCount} queries generated", sessionNum, queries.Count);

                            // Log first query as sample
                            if (queries.Count > 0)
                            {
                                _logger.LogDebug("    Sample: \"{Query}\"", queries[0]);
                            }
                        }
                    }
                }

                if (dict.Count > 0)
                {
                    _logger.LogInformation("✅ Successfully generated queries for {Count}/{Total} sessions in this chunk",
                        dict.Count, sessions.Length);
                    return dict;
                }
                else
                {
                    _logger.LogWarning("⚠️ No queries extracted from response (attempt {Attempt})", attempt);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ JSON parsing error (Attempt {Attempt}). Response may be malformed.", attempt);
                if (attempt < MaxRetries)
                {
                    await Task.Delay(BaseDelayMs * attempt, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Batch query generation failed (Attempt {Attempt})", attempt);
                if (attempt < MaxRetries)
                {
                    await Task.Delay(BaseDelayMs * attempt, cancellationToken);
                }
            }
        }

        _logger.LogError("❌ Batch generation failed after all retries for chunk of {Count} sessions.", sessions.Length);
        return new Dictionary<int, List<string>>();
    }

    private string BuildBatchQueryPrompt(SyllabusSessionDto[] sessions, string subjectContext, SubjectCategory category, List<string> technologyKeywords)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generate optimized Google Search queries for each syllabus session to find ACADEMIC/TUTORIAL resources.");
        sb.AppendLine();
        sb.AppendLine("📚 Context:");
        sb.AppendLine($"- Subject: {subjectContext}");
        sb.AppendLine($"- Category: {category}");
        // ← ✅ ADD THIS ENTIRE SECTION (NEW):
        if (technologyKeywords != null && technologyKeywords.Any())
        {
            var techList = string.Join(", ", technologyKeywords);
            sb.AppendLine($"- Technologies: {techList}");
            sb.AppendLine($"- ⭐⭐⭐ CRITICAL REQUIREMENT ⭐⭐⭐");
            sb.AppendLine($"  EVERY single query MUST explicitly mention one or more of: {techList}");
            sb.AppendLine($"  DO NOT generate generic queries that could apply to any language/technology");
            sb.AppendLine($"  Example (BAD): 'Function definitions tutorial'");
            sb.AppendLine($"  Example (GOOD): 'C function definitions tutorial'");
        }
        var hints = BuildCategoryHints(category);
        if (!string.IsNullOrWhiteSpace(hints))
        {
            sb.AppendLine($"- Keywords to include: {hints}");
        }

        sb.AppendLine();
        sb.AppendLine("📋 Rules:");
        sb.AppendLine("1. Generate 3-5 DIVERSE search queries per session (variety is important!)");
        sb.AppendLine("2. Each query: 5-12 words, include educational keywords");
        sb.AppendLine("3. Mix query styles: specific terms, broader context, official docs");
        sb.AppendLine("4. For Programming/CS: Include technology name + 'tutorial'/'guide'/'documentation'");
        sb.AppendLine("5. For Vietnamese topics: Use Vietnamese educational keywords");
        sb.AppendLine("6. Make queries DIFFERENT from each other (avoid near-duplicates)");
        sb.AppendLine();
        sb.AppendLine("🎯 Query Diversity Strategy:");
        sb.AppendLine("- Query 1: Specific + tutorial (e.g., 'Android RecyclerView tutorial')");
        sb.AppendLine("- Query 2: Broader context (e.g., 'Android list view implementation guide')");
        sb.AppendLine("- Query 3: Official docs focus (e.g., 'Android RecyclerView documentation')");
        sb.AppendLine("- Query 4: Example-focused (e.g., 'RecyclerView example code')");
        sb.AppendLine("- Query 5: Alternative phrasing or Vietnamese (if applicable)");
        sb.AppendLine();
        sb.AppendLine("📝 Sessions:");

        foreach (var s in sessions)
        {
            sb.AppendLine($"- Session {s.SessionNumber}: {s.Topic}");
        }

        sb.AppendLine();
        sb.AppendLine("📤 OUTPUT (JSON only, no markdown, no code fences):");
        sb.AppendLine("{");
        sb.AppendLine("  \"1\": [\"query A\", \"query B\", \"query C\", \"query D\", \"query E\"],");
        sb.AppendLine("  \"2\": [\"query F\", \"query G\", \"query H\", \"query I\", \"query J\"],");
        sb.AppendLine("  ...");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("CRITICAL: Return VALID JSON. No markdown. No explanations. Just the JSON object.");

        return sb.ToString();
    }

    private string CleanJsonResponse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "{}";

        var cleaned = Regex.Replace(raw, @"```json\s*", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"```\s*", "");
        cleaned = cleaned.Trim();

        var firstBrace = cleaned.IndexOf('{');
        var lastBrace = cleaned.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            cleaned = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        return cleaned;
    }

    private string CleanMarkdownFormatting(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        text = Regex.Replace(text, @"```[a-z]*\s*", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"```\s*", "");
        text = text.Replace("**", "").Replace("*", "");
        text = text.Trim('"', '\'', '`');
        return text.Trim();
    }

    private List<string> CreateFallbackQueries(string topic, string? subjectContext, SubjectCategory category)
    {
        var queries = new List<string>();

        switch (category)
        {
            case SubjectCategory.Programming:
                if (!string.IsNullOrWhiteSpace(subjectContext))
                {
                    queries.Add($"{subjectContext} {topic} tutorial");
                    queries.Add($"{subjectContext} {topic} guide example");
                    queries.Add($"{topic} {subjectContext} documentation");
                }
                else
                {
                    queries.Add($"{topic} programming tutorial");
                    queries.Add($"{topic} coding guide");
                }
                break;

            case SubjectCategory.ComputerScience:
                queries.Add($"{topic} computer science tutorial");
                queries.Add($"{topic} theory explanation guide");
                queries.Add($"{topic} architecture overview");
                break;

            case SubjectCategory.VietnamesePolitics:
                queries.Add($"{topic} lý thuyết bài giảng");
                queries.Add($"{topic} tài liệu chính trị");
                queries.Add($"{topic} giáo trình");
                break;

            case SubjectCategory.History:
                queries.Add($"{topic} lịch sử tài liệu");
                queries.Add($"{topic} historical analysis");
                queries.Add($"{topic} lịch sử giáo trình");
                break;

            case SubjectCategory.VietnameseLiterature:
                queries.Add($"{topic} văn học bài tập");
                queries.Add($"{topic} ngữ văn trắc nghiệm");
                queries.Add($"{topic} tài liệu văn học");
                break;

            case SubjectCategory.Science:
                queries.Add($"{topic} lý thuyết công thức");
                queries.Add($"{topic} scientific explanation");
                queries.Add($"{topic} giáo trình khoa học");
                break;

            case SubjectCategory.Business:
                queries.Add($"{topic} business guide");
                queries.Add($"{topic} kinh tế bài giảng");
                queries.Add($"{topic} management tutorial");
                break;

            default:
                var isVietnamese = topic.Contains("ế") || topic.Contains("ư") || topic.Contains("ơ");
                if (isVietnamese)
                {
                    queries.Add($"{topic} tài liệu học tập");
                    queries.Add($"{topic} bài giảng");
                }
                else
                {
                    queries.Add($"{topic} tutorial guide");
                    queries.Add($"{topic} educational resource");
                }
                break;
        }

        _logger.LogInformation("Using {Count} fallback queries for category {Category}", queries.Count, category);
        return queries.Distinct().ToList();
    }

    private string BuildCategoryHints(SubjectCategory category)
    {
        return category switch
        {
            SubjectCategory.ComputerScience => "architecture, theory, concepts, algorithms, data structures, operating systems",
            SubjectCategory.Programming => "tutorial, guide, example code, documentation, hands-on",
            SubjectCategory.History => "lịch sử, historical analysis, tài liệu, timeline",
            SubjectCategory.VietnamesePolitics => "lý thuyết, bài giảng, chính trị, tư tưởng",
            SubjectCategory.VietnameseLiterature => "văn học, ngữ văn, bài tập, trắc nghiệm",
            SubjectCategory.Business => "kinh tế, business, management, bài giảng",
            SubjectCategory.Science => "lý thuyết, công thức, theory, scientific explanation",
            _ => "tutorial, guide, educational"
        };
    }
}
