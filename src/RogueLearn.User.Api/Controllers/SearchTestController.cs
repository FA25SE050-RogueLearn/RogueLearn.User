//// RogueLearn.User/src/RogueLearn.User.Api/Controllers/SearchTestController.cs
//using Microsoft.AspNetCore.Mvc;
//using RogueLearn.User.Application.Interfaces;

//namespace RogueLearn.User.Api.Controllers;

///// <summary>
///// TEST ENDPOINTS for debugging web search and URL sourcing functionality.
///// These endpoints should be removed or secured in production.
///// </summary>
//[ApiController]
//[Route("api/[controller]")]
//public class SearchTestController : ControllerBase
//{
//    private readonly IWebSearchService _webSearchService;
//    private readonly IReadingUrlService _readingUrlService;
//    private readonly IUrlValidationService _urlValidationService;
//    private readonly ILogger<SearchTestController> _logger;

//    public SearchTestController(
//        IWebSearchService webSearchService,
//        IReadingUrlService readingUrlService,
//        IUrlValidationService urlValidationService,
//        ILogger<SearchTestController> logger)
//    {
//        _webSearchService = webSearchService;
//        _readingUrlService = readingUrlService;
//        _urlValidationService = urlValidationService;
//        _logger = logger;
//    }

//    /// <summary>
//    /// Test raw web search with query enhancement.
//    /// GET /api/searchtest/web-search?query=android activity tutorial&count=10
//    /// </summary>
//    [HttpGet("web-search")]
//    public async Task<IActionResult> TestWebSearch(
//        [FromQuery] string query,
//        [FromQuery] int count = 10,
//        [FromQuery] int offset = 0,
//        CancellationToken cancellationToken = default)
//    {
//        if (string.IsNullOrWhiteSpace(query))
//        {
//            return BadRequest(new { error = "Query parameter is required" });
//        }

//        _logger.LogInformation("🔍 TEST: Web search for query: '{Query}'", query);

//        var results = await _webSearchService.SearchAsync(query, count, offset, cancellationToken);
//        var resultsList = results?.ToList() ?? new List<string>();

//        return Ok(new
//        {
//            query = query,
//            count = resultsList.Count,
//            results = resultsList.Select((result, index) => new
//            {
//                rank = index + 1,
//                content = result
//            })
//        });
//    }

//    /// <summary>
//    /// Test the full URL sourcing pipeline for a topic (mimics session enrichment).
//    /// GET /api/searchtest/get-url?topic=Android Activity Lifecycle&context=Android Mobile Programming
//    /// </summary>
//    [HttpGet("get-url")]
//    public async Task<IActionResult> TestGetUrlForTopic(
//        [FromQuery] string topic,
//        [FromQuery] string? context = null,  // ⭐ NEW: Optional subject context
//        [FromQuery] string[]? readings = null,
//        CancellationToken cancellationToken = default)
//    {
//        if (string.IsNullOrWhiteSpace(topic))
//        {
//            return BadRequest(new { error = "Topic parameter is required" });
//        }

//        _logger.LogInformation("🎯 TEST: Getting URL for topic: '{Topic}' | Context: '{Context}'",
//            topic, context ?? "none");

//        var readingsList = readings ?? Array.Empty<string>();

//        var startTime = DateTime.UtcNow;

//        // ⭐ PASS CONTEXT (3rd parameter)
//        var foundUrl = await _readingUrlService.GetValidUrlForTopicAsync(
//            topic,
//            readingsList,
//            context,  // ⭐ NEW PARAMETER
//            cancellationToken);

//        var elapsed = DateTime.UtcNow - startTime;

//        if (!string.IsNullOrWhiteSpace(foundUrl))
//        {
//            return Ok(new
//            {
//                success = true,
//                topic = topic,
//                subjectContext = context,  // ⭐ Include in response
//                providedReadings = readingsList,
//                foundUrl = foundUrl,
//                timeTakenMs = elapsed.TotalMilliseconds,
//                message = "✅ Successfully found and validated URL"
//            });
//        }
//        else
//        {
//            return Ok(new
//            {
//                success = false,
//                topic = topic,
//                subjectContext = context,  // ⭐ Include in response
//                providedReadings = readingsList,
//                foundUrl = (string?)null,
//                timeTakenMs = elapsed.TotalMilliseconds,
//                message = "❌ Could not find a valid URL after all tiers"
//            });
//        }
//    }

//    /// <summary>
//    /// Test URL validation service directly.
//    /// GET /api/searchtest/validate-url?url=https://dev.to/some-article
//    /// </summary>
//    [HttpGet("validate-url")]
//    public async Task<IActionResult> TestUrlValidation(
//        [FromQuery] string url,
//        CancellationToken cancellationToken = default)
//    {
//        if (string.IsNullOrWhiteSpace(url))
//        {
//            return BadRequest(new { error = "URL parameter is required" });
//        }

//        _logger.LogInformation("✔️ TEST: Validating URL: '{Url}'", url);

//        var startTime = DateTime.UtcNow;
//        var isValid = await _urlValidationService.IsUrlAccessibleAsync(url, cancellationToken);
//        var elapsed = DateTime.UtcNow - startTime;

//        return Ok(new
//        {
//            url = url,
//            isValid = isValid,
//            timeTakenMs = elapsed.TotalMilliseconds,
//            message = isValid
//                ? "✅ URL is accessible and valid"
//                : "❌ URL is not accessible or invalid"
//        });
//    }

//    /// <summary>
//    /// Batch test: Get URLs for multiple topics at once.
//    /// POST /api/searchtest/batch-get-urls
//    /// Body: { "topics": ["Android Activity", "ASP.NET MVC", "React Hooks"], "subjectContext": "Android Mobile Programming" }
//    /// </summary>
//    [HttpPost("batch-get-urls")]
//    public async Task<IActionResult> TestBatchGetUrls(
//        [FromBody] BatchGetUrlsRequest request,
//        CancellationToken cancellationToken = default)
//    {
//        if (request?.Topics == null || !request.Topics.Any())
//        {
//            return BadRequest(new { error = "Topics array is required" });
//        }

//        _logger.LogInformation("📋 TEST: Batch URL search for {Count} topics | Context: '{Context}'",
//            request.Topics.Count, request.SubjectContext ?? "none");

//        var results = new List<object>();
//        var startTime = DateTime.UtcNow;

//        foreach (var topic in request.Topics)
//        {
//            var topicStartTime = DateTime.UtcNow;

//            // ⭐ PASS CONTEXT
//            var foundUrl = await _readingUrlService.GetValidUrlForTopicAsync(
//                topic,
//                Array.Empty<string>(),
//                request.SubjectContext,  // ⭐ NEW PARAMETER
//                cancellationToken);

//            var topicElapsed = DateTime.UtcNow - topicStartTime;

//            results.Add(new
//            {
//                topic = topic,
//                success = !string.IsNullOrWhiteSpace(foundUrl),
//                url = foundUrl,
//                timeTakenMs = topicElapsed.TotalMilliseconds
//            });
//        }

//        var totalElapsed = DateTime.UtcNow - startTime;

//        return Ok(new
//        {
//            totalTopics = request.Topics.Count,
//            subjectContext = request.SubjectContext,  // ⭐ Include in response
//            successCount = results.Count(r => (bool)(r.GetType().GetProperty("success")?.GetValue(r) ?? false)),
//            totalTimeTakenMs = totalElapsed.TotalMilliseconds,
//            results = results
//        });
//    }

//    /// <summary>
//    /// ⭐ NEW: Test context-aware URL search with comparison.
//    /// Shows how subject context improves relevance.
//    /// GET /api/searchtest/compare-with-without-context?topic=Layout Manager&context=Android Mobile
//    /// </summary>
//    [HttpGet("compare-with-without-context")]
//    public async Task<IActionResult> CompareWithAndWithoutContext(
//        [FromQuery] string topic,
//        [FromQuery] string context,
//        CancellationToken cancellationToken = default)
//    {
//        if (string.IsNullOrWhiteSpace(topic))
//        {
//            return BadRequest(new { error = "Topic parameter is required" });
//        }

//        if (string.IsNullOrWhiteSpace(context))
//        {
//            return BadRequest(new { error = "Context parameter is required" });
//        }

//        _logger.LogInformation("🔬 TEST: Comparing URL search with/without context for: '{Topic}'", topic);

//        // Search WITHOUT context
//        var startTimeWithout = DateTime.UtcNow;
//        var urlWithoutContext = await _readingUrlService.GetValidUrlForTopicAsync(
//            topic,
//            Array.Empty<string>(),
//            null,  // ❌ NO CONTEXT
//            cancellationToken);
//        var elapsedWithout = DateTime.UtcNow - startTimeWithout;

//        // Search WITH context
//        var startTimeWith = DateTime.UtcNow;
//        var urlWithContext = await _readingUrlService.GetValidUrlForTopicAsync(
//            topic,
//            Array.Empty<string>(),
//            context,  // ✅ WITH CONTEXT
//            cancellationToken);
//        var elapsedWith = DateTime.UtcNow - startTimeWith;

//        return Ok(new
//        {
//            topic = topic,
//            subjectContext = context,
//            withoutContext = new
//            {
//                url = urlWithoutContext,
//                timeTakenMs = elapsedWithout.TotalMilliseconds,
//                success = !string.IsNullOrWhiteSpace(urlWithoutContext)
//            },
//            withContext = new
//            {
//                url = urlWithContext,
//                timeTakenMs = elapsedWith.TotalMilliseconds,
//                success = !string.IsNullOrWhiteSpace(urlWithContext)
//            },
//            improvement = new
//            {
//                urlChanged = urlWithoutContext != urlWithContext,
//                message = urlWithoutContext != urlWithContext
//                    ? "✅ Context improved URL relevance"
//                    : urlWithContext != null
//                        ? "⚠️ Same URL found (already relevant)"
//                        : "❌ No URL found in either case"
//            }
//        });
//    }

//    /// <summary>
//    /// ⭐ NEW: Test URL search for a realistic PRM392 session.
//    /// GET /api/searchtest/test-prm392-session?sessionNumber=7
//    /// </summary>
//    [HttpGet("test-prm392-session")]
//    public async Task<IActionResult> TestPrm392Session(
//        [FromQuery] int sessionNumber = 7,
//        CancellationToken cancellationToken = default)
//    {
//        // Simulate PRM392 sessions
//        var prm392Sessions = new Dictionary<int, string>
//        {
//            { 1, "Mobile Development Overview Android Introduction" },
//            { 2, "Android Studio" },
//            { 3, "Android Application Structure" },
//            { 4, "Build the first application" },
//            { 5, "Simple UI Widgets" },
//            { 6, "Using UI in application" },
//            { 7, "Layout manager (LinearLayout, ConstraintLayout, RelativeLayout)" },
//            { 8, "Event Handling" },
//            { 9, "ListView and RecyclerView" },
//            { 10, "SQLite Database" }
//        };

//        if (!prm392Sessions.ContainsKey(sessionNumber))
//        {
//            return BadRequest(new { error = $"Invalid session number. Valid range: 1-{prm392Sessions.Count}" });
//        }

//        var topic = prm392Sessions[sessionNumber];
//        var context = "Android Mobile Programming, Kotlin, Java";  // PRM392 context

//        _logger.LogInformation("📱 TEST: PRM392 Session {SessionNumber} - '{Topic}'", sessionNumber, topic);

//        var startTime = DateTime.UtcNow;
//        var foundUrl = await _readingUrlService.GetValidUrlForTopicAsync(
//            topic,
//            Array.Empty<string>(),
//            context,
//            cancellationToken);
//        var elapsed = DateTime.UtcNow - startTime;

//        return Ok(new
//        {
//            subject = "PRM392 - Mobile Programming",
//            sessionNumber = sessionNumber,
//            topic = topic,
//            subjectContext = context,
//            foundUrl = foundUrl,
//            success = !string.IsNullOrWhiteSpace(foundUrl),
//            timeTakenMs = elapsed.TotalMilliseconds,
//            expectedRelevance = new
//            {
//                shouldContain = new[] { "android", "mobile", "kotlin", "java" },
//                shouldNotContain = new[] { "c++", "python", "nodejs", ".pdf", "thesis" }
//            },
//            message = !string.IsNullOrWhiteSpace(foundUrl)
//                ? "✅ Found URL - Check relevance manually"
//                : "❌ No URL found"
//        });
//    }

//    /// <summary>
//    /// Test Vietnamese content detection and search.
//    /// GET /api/searchtest/vietnamese-search?query=Lập trình Android
//    /// </summary>
//    [HttpGet("vietnamese-search")]
//    public async Task<IActionResult> TestVietnameseSearch(
//        [FromQuery] string query,
//        CancellationToken cancellationToken = default)
//    {
//        if (string.IsNullOrWhiteSpace(query))
//        {
//            return BadRequest(new { error = "Query parameter is required" });
//        }

//        _logger.LogInformation("🇻🇳 TEST: Vietnamese search for: '{Query}'", query);

//        var results = await _webSearchService.SearchAsync(query, count: 10, offset: 0, cancellationToken);
//        var resultsList = results?.ToList() ?? new List<string>();

//        // Analyze results for Vietnamese sources
//        var vietnameseSources = resultsList
//            .Where(r => r.Contains("viblo.asia", StringComparison.OrdinalIgnoreCase) ||
//                       r.Contains("topdev.vn", StringComparison.OrdinalIgnoreCase) ||
//                       r.Contains("techtalk.vn", StringComparison.OrdinalIgnoreCase) ||
//                       r.Contains("200lab.io", StringComparison.OrdinalIgnoreCase))
//            .ToList();

//        return Ok(new
//        {
//            query = query,
//            totalResults = resultsList.Count,
//            vietnameseSourcesCount = vietnameseSources.Count,
//            vietnameseSourcesPercentage = resultsList.Count > 0
//                ? (vietnameseSources.Count * 100.0 / resultsList.Count)
//                : 0,
//            allResults = resultsList.Select((result, index) => new
//            {
//                rank = index + 1,
//                content = result,
//                isVietnameseSource = vietnameseSources.Contains(result)
//            })
//        });
//    }

//    /// <summary>
//    /// Health check: Verify all search services are registered.
//    /// GET /api/searchtest/health
//    /// </summary>
//    [HttpGet("health")]
//    public IActionResult HealthCheck()
//    {
//        return Ok(new
//        {
//            status = "healthy",
//            services = new
//            {
//                webSearchService = _webSearchService != null ? "✅ Registered" : "❌ Missing",
//                readingUrlService = _readingUrlService != null ? "✅ Registered" : "❌ Missing",
//                urlValidationService = _urlValidationService != null ? "✅ Registered" : "❌ Missing"
//            },
//            timestamp = DateTime.UtcNow,
//            info = new
//            {
//                message = "All services support context-aware URL searching",
//                newParameter = "subjectContext (optional string)"
//            }
//        });
//    }
//}

///// <summary>
///// Request model for batch URL testing.
///// </summary>
//public class BatchGetUrlsRequest
//{
//    public List<string> Topics { get; set; } = new();

//    /// <summary>
//    /// ⭐ NEW: Optional subject context for relevance filtering.
//    /// Example: "Android Mobile Programming, Kotlin, Java"
//    /// </summary>
//    public string? SubjectContext { get; set; }
//}
