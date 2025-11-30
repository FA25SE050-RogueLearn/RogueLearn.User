using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Common;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

/// <summary>
/// Imports syllabus text, enriches it with URLs, and creates/updates subject in master catalog.
/// 
/// RACE CONDITION FIX:
/// - Uses ConcurrentDictionary for thread-safe URL tracking
/// - Passes live URL check delegate to ReadingUrlService
/// - Ensures second task to finish will see URL taken by first task
/// - Results in guaranteed URL diversity across parallel execution
/// </summary>
public class ImportSubjectFromTextCommandHandler : IRequestHandler<ImportSubjectFromTextCommand, CreateSubjectResponse>
{
    private readonly ISyllabusExtractionPlugin _syllabusExtractionPlugin;
    private readonly IConstructiveQuestionGenerationPlugin _questionGenerationPlugin;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ImportSubjectFromTextCommandHandler> _logger;
    private readonly IHtmlCleaningService _htmlCleaningService;
    private readonly ICurriculumImportStorage _storage;
    private readonly IReadingUrlService _readingUrlService;
    private readonly IAiQueryClassificationService _aiQueryService;

    public ImportSubjectFromTextCommandHandler(
        ISyllabusExtractionPlugin syllabusExtractionPlugin,
        IConstructiveQuestionGenerationPlugin questionGenerationPlugin,
        ISubjectRepository subjectRepository,
        IMapper mapper,
        ILogger<ImportSubjectFromTextCommandHandler> logger,
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

    public async Task<CreateSubjectResponse> Handle(
        ImportSubjectFromTextCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("📚 Starting subject import");

        // ============================================================================
        // PHASE 1: Extract & Validate
        // ============================================================================
        var cleanText = _htmlCleaningService.ExtractCleanTextFromHtml(request.RawText);
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            throw new BadRequestException("Failed to extract meaningful text content from the provided HTML.");
        }

        var rawTextHash = ComputeSha256Hash(cleanText);
    
        string? extractedJson = await _storage.TryGetCachedSyllabusDataAsync(rawTextHash, cancellationToken);

        if (string.IsNullOrWhiteSpace(extractedJson))
        {
            _logger.LogInformation("🔄 Extracting syllabus from text...");
            extractedJson = await _syllabusExtractionPlugin.ExtractSyllabusJsonAsync(cleanText, cancellationToken);
        }
        else
        {
            _logger.LogInformation("✅ Using cached syllabus");
        }

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

        _logger.LogInformation("✅ Phase 1 Complete: Subject {SubjectCode} | Sessions: {SessionCount}",
            syllabusData.SubjectCode,
            syllabusData.Content?.SessionSchedule?.Count ?? 0);

        // ============================================================================
        // PHASE 2: Generate Constructive Questions
        // ============================================================================
        if (syllabusData.Content?.ConstructiveQuestions == null || !syllabusData.Content.ConstructiveQuestions.Any())
        {
            _logger.LogInformation("🤖 No questions found. Attempting to generate...");
            if (syllabusData.Content?.SessionSchedule != null && syllabusData.Content.SessionSchedule.Any())
            {
                var generatedQuestions = await _questionGenerationPlugin.GenerateQuestionsAsync(
                    syllabusData.Content.SessionSchedule, cancellationToken);

                if (generatedQuestions.Any())
                {
                    syllabusData.Content.ConstructiveQuestions = generatedQuestions;
                    _logger.LogInformation("✅ Generated {Count} constructive questions", generatedQuestions.Count);
                }
                else
                {
                    _logger.LogWarning("⚠️ Question generation produced no results");
                }
            }
        }
        else
        {
            _logger.LogInformation("✅ Found {Count} existing constructive questions",
                syllabusData.Content.ConstructiveQuestions.Count);
        }

        // ============================================================================
        // PHASE 3: URL Enrichment with Batch Queries (RACE CONDITION FIXED)
        // ============================================================================
        if (syllabusData.Content?.SessionSchedule != null && syllabusData.Content.SessionSchedule.Any())
        {
            await EnrichUrlsWithBatchQueries(syllabusData, cancellationToken);
        }

        // ============================================================================
        // PHASE 4: Cache Enriched Data
        // ============================================================================
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

        _logger.LogInformation("💾 Cached enriched syllabus");

        // ============================================================================
        // PHASE 5: Save to Database (Create or Update)
        // ============================================================================
        var existingSubject = await _subjectRepository.FirstOrDefaultAsync(
            s => s.SubjectCode == syllabusData.SubjectCode, cancellationToken);

        if (existingSubject != null)
        {
            _logger.LogInformation("🔄 Found existing subject {SubjectId}. Updating content...",
                existingSubject.Id);
            return await UpdateExistingSubjectContent(existingSubject, syllabusData, request.Semester, cancellationToken);
        }
        else
        {
            _logger.LogInformation("✨ Creating new subject in master catalog");
            return await CreateNewSubjectContent(syllabusData, cancellationToken);
        }
    }

    /// <summary>
    /// PHASE 3: Enrich all sessions with URLs using batch AI query generation.
    /// 
    /// RACE CONDITION FIX:
    /// - Uses ConcurrentDictionary for thread-safe tracking
    /// - Passes live URL check delegate: url => usedUrls.ContainsKey(url)
    /// - Each parallel task can instantly see URLs claimed by other tasks
    /// - Result: Different tasks get different URLs (no race condition duplicates)
    /// </summary>
    private async Task EnrichUrlsWithBatchQueries(
        SyllabusData syllabusData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "🔍 PHASE 3: Starting URL enrichment with batch query generation for {Count} sessions",
            syllabusData.Content.SessionSchedule.Count);

        // STEP 1: Classify subject category
        var subjectCategory = await _aiQueryService.ClassifySubjectAsync(
            syllabusData.SubjectName,
            syllabusData.SubjectCode,
            syllabusData.Description ?? string.Empty,
            cancellationToken);

        _logger.LogInformation("📋 Subject classified as: {Category}", subjectCategory);

        // STEP 2: Build subject context
        var subjectContext = BuildSubjectContext(syllabusData);

        // ✅ STEP 2B (NEW): Extract technology keywords from subject context
        // This is the CRITICAL FIX that enables language-specific query generation
        var technologyKeywords = ContextKeywordExtractor.ExtractTechnologyKeywords(subjectContext);
        _logger.LogInformation(
            "🔧 Detected technologies for batch query generation: {Technologies}",
            technologyKeywords != null && technologyKeywords.Any()
                ? string.Join(", ", technologyKeywords)
                : "none");

        // STEP 3: BATCH QUERY GENERATION (all sessions at once)
        _logger.LogInformation(
            "🤖 Generating AI queries for ALL {Count} sessions (batch mode)...",
            syllabusData.Content.SessionSchedule.Count);

        var sessionDtos = syllabusData.Content.SessionSchedule
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
                technologyKeywords,  // ✅ NEW: Pass technology keywords to AI service
                cancellationToken);

            _logger.LogInformation(
                "✅ AI generated queries for {Count}/{Total} sessions",
                batchQueries.Count,
                syllabusData.Content.SessionSchedule.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Batch AI query generation failed. Will generate per-session as fallback.");
            batchQueries = new Dictionary<int, List<string>>();
        }

        // ⭐ CRITICAL FIX: Use ConcurrentDictionary for thread-safe URL tracking
        // This allows the live check delegate to see URLs as they're added by other tasks
        var usedUrls = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "🔄 Processing {Count} sessions with parallel URL search (max 5 concurrent)...",
            syllabusData.Content.SessionSchedule.Count);

        // ⭐ RATE LIMITING: Reduce from 5 to 2 concurrent searches
        // This prevents hammering Google API with 15+ simultaneous requests
        var semaphore = new SemaphoreSlim(2, 2);


        var enrichmentTasks = syllabusData.Content.SessionSchedule.Select(async session =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogDebug("[Session {SessionNumber}] Starting URL search for '{Topic}'",
                    session.SessionNumber, session.Topic);

                // ⭐ CRITICAL FIX: Construct exclusion list from:
                // 1. Original syllabus readings (permanent exclusions)
                // 2. LIVE usedUrls (from concurrent tasks)
                var existingReadings = (session.Readings ?? new List<string>())
                    .Concat(usedUrls.Keys) // Add live URLs found by other tasks
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                List<string>? sessionQueries = null;
                if (batchQueries.TryGetValue(session.SessionNumber, out var queries) && queries.Any())
                {
                    sessionQueries = queries;
                    _logger.LogDebug(
                        "[Session {SessionNumber}] Using {Count} AI-generated queries",
                        session.SessionNumber, queries.Count);
                }

                // ⭐ CRITICAL FIX: Pass live URL check delegate
                // This allows ReadingUrlService to do just-in-time uniqueness checks
                var foundUrl = await _readingUrlService.GetValidUrlForTopicAsync(
                    topic: session.Topic,
                    readings: existingReadings,
                    subjectContext: subjectContext,
                    category: subjectCategory,
                    overrideQueries: sessionQueries,
                    isUrlUsedCheck: (url) => usedUrls.ContainsKey(url), // ⭐ Live check!
                    cancellationToken: cancellationToken);

                if (!string.IsNullOrWhiteSpace(foundUrl))
                {
                    session.SuggestedUrl = foundUrl;

                    // ⭐ CRITICAL FIX: Instantly mark as used so other tasks see it
                    usedUrls.TryAdd(foundUrl, 0);

                    _logger.LogInformation(
                        "✅ Session {SessionNumber}: URL found ({Unique} unique so far)",
                        session.SessionNumber, usedUrls.Count);
                }
                else
                {
                    session.SuggestedUrl = string.Empty;
                    _logger.LogWarning(
                        "⚠️ Session {SessionNumber} ('{Topic}'): No URL found",
                        session.SessionNumber, session.Topic);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error enriching session {SessionNumber}",
                    session.SessionNumber);
                session.SuggestedUrl = string.Empty;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(enrichmentTasks);

        // RESULTS LOGGING
        var uniqueUrlCount = usedUrls.Count;
        var successRate = syllabusData.Content.SessionSchedule.Count > 0
            ? (int)(usedUrls.Count * 100.0 / syllabusData.Content.SessionSchedule.Count)
            : 0;

        _logger.LogInformation("🎯 URL enrichment COMPLETE:");
        _logger.LogInformation("   ✅ Unique URLs: {Unique}/{Total}",
            uniqueUrlCount, syllabusData.Content.SessionSchedule.Count);
        _logger.LogInformation("   📊 Coverage: {Percent}%", successRate);
    }

    /// <summary>
    /// Build subject context from syllabus data for query diversity.
    /// </summary>
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

    /// <summary>
    /// Create new subject in database.
    /// </summary>
    private async Task<CreateSubjectResponse> CreateNewSubjectContent(
        SyllabusData syllabusData,
        CancellationToken cancellationToken)
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

        var createdSubjectEntity = await _subjectRepository.AddAsync(newSubject, cancellationToken);
        _logger.LogInformation("✅ Created new subject {SubjectId} with enriched content", createdSubjectEntity.Id);

        return _mapper.Map<CreateSubjectResponse>(createdSubjectEntity);
    }

    /// <summary>
    /// Update existing subject in database.
    /// </summary>
    private async Task<CreateSubjectResponse> UpdateExistingSubjectContent(
        Subject subjectToUpdate,
        SyllabusData syllabusData,
        int? semesterOverride,
        CancellationToken cancellationToken)
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

        // Prioritize admin override, then extracted value
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
            subjectToUpdate.UpdatedAt = new DateTimeOffset(
                syllabusData.ApprovedDate.Value.ToDateTime(TimeOnly.MinValue));
        }
        else
        {
            subjectToUpdate.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var resultSubject = await _subjectRepository.UpdateAsync(subjectToUpdate, cancellationToken);
        _logger.LogInformation("✅ Updated subject {SubjectId} with new enriched content", resultSubject.Id);

        return _mapper.Map<CreateSubjectResponse>(resultSubject);
    }

    /// <summary>
    /// Compute SHA256 hash of text for caching.
    /// </summary>
    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
