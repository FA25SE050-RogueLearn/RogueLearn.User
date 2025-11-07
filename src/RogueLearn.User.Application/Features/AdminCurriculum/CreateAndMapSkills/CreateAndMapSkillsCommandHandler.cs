// RogueLearn.User/src/RogueLearn.User.Application/Features/AdminCurriculum/CreateAndMapSkills/CreateAndMapSkillsCommandHandler.cs
using BuildingBlocks.Shared.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.AdminCurriculum.CreateAndMapSkills;

public class CreateAndMapSkillsCommandHandler : IRequestHandler<CreateAndMapSkillsCommand, CreateAndMapSkillsResponse>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISyllabusVersionRepository _syllabusRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISubjectSkillMappingRepository _mappingRepository;
    private readonly ISubjectExtractionPlugin _extractionPlugin;
    private readonly ILogger<CreateAndMapSkillsCommandHandler> _logger;

    public CreateAndMapSkillsCommandHandler(
        ISubjectRepository subjectRepository,
        ISyllabusVersionRepository syllabusRepository,
        ISkillRepository skillRepository,
        ISubjectSkillMappingRepository mappingRepository,
        ISubjectExtractionPlugin extractionPlugin,
        ILogger<CreateAndMapSkillsCommandHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _syllabusRepository = syllabusRepository;
        _skillRepository = skillRepository;
        _mappingRepository = mappingRepository;
        _extractionPlugin = extractionPlugin;
        _logger = logger;
    }

    public async Task<CreateAndMapSkillsResponse> Handle(CreateAndMapSkillsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("==> CreateAndMapSkillsCommandHandler: Starting execution for SyllabusVersionId {SyllabusVersionId}", request.SyllabusVersionId);

        var syllabus = await _syllabusRepository.GetByIdAsync(request.SyllabusVersionId, cancellationToken)
            ?? throw new NotFoundException(nameof(SyllabusVersion), request.SyllabusVersionId);

        var subject = await _subjectRepository.GetByIdAsync(syllabus.SubjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Subject), syllabus.SubjectId);

        _logger.LogInformation("==> Found Subject: {SubjectCode} from Syllabus Version {VersionNumber}", subject.SubjectCode, syllabus.VersionNumber);

        if (syllabus?.Content == null || !syllabus.Content.Any())
        {
            throw new NotFoundException($"No syllabus content found for Syllabus Version {syllabus.VersionNumber} of Subject {subject.SubjectCode}");
        }

        var learningObjectives = ExtractLearningObjectives(syllabus.Content);
        if (!learningObjectives.Any())
        {
            _logger.LogWarning("==> No learning objectives extracted from syllabus.");
            return new CreateAndMapSkillsResponse { Message = "No learning objectives found in syllabus to generate skills from." };
        }

        _logger.LogInformation("==> Extracted {Count} learning objectives. Calling AI to suggest skills.", learningObjectives.Count);
        var suggestedNames = (await _extractionPlugin.ExtractSkillsFromObjectivesAsync(learningObjectives, cancellationToken))
            .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (!suggestedNames.Any())
        {
            _logger.LogWarning("==> AI did not suggest any skills.");
            return new CreateAndMapSkillsResponse { Message = "AI could not suggest any skills from the syllabus." };
        }
        _logger.LogInformation("==> AI suggested {Count} unique skills: {Skills}", suggestedNames.Count, string.Join(", ", suggestedNames));


        var allSkills = (await _skillRepository.GetAllAsync(cancellationToken)).ToList();
        var skillMap = allSkills.ToDictionary(s => s.Name.ToSlug(), s => s);
        _logger.LogInformation("==> Loaded {Count} existing skills from catalog.", allSkills.Count);

        var response = new CreateAndMapSkillsResponse();

        foreach (var name in suggestedNames)
        {
            var slug = name.ToSlug();
            if (!skillMap.TryGetValue(slug, out var skill))
            {
                _logger.LogInformation("==> Creating new skill from suggestion: '{SkillName}'", name);
                skill = new Skill
                {
                    Name = name,
                    Domain = "Academic",
                    Tier = Domain.Enums.SkillTierLevel.Foundation,
                    Description = $"Auto-generated skill from syllabus of {subject.SubjectCode}",
                    SourceSubjectId = subject.Id
                };
                skill = await _skillRepository.AddAsync(skill, cancellationToken);
                skillMap[slug] = skill;
                response.SkillsCreated.Add(name);
                _logger.LogInformation("==> [DATABASE-CREATE] Created Skill with ID {SkillId}", skill.Id);
            }
            else
            {
                _logger.LogInformation("==> Reusing existing skill: '{SkillName}' with ID {SkillId}", name, skill.Id);
                response.SkillsReused.Add(name);
            }

            var mappingExists = await _mappingRepository.AnyAsync(m => m.SubjectId == subject.Id && m.SkillId == skill.Id, cancellationToken);
            if (!mappingExists)
            {
                var mapping = new SubjectSkillMapping { SubjectId = subject.Id, SkillId = skill.Id };
                await _mappingRepository.AddAsync(mapping, cancellationToken);
                response.MappingsCreated.Add($"{subject.SubjectCode} -> {skill.Name}");
                _logger.LogInformation("==> [DATABASE-CREATE] Created mapping for Subject {SubjectId} to Skill {SkillId}", subject.Id, skill.Id);
            }
            else
            {
                response.MappingsExisted.Add($"{subject.SubjectCode} -> {skill.Name}");
            }
        }

        response.Message = $"Function executed successfully. Skills Created: {response.SkillsCreated.Count}. Skills Reused: {response.SkillsReused.Count}. Mappings Created: {response.MappingsCreated.Count}.";
        _logger.LogInformation("==> CreateAndMapSkillsCommandHandler: Execution finished. {Message}", response.Message);

        return response;
    }

    private List<string> ExtractLearningObjectives(Dictionary<string, object> content)
    {
        var objectives = new List<string>();
        try
        {
            if (content.TryGetValue("sessions", out var sessionsObj) && (sessionsObj is JsonElement sessionsElement && sessionsElement.ValueKind == JsonValueKind.Array))
            {
                foreach (var session in sessionsElement.EnumerateArray())
                {
                    if (session.TryGetProperty("lo", out var loElement) && loElement.ValueKind == JsonValueKind.String)
                    {
                        var lo = loElement.GetString();
                        if (!string.IsNullOrWhiteSpace(lo)) objectives.Add(lo);
                    }
                }
            }
            else if (sessionsObj is Newtonsoft.Json.Linq.JArray sessionsArray)
            {
                foreach (var sessionToken in sessionsArray)
                {
                    if (sessionToken is Newtonsoft.Json.Linq.JObject sessionObject &&
                        sessionObject.TryGetValue("lo", StringComparison.OrdinalIgnoreCase, out var loToken) &&
                        loToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
                    {
                        var learningObjective = loToken.Value<string>();
                        if (!string.IsNullOrWhiteSpace(learningObjective))
                        {
                            objectives.Add(learningObjective);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not process syllabus content to extract learning objectives.");
        }
        return objectives;
    }
}