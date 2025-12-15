// src/RogueLearn.User.Application/Features/Subjects/Queries/GetSubjectContent/GetSubjectContentQueryHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json; // Keep for Deserialization
using System.Text.Json.Serialization;
using RogueLearn.User.Application.Common; // For SyllabusSessionDtoConverter

// Alias Newtonsoft to avoid ambiguity
using NewtonsoftJson = Newtonsoft.Json;

namespace RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectContent;

public class GetSubjectContentQueryHandler : IRequestHandler<GetSubjectContentQuery, SyllabusContent>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<GetSubjectContentQueryHandler> _logger;

    public GetSubjectContentQueryHandler(
        ISubjectRepository subjectRepository,
        ILogger<GetSubjectContentQueryHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    public async Task<SyllabusContent> Handle(
        GetSubjectContentQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 Starting GetSubjectContent for Subject {SubjectId}", request.SubjectId);

        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);

        if (subject == null)
        {
            _logger.LogError("❌ Subject with ID {SubjectId} not found", request.SubjectId);
            throw new NotFoundException("Subject", request.SubjectId);
        }

        _logger.LogInformation("✅ Subject found: {Code} - {Name}", subject.SubjectCode, subject.SubjectName);

        if (subject.Content == null || subject.Content.Count == 0)
        {
            _logger.LogWarning("⚠️ Subject {SubjectId} has no content", subject.Id);
            return new SyllabusContent();
        }

        try
        {
            // ---------------------------------------------------------------------------
            // STEP 1: Serialize Dictionary -> JSON String using NEWTONSOFT.JSON
            // ---------------------------------------------------------------------------
            // Supabase's dictionary contains JTokens/JArrays (Newtonsoft types).
            // System.Text.Json CANNOT serialize these correctly. We must use Newtonsoft here.
            var jsonString = NewtonsoftJson.JsonConvert.SerializeObject(subject.Content);

            _logger.LogInformation("✅ Serialized Dictionary to JSON string ({Length} bytes)", jsonString.Length);

            // Log a safe preview of the JSON to verify it looks like valid data (not internal object props)
            var preview = jsonString.Length > 500 ? jsonString.Substring(0, 500) + "..." : jsonString;
            _logger.LogInformation("📄 JSON Content Preview: {Json}", preview);

            // ---------------------------------------------------------------------------
            // STEP 2: Deserialize JSON String -> DTOs using SYSTEM.TEXT.JSON
            // ---------------------------------------------------------------------------
            // Now that we have a valid JSON string, we switch back to System.Text.Json
            // to ensure all [JsonPropertyName] attributes on your DTOs are respected.
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // Critical for case-insensitive matching
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            // Add global converters
            options.Converters.Add(new JsonStringEnumConverter());

            // ADDED: Register the custom converter for SyllabusSessionDto to handle
            // both PascalCase keys AND the {"ValueKind": 7} corruption issue.
            options.Converters.Add(new SyllabusSessionDtoConverter());

            var content = JsonSerializer.Deserialize<SyllabusContent>(jsonString, options);

            if (content == null)
            {
                _logger.LogError("❌ Deserialization resulted in null object for Subject {SubjectId}", subject.Id);
                return new SyllabusContent();
            }

            // ---------------------------------------------------------------------------
            // STEP 3: Initialize Collections
            // ---------------------------------------------------------------------------
            content.CourseLearningOutcomes ??= new List<CourseLearningOutcome>();
            content.SessionSchedule ??= new List<SyllabusSessionDto>();
            content.ConstructiveQuestions ??= new List<ConstructiveQuestion>();

            _logger.LogInformation(
                "✅ Successfully parsed content: {Sessions} sessions, {Outcomes} outcomes, {Questions} questions",
                content.SessionSchedule.Count,
                content.CourseLearningOutcomes.Count,
                content.ConstructiveQuestions.Count);

            return content;
        }
        catch (NewtonsoftJson.JsonException njEx)
        {
            _logger.LogError(njEx, "❌ Newtonsoft Serialization Failed. The database content structure might be corrupted.");
            throw new InvalidOperationException($"Failed to serialize subject content from DB: {njEx.Message}", njEx);
        }
        catch (JsonException stjEx)
        {
            _logger.LogError(stjEx,
                "❌ System.Text.Json Deserialization Failed. Path: {Path} | Error: {Message}",
                stjEx.Path ?? "root", stjEx.Message);

            // Provide a clearer error message for debugging
            throw new InvalidOperationException(
                $"The data in the database is valid JSON but does not match the SyllabusContent schema. Path: {stjEx.Path}. Error: {stjEx.Message}", stjEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error during GetSubjectContent");
            throw;
        }
    }
}