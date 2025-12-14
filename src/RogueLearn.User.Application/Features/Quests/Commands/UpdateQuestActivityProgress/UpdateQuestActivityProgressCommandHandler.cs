// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestActivityProgress/UpdateQuestActivityProgressCommandHandler.cs
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Application.Services; // Needed for IQuestDifficultyResolver

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;

public class UpdateQuestActivityProgressCommandHandler : IRequestHandler<UpdateQuestActivityProgressCommand>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IMediator _mediator;
    // MODIFIED: Added dependencies for dynamic difficulty calculation
    private readonly IStudentSemesterSubjectRepository _studentSubjectRepository;
    private readonly IQuestDifficultyResolver _difficultyResolver;
    private readonly ILogger<UpdateQuestActivityProgressCommandHandler> _logger;

    public UpdateQuestActivityProgressCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository,
        IUserSkillRepository userSkillRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        IQuestRepository questRepository,
        IMediator mediator,
        IStudentSemesterSubjectRepository studentSubjectRepository,
        IQuestDifficultyResolver difficultyResolver,
        ILogger<UpdateQuestActivityProgressCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository;
        _userSkillRepository = userSkillRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _questRepository = questRepository;
        _mediator = mediator;
        _studentSubjectRepository = studentSubjectRepository;
        _difficultyResolver = difficultyResolver;
        _logger = logger;
    }

    public async Task Handle(UpdateQuestActivityProgressCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the Quest Step
        var step = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
        if (step == null)
        {
            throw new NotFoundException($"Quest Step {request.StepId} not found");
        }

        // 2. Fetch the User's Quest Attempt (Must already exist)
        var attempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        if (attempt == null)
        {
            _logger.LogWarning("User {UserId} tried to update progress for Quest {QuestId} but hasn't started it.", request.AuthUserId, request.QuestId);
            throw new NotFoundException("Quest not started. Please start the quest before tracking progress.");
        }

        // 3. Resolve Difficulty Dynamically (Replaces attempt.AssignedDifficulty)
        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
        string calculatedDifficulty = "Standard";

        if (quest != null && quest.SubjectId.HasValue)
        {
            var gradeRecords = await _studentSubjectRepository.GetSemesterSubjectsByUserAsync(request.AuthUserId, cancellationToken);
            var subjectRecord = gradeRecords.FirstOrDefault(s => s.SubjectId == quest.SubjectId.Value);
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(subjectRecord);
            calculatedDifficulty = difficultyInfo.ExpectedDifficulty;
        }
        else if (quest != null && !string.IsNullOrEmpty(quest.ExpectedDifficulty))
        {
            calculatedDifficulty = quest.ExpectedDifficulty;
        }

        // Verify Track Match using calculated difficulty
        if (!string.Equals(step.DifficultyVariant, calculatedDifficulty, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Difficulty mismatch: Step is {StepVar}, User Calculated is {UserVar}. User might be accessing a different track than assigned.", step.DifficultyVariant, calculatedDifficulty);
        }

        // 4. Get or Create Step Progress (In Memory)
        var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
            p => p.AttemptId == attempt.Id && p.StepId == request.StepId,
            cancellationToken);

        bool isNewStepProgress = false;

        if (stepProgress == null)
        {
            isNewStepProgress = true;
            stepProgress = new UserQuestStepProgress
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                StepId = request.StepId,
                Status = StepCompletionStatus.InProgress,
                CompletedActivityIds = new Guid[] { },
                StartedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }

        var completedList = stepProgress.CompletedActivityIds != null
            ? stepProgress.CompletedActivityIds.ToList()
            : new List<Guid>();

        bool isAlreadyCompleted = completedList.Contains(request.ActivityId);
        bool progressChanged = false;

        if (request.Status == StepCompletionStatus.Completed && !isAlreadyCompleted)
        {
            completedList.Add(request.ActivityId);
            stepProgress.CompletedActivityIds = completedList.ToArray();
            progressChanged = true;

            // Mastery Override Logic
            bool isQuiz = IsQuizActivity(step, request.ActivityId);
            bool isCompleteByCount = CheckIfStepIsComplete(step, completedList);

            if (isQuiz || isCompleteByCount)
            {
                _logger.LogInformation("Step {StepId} COMPLETE. Reason: {Reason}", step.Id, isQuiz ? "Quiz Mastery" : "All Activities Done");

                stepProgress.Status = StepCompletionStatus.Completed;
                stepProgress.CompletedAt = DateTimeOffset.UtcNow;

                // Award XP to Attempt total
                attempt.TotalExperienceEarned += step.ExperiencePoints;
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);

                // --- NEW: DISTRIBUTE XP TO USER PROFILE & SKILLS ---
                // We assume the quest is linked to a Subject, which is linked to Skills.
                if (quest != null && quest.SubjectId.HasValue)
                {
                    // Fetch skill mappings for this subject
                    var skillMappings = await _subjectSkillMappingRepository.GetMappingsBySubjectIdsAsync(
                        new[] { quest.SubjectId.Value }, cancellationToken);

                    if (skillMappings.Any())
                    {
                        // Distribute the step's XP across the skills based on relevance weight
                        int totalXpToDistribute = step.ExperiencePoints;

                        foreach (var mapping in skillMappings)
                        {
                            int pointsForSkill = (int)(totalXpToDistribute * mapping.RelevanceWeight);
                            if (pointsForSkill > 0)
                            {
                                await _mediator.Send(new IngestXpEventCommand
                                {
                                    AuthUserId = request.AuthUserId,
                                    SkillId = mapping.SkillId,
                                    Points = pointsForSkill,
                                    SourceService = "QuestSystem",
                                    SourceType = "QuestComplete", // Using generic type for step completion
                                    SourceId = request.StepId,    // Idempotency key: StepId
                                    Reason = $"Completed step: {step.Title}"
                                }, cancellationToken);
                            }
                        }
                    }
                }
                // ---------------------------------------------------
            }
            else
            {
                stepProgress.Status = StepCompletionStatus.InProgress;
            }
        }
        else if (request.Status != StepCompletionStatus.Completed && isAlreadyCompleted)
        {
            completedList.Remove(request.ActivityId);
            stepProgress.CompletedActivityIds = completedList.ToArray();
            progressChanged = true;

            stepProgress.Status = StepCompletionStatus.InProgress;
            stepProgress.CompletedAt = null;
        }

        // 5. Persist Changes
        if (isNewStepProgress)
        {
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            await _stepProgressRepository.AddAsync(stepProgress, cancellationToken);
            _logger.LogInformation("Created NEW progress record for Step {StepId}", stepProgress.StepId);
        }
        else if (progressChanged)
        {
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
            _logger.LogInformation("Updated EXISTING progress record for Step {StepId}", stepProgress.StepId);
        }
    }

    private static string ExtractJsonString(object? content)
    {
        if (content is null) return "{}";
        if (content is string s) return s;
        if (content is JsonElement je) return je.GetRawText();

        var typeName = content.GetType().Name;
        if (typeName == "JObject" || typeName == "JArray" || typeName == "JToken")
            return content.ToString()!;

        return JsonSerializer.Serialize(content);
    }

    private static JsonElement? TryGetActivitiesElement(JsonDocument doc)
    {
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array) return root;
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, "activities", StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array) return prop.Value;
                }
            }
        }
        return null;
    }

    private bool IsQuizActivity(QuestStep step, Guid activityId)
    {
        try
        {
            var jsonString = ExtractJsonString(step.Content);
            _logger.LogInformation("🔍 IsQuizActivity: Checking activity {ActivityId} in step content", activityId);

            using var doc = JsonDocument.Parse(jsonString);

            var activitiesElement = TryGetActivitiesElement(doc);
            if (activitiesElement == null)
            {
                _logger.LogWarning("🔍 IsQuizActivity: No activities element found in step content");
                return false;
            }

            foreach (var activity in activitiesElement.Value.EnumerateArray())
            {
                if (activity.ValueKind != JsonValueKind.Object)
                    continue;

                // Case-insensitive lookup for activityId
                var idEl = GetPropertyCaseInsensitive(activity, "activityId");
                if (idEl == null || !Guid.TryParse(idEl.Value.GetString(), out var id))
                    continue;

                if (id == activityId)
                {
                    // Case-insensitive lookup for type
                    var typeEl = GetPropertyCaseInsensitive(activity, "type");
                    if (typeEl != null)
                    {
                        var type = typeEl.Value.GetString();
                        var isQuiz = string.Equals(type, "Quiz", StringComparison.OrdinalIgnoreCase);
                        _logger.LogInformation("🔍 IsQuizActivity: Found activity {ActivityId}, Type={Type}, IsQuiz={IsQuiz}",
                            activityId, type, isQuiz);
                        return isQuiz;
                    }
                }
            }

            _logger.LogWarning("🔍 IsQuizActivity: Activity {ActivityId} not found in activities array", activityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔍 IsQuizActivity: Error checking if activity {ActivityId} is quiz", activityId);
        }
        return false;
    }

    private bool CheckIfStepIsComplete(QuestStep step, List<Guid> completedActivityIds)
    {
        try
        {
            var jsonString = ExtractJsonString(step.Content);
            _logger.LogInformation("📊 CheckIfStepIsComplete: Checking step {StepId}, CompletedActivityIds count: {Count}",
                step.Id, completedActivityIds.Count);

            using var doc = JsonDocument.Parse(jsonString);

            var activitiesElement = TryGetActivitiesElement(doc);

            if (activitiesElement.HasValue && activitiesElement.Value.ValueKind == JsonValueKind.Array)
            {
                int totalActivities = activitiesElement.Value.GetArrayLength();
                int matchedCount = 0;

                foreach (var activity in activitiesElement.Value.EnumerateArray())
                {
                    if (activity.ValueKind != JsonValueKind.Object)
                        continue;

                    // Case-insensitive lookup for activityId
                    var idElement = GetPropertyCaseInsensitive(activity, "activityId");
                    if (idElement != null && Guid.TryParse(idElement.Value.GetString(), out var activityGuid))
                    {
                        if (completedActivityIds.Contains(activityGuid))
                        {
                            matchedCount++;
                        }
                    }
                }

                var isComplete = matchedCount >= totalActivities;
                _logger.LogInformation("📊 CheckIfStepIsComplete: Step {StepId} - Matched {Matched}/{Total}, IsComplete={IsComplete}",
                    step.Id, matchedCount, totalActivities, isComplete);

                return isComplete;
            }
            else
            {
                _logger.LogWarning("📊 CheckIfStepIsComplete: No activities element found for step {StepId}", step.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "📊 CheckIfStepIsComplete: Error checking step completion for Step {StepId}", step.Id);
        }
        return false;
    }

    /// <summary>
    /// Gets a property from a JSON element using case-insensitive matching.
    /// Supports both PascalCase and camelCase property names.
    /// </summary>
    private static JsonElement? GetPropertyCaseInsensitive(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }
}