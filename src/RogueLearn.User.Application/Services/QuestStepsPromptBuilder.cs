// src/RogueLearn.User.Application/Services/QuestStepsPromptBuilder.cs
using System.Text;
using System.Text.Json;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Builds LLM-friendly prompts for generating quest steps using a resource-pooling strategy.
/// Aggregates topics and resources for a week to create cohesive activities.
/// </summary>
public class QuestStepsPromptBuilder
{
    public string BuildPrompt(
        WeekContext weekContext,
        string userContext,
        List<Skill> relevantSkills,
        string subjectName,
        string courseDescription,
        string? errorHint = null)
    {
        var prompt = new StringBuilder();

        // System-level instruction
        prompt.AppendLine("You are an expert curriculum designer creating a gamified weekly learning module.");
        prompt.AppendLine($"**Objective:** Create a structured learning path for Week {weekContext.WeekNumber} of {weekContext.TotalWeeks} for the subject '{subjectName}'.");
        prompt.AppendLine();
        prompt.AppendLine("---");

        // 1. Context
        prompt.AppendLine("## 1. Learning Context");
        prompt.AppendLine($"**Subject:** {subjectName}");
        prompt.AppendLine($"**Course Description:** {courseDescription}");
        prompt.AppendLine("**Student Profile:**");
        prompt.AppendLine(userContext);
        prompt.AppendLine();

        // 2. Week Objectives (The "What")
        prompt.AppendLine("## 2. Week Learning Objectives (Topics to Cover)");
        prompt.AppendLine("You must cover the following topics. Group related topics into logical activities.");
        foreach (var topic in weekContext.TopicsToCover)
        {
            prompt.AppendLine($"- {topic}");
        }
        prompt.AppendLine();

        // 3. Approved Resources (The "How")
        prompt.AppendLine("## 3. Approved Resource Pool");
        prompt.AppendLine("Assign these specific URLs to your Reading activities. Do NOT invent URLs.");
        if (weekContext.AvailableResources.Any())
        {
            for (int i = 0; i < weekContext.AvailableResources.Count; i++)
            {
                var res = weekContext.AvailableResources[i];
                prompt.AppendLine($"[{i + 1}] {res.Url} (Context: {res.SourceContext})");
            }
        }
        else
        {
            prompt.AppendLine("**Note:** No external URLs are available for this week. Set `url` to \"\" for reading activities.");
        }
        prompt.AppendLine();

        // 4. Skills
        prompt.AppendLine("## 4. Skill Mapping");
        prompt.AppendLine("Assign a `skillId` from this list to every activity based on relevance:");
        if (relevantSkills.Any())
        {
            var skillsJson = JsonSerializer.Serialize(
                relevantSkills.Select(s => new { skillId = s.Id, skillName = s.Name }),
                new JsonSerializerOptions { WriteIndented = true }
            );
            prompt.AppendLine("```json");
            prompt.AppendLine(skillsJson);
            prompt.AppendLine("```");
        }
        else
        {
            prompt.AppendLine("WARNING: No skills provided. Use placeholder GUIDs if necessary, but prefer real mapping.");
        }
        prompt.AppendLine();

        // 5. Rules & Schema
        prompt.AppendLine("## 5. Construction Rules");
        prompt.AppendLine("1. **Group Topics:** Instead of 1 activity per topic, combine 2-3 related topics into a single comprehensive `Reading` activity.");
        prompt.AppendLine("2. **Distribute Resources:** Use the Approved Resource Pool. If you have 3 topics and 1 good URL that covers them (e.g., a chapter link), use that URL for the grouped activity.");
        prompt.AppendLine("3. **Activity Count:** Generate **6 to 9** activities total.");
        prompt.AppendLine("   - 2-4 `Reading` activities (Grouped topics)");
        prompt.AppendLine("   - 1-2 `KnowledgeCheck` (Formative assessment)");
        prompt.AppendLine("   - 1 `Quiz` (Summative assessment, 10-15 questions)");
        prompt.AppendLine("4. **No Coding:** Do not generate coding activities.");
        prompt.AppendLine("5. **JSON Only:** Output valid JSON matching the schema below. No markdown.");
        prompt.AppendLine();
        prompt.AppendLine("**Output Schema:**");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"activities\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"activityId\": \"<UUID>\",");
        prompt.AppendLine("      \"type\": \"Reading | KnowledgeCheck | Quiz\",");
        prompt.AppendLine("      \"payload\": {");
        prompt.AppendLine("         // For Reading:");
        prompt.AppendLine("         \"skillId\": \"<UUID>\", \"experiencePoints\": 15, \"articleTitle\": \"<Title>\", \"summary\": \"<Text>\", \"url\": \"<From Resource Pool>\"");
        prompt.AppendLine("         // For KnowledgeCheck:");
        prompt.AppendLine("         \"skillId\": \"<UUID>\", \"experiencePoints\": 35, \"topic\": \"<Text>\", \"questions\": [ { \"question\": \"...\", \"options\": [...], \"correctAnswer\": \"...\", \"explanation\": \"...\" } ]");
        prompt.AppendLine("         // For Quiz:");
        prompt.AppendLine("         \"skillId\": \"<UUID>\", \"experiencePoints\": 50, \"questions\": [ ...10-15 questions... ]");
        prompt.AppendLine("      }");
        prompt.AppendLine("    }");
        prompt.AppendLine("  ]");
        prompt.AppendLine("}");
        prompt.AppendLine("```");

        if (!string.IsNullOrWhiteSpace(errorHint))
        {
            prompt.AppendLine();
            prompt.AppendLine("## CORRECTION REQUIRED");
            prompt.AppendLine("Your previous output was invalid. Please fix the following error:");
            prompt.AppendLine(errorHint);
        }

        return prompt.ToString();
    }
}