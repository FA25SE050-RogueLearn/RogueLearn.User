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
using System.Security.Cryptography;
using System.Text;

namespace RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

public class ImportSubjectFromTextCommandHandler : IRequestHandler<ImportSubjectFromTextCommand, CreateSubjectResponse>
{
    private readonly ISyllabusExtractionPlugin _syllabusExtractionPlugin;
    private readonly IConstructiveQuestionGenerationPlugin _questionGenerationPlugin;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ImportSubjectFromTextCommandHandler> _logger;
    private readonly IHtmlCleaningService _htmlCleaningService;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurriculumImportStorage _storage;

    public ImportSubjectFromTextCommandHandler(
        ISyllabusExtractionPlugin syllabusExtractionPlugin,
        IConstructiveQuestionGenerationPlugin questionGenerationPlugin,
        ISubjectRepository subjectRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        IMapper mapper,
        ILogger<ImportSubjectFromTextCommandHandler> logger,
        IHtmlCleaningService htmlCleaningService,
        IUserProfileRepository userProfileRepository,
        ICurriculumImportStorage storage)
    {
        _syllabusExtractionPlugin = syllabusExtractionPlugin;
        _questionGenerationPlugin = questionGenerationPlugin;
        _subjectRepository = subjectRepository;
        _programSubjectRepository = programSubjectRepository;
        _mapper = mapper;
        _logger = logger;
        _htmlCleaningService = htmlCleaningService;
        _userProfileRepository = userProfileRepository;
        _storage = storage;
    }

    public async Task<CreateSubjectResponse> Handle(ImportSubjectFromTextCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting single subject syllabus import for user {AuthUserId}.", request.AuthUserId);

        var cleanText = _htmlCleaningService.ExtractCleanTextFromHtml(request.RawText);
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            throw new BadRequestException("Failed to extract meaningful text content from the provided HTML.");
        }

        var rawTextHash = ComputeSha256Hash(cleanText);

        string? extractedJson = await _storage.TryGetCachedSyllabusDataAsync(rawTextHash, cancellationToken);
        bool isCacheHit = !string.IsNullOrWhiteSpace(extractedJson);

        if (isCacheHit)
        {
            _logger.LogInformation("Cache HIT for syllabus hash {Hash}. Skipping AI extraction.", rawTextHash);
        }
        else
        {
            _logger.LogInformation("Cache MISS for syllabus hash {Hash}. Proceeding with AI extraction.", rawTextHash);
            extractedJson = await _syllabusExtractionPlugin.ExtractSyllabusJsonAsync(cleanText, cancellationToken);
            if (string.IsNullOrWhiteSpace(extractedJson))
            {
                throw new BadRequestException("AI extraction failed to produce valid JSON from the provided syllabus text.");
            }
        }

        SyllabusData? syllabusData;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
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

        if (syllabusData.Content?.ConstructiveQuestions == null || !syllabusData.Content.ConstructiveQuestions.Any())
        {
            _logger.LogInformation("No constructive questions found in extracted syllabus. Attempting to generate them with AI.");
            if (syllabusData.Content?.SessionSchedule != null && syllabusData.Content.SessionSchedule.Any())
            {
                var generatedQuestions = await _questionGenerationPlugin.GenerateQuestionsAsync(syllabusData.Content.SessionSchedule, cancellationToken);
                if (generatedQuestions.Any())
                {
                    syllabusData.Content.ConstructiveQuestions = generatedQuestions;
                    _logger.LogInformation("Successfully generated {Count} constructive questions for the syllabus.", generatedQuestions.Count);
                }
                else
                {
                    _logger.LogWarning("AI question generation did not produce any questions.");
                }
            }
        }

        var finalJsonToCache = JsonSerializer.Serialize(syllabusData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } });

        if (!isCacheHit)
        {
            await _storage.SaveSyllabusDataAsync(
                syllabusData.SubjectCode,
                syllabusData.VersionNumber,
                syllabusData,
                finalJsonToCache,
                rawTextHash,
                cancellationToken);
            _logger.LogInformation("Cache WRITE for syllabus hash {Hash}.", rawTextHash);
        }

        var existingSubject = await _subjectRepository.GetSubjectForUserContextAsync(
            syllabusData.SubjectCode,
            request.AuthUserId,
            cancellationToken);

        if (existingSubject != null)
        {
            _logger.LogInformation("Found existing subject {SubjectId} within user's context. Updating content.", existingSubject.Id);
            return await UpdateExistingSubjectContent(existingSubject, syllabusData, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Subject with code {SubjectCode} not found within user's context. Creating a new subject shell and linking it to the user's program.", syllabusData.SubjectCode);

            var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
                ?? throw new NotFoundException("UserProfile", request.AuthUserId);

            if (userProfile.RouteId == null)
            {
                throw new BadRequestException("Cannot create a new subject because the user has not selected a curriculum program (route).");
            }

            var newSubject = new Subject
            {
                SubjectCode = syllabusData.SubjectCode,
                Description = syllabusData.Description,
                Credits = syllabusData.Credits,
                // MODIFICATION: Populate semester and prerequisites on creation.
                Semester = syllabusData.Semester,
                PrerequisiteSubjectIds = await ResolvePrerequisiteIdsAsync(syllabusData.PreRequisite, cancellationToken)
            };

            var contentJson = JsonSerializer.Serialize(syllabusData.Content);
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new ObjectToInferredTypesConverter() }
            };
            newSubject.Content = JsonSerializer.Deserialize<Dictionary<string, object>>(contentJson, serializerOptions);

            newSubject.SubjectName = syllabusData.SubjectName;

            if (syllabusData.ApprovedDate.HasValue)
            {
                newSubject.UpdatedAt = new DateTimeOffset(syllabusData.ApprovedDate.Value.ToDateTime(TimeOnly.MinValue));
            }

            var createdSubjectEntity = await _subjectRepository.AddAsync(newSubject, cancellationToken);
            _logger.LogInformation("Successfully created new subject {SubjectId} with syllabus content.", createdSubjectEntity.Id);

            var programSubjectLink = new CurriculumProgramSubject
            {
                ProgramId = userProfile.RouteId.Value,
                SubjectId = createdSubjectEntity.Id
            };
            await _programSubjectRepository.AddAsync(programSubjectLink, cancellationToken);
            _logger.LogInformation("Linked new subject {SubjectId} to program {ProgramId}", createdSubjectEntity.Id, userProfile.RouteId.Value);

            return _mapper.Map<CreateSubjectResponse>(createdSubjectEntity);
        }
    }

    private async Task<CreateSubjectResponse> UpdateExistingSubjectContent(Subject subjectToUpdate, SyllabusData syllabusData, CancellationToken cancellationToken)
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

        // MODIFICATION START: Implement intelligent, conditional updates to prevent data loss.
        // Only update the semester if the syllabus explicitly provides a value.
        if (syllabusData.Semester.HasValue)
        {
            subjectToUpdate.Semester = syllabusData.Semester.Value;
        }

        // Only update prerequisites if the syllabus text provides them.
        if (!string.IsNullOrWhiteSpace(syllabusData.PreRequisite))
        {
            subjectToUpdate.PrerequisiteSubjectIds = await ResolvePrerequisiteIdsAsync(syllabusData.PreRequisite, cancellationToken);
        }
        // MODIFICATION END

        if (syllabusData.ApprovedDate.HasValue)
        {
            subjectToUpdate.UpdatedAt = new DateTimeOffset(syllabusData.ApprovedDate.Value.ToDateTime(TimeOnly.MinValue));
        }
        else
        {
            subjectToUpdate.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var resultSubject = await _subjectRepository.UpdateAsync(subjectToUpdate, cancellationToken);

        _logger.LogInformation("Successfully updated subject {SubjectId} with new syllabus content.", resultSubject.Id);

        return _mapper.Map<CreateSubjectResponse>(resultSubject);
    }

    // MODIFICATION START: Added a new helper method to resolve subject codes to UUIDs.
    private async Task<Guid[]> ResolvePrerequisiteIdsAsync(string? preRequisiteText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(preRequisiteText))
        {
            return Array.Empty<Guid>();
        }

        var codes = preRequisiteText.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(code => code.Trim())
                                    .ToList();

        if (!codes.Any())
        {
            return Array.Empty<Guid>();
        }

        var prereqIds = new List<Guid>();
        foreach (var code in codes)
        {
            // This query assumes subject codes are unique.
            var subject = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == code, cancellationToken);
            if (subject != null)
            {
                prereqIds.Add(subject.Id);
            }
            else
            {
                _logger.LogWarning("Could not resolve prerequisite subject code '{SubjectCode}' to a valid subject ID.", code);
            }
        }

        return prereqIds.ToArray();
    }
    // MODIFICATION END

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}