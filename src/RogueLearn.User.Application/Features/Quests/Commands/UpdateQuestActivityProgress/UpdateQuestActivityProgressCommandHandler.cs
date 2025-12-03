// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestActivityProgress/UpdateQuestActivityProgressCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;
using System.Text.Json;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Application.Common;
using RogueLearn.User.Application.Features.Quests.Services;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;

public class UpdateQuestActivityProgressCommandHandler : IRequestHandler<UpdateQuestActivityProgressCommand>
{
    private record ActivityPayload(JsonElement SkillId, JsonElement ExperiencePoints);

    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IQuestSubmissionRepository _submissionRepository;
    private readonly ActivityValidationService _activityValidationService;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateQuestActivityProgressCommandHandler> _logger;

    public UpdateQuestActivityProgressCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository,
        IQuestRepository questRepository,
        IQuestSubmissionRepository submissionRepository,
        ActivityValidationService activityValidationService,
        IMediator mediator,
        ILogger<UpdateQuestActivityProgressCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository;
        _questRepository = questRepository;
        _submissionRepository = submissionRepository;
        _activityValidationService = activityValidationService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(UpdateQuestActivityProgressCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating activity progress for User:{AuthUserId}, Quest:{QuestId}, Step:{StepId}, Activity:{ActivityId} to Status:{Status}",
            request.AuthUserId, request.QuestId, request.StepId, request.ActivityId, request.Status);

        var questStep = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
        if (questStep is null || questStep.QuestId != request.QuestId)
        {
            throw new NotFoundException("QuestStep (weekly module)", request.StepId);
        }

        var attempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken) ?? await CreateNewAttemptAsync(request.AuthUserId, request.QuestId, cancellationToken);

        await MarkParentQuestAsInProgressAsync(request.QuestId, cancellationToken);

        // ⭐ FIX: Get or create step progress with proper error handling for race conditions
        var stepProgress = await GetOrCreateStepProgressAsync(attempt.Id, request.StepId, cancellationToken);

        if (stepProgress.CompletedActivityIds?.Contains(request.ActivityId) == true && request.Status == StepCompletionStatus.Completed)
        {
            _logger.LogInformation("Activity {ActivityId} is already completed for Step {StepId}. No action taken.", request.ActivityId, request.StepId);
            return;
        }

        if (request.Status == StepCompletionStatus.Completed)
        {
            // ⭐ VALIDATION: Check if activity type requires submission validation (Quiz/KnowledgeCheck)
            var activityType = ExtractActivityType(questStep.Content!, request.ActivityId);

            var (canComplete, validationMessage) = await _activityValidationService.ValidateActivityCompletion(
                request.ActivityId,
                request.AuthUserId,
                activityType,
                cancellationToken);

            if (!canComplete)
            {
                _logger.LogWarning(
                    "Activity completion validation failed for {ActivityId}: {Message}. " +
                    "Activity Type: {ActivityType}",
                    request.ActivityId, validationMessage, activityType);

                // ✅ GRACEFUL: Return early with 400 Bad Request instead of throwing
                // This allows the controller to handle it properly
                throw new ValidationException(validationMessage);  // ✅ Use ValidationException instead
            }

            _logger.LogInformation("Activity {ActivityId} passed completion validation", request.ActivityId);

            await CompleteActivityAndCheckForModuleCompletion(questStep, stepProgress, attempt, request.ActivityId, cancellationToken);
        }
        else
        {
            stepProgress.Status = request.Status;
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
        }

        _logger.LogInformation("Successfully updated progress for Activity:{ActivityId} to Status:{Status}", request.ActivityId, request.Status);
    }

    private async Task<UserQuestAttempt> CreateNewAttemptAsync(Guid authUserId, Guid questId, CancellationToken cancellationToken)
    {
        var newAttempt = new UserQuestAttempt
        {
            AuthUserId = authUserId,
            QuestId = questId,
            Status = QuestAttemptStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow
        };
        var createdAttempt = await _attemptRepository.AddAsync(newAttempt, cancellationToken);
        _logger.LogInformation("Created new UserQuestAttempt {AttemptId} for Quest {QuestId}", createdAttempt.Id, questId);
        return createdAttempt;
    }

    private async Task MarkParentQuestAsInProgressAsync(Guid questId, CancellationToken cancellationToken)
    {
        var parentQuest = await _questRepository.GetByIdAsync(questId, cancellationToken)
            ?? throw new NotFoundException("Quest", questId);

        if (parentQuest.Status == QuestStatus.NotStarted)
        {
            parentQuest.Status = QuestStatus.InProgress;
            await _questRepository.UpdateAsync(parentQuest, cancellationToken);
            _logger.LogInformation("Parent Quest {QuestId} status updated to 'InProgress' due to first user action.", parentQuest.Id);
        }
    }

    /// <summary>
    /// ⭐ FIX: Get or create step progress with proper handling of race conditions
    /// 
    /// Problem: Two concurrent requests could both try to INSERT the same (attemptId, stepId) record
    /// causing a unique constraint violation.
    /// 
    /// Solution: Use try-catch to handle the race condition gracefully:
    /// 1. First, try to fetch existing record
    /// 2. If not found, create new one with assigned ID
    /// 3. If INSERT fails due to duplicate key, fetch the record that was just created
    /// </summary>
    private async Task<UserQuestStepProgress> GetOrCreateStepProgressAsync(
        Guid attemptId, Guid stepId, CancellationToken cancellationToken)
    {
        // Step 1: Try to find existing record
        var existing = await _stepProgressRepository.FirstOrDefaultAsync(
            sp => sp.AttemptId == attemptId && sp.StepId == stepId,
            cancellationToken);

        if (existing != null)
        {
            _logger.LogInformation("✅ Found existing UserQuestStepProgress {ProgressId}", existing.Id);
            return existing;
        }

        // Step 2: Create new record with assigned ID (prevents empty GUID issues)
        var newStepProgress = new UserQuestStepProgress
        {
            Id = Guid.NewGuid(),  // ⭐ CRITICAL: Assign ID before INSERT
            AttemptId = attemptId,
            StepId = stepId,
            Status = StepCompletionStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            AttemptsCount = 1,
            CompletedActivityIds = Array.Empty<Guid>()
        };

        try
        {
            var created = await _stepProgressRepository.AddAsync(newStepProgress, cancellationToken);
            _logger.LogInformation("✅ Created new UserQuestStepProgress {ProgressId} for Attempt {AttemptId}, Step {StepId}",
                created.Id, attemptId, stepId);
            return created;
        }
        catch (Exception ex) when (ex.Message.Contains("23505") || ex.Message.Contains("duplicate"))
        {
            // Step 3: Handle race condition - another request inserted it first
            _logger.LogWarning(
                "⚠️ Race condition detected! Another request created the record first. " +
                "Fetching the record that was just created. Error: {Error}",
                ex.Message);

            // Try to fetch it now - another request must have created it
            var justCreated = await _stepProgressRepository.FirstOrDefaultAsync(
                sp => sp.AttemptId == attemptId && sp.StepId == stepId,
                cancellationToken);

            if (justCreated != null)
            {
                _logger.LogInformation(
                    "✅ Successfully recovered from race condition. Using record {ProgressId} " +
                    "that was created by concurrent request.",
                    justCreated.Id);
                return justCreated;
            }

            // If we still can't find it, something is seriously wrong
            _logger.LogError(
                "❌ CRITICAL: Race condition handling failed - record should exist but wasn't found. " +
                "Original error: {Error}",
                ex.Message);
            throw;
        }
    }

    // ⭐ FIXED: Now handles both JSON string and Dictionary content formats
    private async Task CompleteActivityAndCheckForModuleCompletion(
        QuestStep questStep,
        UserQuestStepProgress stepProgress,
        UserQuestAttempt attempt,
        Guid activityIdToComplete,
        CancellationToken cancellationToken)
    {
        // 1. Parse the content - it might be a JSON string or Dictionary
        List<object> activities = new();

        _logger.LogInformation("🔍 Content type: {ContentType}", questStep.Content?.GetType().Name ?? "null");
        _logger.LogInformation("🔍 Content value: {Content}", questStep.Content?.ToString() ?? "null");

        try
        {
            // ⭐ CRITICAL: Handle case when Content is a string stored in JSONB
            if (questStep.Content is string jsonString)
            {
                _logger.LogInformation("📄 Content detected as string, length: {Length}", jsonString.Length);

                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    throw new InvalidOperationException("Quest step content JSON string is empty");
                }

                // Try to parse the JSON string
                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    var root = doc.RootElement;
                    _logger.LogInformation("✅ Parsed JSON root type: {Type}", root.ValueKind);

                    if (root.TryGetProperty("activities", out var activitiesElement))
                    {
                        _logger.LogInformation("📄 Found 'activities' property, type: {Type}", activitiesElement.ValueKind);

                        if (activitiesElement.ValueKind == JsonValueKind.Array)
                        {
                            var count = activitiesElement.GetArrayLength();
                            _logger.LogInformation("✅ Activities is array with {Count} items", count);

                            activities = activitiesElement
                                .EnumerateArray()
                                .Select(item => (object)item.GetRawText())
                                .ToList();
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ 'activities' property is not an array, it's: {Type}", activitiesElement.ValueKind);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ 'activities' property not found in root");
                        // Log available properties
                        var properties = string.Join(", ", root.EnumerateObject().Select(p => p.Name));
                        _logger.LogWarning("📋 Available properties: {Properties}", properties);
                    }
                }
            }
            else if (questStep.Content is Dictionary<string, object> contentDict)
            {
                _logger.LogInformation("📄 Content is Dictionary, extracting activities...");

                if (contentDict.TryGetValue("activities", out var activitiesObj))
                {
                    _logger.LogInformation("✅ Found 'activities' in Dictionary, type: {Type}", activitiesObj?.GetType().Name ?? "null");

                    if (activitiesObj is List<object> activitiesList)
                    {
                        activities = activitiesList;
                        _logger.LogInformation("✅ Extracted {Count} activities from Dictionary", activities.Count);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ 'activities' is not List<object>, it's: {Type}", activitiesObj?.GetType().Name ?? "null");
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ 'activities' key not found in Dictionary");
                    var keys = string.Join(", ", contentDict.Keys);
                    _logger.LogWarning("📋 Available keys: {Keys}", keys);
                }
            }
            // ⭐ NEW: Handle Newtonsoft JObject (from JSONB storage)
            else if (questStep.Content != null && questStep.Content.GetType().Name == "JObject")
            {
                _logger.LogInformation("📄 Content is JObject (Newtonsoft), converting to Dictionary...");

                try
                {
                    // Convert JObject to JSON string first
                    var jObjectJson = questStep.Content.ToString();
                    _logger.LogInformation("✅ Converted JObject to string");

                    // Parse as JSON
                    using (JsonDocument doc = JsonDocument.Parse(jObjectJson!))
                    {
                        var root = doc.RootElement;

                        if (root.TryGetProperty("activities", out var activitiesElement) &&
                            activitiesElement.ValueKind == JsonValueKind.Array)
                        {
                            var count = activitiesElement.GetArrayLength();
                            _logger.LogInformation("✅ Found 'activities' array with {Count} items", count);

                            activities = activitiesElement
                                .EnumerateArray()
                                .Select(item => (object)item.GetRawText())
                                .ToList();

                            _logger.LogInformation("✅ Extracted {Count} activities from JObject", activities.Count);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ 'activities' property not found in JObject");
                            var properties = string.Join(", ", root.EnumerateObject().Select(p => p.Name));
                            _logger.LogWarning("📋 Available properties: {Properties}", properties);
                        }
                    }
                }
                catch (Exception jEx)
                {
                    _logger.LogError(jEx, "❌ Failed to parse JObject content");
                    throw;
                }
            }
            else
            {
                _logger.LogError("❌ Content is unsupported type: {Type}", questStep.Content?.GetType().FullName ?? "null");
            }

            if (activities == null || activities.Count == 0)
            {
                _logger.LogError("❌ QuestStep {StepId} - activities is null or empty after parsing", questStep.Id);
                throw new InvalidOperationException("Quest step content is invalid - no activities found.");
            }

            _logger.LogInformation("✅ Successfully prepared {Count} activities for processing", activities.Count);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "❌ Failed to parse quest step content as JSON");
            throw new InvalidOperationException("Failed to parse quest step content", jsonEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error parsing quest step content");
            throw;
        }

        // 2. Find the specific activity being completed
        Dictionary<string, object> activityToComplete = new();

        foreach (var activityObj in activities)
        {
            if (activityObj is Dictionary<string, object> dict)
            {
                // Already a dictionary
                if (dict.TryGetValue("activityId", out var idObj) &&
                    idObj is string idStr &&
                    Guid.TryParse(idStr, out var id) &&
                    id == activityIdToComplete)
                {
                    activityToComplete = dict;
                    break;
                }
            }
            else if (activityObj is string jsonStr)
            {
                // Parse JSON string
                using (JsonDocument doc = JsonDocument.Parse(jsonStr))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("activityId", out var idElement) &&
                        idElement.ValueKind != JsonValueKind.Null &&
                        Guid.TryParse(idElement.GetString(), out var id) &&
                        id == activityIdToComplete)
                    {
                        // Convert to Dictionary for further processing
                        activityToComplete = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr)!;
                        break;
                    }
                }
            }
        }

        if (activityToComplete == null)
        {
            _logger.LogError("❌ Activity {ActivityId} not found in quest step {StepId}", activityIdToComplete, questStep.Id);
            throw new NotFoundException("Activity", activityIdToComplete);
        }

        // 3. Dispatch the XP event for this specific activity
        if (activityToComplete.TryGetValue("payload", out var payloadObj))
        {
            _logger.LogInformation("📦 Payload type: {Type}", payloadObj?.GetType().Name ?? "null");

            Dictionary<string, object> payload = new();

            // Handle different payload types
            if (payloadObj is Dictionary<string, object> dictPayload)
            {
                payload = dictPayload;
                _logger.LogInformation("✅ Payload is Dictionary");
            }
            else if (payloadObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                // Convert JsonElement to Dictionary
                var payloadJson = jsonElement.GetRawText();
                payload = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson)!;
                _logger.LogInformation("✅ Converted JsonElement payload to Dictionary");
            }
            else if (payloadObj is string payloadStr)
            {
                // Parse string as JSON
                payload = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadStr)!;
                _logger.LogInformation("✅ Parsed string payload as Dictionary");
            }

            if (payload != null)
            {
                _logger.LogInformation("💰 Extracting skillId and experiencePoints from payload...");

                // Extract skillId
                Guid? skillId = null;
                if (payload.TryGetValue("skillId", out var skillIdObj))
                {
                    var skillIdStr = skillIdObj?.ToString();
                    if (Guid.TryParse(skillIdStr, out var parsedSkillId))
                    {
                        skillId = parsedSkillId;
                        _logger.LogInformation("✅ Extracted skillId: {SkillId}", skillId);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Failed to parse skillId: {SkillId}", skillIdStr);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ skillId not found in payload");
                }

                // Extract experiencePoints
                int? xp = null;
                if (payload.TryGetValue("experiencePoints", out var xpObj))
                {
                    var xpStr = xpObj?.ToString();
                    if (int.TryParse(xpStr, out var parsedXp))
                    {
                        xp = parsedXp;
                        _logger.LogInformation("✅ Extracted XP: {XP}", xp);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Failed to parse experiencePoints: {XP}", xpStr);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ experiencePoints not found in payload");
                }

                // Only dispatch if both skillId and XP extracted successfully
                if (skillId.HasValue && xp.HasValue)
                {
                    var xpEvent = new IngestXpEventCommand
                    {
                        AuthUserId = attempt.AuthUserId,
                        SourceService = "QuestsService",
                        SourceType = SkillRewardSourceType.QuestComplete.ToString(),
                        SourceId = activityIdToComplete,
                        SkillId = skillId.Value,
                        Points = xp.Value,
                        Reason = $"Completed activity in quest: {questStep.Title}"
                    };
                    await _mediator.Send(xpEvent, cancellationToken);
                    _logger.LogInformation("✅ DISPATCHED IngestXpEvent for SkillId '{SkillId}' with {XP} XP from Activity '{ActivityId}'", skillId, xp, activityIdToComplete);
                }
                else
                {
                    _logger.LogError("❌ Cannot dispatch XP event: skillId={SkillId}, xp={XP}", skillId, xp);
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Could not convert payload to Dictionary");
            }
        }
        else
        {
            _logger.LogWarning("⚠️ 'payload' property not found in activity");
        }

        // 4. Update the progress record by adding the completed activity's ID
        stepProgress.CompletedActivityIds = (stepProgress.CompletedActivityIds ?? Array.Empty<Guid>()).Append(activityIdToComplete).ToArray();
        stepProgress.UpdatedAt = DateTimeOffset.UtcNow;

        // 5. Check if the entire module (quest_step) is now complete
        var allActivityIds = new HashSet<Guid>();

        foreach (var activityObj in activities)
        {
            Guid activityId = Guid.Empty;

            if (activityObj is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("activityId", out var idObj) && idObj is string idStr && Guid.TryParse(idStr, out var id))
                {
                    activityId = id;
                }
            }
            else if (activityObj is string jsonStr)
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonStr))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("activityId", out var idElement) &&
                        idElement.ValueKind != JsonValueKind.Null &&
                        Guid.TryParse(idElement.GetString(), out var id))
                    {
                        activityId = id;
                    }
                }
            }

            if (activityId != Guid.Empty)
            {
                allActivityIds.Add(activityId);
            }
        }

        if (allActivityIds.IsSubsetOf(stepProgress.CompletedActivityIds.ToHashSet()))
        {
            stepProgress.Status = StepCompletionStatus.Completed;
            stepProgress.CompletedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation("All activities for Step {StepId} are complete. Marking step as 'Completed'.", questStep.Id);

            await CheckForOverallQuestCompletion(stepProgress.AttemptId, cancellationToken);
        }

        // ⭐ CRITICAL FIX: Always use UPDATE since we now guarantee the record exists
        // (created in GetOrCreateStepProgressAsync with proper error handling)
        try
        {
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
            _logger.LogInformation("✅ Updated UserQuestStepProgress {ProgressId}", stepProgress.Id);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no results"))
        {
            // This should not happen now, but log if it does
            _logger.LogError("❌ CRITICAL: StepProgress {ProgressId} vanished after creation! Error: {Error}",
                stepProgress.Id, ex.Message);
            throw;
        }
    }

    private async Task CheckForOverallQuestCompletion(Guid attemptId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 Checking for overall quest completion for Attempt {AttemptId}", attemptId);

        try
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId, cancellationToken)
                ?? throw new NotFoundException("UserQuestAttempt", attemptId);

            var allStepsForQuest = await _questStepRepository.FindByQuestIdAsync(attempt.QuestId, cancellationToken);
            var allStepIdsForQuest = allStepsForQuest.Select(s => s.Id).ToHashSet();

            // ⭐ FIX: Fetch ALL step progress first (no enum filter), then filter in-memory
            var allStepProgressForAttempt = (await _stepProgressRepository.FindAsync(
                sp => sp.AttemptId == attemptId,
                cancellationToken
            )).ToList();

            // ✅ Now filter in-memory using enum comparison (this is safe)
            var completedStepsForAttempt = allStepProgressForAttempt
                .Where(sp => sp.Status == StepCompletionStatus.Completed)
                .Select(sp => sp.StepId)
                .ToHashSet();

            _logger.LogInformation("📊 Quest completion check: {Completed}/{Total} steps completed",
                completedStepsForAttempt.Count, allStepIdsForQuest.Count);

            // If all steps are completed, mark quest as complete
            if (allStepIdsForQuest.IsSubsetOf(completedStepsForAttempt))
            {
                if (attempt.Status != QuestAttemptStatus.Completed)
                {
                    attempt.Status = QuestAttemptStatus.Completed;
                    attempt.CompletedAt = DateTimeOffset.UtcNow;
                    await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                    _logger.LogInformation("🏆 All steps completed. Marked Quest Attempt {AttemptId} as 'Completed'.", attempt.Id);
                }

                // Update parent quest status
                var parentQuest = await _questRepository.GetByIdAsync(attempt.QuestId, cancellationToken);
                if (parentQuest != null && parentQuest.Status != QuestStatus.Completed)
                {
                    parentQuest.Status = QuestStatus.Completed;
                    await _questRepository.UpdateAsync(parentQuest, cancellationToken);
                    _logger.LogInformation("🏆 Parent Quest {QuestId} status updated to 'Completed'.", parentQuest.Id);
                }
            }
            else
            {
                // Some steps still pending - keep attempt as InProgress
                if (attempt.Status != QuestAttemptStatus.InProgress)
                {
                    attempt.Status = QuestAttemptStatus.InProgress;
                    attempt.UpdatedAt = DateTimeOffset.UtcNow;
                    await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                }
                _logger.LogInformation("⏳ Quest {QuestId} remains InProgress ({Completed}/{Total} steps)",
                    attempt.QuestId, completedStepsForAttempt.Count, allStepIdsForQuest.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in CheckForOverallQuestCompletion for Attempt {AttemptId}", attemptId);
            throw;
        }
    }

    // ⭐ UPDATED: Helper method to extract activity type from step content JSON
    // Handles string, Dictionary, JObject, and other types with extensive logging
    private string ExtractActivityType(object stepContent, Guid activityId)
    {
        try
        {
            if (stepContent == null)
            {
                _logger.LogWarning("🔍 Step content is NULL for activity {ActivityId}", activityId);
                return "Unknown";
            }

            _logger.LogInformation("🔍 Extracting activity type - Content type: {ContentType}", stepContent.GetType().Name);

            string jsonString = string.Empty;

            // ⭐ Handle different content types
            if (stepContent is string strContent)
            {
                _logger.LogInformation("📄 Content is string, length: {Length}", strContent?.Length ?? 0);

                if (string.IsNullOrWhiteSpace(strContent))
                {
                    _logger.LogWarning("⚠️ Step content string is empty or whitespace for activity {ActivityId}", activityId);
                    return "Unknown";
                }

                jsonString = strContent;
                _logger.LogInformation("📝 String content (first 200 chars): {Content}",
                    strContent.Length > 200 ? strContent.Substring(0, 200) + "..." : strContent);
            }
            else if (stepContent is Dictionary<string, object> dictContent)
            {
                _logger.LogInformation("📦 Content is Dictionary with {KeyCount} keys", dictContent.Count);
                _logger.LogInformation("📋 Dictionary keys: {Keys}", string.Join(", ", dictContent.Keys));

                jsonString = System.Text.Json.JsonSerializer.Serialize(dictContent);
                _logger.LogInformation("✅ Converted Dictionary to JSON string");
            }
            else if (stepContent.GetType().Name == "JObject")
            {
                _logger.LogInformation("📦 Content is JObject (Newtonsoft)");

                try
                {
                    jsonString = stepContent.ToString()!;
                    _logger.LogInformation("✅ Converted JObject to string, length: {Length}", jsonString?.Length ?? 0);
                }
                catch (Exception jEx)
                {
                    _logger.LogError(jEx, "❌ Failed to convert JObject to string");
                    return "Unknown";
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Content is unsupported type: {Type}", stepContent.GetType().FullName);

                // Try to convert to string as fallback
                try
                {
                    jsonString = stepContent.ToString()!;
                    _logger.LogInformation("🔄 Converted unsupported type to string via ToString()");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to convert unsupported type to string");
                    return "Unknown";
                }
            }

            // ⭐ Validate JSON string before parsing
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                _logger.LogWarning("⚠️ JSON string is empty after conversion for activity {ActivityId}", activityId);
                return "Unknown";
            }

            _logger.LogInformation("📝 JSON string to parse, length: {Length}", jsonString.Length);
            _logger.LogInformation("📄 JSON preview (first 300 chars): {JsonPreview}",
                jsonString.Length > 300 ? jsonString.Substring(0, 300) + "..." : jsonString);

            // ⭐ Check for data corruption (nested arrays)
            if (jsonString.Contains("[[") || jsonString.Contains("]]"))
            {
                _logger.LogWarning("⚠️ ⚠️ POTENTIAL DATA CORRUPTION DETECTED - Nested arrays found in JSON!");
                _logger.LogWarning("📋 Full JSON content: {FullJson}", jsonString);
            }

            // Parse JSON
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                var root = doc.RootElement;
                _logger.LogInformation("✅ Successfully parsed JSON, root type: {RootType}", root.ValueKind);

                if (root.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogError("❌ JSON root is not an object, it's: {Type}", root.ValueKind);
                    _logger.LogError("📋 JSON root content: {Content}", root.GetRawText());
                    return "Unknown";
                }

                if (!root.TryGetProperty("activities", out var activitiesElement))
                {
                    _logger.LogWarning("⚠️ 'activities' property not found in root");
                    var properties = string.Join(", ", root.EnumerateObject().Select(p => p.Name));
                    _logger.LogWarning("📋 Available root properties: {Properties}", properties);
                    return "Unknown";
                }

                _logger.LogInformation("✅ Found 'activities' property, type: {Type}", activitiesElement.ValueKind);

                if (activitiesElement.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogError("❌ 'activities' is not an array, it's: {Type}", activitiesElement.ValueKind);
                    _logger.LogError("📋 'activities' content: {Content}", activitiesElement.GetRawText());
                    return "Unknown";
                }

                var arrayLength = activitiesElement.GetArrayLength();
                _logger.LogInformation("✅ Activities array has {Count} items", arrayLength);

                if (arrayLength == 0)
                {
                    _logger.LogWarning("⚠️ Activities array is empty");
                    return "Unknown";
                }

                // ⭐ Iterate through activities with detailed logging
                int activityIndex = 0;
                foreach (var activityElement in activitiesElement.EnumerateArray())
                {
                    _logger.LogInformation("🔍 Checking activity index {Index}, type: {Type}", activityIndex, activityElement.ValueKind);

                    if (activityElement.ValueKind != JsonValueKind.Object)
                    {
                        _logger.LogWarning("⚠️ Activity at index {Index} is not an object, it's: {Type}. Content: {Content}",
                            activityIndex, activityElement.ValueKind, activityElement.GetRawText());
                        activityIndex++;
                        continue;
                    }

                    if (!activityElement.TryGetProperty("activityId", out var idElement))
                    {
                        _logger.LogDebug("⏭️ Activity index {Index} has no 'activityId', skipping", activityIndex);
                        activityIndex++;
                        continue;
                    }

                    if (idElement.ValueKind == JsonValueKind.Null)
                    {
                        _logger.LogDebug("⏭️ Activity index {Index} has null 'activityId', skipping", activityIndex);
                        activityIndex++;
                        continue;
                    }

                    var idString = idElement.GetString();
                    _logger.LogInformation("📌 Activity index {Index} has activityId: {ActivityId}", activityIndex, idString);

                    if (!Guid.TryParse(idString, out var id))
                    {
                        _logger.LogWarning("⚠️ Activity index {Index} has invalid GUID format: {Id}", activityIndex, idString);
                        activityIndex++;
                        continue;
                    }

                    if (id == activityId)
                    {
                        _logger.LogInformation("🎯 FOUND matching activity at index {Index}, ID: {ActivityId}", activityIndex, id);

                        if (!activityElement.TryGetProperty("type", out var typeElement))
                        {
                            _logger.LogWarning("⚠️ Activity {ActivityId} found but 'type' property is missing", id);
                            var availableProps = string.Join(", ", activityElement.EnumerateObject().Select(p => p.Name));
                            _logger.LogWarning("📋 Available properties in activity: {Properties}", availableProps);
                            return "Unknown";
                        }

                        if (typeElement.ValueKind == JsonValueKind.Null)
                        {
                            _logger.LogWarning("⚠️ Activity {ActivityId} 'type' property is null", id);
                            return "Unknown";
                        }

                        var activityType = typeElement.GetString() ?? "Unknown";
                        _logger.LogInformation("✅ ✅ EXTRACTED activity type '{Type}' for activity {ActivityId}", activityType, id);
                        return activityType;
                    }

                    activityIndex++;
                }

                _logger.LogError("❌ Activity {ActivityId} not found in activities array (searched {Count} items)",
                    activityId, arrayLength);
                return "Unknown";
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "❌ JSON parsing error while extracting activity type for activity {ActivityId}", activityId);
            _logger.LogError("📋 JSON Exception details: {Message}", jsonEx.Message);
            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error extracting activity type for activity {ActivityId}", activityId);
            _logger.LogError("📋 Exception details: {Message}", ex.Message);
            return "Unknown";
        }
    }
}
