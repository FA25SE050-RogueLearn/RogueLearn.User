// RogueLearn.User/src/RogueLearn.User.Application/Features/Subjects/Commands/UpdateSubjectContent/UpdateSubjectContentHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json; // Keep System.Text.Json for serializing the DTO
using System.Text.Json.Serialization;

// ⭐ Alias Newtonsoft to handle the Dictionary conversion safely
using NewtonsoftJson = Newtonsoft.Json;

namespace RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubjectContent;

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

        try
        {
            // STEP 1: Serialize DTO -> JSON String (using System.Text.Json)
            // We MUST use System.Text.Json here to respect the [JsonPropertyName] attributes
            // defined in your SyllabusContent/SyllabusSessionDto classes.
            var jsonString = JsonSerializer.Serialize(
                request.Content,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Converters = { new JsonStringEnumConverter() }
                });

            _logger.LogInformation(
                "Serialized subject content for subject {SubjectId}: {Length} bytes",
                subject.Id,
                jsonString.Length);

            // STEP 2: Deserialize JSON String -> Dictionary (using NEWTONSOFT.JSON)
            // ⭐ FIXED: We use Newtonsoft here. System.Text.Json would create 'JsonElement' objects,
            // which results in {"ValueKind": ...} artifacts when saved to Supabase.
            // Newtonsoft creates JObjects/JArrays/Primitives which Supabase handles correctly.
            var contentDict = NewtonsoftJson.JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

            if (contentDict == null)
            {
                throw new InvalidOperationException("Failed to convert content to Dictionary");
            }

            // Update the subject's content
            subject.Content = contentDict;
            subject.UpdatedAt = DateTimeOffset.UtcNow;

            // Save to database
            await _subjectRepository.UpdateAsync(subject, cancellationToken);

            _logger.LogInformation(
                "Successfully updated subject {SubjectId} with {Sessions} sessions",
                subject.Id,
                request.Content.SessionSchedule?.Count ?? 0);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "System.Text.Json serialization error for subject {SubjectId}", subject.Id);
            throw new InvalidOperationException($"Failed to serialize DTO: {jsonEx.Message}", jsonEx);
        }
        catch (NewtonsoftJson.JsonException njEx)
        {
            _logger.LogError(njEx, "Newtonsoft deserialization error for subject {SubjectId}", subject.Id);
            throw new InvalidOperationException($"Failed to prepare dictionary for DB: {njEx.Message}", njEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating subject {SubjectId}", subject.Id);
            throw;
        }

        return request.Content;
    }
}