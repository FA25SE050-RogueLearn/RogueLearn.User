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
            .Select(s => CleanTitle(s.Topic)) // Use simplified title cleaner
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        module.KeyTopics = topics;

        // Smart Title Strategy:
        if (topics.Count > 0)
        {
            // 1. Pick the first topic as the primary candidate
            var bestTitle = topics[0];

            // 2. If it's short (< 30 chars) AND we have a second distinct topic, consider combining ONLY if short
            if (topics.Count > 1 && bestTitle.Length < 25 && topics[1].Length < 25)
            {
                // Check if they are redundant (e.g. "Java Basics" & "Java Syntax")
                if (!IsRedundant(bestTitle, topics[1]))
                {
                    bestTitle = $"{bestTitle} & {topics[1]}";
                }
            }

            // 3. Final safety truncate to ensure DB compliance (though CleanTitle helps)
            if (bestTitle.Length > 100)
            {
                bestTitle = bestTitle.Substring(0, 97) + "...";
            }

            module.Title = bestTitle;
        }
        else
        {
            module.Title = $"Module {module.ModuleNumber}";
        }
    }

    private string NormalizeTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic)) return "General";
        var cleaned = topic.Trim().ToLowerInvariant();
        cleaned = Regex.Replace(cleaned, @"^(chapter|unit|lesson|session)\s*\d+\s*[:.-]\s*", "");
        return cleaned;
    }

    /// <summary>
    /// Cleans academic titles to be short and readable.
    /// Removes technical specs, versions, and redundant prefixes.
    /// </summary>
    private string CleanTitle(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic)) return string.Empty;

        // 1. Split by common separators (colon, dash, pipe, plus) and take the first meaningful part
        var parts = Regex.Split(topic, @"\s*[:|\-–+]\s*");
        var title = parts[0].Trim();

        // 2. Remove common noise like "Introduction to..." if it makes it too long, but keep it if short
        // actually "Introduction to Java" is fine. "Introduction to Java Web Application Development..." is not.

        // 3. Remove version numbers (e.g., "JDK 1.8", "Tomcat 10")
        title = Regex.Replace(title, @"\s+v?\d+(\.\d+)*.*", "");

        // 4. Remove parentheticals
        title = Regex.Replace(title, @"\s*\(.*?\)", "");

        // 5. Hard truncate if the *first part* itself was huge
        if (title.Length > 60)
        {
            var words = title.Split(' ');
            if (words.Length > 6)
            {
                title = string.Join(" ", words.Take(6));
            }
        }

        return title.Trim();
    }

    private bool IsRedundant(string title1, string title2)
    {
        var t1 = title1.ToLowerInvariant();
        var t2 = title2.ToLowerInvariant();
        // If one contains the other, or they share > 50% words
        if (t1.Contains(t2) || t2.Contains(t1)) return true;

        var w1 = t1.Split(' ').ToHashSet();
        var w2 = t2.Split(' ').ToHashSet();
        var common = w1.Intersect(w2).Count();

        return common >= Math.Min(w1.Count, w2.Count) / 2.0;
    }

    private bool IsTopicRelated(HashSet<string> currentTopics, string newTopic)
    {
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