// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;
// ADDED: For hashing the raw HTML content to create a unique cache key.
using System.Security.Cryptography;
using System.Text;

namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

public class FapRecordData
{
    public double? Gpa { get; set; }
    public List<FapSubjectData> Subjects { get; set; } = new();
}

public class FapSubjectData
{
    public string SubjectCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double? Mark { get; set; }
    public int Semester { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
}

public class ProcessAcademicRecordCommandHandler : IRequestHandler<ProcessAcademicRecordCommand, ProcessAcademicRecordResponse>
{
    private readonly IFapExtractionPlugin _fapPlugin;
    private readonly IStudentEnrollmentRepository _enrollmentRepository;
    private readonly IStudentSemesterSubjectRepository _semesterSubjectRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ICurriculumProgramRepository _programRepository;
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly IClassSpecializationSubjectRepository _classSpecializationSubjectRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IHtmlCleaningService _htmlCleaningService;
    private readonly ILogger<ProcessAcademicRecordCommandHandler> _logger;
    private readonly IMediator _mediator;
    // ADDED: Inject the storage service to handle caching operations.
    private readonly ICurriculumImportStorage _storage;

    public ProcessAcademicRecordCommandHandler(
        IFapExtractionPlugin fapPlugin,
        IStudentEnrollmentRepository enrollmentRepository,
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ISubjectRepository subjectRepository,
        ICurriculumProgramRepository programRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository,
        IUserProfileRepository userProfileRepository,
        IHtmlCleaningService htmlCleaningService,
        ILogger<ProcessAcademicRecordCommandHandler> logger,
        IMediator mediator,
        // ADDED: The storage service is now a required dependency.
        ICurriculumImportStorage storage)
    {
        _fapPlugin = fapPlugin;
        _enrollmentRepository = enrollmentRepository;
        _semesterSubjectRepository = semesterSubjectRepository;
        _subjectRepository = subjectRepository;
        _programRepository = programRepository;
        _programSubjectRepository = programSubjectRepository;
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
        _userProfileRepository = userProfileRepository;
        _htmlCleaningService = htmlCleaningService;
        _logger = logger;
        _mediator = mediator;
        // ADDED: Assign the injected storage service to the private field.
        _storage = storage;
    }

    public async Task<ProcessAcademicRecordResponse> Handle(ProcessAcademicRecordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[START] Processing academic record for user {AuthUserId}", request.AuthUserId);

        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), request.AuthUserId);

        if (!userProfile.ClassId.HasValue)
        {
            throw new BadRequestException("User has not selected a specialization class. Onboarding may be incomplete.");
        }

        if (!await _programRepository.ExistsAsync(request.CurriculumProgramId, cancellationToken))
        {
            _logger.LogWarning("Invalid CurriculumProgramId provided: {ProgramId}", request.CurriculumProgramId);
            throw new NotFoundException(nameof(CurriculumProgram), request.CurriculumProgramId);
        }

        var allowedSubjectIds = await BuildAllowedSubjectList(request.CurriculumProgramId, userProfile.ClassId.Value, cancellationToken);

        // --- CACHING LOGIC START ---

        // 1. Compute a hash of the raw HTML to use as a unique key for caching.
        var rawTextHash = ComputeSha256Hash(request.FapHtmlContent);
        string? extractedJson = await _storage.TryGetByHashJsonAsync("academic-records", rawTextHash, cancellationToken);

        if (!string.IsNullOrWhiteSpace(extractedJson))
        {
            _logger.LogInformation("Cache HIT: Found cached academic record for hash {Hash}. Skipping AI extraction.", rawTextHash);
        }
        else
        {
            _logger.LogInformation("Cache MISS: No cached record found for hash {Hash}. Proceeding with AI extraction.", rawTextHash);
            var cleanText = _htmlCleaningService.ExtractCleanTextFromHtml(request.FapHtmlContent);
            extractedJson = await _fapPlugin.ExtractFapRecordJsonAsync(cleanText, cancellationToken);

            // 3. After a successful extraction (cache miss), save the result to storage for future requests.
            if (!string.IsNullOrWhiteSpace(extractedJson))
            {
                await _storage.SaveLatestAsync(
                    bucketName: "academic-records",
                    programCode: request.CurriculumProgramId.ToString(), // Using program ID for folder structure
                    versionCode: "fap-sync",
                    jsonContent: extractedJson,
                    rawTextContent: request.FapHtmlContent,
                    rawTextHash: rawTextHash,
                    cancellationToken: cancellationToken);
                _logger.LogInformation("Cache WRITE: Saved new academic record to cache for hash {Hash}.", rawTextHash);
            }
        }

        // --- CACHING LOGIC END ---

        var fapData = JsonSerializer.Deserialize<FapRecordData>(extractedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (fapData == null)
        {
            _logger.LogError("Failed to deserialize the extracted academic JSON from FAP content.");
            throw new BadRequestException("Failed to deserialize the extracted academic data.");
        }
        _logger.LogInformation("Successfully deserialized {SubjectCount} subjects from transcript.", fapData.Subjects.Count);

        var enrollment = await _enrollmentRepository.FirstOrDefaultAsync(e => e.AuthUserId == request.AuthUserId, cancellationToken);
        if (enrollment == null)
        {
            _logger.LogInformation("Creating new enrollment for user {AuthUserId}.", request.AuthUserId);
            enrollment = new StudentEnrollment
            {
                AuthUserId = request.AuthUserId,
                EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = EnrollmentStatus.Active
            };
            await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
        }

        var allAllowedSubjects = (await _subjectRepository.GetAllAsync(cancellationToken))
            .Where(s => allowedSubjectIds.Contains(s.Id))
            .ToList();

        if (!allAllowedSubjects.Any())
        {
            _logger.LogWarning("Could not find any subjects linked to ProgramId {ProgramId} or ClassId {ClassId}", request.CurriculumProgramId, userProfile.ClassId.Value);
            throw new NotFoundException($"No subjects are associated with program {request.CurriculumProgramId} or class {userProfile.ClassId.Value}");
        }

        var subjectCatalog = allAllowedSubjects.ToDictionary(s => s.SubjectCode);
        _logger.LogInformation("Built combined subject catalog for program and class with {Count} subjects.", subjectCatalog.Count);

        var existingSemesterSubjects = (await _semesterSubjectRepository.FindAsync(
            ss => ss.AuthUserId == request.AuthUserId, cancellationToken)
            ).ToList();

        int recordsAdded = 0;
        int recordsUpdated = 0;
        int recordsIgnored = 0;

        foreach (var subjectRecord in fapData.Subjects)
        {
            if (!subjectCatalog.TryGetValue(subjectRecord.SubjectCode, out var subject))
            {
                _logger.LogWarning("Subject {SubjectCode} from transcript not found in user's program/class catalog. Skipping.", subjectRecord.SubjectCode);
                recordsIgnored++;
                continue;
            }

            var existingRecord = existingSemesterSubjects.FirstOrDefault(ss =>
                ss.SubjectId == subject.Id &&
                ss.AcademicYear == subjectRecord.AcademicYear);

            var parsedStatus = MapFapStatusToEnum(subjectRecord.Status);

            if (existingRecord == null)
            {
                var newSemesterSubject = new StudentSemesterSubject
                {
                    AuthUserId = request.AuthUserId,
                    SubjectId = subject.Id,
                    AcademicYear = subjectRecord.AcademicYear,
                    Status = parsedStatus,
                    Grade = subjectRecord.Mark?.ToString("F1"),
                    CreditsEarned = parsedStatus == SubjectEnrollmentStatus.Passed ? subject.Credits : 0
                };
                await _semesterSubjectRepository.AddAsync(newSemesterSubject, cancellationToken);
                recordsAdded++;
            }
            else if (existingRecord.Status != parsedStatus || existingRecord.Grade != subjectRecord.Mark?.ToString("F1"))
            {
                existingRecord.Status = parsedStatus;
                existingRecord.Grade = subjectRecord.Mark?.ToString("F1");
                existingRecord.CreditsEarned = parsedStatus == SubjectEnrollmentStatus.Passed ? subject.Credits : 0;
                await _semesterSubjectRepository.UpdateAsync(existingRecord, cancellationToken);
                recordsUpdated++;
            }
        }

        _logger.LogInformation(
            "Academic record sync complete. Records Added: {Added}, Records Updated: {Updated}, Records Ignored: {Ignored}",
            recordsAdded, recordsUpdated, recordsIgnored);

        _logger.LogInformation("Dispatching GenerateQuestLine command for user {AuthUserId} to create learning path.", request.AuthUserId);
        var questLineResponse = await _mediator.Send(new GenerateQuestLine { AuthUserId = request.AuthUserId }, cancellationToken);

        return new ProcessAcademicRecordResponse
        {
            IsSuccess = true,
            Message = "Academic record processed successfully. Your gradebook and learning path have been updated.",
            LearningPathId = questLineResponse.LearningPathId,
            SubjectsProcessed = fapData.Subjects.Count,
            CalculatedGpa = fapData.Gpa ?? 0.0
        };
    }

    private async Task<HashSet<Guid>> BuildAllowedSubjectList(Guid programId, Guid classId, CancellationToken cancellationToken)
    {
        var programSubjects = await _programSubjectRepository.FindAsync(ps => ps.ProgramId == programId, cancellationToken);
        var allowedSet = new HashSet<Guid>(programSubjects.Select(ps => ps.SubjectId));

        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(classId, cancellationToken);
        foreach (var subject in classSubjects)
        {
            allowedSet.Add(subject.Id);
        }

        _logger.LogInformation("Built allowed subject list for Program {ProgramId} and Class {ClassId}. Total allowed subjects: {Count}", programId, classId, allowedSet.Count);

        return allowedSet;
    }

    private SubjectEnrollmentStatus MapFapStatusToEnum(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "passed" => SubjectEnrollmentStatus.Passed,
            "studying" => SubjectEnrollmentStatus.Studying,
            "not started" => SubjectEnrollmentStatus.NotStarted,
            "failed" => SubjectEnrollmentStatus.NotPassed,
            "not passed" => SubjectEnrollmentStatus.NotPassed,
            _ => SubjectEnrollmentStatus.NotStarted
        };
    }

    // ADDED: Helper method to compute a consistent hash for the raw HTML content.
    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}