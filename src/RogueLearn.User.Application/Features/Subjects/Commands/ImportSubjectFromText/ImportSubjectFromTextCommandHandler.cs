// RogueLearn.User/src/RogueLearn.User.Application/Features/Subjects/Commands/ImportSubjectFromText/ImportSubjectFromTextCommandHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

public class ImportSubjectFromTextCommandHandler : IRequestHandler<ImportSubjectFromTextCommand, CreateSubjectResponse>
{
    // CORRECT DEPENDENCY: This handler only needs the syllabus plugin.
    private readonly ISyllabusExtractionPlugin _syllabusExtractionPlugin;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ImportSubjectFromTextCommandHandler> _logger;

    public ImportSubjectFromTextCommandHandler(
        ISyllabusExtractionPlugin syllabusExtractionPlugin,
        ISubjectRepository subjectRepository,
        IMapper mapper,
        ILogger<ImportSubjectFromTextCommandHandler> logger)
    {
        _syllabusExtractionPlugin = syllabusExtractionPlugin;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CreateSubjectResponse> Handle(ImportSubjectFromTextCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting single subject syllabus import from raw text.");

        // CORRECT METHOD CALL: Use the syllabus-specific extraction method.
        var extractedJson = await _syllabusExtractionPlugin.ExtractSyllabusJsonAsync(request.RawText, cancellationToken);
        if (string.IsNullOrWhiteSpace(extractedJson))
        {
            throw new BadRequestException("AI extraction failed to produce valid JSON from the provided syllabus text.");
        }

        // We deserialize to a temporary object to get the identifiers for our database query.
        SyllabusImportData? syllabusData;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
            syllabusData = JsonSerializer.Deserialize<SyllabusImportData>(extractedJson, options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize extracted syllabus JSON: {Json}", extractedJson);
            throw new BadRequestException("Failed to deserialize the extracted syllabus data.");
        }

        if (syllabusData == null || string.IsNullOrWhiteSpace(syllabusData.SubjectCode) || string.IsNullOrWhiteSpace(syllabusData.Version))
        {
            throw new BadRequestException("Extracted syllabus data is missing a valid SubjectCode or Version.");
        }

        // Find the existing subject shell that was created during the curriculum import.
        var existingSubject = await _subjectRepository.FirstOrDefaultAsync(
            s => s.SubjectCode == syllabusData.SubjectCode && s.Version == syllabusData.Version,
            cancellationToken);

        if (existingSubject == null)
        {
            // In a strict workflow, this should be an error. The subject shell should already exist.
            _logger.LogError("Subject with Code {SubjectCode} and Version {Version} not found. Please import the main curriculum first.", syllabusData.SubjectCode, syllabusData.Version);
            throw new NotFoundException($"Subject with Code '{syllabusData.SubjectCode}' and Version '{syllabusData.Version}' not found. It must be created via curriculum import before its syllabus can be added.");
        }

        _logger.LogInformation("Found existing subject {SubjectId}. Updating with syllabus content.", existingSubject.Id);

        // CORRECT PERSISTENCE LOGIC:
        // 1. Deserialize the "content" part of the JSON into a dictionary.
        // 2. Assign this dictionary to the subject's Content property.
        using (var jsonDoc = JsonDocument.Parse(extractedJson))
        {
            if (jsonDoc.RootElement.TryGetProperty("content", out var contentElement))
            {
                var contentDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(contentElement.GetRawText());
                existingSubject.Content = contentDictionary;
            }
        }

        // Optionally update other metadata from the syllabus if needed
        existingSubject.SubjectName = syllabusData.SubjectName;
        existingSubject.UpdatedAt = DateTimeOffset.UtcNow;

        var resultSubject = await _subjectRepository.UpdateAsync(existingSubject, cancellationToken);
        _logger.LogInformation("Successfully updated subject {SubjectId} with syllabus content.", resultSubject.Id);

        return _mapper.Map<CreateSubjectResponse>(resultSubject);
    }
}

// A temporary DTO for deserializing the top-level fields from the syllabus JSON
public class SyllabusImportData
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}