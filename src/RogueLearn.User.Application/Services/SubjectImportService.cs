// RogueLearn.User/src/RogueLearn.User.Application/Services/SubjectImportService.cs
using AutoMapper;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Common;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Services;

public class SubjectImportService : ISubjectImportService
{
    private const int MAX_SESSIONS_TO_ENRICH = 5;

    private readonly ISyllabusExtractionPlugin _syllabusExtractionPlugin;
    private readonly IConstructiveQuestionGenerationPlugin _questionGenerationPlugin;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SubjectImportService> _logger;
    private readonly IHtmlCleaningService _htmlCleaningService;
    private readonly ICurriculumImportStorage _storage;
    private readonly IReadingUrlService _readingUrlService;
    private readonly IAiQueryClassificationService _aiQueryService;

    public SubjectImportService(
        ISyllabusExtractionPlugin syllabusExtractionPlugin,
        IConstructiveQuestionGenerationPlugin questionGenerationPlugin,
        ISubjectRepository subjectRepository,
        IMapper mapper,
        ILogger<SubjectImportService> logger,
        IHtmlCleaningService htmlCleaningService,
        ICurriculumImportStorage storage,
        IReadingUrlService readingUrlService,
        IAiQueryClassificationService aiQueryService)
    {
        _syllabusExtractionPlugin = syllabusExtractionPlugin;
        _questionGenerationPlugin = questionGenerationPlugin;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
        _htmlCleaningService = htmlCleaningService;
        _storage = storage;
        _readingUrlService = readingUrlService;
        _aiQueryService = aiQueryService;
    }

    public async Task ImportSubjectAsync(ImportSubjectFromTextCommand request, PerformContext context)
    {
        var cancellationToken = CancellationToken.None; // Background jobs usually run to completion or perform context cancellation
        UpdateProgress(context, 0, "Starting HTML processing...");

        // ============================================================================
        // PHASE 1: Extract & Validate (0-20%)
        // ============================================================================
        _logger.LogInformation("Cleaning HTML...");
        var cleanText = _htmlCleaningService.ExtractCleanTextFromHtml(request.RawText);

        if (string.IsNullOrWhiteSpace(cleanText))
        {
            throw new BadRequestException("Failed to extract meaningful text content from the provided HTML.");
        }

        var rawTextHash = ComputeSha256Hash(cleanText);
        UpdateProgress(context, 10, "Checking cache...");

        string? extractedJson = await _storage.TryGetCachedSyllabusDataAsync(rawTextHash, cancellationToken);

        if (string.IsNullOrWhiteSpace(extractedJson))
        {
            UpdateProgress(context, 15, "Analyzing syllabus with AI (this may take a moment)...");
            _logger.LogInformation("Extracting syllabus from text...");
            extractedJson = await _syllabusExtractionPlugin.ExtractSyllabusJsonAsync(cleanText, cancellationToken);
        }
        else
        {
            UpdateProgress(context, 15, "Cache hit! Using existing syllabus analysis.");
            _logger.LogInformation("Using cached syllabus");
        }

        UpdateProgress(context, 20, "Parsing extracted data...");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        SyllabusData? syllabusData;
        try
        {
            syllabusData = JsonSerializer.Deserialize<SyllabusData>(extractedJson!, options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize extracted syllabus JSON: {Json}", extractedJson);
            throw new BadRequestException("Failed to deserialize the extracted syllabus data.");
        }

        if (syllabusData == null || string.IsNullOrWhiteSpace(syllabusData.SubjectCode))
        {
            throw new BadRequestException("Extracted syllabus data is missing a valid SubjectCode.");
        }

        _logger.LogInformation("Phase 1 Complete: Subject {SubjectCode}", syllabusData.SubjectCode);

        // ============================================================================
        // PHASE 2: Generate Constructive Questions (20-40%)
        // ============================================================================
        UpdateProgress(context, 25, "Evaluating constructive questions...");

        if (syllabusData.Content?.ConstructiveQuestions == null || !syllabusData.Content.ConstructiveQuestions.Any())
        {
            if (syllabusData.Content?.SessionSchedule != null && syllabusData.Content.SessionSchedule.Any())
            {
                UpdateProgress(context, 30, "Generating new constructive questions via AI...");
                _logger.LogInformation("Generating questions...");

                var generatedQuestions = await _questionGenerationPlugin.GenerateQuestionsAsync(
                    syllabusData.Content.SessionSchedule, cancellationToken);

                if (generatedQuestions?.Any() == true)
                {
                    syllabusData.Content.ConstructiveQuestions = generatedQuestions;
                    _logger.LogInformation("Generated {Count} constructive questions", generatedQuestions.Count);
                }
            }
        }

        UpdateProgress(context, 40, "Question generation complete.");

        // ============================================================================
        // PHASE 3: URL Enrichment (40-80%)
        // ============================================================================
        if (syllabusData.Content?.SessionSchedule != null && syllabusData.Content.SessionSchedule.Any())
        {
            UpdateProgress(context, 45, "Enriching learning materials with AI Search...");
            await EnrichUrlsWithBatchQueries(syllabusData, context, cancellationToken);
        }

        UpdateProgress(context, 80, "Content enrichment complete.");

        // ============================================================================
        // PHASE 4: Cache Enriched Data (80-85%)
        // ============================================================================
        UpdateProgress(context, 80, "Caching final dataset...");

        var finalJsonToCache = JsonSerializer.Serialize(syllabusData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true
        });

        await _storage.SaveSyllabusDataAsync(
            syllabusData.SubjectCode,
            syllabusData.VersionNumber,
            syllabusData,
            finalJsonToCache,
            rawTextHash,
            cancellationToken);

        // ============================================================================
        // PHASE 5: Save to Database (85-100%)
        // ============================================================================
        UpdateProgress(context, 90, "Saving subject to database...");

        var existingSubject = await _subjectRepository.FirstOrDefaultAsync(
            s => s.SubjectCode == syllabusData.SubjectCode, cancellationToken);

        if (existingSubject != null)
        {
            await UpdateExistingSubjectContent(existingSubject, syllabusData, request.Semester, cancellationToken);
        }
        else
        {
            await CreateNewSubjectContent(syllabusData, cancellationToken);
        }

        UpdateProgress(context, 100, $"Successfully imported {syllabusData.SubjectName}!");
    }

    private void UpdateProgress(PerformContext context, int percentage, string message)
    {
        // 1. Write to Hangfire Console for detailed logs in Dashboard
        context.WriteLine($"{percentage}% - {message}");

        // 2. Update Job Parameter for API polling
        // We serialize a small JSON object so the frontend can easily read { "percent": 50, "message": "..." }
        var status = new { Percent = percentage, Message = message, Timestamp = DateTime.UtcNow };
        var connection = Hangfire.JobStorage.Current.GetConnection();
        connection.SetJobParameter(context.BackgroundJob.Id, "ImportProgress", JsonSerializer.Serialize(status));
    }

    /// <summary>
    /// PHASE 3: Enrich all sessions with URLs using batch AI query generation.
    /// </summary>
    private async Task EnrichUrlsWithBatchQueries(
        SyllabusData syllabusData,
        PerformContext context,
        CancellationToken cancellationToken)
    {
        var totalSessions = syllabusData.Content.SessionSchedule!.Count;

        // STEP 1: Classify subject category
        var subjectCategory = await _aiQueryService.ClassifySubjectAsync(
            syllabusData.SubjectName,
            syllabusData.SubjectCode,
            syllabusData.Description ?? string.Empty,
            cancellationToken);

        // STEP 2: Build subject context
        var subjectContext = BuildSubjectContext(syllabusData);
        var technologyKeywords = ContextKeywordExtractor.ExtractTechnologyKeywords(subjectContext);

        // STEP 3: BATCH QUERY GENERATION
        var sessionsToEnrich = syllabusData.Content.SessionSchedule
            .Take(MAX_SESSIONS_TO_ENRICH)
            .ToList();

        UpdateProgress(context, 50, $"Generating search queries for {sessionsToEnrich.Count} sessions...");

        var sessionDtos = sessionsToEnrich
            .Select(s => new RogueLearn.User.Application.Models.SyllabusSessionDto
            {
                SessionNumber = s.SessionNumber,
                Topic = s.Topic
            })
            .ToList();

        Dictionary<int, List<string>> batchQueries;
        try
        {
            batchQueries = await _aiQueryService.GenerateBatchQueryVariantsAsync(
                sessionDtos,
                subjectContext,
                subjectCategory,
                technologyKeywords!,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch AI query generation failed.");
            batchQueries = new Dictionary<int, List<string>>();
        }

        var usedUrls = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        UpdateProgress(context, 60, "Searching for high-quality resources...");

        // Rate limit concurrent searches
        var semaphore = new SemaphoreSlim(2, 2);
        int processedCount = 0;

        var enrichmentTasks = sessionsToEnrich.Select(async session =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var existingReadings = (session.Readings ?? new List<string>())
                    .Concat(usedUrls.Keys)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                List<string>? sessionQueries = null;
                if (batchQueries.TryGetValue(session.SessionNumber, out var queries) && queries.Any())
                {
                    sessionQueries = queries;
                }

                var foundUrl = await _readingUrlService.GetValidUrlForTopicAsync(
                    topic: session.Topic,
                    readings: existingReadings,
                    subjectContext: subjectContext,
                    category: subjectCategory,
                    overrideQueries: sessionQueries,
                    isUrlUsedCheck: (url) => usedUrls.ContainsKey(url),
                    cancellationToken: cancellationToken);

                if (!string.IsNullOrWhiteSpace(foundUrl))
                {
                    session.SuggestedUrl = foundUrl;
                    usedUrls.TryAdd(foundUrl, 0);
                }
                else
                {
                    session.SuggestedUrl = string.Empty;
                }
            }
            finally
            {
                semaphore.Release();
                Interlocked.Increment(ref processedCount);
                // Report progress every few items
                if (processedCount % 3 == 0)
                {
                    // Map 60-80% progress range to this loop
                    int progress = 60 + (int)((double)processedCount / sessionsToEnrich.Count * 20);
                    // Don't overwhelm the job storage
                    // context.WriteLine($"Processed {processedCount}/{sessionsToEnrich.Count} sessions..."); 
                }
            }
        });

        await Task.WhenAll(enrichmentTasks);
    }

    private string BuildSubjectContext(SyllabusData syllabusData)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(syllabusData.TechnologyStack))
        {
            parts.Add(syllabusData.TechnologyStack);
        }
        parts.Add(syllabusData.SubjectName ?? "General");
        return string.Join(", ", parts);
    }

    private async Task CreateNewSubjectContent(SyllabusData syllabusData, CancellationToken cancellationToken)
    {
        var newSubject = new Subject
        {
            Id = Guid.NewGuid(),
            SubjectCode = syllabusData.SubjectCode,
            SubjectName = syllabusData.SubjectName,
            Description = syllabusData.Description,
            Credits = syllabusData.Credits,
            Semester = syllabusData.Semester,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var contentJson = JsonSerializer.Serialize(syllabusData.Content);
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new ObjectToInferredTypesConverter() }
        };
        newSubject.Content = JsonSerializer.Deserialize<Dictionary<string, object>>(contentJson, serializerOptions);

        await _subjectRepository.AddAsync(newSubject, cancellationToken);
    }

    private async Task UpdateExistingSubjectContent(Subject subjectToUpdate, SyllabusData syllabusData, int? semesterOverride, CancellationToken cancellationToken)
    {
        var contentJson = JsonSerializer.Serialize(syllabusData.Content);
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new ObjectToInferredTypesConverter() }
        };
        subjectToUpdate.Content = JsonSerializer.Deserialize<Dictionary<string, object>>(contentJson, serializerOptions);

        subjectToUpdate.SubjectName = syllabusData.SubjectName;
        subjectToUpdate.Description = syllabusData.Description;
        subjectToUpdate.Credits = syllabusData.Credits;

        if (semesterOverride.HasValue)
        {
            subjectToUpdate.Semester = semesterOverride.Value;
        }
        else if (syllabusData.Semester.HasValue)
        {
            subjectToUpdate.Semester = syllabusData.Semester.Value;
        }

        if (syllabusData.ApprovedDate.HasValue)
        {
            subjectToUpdate.UpdatedAt = new DateTimeOffset(syllabusData.ApprovedDate.Value.ToDateTime(TimeOnly.MinValue));
        }
        else
        {
            subjectToUpdate.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _subjectRepository.UpdateAsync(subjectToUpdate, cancellationToken);
    }

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}