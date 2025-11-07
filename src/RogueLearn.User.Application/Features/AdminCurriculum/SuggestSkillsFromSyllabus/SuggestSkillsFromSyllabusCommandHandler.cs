// RogueLearn.User/src/RogueLearn.User.Application/Features/AdminCurriculum/SuggestSkillsFromSyllabus/SuggestSkillsFromSyllabusCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Interfaces;
// Replaced System.Text.Json with Newtonsoft.Json.Linq to handle the correct object types.
using Newtonsoft.Json.Linq;

namespace RogueLearn.User.Application.Features.AdminCurriculum.SuggestSkillsFromSyllabus;

public class SuggestSkillsFromSyllabusCommandHandler : IRequestHandler<SuggestSkillsFromSyllabusCommand, SuggestSkillsFromSyllabusResponse>
{
    private readonly ISyllabusVersionRepository _syllabusRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISubjectExtractionPlugin _subjectExtractionPlugin;
    private readonly ILogger<SuggestSkillsFromSyllabusCommandHandler> _logger;

    public SuggestSkillsFromSyllabusCommandHandler(
        ISyllabusVersionRepository syllabusRepository,
        ISkillRepository skillRepository,
        ISubjectExtractionPlugin subjectExtractionPlugin,
        ILogger<SuggestSkillsFromSyllabusCommandHandler> logger)
    {
        _syllabusRepository = syllabusRepository;
        _skillRepository = skillRepository;
        _subjectExtractionPlugin = subjectExtractionPlugin;
        _logger = logger;
    }

    public async Task<SuggestSkillsFromSyllabusResponse> Handle(SuggestSkillsFromSyllabusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin Tool: Suggesting skills for SyllabusVersionId {SyllabusVersionId}", request.SyllabusVersionId);

        var syllabus = await _syllabusRepository.GetByIdAsync(request.SyllabusVersionId, cancellationToken);

        if (syllabus?.Content == null || !syllabus.Content.Any())
        {
            throw new NotFoundException($"No syllabus content found for SyllabusVersionId {request.SyllabusVersionId}");
        }

        var learningObjectives = ExtractLearningObjectives(syllabus.Content);
        if (!learningObjectives.Any())
        {
            _logger.LogWarning("No learning objectives found in syllabus for SyllabusVersionId {SyllabusVersionId}", request.SyllabusVersionId);
            return new SuggestSkillsFromSyllabusResponse { Message = "No learning objectives found in syllabus content." };
        }

        var extractedSkillNames = await _subjectExtractionPlugin.ExtractSkillsFromObjectivesAsync(learningObjectives, cancellationToken);
        var uniqueSkillNames = extractedSkillNames
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var allCatalogSkills = (await _skillRepository.GetAllAsync(cancellationToken))
            .ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        var suggestedSkills = uniqueSkillNames.Select(name => new SuggestedSkillDto
        {
            Name = name,
            ExistsInCatalog = allCatalogSkills.ContainsKey(name)
        }).ToList();

        return new SuggestSkillsFromSyllabusResponse
        {
            Message = $"Found {suggestedSkills.Count} potential skills.",
            SuggestedSkills = suggestedSkills
        };
    }

    private List<string> ExtractLearningObjectives(Dictionary<string, object> content)
    {
        var objectives = new List<string>();
        try
        {
            // The parsing logic is updated to handle Newtonsoft's JArray and JObject.
            // It correctly checks for the "sessions" key and then iterates through the JArray.
            if (content.TryGetValue("sessions", out var sessionsObj) && sessionsObj is JArray sessionsArray)
            {
                foreach (var sessionToken in sessionsArray)
                {
                    if (sessionToken is JObject sessionObject &&
                        sessionObject.TryGetValue("lo", StringComparison.OrdinalIgnoreCase, out var loToken) &&
                        loToken.Type == JTokenType.String)
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