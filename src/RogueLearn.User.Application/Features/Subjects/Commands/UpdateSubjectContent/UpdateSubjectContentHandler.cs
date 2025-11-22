using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubjectContent;

/// <summary>
/// Handler for updating subject content (syllabus JSON).
/// Converts SyllabusContent model to Dictionary<string, object> (JSONB).
/// 
/// Pattern: Supabase expects JSONB as Dictionary<string, object>
/// We convert from our strongly-typed SyllabusContent model.
/// </summary>
public class UpdateSubjectContentHandler : IRequestHandler<UpdateSubjectContentCommand, SyllabusContent>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<UpdateSubjectContentHandler> _logger;

    public UpdateSubjectContentHandler(
        ISubjectRepository subjectRepository,
        ILogger<UpdateSubjectContentHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    /// <summary>
    /// Executes the command to update subject content.
    /// </summary>
    public async Task<SyllabusContent> Handle(
        UpdateSubjectContentCommand request,
        CancellationToken cancellationToken)
    {
        // Validate command
        if (request.Content == null)
        {
            _logger.LogError("UpdateSubjectContentCommand has null Content");
            throw new ArgumentNullException(nameof(request.Content), "Subject content cannot be null.");
        }

        // Fetch the subject
        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);

        if (subject == null)
        {
            _logger.LogError("Subject with ID {SubjectId} not found", request.SubjectId);
            throw new NotFoundException("Subject", request.SubjectId);
        }

        // ⭐ CRITICAL: Convert SyllabusContent model to Dictionary<string, object> (JSONB)
        try
        {
            // Step 1: Serialize SyllabusContent to JSON string
            var jsonString = JsonSerializer.Serialize(
                request.Content,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

            _logger.LogInformation(
                "Serialized subject content for subject {SubjectId}: {Length} bytes",
                subject.Id,
                jsonString.Length);

            // Step 2: Deserialize JSON string to Dictionary<string, object>
            // This is what Supabase expects for JSONB columns
            var contentDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                jsonString,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (contentDict == null)
            {
                throw new InvalidOperationException("Failed to convert content to Dictionary");
            }

            // Update the subject's content
            subject.Content = contentDict;
            subject.UpdatedAt = DateTimeOffset.UtcNow;

            // Save to database via repository
            // GenericRepository handles the Supabase update automatically
            await _subjectRepository.UpdateAsync(subject, cancellationToken);

            _logger.LogInformation(
                "Successfully updated subject {SubjectId} with {Sessions} sessions",
                subject.Id,
                request.Content.SessionSchedule?.Count ?? 0);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx,
                "JSON conversion error for subject {SubjectId}",
                subject.Id);
            throw new InvalidOperationException(
                $"Failed to convert subject content: {jsonEx.Message}", jsonEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error updating subject {SubjectId}",
                subject.Id);
            throw;
        }

        // Return the content object (not the Dictionary)
        return request.Content;
    }
}
