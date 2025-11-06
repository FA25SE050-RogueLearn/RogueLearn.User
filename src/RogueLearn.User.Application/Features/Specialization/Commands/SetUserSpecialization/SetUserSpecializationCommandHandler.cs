// RogueLearn.User/src/RogueLearn.User.Application/Features/Specialization/Commands/SetUserSpecialization/SetUserSpecializationCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Specialization.Commands.SetUserSpecialization;

public class SetUserSpecializationCommandHandler : IRequestHandler<SetUserSpecializationCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IClassRepository _classRepository;
    private readonly IClassSpecializationSubjectRepository _specSubjectRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestProgressRepository _questProgressRepository;
    private readonly ILogger<SetUserSpecializationCommandHandler> _logger;

    public SetUserSpecializationCommandHandler(
        IUserProfileRepository userProfileRepository,
        IClassRepository classRepository,
        IClassSpecializationSubjectRepository specSubjectRepository,
        IQuestRepository questRepository,
        IUserQuestProgressRepository questProgressRepository,
        ILogger<SetUserSpecializationCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _classRepository = classRepository;
        _specSubjectRepository = specSubjectRepository;
        _questRepository = questRepository;
        _questProgressRepository = questProgressRepository;
        _logger = logger;
    }

    public async Task Handle(SetUserSpecializationCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate inputs and retrieve user profile
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), request.AuthUserId);

        var selectedClass = await _classRepository.GetByIdAsync(request.ClassId, cancellationToken)
            ?? throw new BadRequestException($"Invalid ClassId: {request.ClassId}");

        // 2. Update the user's profile with the new specialization (class_id)
        userProfile.ClassId = request.ClassId;
        await _userProfileRepository.UpdateAsync(userProfile, cancellationToken);
        _logger.LogInformation("User {AuthUserId} selected specialization ClassId {ClassId}", request.AuthUserId, request.ClassId);

        // 3. Find all subjects linked to this specialization
        var specializationSubjects = await _specSubjectRepository.FindAsync(
            spec => spec.ClassId == request.ClassId,
            cancellationToken);

        var subjectIds = specializationSubjects.Select(ss => ss.SubjectId).ToList();
        if (!subjectIds.Any())
        {
            _logger.LogWarning("No subjects are mapped to ClassId {ClassId}. No quests will be made available.", request.ClassId);
            return;
        }

        // 4. Find all quests associated with those subjects
        var questsToMakeAvailable = (await _questRepository.GetAllAsync(cancellationToken))
            .Where(q => q.SubjectId.HasValue && subjectIds.Contains(q.SubjectId.Value))
            .ToList();

        _logger.LogInformation("Found {QuestCount} quests linked to the {ClassName} specialization.", questsToMakeAvailable.Count, selectedClass.Name);

        // 5. For each quest, create a 'NotStarted' progress record if one doesn't already exist.
        // This makes the quests appear in the user's "Available Quests" list.
        var existingProgress = (await _questProgressRepository.GetUserProgressForQuestsAsync(request.AuthUserId, questsToMakeAvailable.Select(q => q.Id).ToList(), cancellationToken))
            .ToDictionary(p => p.QuestId);

        var newProgressRecords = new List<UserQuestProgress>();

        foreach (var quest in questsToMakeAvailable)
        {
            if (!existingProgress.ContainsKey(quest.Id))
            {
                newProgressRecords.Add(new UserQuestProgress
                {
                    AuthUserId = request.AuthUserId,
                    QuestId = quest.Id,
                    Status = QuestStatus.NotStarted,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        if (newProgressRecords.Any())
        {
            await _questProgressRepository.AddRangeAsync(newProgressRecords, cancellationToken);
            _logger.LogInformation("Created {Count} new 'NotStarted' quest progress records for user {AuthUserId}.", newProgressRecords.Count, request.AuthUserId);
        }
    }
}