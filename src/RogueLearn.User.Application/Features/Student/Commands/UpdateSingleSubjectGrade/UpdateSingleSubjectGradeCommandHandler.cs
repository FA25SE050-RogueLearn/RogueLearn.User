using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;
using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Student.Commands.UpdateSingleSubjectGrade;

public class UpdateSingleSubjectGradeCommandHandler : IRequestHandler<UpdateSingleSubjectGradeCommand, UpdateSingleSubjectGradeResponse>
{
    private readonly IStudentSemesterSubjectRepository _semesterSubjectRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly IGradeExperienceCalculator _gradeExperienceCalculator;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateSingleSubjectGradeCommandHandler> _logger;

    public UpdateSingleSubjectGradeCommandHandler(
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ISubjectRepository subjectRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        IGradeExperienceCalculator gradeExperienceCalculator,
        IMediator mediator,
        ILogger<UpdateSingleSubjectGradeCommandHandler> logger)
    {
        _semesterSubjectRepository = semesterSubjectRepository;
        _subjectRepository = subjectRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _gradeExperienceCalculator = gradeExperienceCalculator;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<UpdateSingleSubjectGradeResponse> Handle(UpdateSingleSubjectGradeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating single subject grade for User {UserId}, Subject {SubjectId}", request.AuthUserId, request.SubjectId);

        // 1. Validate Subject Exists
        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null)
        {
            throw new NotFoundException("Subject", request.SubjectId);
        }

        // 2. Validate Grade Range
        if (request.Grade < 0 || request.Grade > 10)
        {
            throw new BadRequestException("Grade must be between 0 and 10.");
        }

        // 3. Find or Create Student Semester Subject Record
        // Note: Using FindAsync because GetByIdAsync expects the record ID, not composite key
        var existingRecords = await _semesterSubjectRepository.FindAsync(
            ss => ss.AuthUserId == request.AuthUserId && ss.SubjectId == request.SubjectId,
            cancellationToken);

        var record = existingRecords.FirstOrDefault();
        bool isNewRecord = false;

        if (record == null)
        {
            isNewRecord = true;
            record = new StudentSemesterSubject
            {
                Id = Guid.NewGuid(),
                AuthUserId = request.AuthUserId,
                SubjectId = request.SubjectId,
                AcademicYear = request.AcademicYear,
                EnrolledAt = DateTimeOffset.UtcNow
            };
        }

        // 4. Update Properties
        record.Grade = request.Grade.ToString("F1");
        record.Status = request.Status;
        record.CreditsEarned = request.Status == SubjectEnrollmentStatus.Passed ? subject.Credits : 0;

        // If updating an existing record, update AcademicYear only if provided, otherwise keep existing
        if (!string.IsNullOrWhiteSpace(request.AcademicYear))
        {
            record.AcademicYear = request.AcademicYear;
        }
        else if (isNewRecord)
        {
            // Default for new records if not provided
            record.AcademicYear = $"{DateTime.UtcNow.Year}";
        }

        if (request.Status == SubjectEnrollmentStatus.Passed)
        {
            record.CompletedAt = DateTimeOffset.UtcNow;
        }

        // 5. Persist Changes
        if (isNewRecord)
        {
            await _semesterSubjectRepository.AddAsync(record, cancellationToken);
        }
        else
        {
            await _semesterSubjectRepository.UpdateAsync(record, cancellationToken);
        }

        _logger.LogInformation("Successfully updated grade record for {SubjectCode}. New Grade: {Grade}, Status: {Status}",
            subject.SubjectCode, record.Grade, record.Status);

        // 6. Award XP if Passed (Reusing logic from bulk import via shared helper or direct implementation)
        // Since this is a single update, we can implement the logic directly here for clarity
        XpAwardSummary? xpSummary = null;

        if (record.Status == SubjectEnrollmentStatus.Passed)
        {
            xpSummary = await AwardXpForSubjectAsync(request.AuthUserId, subject, request.Grade, cancellationToken);
        }

        // 7. Trigger Quest Line Update
        // This ensures the quest for this subject (if any) gets its difficulty/status updated
        // and unlocks any subsequent quests dependent on this one.
        var questLineCommand = new GenerateQuestLine
        {
            AuthUserId = request.AuthUserId,
            // We pass null for analysis report as we are doing a single update, 
            // the difficulty resolver will use standard logic or existing cached analysis if applicable
            AiAnalysisReport = null
        };

        // Fire and forget or await? Await ensures consistency before returning.
        await _mediator.Send(questLineCommand, cancellationToken);

        return new UpdateSingleSubjectGradeResponse
        {
            SubjectId = subject.Id,
            SubjectCode = subject.SubjectCode,
            NewGrade = record.Grade,
            NewStatus = record.Status.ToString(),
            XpAwarded = xpSummary
        };
    }

    private async Task<XpAwardSummary> AwardXpForSubjectAsync(
        Guid authUserId,
        Subject subject,
        double grade,
        CancellationToken cancellationToken)
    {
        var summary = new XpAwardSummary();

        // Get mappings
        var mappings = await _subjectSkillMappingRepository.FindAsync(m => m.SubjectId == subject.Id, cancellationToken);
        if (!mappings.Any()) return summary;

        var semester = subject.Semester ?? 1;
        var tierInfo = _gradeExperienceCalculator.GetTierInfo(semester);

        foreach (var mapping in mappings)
        {
            var xpAmount = _gradeExperienceCalculator.CalculateXpAward(grade, semester, mapping.RelevanceWeight);

            // Use IngestXpEventCommand for idempotency
            var response = await _mediator.Send(new IngestXpEventCommand
            {
                AuthUserId = authUserId,
                SkillId = mapping.SkillId,
                Points = xpAmount,
                SourceService = "ManualGradeUpdate",
                SourceType = "GradeUpdate",
                SourceId = subject.Id, // Idempotency key
                Reason = $"Manual grade update: {subject.SubjectCode} ({grade:F1}/10.0)"
            }, cancellationToken);

            if (response.Processed)
            {
                summary.TotalXp += xpAmount;
                summary.SkillsAffected++;
                summary.SkillAwards.Add(new SkillXpAward
                {
                    SkillId = mapping.SkillId,
                    SkillName = response.SkillName,
                    XpAwarded = xpAmount,
                    NewTotalXp = response.NewExperiencePoints,
                    NewLevel = response.NewLevel,
                    SourceSubjectCode = subject.SubjectCode,
                    Grade = grade.ToString("F1"),
                    TierDescription = tierInfo.Description
                });
            }
        }

        return summary;
    }
}