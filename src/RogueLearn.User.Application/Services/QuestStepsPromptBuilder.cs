// RogueLearn.User/src/RogueLearn.User.Application/Services/QuestStepsPromptBuilder.cs
using System.Text;
using System.Text.Json;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Builds LLM-friendly prompts for generating quest steps based on syllabus content and user context.
/// IMPORTANT: Uses StringBuilder for efficient string concatenation in large prompts.
/// </summary>
public class QuestStepsPromptBuilder
{
    /// <summary>
    /// Generates a comprehensive, structured prompt for the LLM to create quest steps.
    /// This version is designed to produce a single 'quest_step' that acts as a weekly module,
    /// containing an array of different learning activities.
    /// </summary>
    public string BuildPrompt(
        string syllabusJson,
        string userContext,
        List<Skill> relevantSkills,
        string subjectName,
        string courseDescription)
    {
        var prompt = new StringBuilder();

        // System-level instruction and main goal
        prompt.AppendLine("You are an expert curriculum designer creating a gamified, week-long learning module for a university subject.");
        prompt.AppendLine($"Your task is to analyze the syllabus for the subject '{subjectName}' and generate a structured JSON object representing a single weekly module (a Quest Step).");
        prompt.AppendLine("This module MUST contain a variety of learning activities (Reading, KnowledgeCheck, Quiz, Coding).");
        prompt.AppendLine();
        prompt.AppendLine("---");

        // High-level context
        prompt.AppendLine("## 1. Context");
        prompt.AppendLine($"**Subject:** {subjectName}");
        prompt.AppendLine($"**Course Description:** {courseDescription}");
        prompt.AppendLine("**Student Context:**");
        prompt.AppendLine(userContext);
        prompt.AppendLine();
        prompt.AppendLine("---");

        // Pre-approved skills (the allowlist)
        prompt.AppendLine("## 2. Pre-Approved Skills");
        prompt.AppendLine("For each activity you generate, you MUST assign a `skillId` by selecting the most relevant skill from this pre-approved list. Do NOT invent IDs.");
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
            prompt.AppendLine("**WARNING:** No skills provided. Skill mapping is required.");
        }
        prompt.AppendLine();
        prompt.AppendLine("---");

        // Syllabus Content (Source of Truth)
        prompt.AppendLine("## 3. Source of Truth: Syllabus Content");
        prompt.AppendLine("Base ALL generated activities on the topics, readings, and schedule provided in this syllabus JSON. Adhere strictly to the content.");
        prompt.AppendLine("```json");
        prompt.AppendLine(syllabusJson);
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("---");

        // The core instruction: Output Schema
        prompt.AppendLine("## 4. Your Task: Generate the JSON for the Weekly Module");
        prompt.AppendLine("Your entire output MUST be a single, valid JSON object that conforms to the schema below. This object represents ONE `quest_step` (a weekly module).");
        prompt.AppendLine();
        prompt.AppendLine("**Output Schema:**");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"activities\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"activityId\": \"<generate a new UUID>\",");
        prompt.AppendLine("      \"type\": \"Reading | KnowledgeCheck | Quiz | Coding\",");
        prompt.AppendLine("      \"payload\": {");
        prompt.AppendLine("        \"skillId\": \"<UUID from pre-approved list>\",");
        prompt.AppendLine("        \"experiencePoints\": <number>,");
        prompt.AppendLine("        \"...payload specific fields...\"");
        prompt.AppendLine("      }");
        prompt.AppendLine("    }");
        prompt.AppendLine("  ]");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("### Activity Payload Schemas:");
        prompt.AppendLine();
        prompt.AppendLine("**A. `Reading` Payload:**");
        prompt.AppendLine("```");
        prompt.AppendLine("\"payload\": {");
        prompt.AppendLine("  \"skillId\": \"...\", \"experiencePoints\": 15,");
        prompt.AppendLine("  \"articleTitle\": \"<MUST match session topic from syllabus>\",");
        prompt.AppendLine("  \"summary\": \"<Brief summary>\",");
        prompt.AppendLine("  \"url\": \"<CRITICAL: Use the 'suggestedUrl' field from the corresponding syllabus session. If missing, use empty string>\"");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("⚠️ **CRITICAL READING URL RULE:**");
        prompt.AppendLine("- For EACH Reading activity, you MUST use the `suggestedUrl` from the corresponding session in the syllabus.");
        prompt.AppendLine("- Example: If syllabus session 1 has `\"suggestedUrl\": \"https://www.geeksforgeeks.org/...\",` then use that EXACT URL.");
        prompt.AppendLine("- Do NOT generate your own URLs or search the web.");
        prompt.AppendLine("- If a session has no `suggestedUrl` or it's empty, set `\"url\": \"\"` (empty string).");
        prompt.AppendLine();
        prompt.AppendLine("**C. `Quiz` Payload (10+ questions for a comprehensive weekly quiz):**");
        prompt.AppendLine("```json");
        prompt.AppendLine("\"payload\": {");
        prompt.AppendLine("  \"skillId\": \"...\", \"experiencePoints\": 50,");
        prompt.AppendLine("  \"questions\": [ { \"question\": \"...\", \"options\": [\"...\"], \"correctAnswer\": \"...\", \"explanation\": \"...\" } ]");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("**D. `Coding` Payload:**");
        prompt.AppendLine("```json");
        prompt.AppendLine("\"payload\": {");
        prompt.AppendLine("  \"skillId\": \"...\", \"experiencePoints\": 100,");
        prompt.AppendLine("  \"language\": \"<e.g., Kotlin, C#, Java>\",");
        prompt.AppendLine("  \"difficulty\": \"<Beginner|Intermediate|Advanced>\",");
        prompt.AppendLine("  \"topic\": \"<Specific coding topic from the syllabus>\"");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("### CRITICAL RULES:");
        prompt.AppendLine("1.  **Activity Distribution:** Generate a rich set of activities for a week's worth of learning. Include multiple `Reading` activities based on the syllabus, several `KnowledgeCheck`s, at least one comprehensive `Quiz` (10+ questions), and at least one `Coding` challenge.");
        prompt.AppendLine("2.  **UUIDs:** Generate a new, unique UUID for every `activityId`.");
        prompt.AppendLine("3.  **Skill ID Enforcement:** Every activity's payload MUST contain a valid `skillId` from the pre-approved list.");
        prompt.AppendLine("4.  **JSON ONLY:** Your entire output must be a single, raw JSON object. Do not include commentary or markdown fences.");
        prompt.AppendLine();
        prompt.AppendLine("Generate the JSON object for this weekly learning module now.");

        return prompt.ToString();
    }
}