using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
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
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
    private readonly IQuestDifficultyResolver _difficultyResolver;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateSingleSubjectGradeCommandHandler> _logger;

    public UpdateSingleSubjectGradeCommandHandler(
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ISubjectRepository subjectRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        IGradeExperienceCalculator gradeExperienceCalculator,
        IQuestRepository questRepository,
        IUserQuestAttemptRepository userQuestAttemptRepository,
        IQuestDifficultyResolver difficultyResolver,
        IUserQuestStepProgressRepository stepProgressRepository,
        IMediator mediator,
        ILogger<UpdateSingleSubjectGradeCommandHandler> logger)
    {
        _semesterSubjectRepository = semesterSubjectRepository;
        _subjectRepository = subjectRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _gradeExperienceCalculator = gradeExperienceCalculator;
        _questRepository = questRepository;
        _userQuestAttemptRepository = userQuestAttemptRepository;
        _difficultyResolver = difficultyResolver;
        _stepProgressRepository = stepProgressRepository;
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
        // Logic Change: We trust the user-provided status explicitly.
        // We do NOT override it based on the grade (e.g. 9.0 but Failed due to attendance is valid).

        record.Grade = request.Grade.ToString("F1");
        record.Status = request.Status; // Trust the status from the command
        record.CreditsEarned = request.Status == SubjectEnrollmentStatus.Passed ? subject.Credits : 0;

        // Handle Academic Year updates
        if (!string.IsNullOrWhiteSpace(request.AcademicYear))
        {
            record.AcademicYear = request.AcademicYear;
        }
        else if (isNewRecord)
        {
            record.AcademicYear = $"{DateTime.UtcNow.Year}";
        }

        // Handle completion date logic
        if (request.Status == SubjectEnrollmentStatus.Passed)
        {
            // If it wasn't passed before, set completed date
            if (!record.CompletedAt.HasValue)
            {
                record.CompletedAt = DateTimeOffset.UtcNow;
            }
        }
        else
        {
            // If status changed to NotPassed/Studying, clear completion date
            record.CompletedAt = null;
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

        // 6. Award XP if Passed
        XpAwardSummary? xpSummary = null;
        if (record.Status == SubjectEnrollmentStatus.Passed)
        {
            xpSummary = await AwardXpForSubjectAsync(request.AuthUserId, subject, request.Grade, cancellationToken);
        }

        // 7. Update Difficulty Directly
        var masterQuest = await _questRepository.GetActiveQuestBySubjectIdAsync(subject.Id, cancellationToken);

        if (masterQuest != null)
        {
            var attempt = await _userQuestAttemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == masterQuest.Id,
                cancellationToken);

            // Calculate difficulty.
            // IMPORTANT: The resolver logic needs to handle cases where High Grade + NotPassed = Supportive/Standard (Retake)
            // We pass the updated record which now has the explicit user status.
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(record, -1.0, subject);
            var newDifficulty = difficultyInfo.ExpectedDifficulty;

            if (attempt == null)
            {
                attempt = new UserQuestAttempt
                {
                    AuthUserId = request.AuthUserId,
                    QuestId = masterQuest.Id,
                    Status = QuestAttemptStatus.NotStarted,
                    AssignedDifficulty = newDifficulty,
                    Notes = difficultyInfo.DifficultyReason,
                    StartedAt = DateTimeOffset.UtcNow
                };
                await _userQuestAttemptRepository.AddAsync(attempt, cancellationToken);
                _logger.LogInformation("Created new quest attempt for {SubjectCode} with difficulty {Difficulty}", subject.SubjectCode, newDifficulty);
            }
            else
            {
                // Logic change: Always allow update if status changed, even if difficulty string is same (to update reason)
                bool difficultyChanged = attempt.AssignedDifficulty != newDifficulty;

                // If the user failed (NotPassed), we might want to reset their progress even if difficulty stays "Standard"
                // to force a retake flow. However, usually difficulty change triggers the reset.
                // Let's stick to difficulty change triggering reset for now to be safe.

                if (difficultyChanged)
                {
                    _logger.LogInformation("Adjusting difficulty for {SubjectCode} from {Old} to {New} based on grade/status update.",
                        subject.SubjectCode, attempt.AssignedDifficulty, newDifficulty);

                    await _stepProgressRepository.DeleteByAttemptIdAsync(attempt.Id, cancellationToken);

                    attempt.AssignedDifficulty = newDifficulty;
                    attempt.Notes = $"Difficulty updated on {DateTime.UtcNow:yyyy-MM-dd}. Reason: {difficultyInfo.DifficultyReason}";

                    if (attempt.Status == QuestAttemptStatus.Completed)
                    {
                        attempt.Status = QuestAttemptStatus.InProgress;
                    }
                    attempt.CurrentStepId = null;

                    await _userQuestAttemptRepository.UpdateAsync(attempt, cancellationToken);
                }
                else
                {
                    // Update notes/reason even if difficulty didn't change
                    attempt.Notes = $"Grade updated. Reason: {difficultyInfo.DifficultyReason}";
                    await _userQuestAttemptRepository.UpdateAsync(attempt, cancellationToken);
                }
            }
        }

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
        var mappings = await _subjectSkillMappingRepository.FindAsync(m => m.SubjectId == subject.Id, cancellationToken);
        if (!mappings.Any()) return summary;

        var semester = subject.Semester ?? 1;
        var tierInfo = _gradeExperienceCalculator.GetTierInfo(semester);

        foreach (var mapping in mappings)
        {
            var xpAmount = _gradeExperienceCalculator.CalculateXpAward(grade, semester, mapping.RelevanceWeight);
            var response = await _mediator.Send(new IngestXpEventCommand
            {
                AuthUserId = authUserId,
                SkillId = mapping.SkillId,
                Points = xpAmount,
                SourceService = SkillRewardSourceType.AcademicRecord.ToString(),
                SourceId = subject.Id,
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