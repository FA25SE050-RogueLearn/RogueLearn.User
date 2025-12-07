// RogueLearn.User/src/RogueLearn.User.Application/Services/TopicGrouperService.cs
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Models;
using System.Text.RegularExpressions;

namespace RogueLearn.User.Application.Services;

public class QuestStepDefinition
{
    public int ModuleNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<SyllabusSessionDto> Sessions { get; set; } = new();
    public List<string> KeyTopics { get; set; } = new();
}

public interface ITopicGrouperService
{
    List<QuestStepDefinition> GroupSessionsIntoModules(List<SyllabusSessionDto> sessions);
}

public class TopicGrouperService : ITopicGrouperService
{
    private readonly ILogger<TopicGrouperService> _logger;

    // Configuration for grouping heuristics
    private const int MinSessionsPerModule = 2;
    private const int MaxSessionsPerModule = 6;
    private const int IdealSessionsPerModule = 4;

    public TopicGrouperService(ILogger<TopicGrouperService> logger)
    {
        _logger = logger;
    }

    public List<QuestStepDefinition> GroupSessionsIntoModules(List<SyllabusSessionDto> sessions)
    {
        if (sessions == null || !sessions.Any())
            return new List<QuestStepDefinition>();

        // Sort by session number to ensure chronological order
        var sortedSessions = sessions.OrderBy(s => s.SessionNumber).ToList();
        var modules = new List<QuestStepDefinition>();
        var currentModule = new QuestStepDefinition { ModuleNumber = 1 };
        var currentTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("Starting topic grouping for {Count} sessions.", sortedSessions.Count);

        for (int i = 0; i < sortedSessions.Count; i++)
        {
            var session = sortedSessions[i];
            var topic = NormalizeTopic(session.Topic);

            // Decision: Should we start a new module?
            bool shouldBreak = ShouldStartNewModule(
                currentModule.Sessions.Count,
                currentTopics,
                topic,
                i,
                sortedSessions.Count);

            if (shouldBreak)
            {
                // Finalize current module
                FinalizeModule(currentModule);
                modules.Add(currentModule);

                // Start new module
                currentModule = new QuestStepDefinition
                {
                    ModuleNumber = modules.Count + 1
                };
                currentTopics.Clear();
            }

            // Add session to current module
            currentModule.Sessions.Add(session);
            currentTopics.Add(topic);
        }

        // Add the final module
        if (currentModule.Sessions.Any())
        {
            FinalizeModule(currentModule);
            modules.Add(currentModule);
        }

        _logger.LogInformation("Grouped sessions into {ModuleCount} modules.", modules.Count);
        return modules;
    }

    private bool ShouldStartNewModule(
        int currentCount,
        HashSet<string> currentTopics,
        string newTopic,
        int currentIndex,
        int totalSessions)
    {
        // Rule 1: Must meet minimum size (unless it's the very end)
        if (currentCount < MinSessionsPerModule) return false;

        // Rule 2: Hard cap on size
        if (currentCount >= MaxSessionsPerModule) return true;

        // Rule 3: Topic Shift
        // If we have enough sessions and the topic changes significantly, break.
        // We check if the new topic is NOT in the current set of topics.
        bool isTopicChange = !currentTopics.Contains(newTopic) && !IsTopicRelated(currentTopics, newTopic);

        if (currentCount >= IdealSessionsPerModule && isTopicChange)
        {
            return true;
        }

        // Rule 4: Remaining sessions logic
        // If adding this session leaves too few for the next module, keep adding to this one
        int remaining = totalSessions - (currentIndex + 1); // +1 because we haven't added current yet
        if (remaining < MinSessionsPerModule && currentCount < MaxSessionsPerModule)
        {
            return false; // Absorb the tail end
        }

        return false;
    }

    private void FinalizeModule(QuestStepDefinition module)
    {
        // Generate a title based on the most frequent or first topic
        var topics = module.Sessions
            .Select(s => NormalizeTopic(s.Topic))
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        module.KeyTopics = topics;

        // Simple title strategy: Top 1-2 topics
        if (topics.Count > 0)
        {
            module.Title = topics.Count > 1
                ? $"{topics[0]} & {topics[1]}"
                : topics[0];
        }
        else
        {
            module.Title = $"Module {module.ModuleNumber}";
        }
    }

    private string NormalizeTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic)) return "General";

        // Basic normalization: remove punctuation, lower case, trim
        // "Introduction to C#" -> "introduction to c#"
        var cleaned = topic.Trim().ToLowerInvariant();

        // Remove common prefixes like "Chapter 1:", "Unit 2:"
        cleaned = Regex.Replace(cleaned, @"^(chapter|unit|lesson|session)\s*\d+\s*[:.-]\s*", "");

        return cleaned;
    }

    private bool IsTopicRelated(HashSet<string> currentTopics, string newTopic)
    {
        // Simple heuristic: do they share significant words?
        var newWords = newTopic.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3).ToHashSet();

        foreach (var existing in currentTopics)
        {
            var existingWords = existing.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (existingWords.Any(w => w.Length > 3 && newWords.Contains(w)))
            {
                return true;
            }
        }
        return false;
    }
}