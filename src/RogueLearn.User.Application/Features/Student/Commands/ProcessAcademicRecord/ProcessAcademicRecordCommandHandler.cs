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

namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

// These DTOs are defined here as they are specific to the FAP extraction process.
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
    private readonly IClassSpecializationSubjectRepository _classSubjectRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IHtmlCleaningService _htmlCleaningService;
    private readonly ILogger<ProcessAcademicRecordCommandHandler> _logger;

    public ProcessAcademicRecordCommandHandler(
        IFapExtractionPlugin fapPlugin,
        IStudentEnrollmentRepository enrollmentRepository,
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ISubjectRepository subjectRepository,
        ICurriculumProgramRepository programRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        IClassSpecializationSubjectRepository classSubjectRepository,
        IUserProfileRepository userProfileRepository,
        IHtmlCleaningService htmlCleaningService,
        ILogger<ProcessAcademicRecordCommandHandler> logger)
    {
        _fapPlugin = fapPlugin;
        _enrollmentRepository = enrollmentRepository;
        _semesterSubjectRepository = semesterSubjectRepository;
        _subjectRepository = subjectRepository;
        _programRepository = programRepository;
        _programSubjectRepository = programSubjectRepository;
        _classSubjectRepository = classSubjectRepository;
        _userProfileRepository = userProfileRepository;
        _htmlCleaningService = htmlCleaningService;
        _logger = logger;
    }

    public async Task<ProcessAcademicRecordResponse> Handle(ProcessAcademicRecordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[START] Processing academic record for user {AuthUserId}", request.AuthUserId);

        // STEP 1: Fetch User Profile to get their chosen Class and enforce onboarding completion.
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

        // STEP 2: Build the definitive "Allowed Subject" list to enforce the "One-Way Path" rule.
        var allowedSubjectIds = await BuildAllowedSubjectList(request.CurriculumProgramId, userProfile.ClassId.Value, cancellationToken);

        // STEP 3: Clean HTML and extract data using the AI plugin.
        var cleanText = _htmlCleaningService.ExtractCleanTextFromHtml(request.FapHtmlContent);
        var extractedJson = await _fapPlugin.ExtractFapRecordJsonAsync(cleanText, cancellationToken);
        var fapData = JsonSerializer.Deserialize<FapRecordData>(extractedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (fapData == null)
        {
            _logger.LogError("Failed to deserialize the extracted academic JSON from FAP content.");
            throw new BadRequestException("Failed to deserialize the extracted academic data.");
        }
        _logger.LogInformation("Successfully deserialized {SubjectCount} subjects from transcript.", fapData.Subjects.Count);

        // STEP 4: Create or get the simplified student enrollment record.
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

        // MODIFICATION START: The entire version-filtering logic is removed.
        // STEP 5: Get the definitive list of subjects for the program.
        var allProgramSubjects = await _subjectRepository.GetSubjectsByRoute(request.CurriculumProgramId, cancellationToken);

        if (!allProgramSubjects.Any())
        {
            _logger.LogWarning("Could not find any subjects linked to ProgramId: {ProgramId}", request.CurriculumProgramId);
            throw new NotFoundException($"No subjects are associated with program {request.CurriculumProgramId}");
        }

        var subjectCatalog = allProgramSubjects.ToDictionary(s => s.SubjectCode);
        _logger.LogInformation("Built subject catalog for program with {Count} subjects.", subjectCatalog.Count);
        // MODIFICATION END

        // STEP 6: Synchronize subjects into the student's "gradebook" (`student_semester_subjects`).
        var existingSemesterSubjects = (await _semesterSubjectRepository.FindAsync(
            ss => ss.AuthUserId == request.AuthUserId, cancellationToken)
            ).ToList();

        int recordsAdded = 0;
        int recordsUpdated = 0;
        int recordsIgnored = 0;

        foreach (var subjectRecord in fapData.Subjects)
        {
            // MODIFIED: Use the new 'subjectCatalog' dictionary.
            if (!subjectCatalog.TryGetValue(subjectRecord.SubjectCode, out var subject))
            {
                // MODIFIED: Log message no longer references a version.
                _logger.LogWarning("Subject {SubjectCode} not found in this program's catalog. Skipping.", subjectRecord.SubjectCode);
                continue;
            }

            // ENFORCEMENT of the "One-Way Path" rule.
            if (!allowedSubjectIds.Contains(subject.Id))
            {
                _logger.LogWarning("Ignoring subject {SubjectCode} from transcript because it does not belong to the user's chosen program or specialization class.", subject.SubjectCode);
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
            "[END] Academic record processed. Records Added: {Added}, Records Updated: {Updated}, Records Ignored: {Ignored}",
            recordsAdded, recordsUpdated, recordsIgnored);

        return new ProcessAcademicRecordResponse
        {
            IsSuccess = true,
            Message = "Academic record processed successfully. Your gradebook has been updated.",
            SubjectsProcessed = fapData.Subjects.Count,
            CalculatedGpa = fapData.Gpa ?? 0.0
        };
    }

    private async Task<HashSet<Guid>> BuildAllowedSubjectList(Guid programId, Guid classId, CancellationToken cancellationToken)
    {
        // Get all generic subjects for the curriculum program
        var programSubjects = await _programSubjectRepository.FindAsync(ps => ps.ProgramId == programId, cancellationToken);
        var allowedSet = new HashSet<Guid>(programSubjects.Select(ps => ps.SubjectId));

        // Get all specialized subjects for the chosen class
        var classSubjects = await _classSubjectRepository.GetSubjectByClassIdAsync(classId, cancellationToken);
        foreach (var subject in classSubjects)
        {
            allowedSet.Add(subject.Id);
        }

        _logger.LogInformation("Built allowed subject list for Program {ProgramId} and Class {ClassId}. Total allowed subjects: {Count}", programId, classId, allowedSet.Count);
        return allowedSet;
    }

    private SubjectEnrollmentStatus MapFapStatusToEnum(string status)
    {
        // This mapping is crucial for correctly interpreting the transcript data.
        return status.Trim().ToLowerInvariant() switch
        {
            "passed" => SubjectEnrollmentStatus.Passed,
            "studying" => SubjectEnrollmentStatus.Studying,
            "not started" => SubjectEnrollmentStatus.NotStarted,
            "not passed" => SubjectEnrollmentStatus.NotPassed,
            _ => SubjectEnrollmentStatus.NotStarted // A safe default
        };
    }
}