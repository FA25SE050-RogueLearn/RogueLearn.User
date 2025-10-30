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

namespace RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

public class ImportSubjectFromTextCommandHandler : IRequestHandler<ImportSubjectFromTextCommand, CreateSubjectResponse>
{
    private readonly ISubjectExtractionPlugin _extractionPlugin;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ImportSubjectFromTextCommandHandler> _logger;

    public ImportSubjectFromTextCommandHandler(
        ISubjectExtractionPlugin extractionPlugin,
        ISubjectRepository subjectRepository,
        IMapper mapper,
        ILogger<ImportSubjectFromTextCommandHandler> logger)
    {
        _extractionPlugin = extractionPlugin;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CreateSubjectResponse> Handle(ImportSubjectFromTextCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting single subject import from raw text.");

        var extractedJson = await _extractionPlugin.ExtractSubjectJsonAsync(request.RawText, cancellationToken);
        if (string.IsNullOrWhiteSpace(extractedJson))
        {
            throw new BadRequestException("AI extraction failed to produce valid JSON from the provided text.");
        }

        SubjectData? subjectData;
        try
        {
            subjectData = JsonSerializer.Deserialize<SubjectData>(extractedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize extracted subject JSON: {Json}", extractedJson);
            throw new BadRequestException("Failed to deserialize the extracted subject data.");
        }

        if (subjectData == null || string.IsNullOrWhiteSpace(subjectData.SubjectCode))
        {
            throw new BadRequestException("Extracted data is missing a valid SubjectCode.");
        }

        // Upsert logic: Update if exists, otherwise create.
        var existingSubject = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == subjectData.SubjectCode, cancellationToken);
        Subject resultSubject;

        if (existingSubject != null)
        {
            _logger.LogInformation("Found existing subject with code {SubjectCode}. Updating record.", subjectData.SubjectCode);
            existingSubject.SubjectName = subjectData.SubjectName;
            existingSubject.Credits = subjectData.Credits;
            existingSubject.Description = subjectData.Description;
            existingSubject.UpdatedAt = DateTimeOffset.UtcNow;
            resultSubject = await _subjectRepository.UpdateAsync(existingSubject, cancellationToken);
            _logger.LogInformation("Successfully updated subject {SubjectId}", resultSubject.Id);
        }
        else
        {
            _logger.LogInformation("No existing subject found for code {SubjectCode}. Creating new record.", subjectData.SubjectCode);
            var newSubject = new Subject
            {
                SubjectCode = subjectData.SubjectCode,
                SubjectName = subjectData.SubjectName,
                Credits = subjectData.Credits,
                Description = subjectData.Description,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            resultSubject = await _subjectRepository.AddAsync(newSubject, cancellationToken);
            _logger.LogInformation("Successfully created subject {SubjectId}", resultSubject.Id);
        }

        return _mapper.Map<CreateSubjectResponse>(resultSubject);
    }
}