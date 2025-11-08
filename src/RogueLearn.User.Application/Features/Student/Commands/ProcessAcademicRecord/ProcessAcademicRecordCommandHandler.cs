// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

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
    private readonly ICurriculumProgramRepository _programRepository; // ADDED
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository; // ADDED
    private readonly ILogger<ProcessAcademicRecordCommandHandler> _logger;

    public ProcessAcademicRecordCommandHandler(
        IFapExtractionPlugin fapPlugin,
        IStudentEnrollmentRepository enrollmentRepository,
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ISubjectRepository subjectRepository,
        ICurriculumProgramRepository programRepository, // ADDED
        ICurriculumProgramSubjectRepository programSubjectRepository, // ADDED
        ILogger<ProcessAcademicRecordCommandHandler> logger)
    {
        _fapPlugin = fapPlugin;
        _enrollmentRepository = enrollmentRepository;
        _semesterSubjectRepository = semesterSubjectRepository;
        _subjectRepository = subjectRepository;
        _programRepository = programRepository;
        _programSubjectRepository = programSubjectRepository;
        _logger = logger;
    }

    public async Task<ProcessAcademicRecordResponse> Handle(ProcessAcademicRecordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[START] Processing academic record for user {AuthUserId}", request.AuthUserId);

        // STEP 1: Validate the Program ID exists.
        if (!await _programRepository.ExistsAsync(request.CurriculumProgramId, cancellationToken))
        {
            _logger.LogWarning("Invalid CurriculumProgramId: {ProgramId}", request.CurriculumProgramId);
            throw new NotFoundException(nameof(CurriculumProgram), request.CurriculumProgramId);
        }

        // STEP 2: Extract data from HTML using the AI plugin.
        var extractedJson = await _fapPlugin.ExtractFapRecordJsonAsync(request.FapHtmlContent, cancellationToken);
        var fapData = JsonSerializer.Deserialize<FapRecordData>(extractedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (fapData == null)
        {
            _logger.LogError("Failed to deserialize the extracted academic JSON.");
            throw new BadRequestException("Failed to deserialize the extracted academic data.");
        }
        _logger.LogInformation("Successfully deserialized {SubjectCount} subjects from transcript.", fapData.Subjects.Count);

        // STEP 3: Create or get the simplified enrollment record.
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

        // STEP 4: Determine the "latest active version" string by looking at the subjects within the program.
        var programSubjectIds = (await _programSubjectRepository.FindAsync(ps => ps.ProgramId == request.CurriculumProgramId, cancellationToken))
            .Select(ps => ps.SubjectId).ToList();

        var allProgramSubjects = (await _subjectRepository.GetAllAsync(cancellationToken))
            .Where(s => programSubjectIds.Contains(s.Id)).ToList();

        var latestVersionCode = allProgramSubjects
            .OrderByDescending(s => s.Version)
            .FirstOrDefault()?.Version;

        if (string.IsNullOrEmpty(latestVersionCode))
        {
            _logger.LogWarning("Could not determine latest active version for ProgramId: {ProgramId}", request.CurriculumProgramId);
            throw new NotFoundException($"Could not determine latest active subject version for program {request.CurriculumProgramId}");
        }
        _logger.LogInformation("Determined latest active version code for program is '{VersionCode}'", latestVersionCode);

        // STEP 5: Sync subjects into the student's "gradebook".
        var subjectsForVersion = allProgramSubjects
            .Where(s => s.Version == latestVersionCode)
            .ToDictionary(s => s.SubjectCode);

        var existingSemesterSubjects = (await _semesterSubjectRepository.FindAsync(
            ss => ss.AuthUserId == request.AuthUserId, cancellationToken)
            ).ToList();

        int recordsAdded = 0;
        int recordsUpdated = 0;

        foreach (var subjectRecord in fapData.Subjects)
        {
            if (!subjectsForVersion.TryGetValue(subjectRecord.SubjectCode, out var subject))
            {
                _logger.LogWarning("Subject {SubjectCode} with version {Version} not found for this program. Skipping.", subjectRecord.SubjectCode, latestVersionCode);
                continue;
            }

            var existingRecord = existingSemesterSubjects.FirstOrDefault(ss =>
                ss.SubjectId == subject.Id &&
                ss.LearnedAtSemester == subjectRecord.Semester &&
                ss.AcademicYear == subjectRecord.AcademicYear);

            var parsedStatus = MapFapStatusToEnum(subjectRecord.Status);

            if (existingRecord == null)
            {
                var newSemesterSubject = new StudentSemesterSubject
                {
                    AuthUserId = request.AuthUserId,
                    SubjectId = subject.Id,
                    AcademicYear = subjectRecord.AcademicYear,
                    LearnedAtSemester = subjectRecord.Semester,
                    Status = parsedStatus,
                    Grade = subjectRecord.Mark?.ToString("F1"),
                    CreditsEarned = parsedStatus == SubjectEnrollmentStatus.Completed ? subject.Credits : 0
                };
                await _semesterSubjectRepository.AddAsync(newSemesterSubject, cancellationToken);
                recordsAdded++;
            }
            else if (existingRecord.Status != parsedStatus || existingRecord.Grade != subjectRecord.Mark?.ToString("F1"))
            {
                existingRecord.Status = parsedStatus;
                existingRecord.Grade = subjectRecord.Mark?.ToString("F1");
                existingRecord.CreditsEarned = parsedStatus == SubjectEnrollmentStatus.Completed ? subject.Credits : 0;
                await _semesterSubjectRepository.UpdateAsync(existingRecord, cancellationToken);
                recordsUpdated++;
            }
        }

        _logger.LogInformation(
            "[END] Academic record processed. Records Added: {Added}, Records Updated: {Updated}",
            recordsAdded, recordsUpdated);

        return new ProcessAcademicRecordResponse
        {
            IsSuccess = true,
            Message = "Academic record processed successfully. Your gradebook has been updated.",
            SubjectsProcessed = fapData.Subjects.Count,
            CalculatedGpa = fapData.Gpa ?? 0.0
        };
    }

    private SubjectEnrollmentStatus MapFapStatusToEnum(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "passed" => SubjectEnrollmentStatus.Completed,
            "failed" => SubjectEnrollmentStatus.Failed,
            "studying" => SubjectEnrollmentStatus.Enrolled,
            _ => SubjectEnrollmentStatus.Enrolled
        };
    }
}