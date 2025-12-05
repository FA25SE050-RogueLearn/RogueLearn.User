// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestSteps/GenerateQuestStepsCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using AutoMapper;
using System.Text.Json.Serialization;
using RogueLearn.User.Application.Common;
using Hangfire;
using RogueLearn.User.Application.Models;
using System.Text.RegularExpressions;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

public class GenerateQuestStepsCommandHandler : IRequestHandler<GenerateQuestStepsCommand, List<GeneratedQuestStepDto>>
{
    // ========== CONFIGURATION CONSTANTS ==========
    private const int SessionsPerWeek = 5;
    private const int MinQuestSteps = 5;
    private const int MaxQuestSteps = 12;
    private const int MinActivitiesPerStep = 6;
    private const int MaxActivitiesPerStep = 10;
    private const int MinXpPerStep = 250;
    private const int MaxXpPerStep = 400;

    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<GenerateQuestStepsCommandHandler> _logger;
    private readonly IQuestGenerationPlugin _questGenerationPlugin;
    private readonly IMapper _mapper;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IClassRepository _classRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly QuestStepsPromptBuilder _promptBuilder;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ITopicGrouperService _topicGrouperService;

    public GenerateQuestStepsCommandHandler(
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        ISubjectRepository subjectRepository,
        ILogger<GenerateQuestStepsCommandHandler> logger,
        IQuestGenerationPlugin questGenerationPlugin,
        IMapper mapper,
        IUserProfileRepository userProfileRepository,
        IClassRepository classRepository,
        ISkillRepository skillRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        QuestStepsPromptBuilder promptBuilder,
        IUserSkillRepository userSkillRepository,
        ITopicGrouperService topicGrouperService)
    {
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
        _questGenerationPlugin = questGenerationPlugin;
        _mapper = mapper;
        _userProfileRepository = userProfileRepository;
        _classRepository = classRepository;
        _skillRepository = skillRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _promptBuilder = promptBuilder;
        _userSkillRepository = userSkillRepository;
        _topicGrouperService = topicGrouperService;
    }

    public async Task<List<GeneratedQuestStepDto>> Handle(GenerateQuestStepsCommand request, CancellationToken cancellationToken)
    {
        // ========== 1. PRE-CONDITION CHECKS ==========
        var questHasSteps = await _questStepRepository.QuestContainsSteps(request.QuestId, cancellationToken);
        if (questHasSteps)
        {
            _logger.LogWarning("Quest {QuestId} already has steps. Proceeding (might create duplicates if not cleared).", request.QuestId);
        }

        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
            ?? throw new NotFoundException("User Profile not found.");

        if (!userProfile.ClassId.HasValue) throw new BadRequestException("Please choose a Class first");

        var userClass = await _classRepository.GetByIdAsync(userProfile.ClassId.Value, cancellationToken)
            ?? throw new BadRequestException("Class not found");

        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (quest.SubjectId is null) throw new BadRequestException("Quest is not associated with a subject.");

        var subject = await _subjectRepository.GetByIdAsync(quest.SubjectId.Value, cancellationToken)
            ?? throw new NotFoundException("Subject", quest.SubjectId.Value);

        if (subject.Content is null || !subject.Content.Any()) throw new BadRequestException("No syllabus content available.");

        // ========== 2. PREPARE SESSIONS ==========
        List<SyllabusSessionDto> allSessions = new();
        try
        {
            var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(subject.Content);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
            options.Converters.Add(new SyllabusSessionDtoConverter());
            var content = JsonSerializer.Deserialize<SyllabusContent>(jsonString, options) ?? new SyllabusContent();
            content.SessionSchedule ??= new List<SyllabusSessionDto>();
            allSessions = content.SessionSchedule;
        }
        catch (JsonException ex) { _logger.LogError(ex, "Failed to deserialize syllabus."); }

        if (!allSessions.Any()) throw new BadRequestException("Syllabus content missing SessionSchedule.");

        // ========== 3. SKILL UNLOCKING ==========
        var skillMappings = await _subjectSkillMappingRepository.GetMappingsBySubjectIdsAsync(new[] { quest.SubjectId.Value }, cancellationToken);
        var relevantSkillIds = skillMappings.Select(m => m.SkillId).ToHashSet();
        var relevantSkills = (await _skillRepository.GetAllAsync(cancellationToken)).Where(s => relevantSkillIds.Contains(s.Id)).ToList();

        // ========== 4. TOPIC GROUPING ==========
        // ⭐ FIXED TYPO HERE: GroupSessionsIntoModules
        var modules = _topicGrouperService.GroupSessionsIntoModules(allSessions);

        UpdateHangfireJobProgress(request.HangfireJobId, 0, modules.Count, "Starting master quest generation...");

        var createdSteps = new List<QuestStep>();
        int processedModules = 0;

        // ========== 5. GENERATE MASTER QUEST STEPS ==========
        foreach (var module in modules)
        {
            try
            {
                UpdateHangfireJobProgress(request.HangfireJobId, processedModules, modules.Count, $"Generating Module {module.ModuleNumber}: {module.Title}...");

                var prompt = _promptBuilder.BuildMasterPrompt(module, relevantSkills, subject.SubjectName, subject.Description ?? "", userClass);
                var generatedJson = await _questGenerationPlugin.GenerateFromPromptAsync(prompt, cancellationToken);

                if (string.IsNullOrWhiteSpace(generatedJson)) continue;

                using var doc = JsonDocument.Parse(generatedJson);
                var root = doc.RootElement;
                var variants = new[] { "standard", "supportive", "challenging" };

                foreach (var variantKey in variants)
                {
                    if (root.TryGetProperty(variantKey, out var variantContent))
                    {
                        if (variantContent.TryGetProperty("activities", out var activitiesElement))
                        {
                            var options = new JsonSerializerOptions();
                            options.Converters.Add(new ObjectToInferredTypesConverter());

                            var activitiesList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                                activitiesElement.GetRawText(),
                                options
                            );

                            // ⭐ CRITICAL FIX: Enforce UUIDs for all activity IDs
                            // This overwrites whatever the AI generated (e.g., "M1-standard-1") with a valid GUID.
                            if (activitiesList != null)
                            {
                                foreach (var activity in activitiesList)
                                {
                                    activity["activityId"] = Guid.NewGuid().ToString();
                                }
                            }

                            int xp = CalculateTotalExperience(activitiesElement);

                            string dbVariant = variantKey switch
                            {
                                "standard" => "Standard",
                                "supportive" => "Supportive",
                                "challenging" => "Challenging",
                                _ => "Standard"
                            };

                            var step = new QuestStep
                            {
                                QuestId = request.QuestId,
                                StepNumber = module.ModuleNumber,
                                ModuleNumber = module.ModuleNumber,
                                DifficultyVariant = dbVariant,
                                Title = $"{module.Title} ({dbVariant})",
                                Description = $"Module {module.ModuleNumber} - {dbVariant} Track",
                                ExperiencePoints = xp,
                                Content = new Dictionary<string, object> { { "activities", activitiesList ?? new List<Dictionary<string, object>>() } },
                                CreatedAt = DateTimeOffset.UtcNow,
                                UpdatedAt = DateTimeOffset.UtcNow
                            };

                            await _questStepRepository.AddAsync(step, cancellationToken);
                            createdSteps.Add(step);
                            _logger.LogInformation("Saved Module {Module} Variant {Variant}", module.ModuleNumber, dbVariant);
                        }
                    }
                }
                processedModules++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Module {Module}", module.ModuleNumber);
            }
        }

        UpdateHangfireJobProgress(request.HangfireJobId, processedModules, modules.Count, "Completed");
        return _mapper.Map<List<GeneratedQuestStepDto>>(createdSteps);
    }

    private int CalculateTotalExperience(JsonElement activitiesElement)
    {
        int total = 0;
        if (activitiesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var act in activitiesElement.EnumerateArray())
            {
                if (act.TryGetProperty("payload", out var payload) &&
                    payload.TryGetProperty("experiencePoints", out var xpEl) &&
                    xpEl.TryGetInt32(out var xp))
                {
                    total += xp;
                }
            }
        }
        return total;
    }

    private void UpdateHangfireJobProgress(string jobId, int current, int total, string message)
    {
        if (string.IsNullOrEmpty(jobId)) return;
        try
        {
            var progressData = new
            {
                CurrentStep = current,
                TotalSteps = total,
                Message = message,
                ProgressPercentage = total > 0 ? (int)Math.Round((decimal)current / total * 100) : 0,
                UpdatedAt = DateTime.UtcNow
            };
            JobStorage.Current.GetConnection().SetJobParameter(jobId, "Progress", JsonSerializer.Serialize(progressData));
        }
        catch { /* Best effort */ }
    }
}