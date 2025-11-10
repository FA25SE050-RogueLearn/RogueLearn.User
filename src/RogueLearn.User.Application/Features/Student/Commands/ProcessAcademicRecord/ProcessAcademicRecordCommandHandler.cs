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
    // FIX: The field name is corrected to match its usage throughout the class.
    private readonly IClassSpecializationSubjectRepository _classSpecializationSubjectRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IHtmlCleaningService _htmlCleaningService;
    private readonly ILogger<ProcessAcademicRecordCommandHandler> _logger;
    private readonly IMediator _mediator;

    public ProcessAcademicRecordCommandHandler(
        IFapExtractionPlugin fapPlugin,
        IStudentEnrollmentRepository enrollmentRepository,
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ISubjectRepository subjectRepository,
        ICurriculumProgramRepository programRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository, // Parameter name is correct
        IUserProfileRepository userProfileRepository,
        IHtmlCleaningService htmlCleaningService,
        ILogger<ProcessAcademicRecordCommandHandler> logger,
        IMediator mediator)
    {
        _fapPlugin = fapPlugin;
        _enrollmentRepository = enrollmentRepository;
        _semesterSubjectRepository = semesterSubjectRepository;
        _subjectRepository = subjectRepository;
        _programRepository = programRepository;
        _programSubjectRepository = programSubjectRepository;
        // FIX: The field assignment is corrected to use the consistent, descriptive name.
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
        _userProfileRepository = userProfileRepository;
        _htmlCleaningService = htmlCleaningService;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<ProcessAcademicRecordResponse> Handle(ProcessAcademicRecordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[START] Processing academic record for user {AuthUserId}", request.AuthUserId);

        // STEP 1: Get the user's profile to find their selected program (RouteId) and specialization (ClassId).
        // This is the source of truth for the user's context.
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

        // STEP 2: Call the helper method to build the master list of all valid subjects for this user.
        // This is where the core validation logic resides.
        var allowedSubjectIds = await BuildAllowedSubjectList(request.CurriculumProgramId, userProfile.ClassId.Value, cancellationToken);

        var cleanText = _htmlCleaningService.ExtractCleanTextFromHtml(request.FapHtmlContent);
        var extractedJson = await _fapPlugin.ExtractFapRecordJsonAsync(cleanText, cancellationToken);
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

        // STEP 3: Build a local "catalog" of all subject details that the user is allowed to access.
        // This query is now filtered by the master list we built in Step 2.
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
            // STEP 4: This is the enforcement step.
            // We look up the subject from the transcript in our master catalog.
            // If it's not found, it means the subject is not part of the user's program OR their class, so we ignore it.
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

        // STEP 5: After successfully syncing the academic record, dispatch the next command in the workflow.
        // This will create the LearningPath and the high-level Quests.
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

    /// <summary>
    /// This private helper method is the core of the contextual validation.
    /// It builds a complete and unique list of all subject IDs a user is associated with.
    /// </summary>
    private async Task<HashSet<Guid>> BuildAllowedSubjectList(Guid programId, Guid classId, CancellationToken cancellationToken)
    {
        // STEP 2a: Get all generic subjects linked directly to the user's main curriculum program.
        // This is where 'PRF192' and other core subjects are found.
        var programSubjects = await _programSubjectRepository.FindAsync(ps => ps.ProgramId == programId, cancellationToken);
        var allowedSet = new HashSet<Guid>(programSubjects.Select(ps => ps.SubjectId));

        // STEP 2b: Get all specialized subjects linked to the user's chosen Class.
        // This is the exact step that addresses your question. It queries the `class_specialization_subjects` table.
        // This is where 'PRM392' is found.
        // FIX: The variable name `_classSubjectRepository` is corrected to `_classSpecializationSubjectRepository` to match the declaration.
        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(classId, cancellationToken);
        foreach (var subject in classSubjects)
        {
            // Add the specialized subject ID to the master list.
            allowedSet.Add(subject.Id);
        }

        _logger.LogInformation("Built allowed subject list for Program {ProgramId} and Class {ClassId}. Total allowed subjects: {Count}", programId, classId, allowedSet.Count);

        // STEP 2c: Return the combined master list.
        return allowedSet;
    }

    private SubjectEnrollmentStatus MapFapStatusToEnum(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "passed" => SubjectEnrollmentStatus.Passed,
            "studying" => SubjectEnrollmentStatus.Studying,
            "not started" => SubjectEnrollmentStatus.NotStarted,
            "not passed" => SubjectEnrollmentStatus.NotPassed,
            _ => SubjectEnrollmentStatus.NotStarted
        };
    }
}