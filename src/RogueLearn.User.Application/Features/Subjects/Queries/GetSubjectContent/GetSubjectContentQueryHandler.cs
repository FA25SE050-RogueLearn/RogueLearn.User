using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

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

        _logger.LogInformation("📊 Content structure: {KeyCount} keys", subject.Content.Count);

        try
        {
            // ⭐ CRITICAL: Same pattern as Quest Handler's ValidateActivities()
            // Serialize to JSON string, then use JsonElement to parse flexibly
            var jsonString = JsonSerializer.Serialize(
                subject.Content,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    WriteIndented = false
                });

            _logger.LogInformation("✅ Serialized to JSON: {Length} bytes", jsonString.Length);
            _logger.LogDebug("📄 JSON Content: {Json}",
                jsonString.Length > 500 ? jsonString.Substring(0, 500) + "..." : jsonString);

            // ⭐ Parse with JsonElement FIRST - handles flexible structure
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                var root = doc.RootElement;
                var content = new SyllabusContent();

                // Parse CourseDescription
                if (root.TryGetProperty("CourseDescription", out var descElement) &&
                    descElement.ValueKind != JsonValueKind.Null)
                {
                    content.CourseDescription = descElement.GetString();
                }

                // Parse SessionSchedule - manually parse each session
                if (root.TryGetProperty("SessionSchedule", out var sessionElement) &&
                    sessionElement.ValueKind == JsonValueKind.Array)
                {
                    content.SessionSchedule = new List<SyllabusSessionDto>();

                    foreach (var sessionItem in sessionElement.EnumerateArray())
                    {
                        if (sessionItem.ValueKind != JsonValueKind.Object)
                        {
                            _logger.LogWarning("⚠️ SessionSchedule item is not an object, skipping");
                            continue;
                        }

                        var session = new SyllabusSessionDto();

                        // Parse SessionNumber
                        if (sessionItem.TryGetProperty("SessionNumber", out var sessionNumElement))
                        {
                            session.SessionNumber = sessionNumElement.GetInt32();
                        }

                        // Parse Topic
                        if (sessionItem.TryGetProperty("Topic", out var topicElement) &&
                            topicElement.ValueKind != JsonValueKind.Null)
                        {
                            session.Topic = topicElement.GetString() ?? string.Empty;
                        }

                        // Parse Activities
                        if (sessionItem.TryGetProperty("Activities", out var activitiesElement) &&
                            activitiesElement.ValueKind == JsonValueKind.Array)
                        {
                            session.Activities = new List<string>();
                            foreach (var activity in activitiesElement.EnumerateArray())
                            {
                                if (activity.ValueKind != JsonValueKind.Null)
                                {
                                    session.Activities.Add(activity.GetString() ?? string.Empty);
                                }
                            }
                        }

                        // Parse Readings
                        if (sessionItem.TryGetProperty("Readings", out var readingsElement) &&
                            readingsElement.ValueKind == JsonValueKind.Array)
                        {
                            session.Readings = new List<string>();
                            foreach (var reading in readingsElement.EnumerateArray())
                            {
                                if (reading.ValueKind != JsonValueKind.Null)
                                {
                                    session.Readings.Add(reading.GetString() ?? string.Empty);
                                }
                            }
                        }

                        // Parse SuggestedUrl
                        if (sessionItem.TryGetProperty("SuggestedUrl", out var urlElement) &&
                            urlElement.ValueKind != JsonValueKind.Null)
                        {
                            session.SuggestedUrl = urlElement.GetString();
                        }

                        content.SessionSchedule.Add(session);
                    }

                    _logger.LogInformation("✅ Parsed {Count} sessions", content.SessionSchedule.Count);
                }

                // Parse CourseLearningOutcomes
                if (root.TryGetProperty("CourseLearningOutcomes", out var outcomesElement) &&
                    outcomesElement.ValueKind == JsonValueKind.Array)
                {
                    content.CourseLearningOutcomes = new List<CourseLearningOutcome>();
                    foreach (var outcomeItem in outcomesElement.EnumerateArray())
                    {
                        if (outcomeItem.ValueKind != JsonValueKind.Object) continue;

                        var outcome = new CourseLearningOutcome();
                        if (outcomeItem.TryGetProperty("Id", out var idElement))
                            outcome.Id = idElement.GetString() ?? string.Empty;
                        if (outcomeItem.TryGetProperty("Details", out var detailsElement))
                            outcome.Details = detailsElement.GetString() ?? string.Empty;

                        content.CourseLearningOutcomes.Add(outcome);
                    }
                    _logger.LogInformation("✅ Parsed {Count} learning outcomes", content.CourseLearningOutcomes.Count);
                }

                // Parse ConstructiveQuestions
                if (root.TryGetProperty("ConstructiveQuestions", out var questionsElement) &&
                    questionsElement.ValueKind == JsonValueKind.Array)
                {
                    content.ConstructiveQuestions = new List<ConstructiveQuestion>();
                    foreach (var questionItem in questionsElement.EnumerateArray())
                    {
                        if (questionItem.ValueKind != JsonValueKind.Object) continue;

                        var question = new ConstructiveQuestion();
                        if (questionItem.TryGetProperty("Name", out var nameElement))
                            question.Name = nameElement.GetString() ?? string.Empty;
                        if (questionItem.TryGetProperty("Question", out var questionTextElement))
                            question.Question = questionTextElement.GetString() ?? string.Empty;
                        if (questionItem.TryGetProperty("SessionNumber", out var sessionNumQElement))
                            question.SessionNumber = sessionNumQElement.GetInt32();

                        content.ConstructiveQuestions.Add(question);
                    }
                    _logger.LogInformation("✅ Parsed {Count} constructive questions", content.ConstructiveQuestions.Count);
                }

                _logger.LogInformation(
                    "✅ Successfully retrieved content: {Sessions} sessions, {Outcomes} outcomes, {Questions} questions",
                    content.SessionSchedule?.Count ?? 0,
                    content.CourseLearningOutcomes?.Count ?? 0,
                    content.ConstructiveQuestions?.Count ?? 0);

                return content;
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx,
                "❌ JSON error at Path: {Path} - {Message}",
                jsonEx.Path ?? "unknown", jsonEx.Message);
            throw new InvalidOperationException(
                $"Failed to convert subject content: {jsonEx.Message}", jsonEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error during deserialization");
            throw;
        }
    }
}
