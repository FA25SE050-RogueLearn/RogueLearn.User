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
    private readonly ISubjectRepository _subjectRepository;
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ImportSubjectFromTextCommandHandler> _logger;
    private readonly IHtmlCleaningService _htmlCleaningService;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurriculumImportStorage _storage;

    public ImportSubjectFromTextCommandHandler(
        ISyllabusExtractionPlugin syllabusExtractionPlugin,
        ISubjectRepository subjectRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        IMapper mapper,
        ILogger<ImportSubjectFromTextCommandHandler> logger,
        IHtmlCleaningService htmlCleaningService,
        IUserProfileRepository userProfileRepository,
        ICurriculumImportStorage storage)
    {
        _syllabusExtractionPlugin = syllabusExtractionPlugin;
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
            syllabusData = JsonSerializer.Deserialize<SyllabusData>(extractedJson, options);
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

        if (!isCacheHit)
        {
            await _storage.SaveSyllabusDataAsync(
                syllabusData.SubjectCode,
                syllabusData.VersionNumber,
                syllabusData,
                extractedJson,
                rawTextHash,
                cancellationToken);
            _logger.LogInformation("Cache WRITE for syllabus hash {Hash}.", rawTextHash);
        }

        var existingSubject = await _subjectRepository.GetSubjectForUserContextAsync(
            syllabusData.SubjectCode,
            request.AuthUserId,
            cancellationToken);

        // MODIFICATION START: The logic is now correctly bifurcated to handle updates and creations separately.
        if (existingSubject != null)
        {
            _logger.LogInformation("Found existing subject {SubjectId} within user's context. Updating content.", existingSubject.Id);
            return await UpdateExistingSubjectContent(existingSubject, extractedJson, syllabusData, cancellationToken);
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

            // Create a new Subject entity. Its ID will be Guid.Empty by default.
            var newSubject = new Subject { SubjectCode = syllabusData.SubjectCode, Description = syllabusData.Description };

            using (var jsonDoc = JsonDocument.Parse(extractedJson))
            {
                if (jsonDoc.RootElement.TryGetProperty("content", out var contentElement))
                {
                    var serializerOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new ObjectToInferredTypesConverter() }
                    };
                    newSubject.Content = JsonSerializer.Deserialize<Dictionary<string, object>>(contentElement.GetRawText(), serializerOptions);
                }
            }

            newSubject.SubjectName = syllabusData.SubjectName;

            if (syllabusData.ApprovedDate.HasValue)
            {
                newSubject.UpdatedAt = new DateTimeOffset(syllabusData.ApprovedDate.Value.ToDateTime(TimeOnly.MinValue));
            }

            // Call AddAsync for the new entity. This is the correct operation.
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
        // MODIFICATION END
    }

    private async Task<CreateSubjectResponse> UpdateExistingSubjectContent(Subject subjectToUpdate, string extractedJson, SyllabusData syllabusData, CancellationToken cancellationToken)
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
        // Also update the top-level description from the syllabus data.
        subjectToUpdate.Description = syllabusData.Description;

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

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}