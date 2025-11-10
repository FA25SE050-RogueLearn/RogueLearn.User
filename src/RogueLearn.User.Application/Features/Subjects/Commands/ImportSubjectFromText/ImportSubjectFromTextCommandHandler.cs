// RogueLearn.User/src/RogueLearn.User.Application/Features/Subjects/Commands/ImportSubjectFromText/ImportSubjectFromTextCommandHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Common;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

public class SyllabusImportData
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? ApprovedDate { get; set; }
}

public class ImportSubjectFromTextCommandHandler : IRequestHandler<ImportSubjectFromTextCommand, CreateSubjectResponse>
{
    private readonly ISyllabusExtractionPlugin _syllabusExtractionPlugin;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ImportSubjectFromTextCommandHandler> _logger;
    private readonly IHtmlCleaningService _htmlCleaningService;
    private readonly IUserProfileRepository _userProfileRepository; // ADDED

    public ImportSubjectFromTextCommandHandler(
        ISyllabusExtractionPlugin syllabusExtractionPlugin,
        ISubjectRepository subjectRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        IMapper mapper,
        ILogger<ImportSubjectFromTextCommandHandler> logger,
        IHtmlCleaningService htmlCleaningService,
        IUserProfileRepository userProfileRepository) // ADDED
    {
        _syllabusExtractionPlugin = syllabusExtractionPlugin;
        _subjectRepository = subjectRepository;
        _programSubjectRepository = programSubjectRepository;
        _mapper = mapper;
        _logger = logger;
        _htmlCleaningService = htmlCleaningService;
        _userProfileRepository = userProfileRepository; // ADDED
    }

    public async Task<CreateSubjectResponse> Handle(ImportSubjectFromTextCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting single subject syllabus import for user {AuthUserId}.", request.AuthUserId);

        var cleanText = _htmlCleaningService.ExtractCleanTextFromHtml(request.RawText);
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            throw new BadRequestException("Failed to extract meaningful text content from the provided HTML.");
        }

        var extractedJson = await _syllabusExtractionPlugin.ExtractSyllabusJsonAsync(cleanText, cancellationToken);
        if (string.IsNullOrWhiteSpace(extractedJson))
        {
            throw new BadRequestException("AI extraction failed to produce valid JSON from the provided syllabus text.");
        }

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

        if (syllabusData == null || string.IsNullOrWhiteSpace(syllabusData.SubjectCode))
        {
            throw new BadRequestException("Extracted syllabus data is missing a valid SubjectCode.");
        }

        // FIX: Use the new, unambiguous method that searches within the user's full context.
        var existingSubject = await _subjectRepository.GetSubjectForUserContextAsync(
            syllabusData.SubjectCode,
            request.AuthUserId,
            cancellationToken);

        if (existingSubject != null)
        {
            _logger.LogInformation("Found existing subject {SubjectId} within user's context. Updating content.", existingSubject.Id);
            return await UpdateSubjectContent(existingSubject, extractedJson, syllabusData, cancellationToken);
        }

        _logger.LogWarning("Subject with code {SubjectCode} not found within user's context. Creating a new subject shell and linking it to the user's program.", syllabusData.SubjectCode);

        // If the subject does not exist in the user's context, create it and link it to their program.
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
            ?? throw new NotFoundException("UserProfile", request.AuthUserId);

        if (userProfile.RouteId == null)
        {
            throw new BadRequestException("Cannot create a new subject because the user has not selected a curriculum program (route).");
        }

        var newSubject = new Subject { SubjectCode = syllabusData.SubjectCode };

        var createdSubject = await UpdateSubjectContent(newSubject, extractedJson, syllabusData, cancellationToken);

        // Link the newly created subject to the user's program.
        var programSubjectLink = new CurriculumProgramSubject
        {
            ProgramId = userProfile.RouteId.Value,
            SubjectId = createdSubject.Id
        };
        await _programSubjectRepository.AddAsync(programSubjectLink, cancellationToken);
        _logger.LogInformation("Linked new subject {SubjectId} to program {ProgramId}", createdSubject.Id, userProfile.RouteId.Value);

        return createdSubject;
    }

    private async Task<CreateSubjectResponse> UpdateSubjectContent(Subject subjectToUpdate, string extractedJson, SyllabusImportData syllabusData, CancellationToken cancellationToken)
    {
        using (var jsonDoc = JsonDocument.Parse(extractedJson))
        {
            if (jsonDoc.RootElement.TryGetProperty("content", out var contentElement))
            {
                var serializerOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new ObjectToInferredTypesConverter() }
                };
                subjectToUpdate.Content = JsonSerializer.Deserialize<Dictionary<string, object>>(contentElement.GetRawText(), serializerOptions);
            }
        }

        subjectToUpdate.SubjectName = syllabusData.SubjectName;

        if (DateTimeOffset.TryParse(syllabusData.ApprovedDate, out var approvedDate))
        {
            subjectToUpdate.UpdatedAt = approvedDate;
        }
        else
        {
            subjectToUpdate.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var resultSubject = subjectToUpdate.Id == Guid.Empty
            ? await _subjectRepository.AddAsync(subjectToUpdate, cancellationToken)
            : await _subjectRepository.UpdateAsync(subjectToUpdate, cancellationToken);

        _logger.LogInformation("Successfully upserted subject {SubjectId} with new syllabus content.", resultSubject.Id);

        return _mapper.Map<CreateSubjectResponse>(resultSubject);
    }
}