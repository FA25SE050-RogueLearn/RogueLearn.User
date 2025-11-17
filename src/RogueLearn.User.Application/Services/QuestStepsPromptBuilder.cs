// RogueLearn.User/src/RogueLearn.User.Application/Services/QuestStepsPromptBuilder.cs
using System.Text;
using System.Text.Json;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Builds LLM-friendly prompts for generating quest steps based on syllabus content and user context.
/// </summary>
public class QuestStepsPromptBuilder
{
    /// <summary>
    /// Generates a comprehensive, structured prompt for the LLM to create quest steps.
    /// </summary>
    /// <param name="syllabusJson">The syllabus content in JSON format.</param>
    /// <param name="userContext">User performance and context information.</param>
    /// <param name="relevantSkills">Pre-approved skills that can be used for quest steps.</param>
    /// <param name="subjectName">The name of the subject (e.g., "Mobile Programming with Android")</param>
    /// <param name="courseDescription">Brief description of the course content</param>
    /// <returns>A structured prompt optimized for LLM consumption.</returns>
    public string BuildPrompt(
        string syllabusJson,
        string userContext,
        List<Skill> relevantSkills,
        string subjectName,
        string courseDescription)
    {
        var prompt = new StringBuilder();

        // ===== CRITICAL HEADER: SUBJECT FOCUS =====
        prompt.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
        prompt.AppendLine("║          QUEST STEP GENERATION - SUBJECT VALIDATION           ║");
        prompt.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
        prompt.AppendLine();
        prompt.AppendLine($"## SUBJECT: {subjectName}");
        prompt.AppendLine();
        prompt.AppendLine("⚠️  CRITICAL REQUIREMENT: STAY ON TOPIC ⚠️");
        prompt.AppendLine();
        prompt.AppendLine($"You are creating quest steps EXCLUSIVELY for: **{subjectName}**");
        prompt.AppendLine($"Course Focus: {courseDescription}");
        prompt.AppendLine();
        prompt.AppendLine("### ABSOLUTE RULES (VIOLATION = STEP REJECTION):");
        prompt.AppendLine();
        prompt.AppendLine($"1. ✓ ONLY create content about **{subjectName}**");
        prompt.AppendLine("2. ✗ NEVER generate content about different technologies or subjects");
        prompt.AppendLine("3. ✓ Every Reading step MUST use topics from the provided syllabus SessionSchedule");
        prompt.AppendLine("4. ✓ Reading step titles MUST align with session Topics from the syllabus");
        prompt.AppendLine("5. ✓ Use URLs from syllabus (Readings or SuggestedUrl) when available");
        prompt.AppendLine($"6. ✓ Stay within the subject domain: {subjectName}");
        prompt.AppendLine();
        prompt.AppendLine("### VALIDATION CHECKPOINT:");
        prompt.AppendLine($"Before generating EACH step, ask: \"Is this about {subjectName} from the syllabus?\"");
        prompt.AppendLine("If the answer is NO → DO NOT generate that step");
        prompt.AppendLine();
        prompt.AppendLine("### EXAMPLES OF CORRECT vs INCORRECT:");
        prompt.AppendLine();

        // Add specific examples based on subject keywords
        if (subjectName.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
            subjectName.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
        {
            prompt.AppendLine("✓ CORRECT: \"Understanding Android Activities and Lifecycle\"");
            prompt.AppendLine("✓ CORRECT: \"Building UI with ConstraintLayout in Android\"");
            prompt.AppendLine("✗ WRONG: \"Introduction to ASP.NET Core MVC\" (Wrong technology!)");
            prompt.AppendLine("✗ WRONG: \"React Component Lifecycle\" (Wrong framework!)");
        }
        else if (subjectName.Contains("ASP.NET", StringComparison.OrdinalIgnoreCase) ||
                 subjectName.Contains("C#", StringComparison.OrdinalIgnoreCase))
        {
            prompt.AppendLine("✓ CORRECT: \"Introduction to ASP.NET Core MVC\"");
            prompt.AppendLine("✓ CORRECT: \"Dependency Injection in ASP.NET Core\"");
            prompt.AppendLine("✗ WRONG: \"Android Activities and Intents\" (Wrong platform!)");
            prompt.AppendLine("✗ WRONG: \"iOS View Controllers\" (Wrong platform!)");
        }
        else
        {
            prompt.AppendLine($"✓ CORRECT: Topics that directly relate to {subjectName}");
            prompt.AppendLine($"✗ WRONG: Topics about technologies NOT mentioned in {subjectName}");
        }

        prompt.AppendLine();
        prompt.AppendLine("---");
        prompt.AppendLine();

        // ===== ORIGINAL SECTIONS =====
        prompt.AppendLine("# Quest Step Generation Task");
        prompt.AppendLine();
        prompt.AppendLine("You are an expert educational content designer creating gamified learning experiences.");
        prompt.AppendLine($"Your task is to analyze the {subjectName} syllabus and generate engaging, progressive quest steps.");
        prompt.AppendLine();

        prompt.AppendLine("---");
        prompt.AppendLine();
        prompt.AppendLine("## Student Context");
        prompt.AppendLine();
        prompt.AppendLine(userContext);
        prompt.AppendLine();

        prompt.AppendLine("---");
        prompt.AppendLine();
        prompt.AppendLine("## Pre-Approved Skills");
        prompt.AppendLine();
        prompt.AppendLine("The following skills are validated and must be used exclusively for quest steps.");
        prompt.AppendLine("Each quest step MUST reference exactly ONE skill from this list using its skillId.");
        prompt.AppendLine();

        if (relevantSkills.Any())
        {
            prompt.AppendLine("| Skill ID | Skill Name | Description |");
            prompt.AppendLine("|----------|------------|-------------|");
            foreach (var skill in relevantSkills.OrderBy(s => s.Name))
            {
                var description = !string.IsNullOrWhiteSpace(skill.Description)
                    ? skill.Description.Replace("|", "\\|").Replace("\n", " ")
                    : "N/A";
                prompt.AppendLine($"| `{skill.Id}` | {skill.Name} | {description} |");
            }
            prompt.AppendLine();

            prompt.AppendLine("**Skills in JSON format:**");
            prompt.AppendLine("```json");
            var skillsJson = JsonSerializer.Serialize(
                relevantSkills.Select(s => new { skillId = s.Id, skillName = s.Name, description = s.Description }),
                new JsonSerializerOptions { WriteIndented = true }
            );
            prompt.AppendLine(skillsJson);
            prompt.AppendLine("```");
            prompt.AppendLine();
        }
        else
        {
            prompt.AppendLine("**WARNING:** No skills provided. This should not happen in production.");
            prompt.AppendLine();
        }

        prompt.AppendLine("---");
        prompt.AppendLine();
        prompt.AppendLine($"## Syllabus Content for {subjectName}");
        prompt.AppendLine();
        prompt.AppendLine($"⚠️  THIS IS YOUR ONLY SOURCE OF TRUTH FOR {subjectName} CONTENT ⚠️");
        prompt.AppendLine();
        prompt.AppendLine("Use this syllabus content as the foundation for creating quest steps:");
        prompt.AppendLine("- Each session represents a week of learning");
        prompt.AppendLine("- The 'Topic' field tells you exactly what to cover");
        prompt.AppendLine("- The 'SuggestedUrl' field (if present) MUST be used for Reading steps");
        prompt.AppendLine("- The 'Readings' array may contain additional reference materials");
        prompt.AppendLine();
        prompt.AppendLine("```json");
        prompt.AppendLine(syllabusJson);
        prompt.AppendLine("```");
        prompt.AppendLine();

        prompt.AppendLine("---");
        prompt.AppendLine();
        prompt.AppendLine("## Generation Instructions");
        prompt.AppendLine();
        prompt.AppendLine("### Requirements");
        prompt.AppendLine();
        prompt.AppendLine("1. **Quantity:** Generate 10 steps, each step represents 1 week");
        prompt.AppendLine("2. **Progression:** Steps should follow a logical learning progression (easy to hard)");
        prompt.AppendLine("3. **Variety:** Use diverse step types to maintain engagement");
        prompt.AppendLine("4. **Personalization:** Consider the student's context, level, and class information");
        prompt.AppendLine("5. **Skill Mapping:** Each step must target exactly ONE skill from the pre-approved list");
        prompt.AppendLine($"6. **Topic Alignment:** Every step MUST be about {subjectName} from the syllabus");
        prompt.AppendLine();

        prompt.AppendLine("### Step Types and Content Schemas");
        prompt.AppendLine();
        prompt.AppendLine("Each quest step must conform to one of these exact types:");
        prompt.AppendLine();

        prompt.AppendLine("#### 1. Reading");
        prompt.AppendLine("Used for: Foundational knowledge, articles, documentation");
        prompt.AppendLine();
        prompt.AppendLine("**CRITICAL for Reading steps:**");
        prompt.AppendLine("- articleTitle MUST come from the session's 'Topic' field in the syllabus");
        prompt.AppendLine("- summary MUST reference the 'Readings' from that session");
        prompt.AppendLine("- url MUST use the 'SuggestedUrl' if available, or a URL from 'Readings' if it exists");
        prompt.AppendLine("- If no URL is available, use an empty string \"\"");
        prompt.AppendLine();
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 1,");
        prompt.AppendLine("  \"title\": \"Introduction to Topic\",");
        prompt.AppendLine("  \"description\": \"Read about the fundamental concepts\",");
        prompt.AppendLine("  \"stepType\": \"Reading\",");
        prompt.AppendLine("  \"experiencePoints\": 15,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine("    \"articleTitle\": \"<MUST match session Topic from syllabus>\",");
        prompt.AppendLine("    \"summary\": \"<reference session Readings from syllabus>\",");
        prompt.AppendLine("    \"url\": \"<use SuggestedUrl or URL from Readings or empty string>\"");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        prompt.AppendLine("#### 2. Interactive");
        prompt.AppendLine("Used for: Hands-on exploration, experimentation, guided practice");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 2,");
        prompt.AppendLine("  \"title\": \"Practice the Concept\",");
        prompt.AppendLine("  \"description\": \"Apply what you've learned through interactive tasks\",");
        prompt.AppendLine("  \"stepType\": \"Interactive\",");
        prompt.AppendLine("  \"experiencePoints\": 25,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine("    \"challenge\": \"Description of the interactive challenge\",");
        prompt.AppendLine("    \"questions\": [");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"task\": \"What action should you take?\",");
        prompt.AppendLine("        \"options\": [\"Option 1\", \"Option 2\", \"Option 3\", \"Option 4\"],");
        prompt.AppendLine("        \"answer\": \"Option 2\"");
        prompt.AppendLine("      }");
        prompt.AppendLine("    ]");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        prompt.AppendLine("#### 3. Quiz");
        prompt.AppendLine("Used for: Knowledge assessment, concept verification");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 3,");
        prompt.AppendLine("  \"title\": \"Test Your Knowledge\",");
        prompt.AppendLine("  \"description\": \"Verify your understanding with a quiz\",");
        prompt.AppendLine("  \"stepType\": \"Quiz\",");
        prompt.AppendLine("  \"experiencePoints\": 20,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine("    \"questions\": [");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"question\": \"What is the correct definition?\",");
        prompt.AppendLine("        \"options\": [\"Option 1\", \"Option 2\", \"Option 3\", \"Option 4\"],");
        prompt.AppendLine("        \"correctAnswer\": \"Option 1\",");
        prompt.AppendLine("        \"explanation\": \"Why this answer is correct\"");
        prompt.AppendLine("      }");
        prompt.AppendLine("    ]");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        prompt.AppendLine("#### 4. Coding");
        prompt.AppendLine("Used for: Generating a request for a practical programming problem");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 4,");
        prompt.AppendLine("  \"title\": \"Coding Challenge: [Topic]\",");
        prompt.AppendLine("  \"description\": \"Implement a solution to practice the concept\",");
        prompt.AppendLine("  \"stepType\": \"Coding\",");
        prompt.AppendLine("  \"experiencePoints\": 40,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine("    \"language\": \"[appropriate language for this subject]\",");
        prompt.AppendLine("    \"difficulty\": \"Beginner|Intermediate|Advanced\",");
        prompt.AppendLine($"    \"topic\": \"[topic from {subjectName} syllabus]\"");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        prompt.AppendLine("### Critical Rules");
        prompt.AppendLine();
        prompt.AppendLine("- **MUST** use only skillIds from the Pre-Approved Skills list");
        prompt.AppendLine("- For any 'Reading' step, you **MUST** use the `SuggestedUrl` provided for that session in the syllabus content. Do NOT use placeholder URLs or URLs from other technologies.");
        prompt.AppendLine("- **MUST** use exact stepType values: `Reading`, `Interactive`, `Quiz`, `Coding`");
        prompt.AppendLine("- **MUST** include all required fields for each content schema");
        prompt.AppendLine("- **MUST** set experiencePoints between 10-50 based on difficulty");
        prompt.AppendLine("- **MUST** return ONLY valid JSON array, no markdown formatting");
        prompt.AppendLine("- **NEVER** invent new skillIds or use IDs not in the approved list");
        prompt.AppendLine("- **NEVER** create custom stepTypes");
        prompt.AppendLine("- **NEVER** omit the skillId field from content objects");
        prompt.AppendLine($"- **NEVER** generate content about technologies not mentioned in {subjectName}");
        prompt.AppendLine("- **NEVER** reference documentation or resources from different technology stacks");
        prompt.AppendLine();

        prompt.AppendLine("### Best Practices");
        prompt.AppendLine();
        prompt.AppendLine("- Start with foundational Reading or Interactive steps");
        prompt.AppendLine("- Include Quiz steps after introducing new concepts");
        prompt.AppendLine("- Use Coding steps for practical application (when relevant)");
        prompt.AppendLine("- Vary difficulty progressively (easy → medium → hard)");
        prompt.AppendLine("- Keep titles concise and engaging (3-6 words)");
        prompt.AppendLine("- Make descriptions clear and motivating (10-20 words)");
        prompt.AppendLine($"- Ensure every step clearly relates to {subjectName}");
        prompt.AppendLine();

        prompt.AppendLine("---");
        prompt.AppendLine();
        prompt.AppendLine("## CONCRETE EXAMPLE");
        prompt.AppendLine();
        prompt.AppendLine("Given this syllabus session:");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"SessionNumber\": 7,");
        prompt.AppendLine("  \"Topic\": \"Layout manager (LinearLayout, ConstraintLayout)\",");
        prompt.AppendLine("  \"Readings\": [\"eBook: Lesson 1, part 1.2\"],");
        prompt.AppendLine("  \"SuggestedUrl\": \"https://developer.android.com/guide/topics/ui/declaring-layout\",");
        prompt.AppendLine("  \"Activities\": [\"eBook, slides\"]");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("✓ CORRECT Generated Step:");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 7,");
        prompt.AppendLine("  \"title\": \"Understanding Android Layout Managers\",");
        prompt.AppendLine("  \"description\": \"Learn about LinearLayout and ConstraintLayout for Android UIs\",");
        prompt.AppendLine("  \"stepType\": \"Reading\",");
        prompt.AppendLine("  \"experiencePoints\": 15,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"5c958045-b8e2-40ab-bb9a-809ad95c94fd\",");
        prompt.AppendLine("    \"articleTitle\": \"Layout manager (LinearLayout, ConstraintLayout)\",");
        prompt.AppendLine("    \"summary\": \"eBook: Lesson 1, part 1.2\",");
        prompt.AppendLine("    \"url\": \"https://developer.android.com/guide/topics/ui/declaring-layout\"");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("✗ WRONG (DO NOT DO THIS):");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"title\": \"Introduction to ASP.NET Core MVC\",  // WRONG - Different technology!");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"articleTitle\": \"ASP.NET Core Overview\",  // WRONG - Not from syllabus!");
        prompt.AppendLine("    \"url\": \"https://docs.microsoft.com/aspnet\"  // WRONG - Wrong documentation!");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        prompt.AppendLine("---");
        prompt.AppendLine();
        prompt.AppendLine("## Output Format");
        prompt.AppendLine();
        prompt.AppendLine("Return your response as a JSON array following this structure:");
        prompt.AppendLine();
        prompt.AppendLine("```json");
        prompt.AppendLine("[");
        prompt.AppendLine("  {");
        prompt.AppendLine("    \"stepNumber\": 1,");
        prompt.AppendLine("    \"title\": \"...\",");
        prompt.AppendLine("    \"description\": \"...\",");
        prompt.AppendLine("    \"stepType\": \"Reading|Interactive|Quiz|Coding\",");
        prompt.AppendLine("    \"experiencePoints\": 10-50,");
        prompt.AppendLine("    \"content\": { /* schema based on stepType */ }");
        prompt.AppendLine("  }");
        prompt.AppendLine("]");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("---");
        prompt.AppendLine();
        prompt.AppendLine("## FINAL VALIDATION CHECKLIST");
        prompt.AppendLine();
        prompt.AppendLine("Before submitting your output, verify EVERY step against this checklist:");
        prompt.AppendLine();
        prompt.AppendLine($"☑ Every step is about {subjectName} (not other technologies)");
        prompt.AppendLine("☑ Every Reading step uses a Topic from the syllabus SessionSchedule");
        prompt.AppendLine("☑ Every Reading step uses SuggestedUrl or a URL from Readings");
        prompt.AppendLine("☑ No steps mention technologies outside the syllabus");
        prompt.AppendLine("☑ All skillId values are from the provided Pre-Approved Skills list");
        prompt.AppendLine("☑ All stepType values are exactly: Reading, Interactive, Quiz, or Coding");
        prompt.AppendLine("☑ Output is a valid JSON array only (no markdown fences, no explanations)");
        prompt.AppendLine();
        prompt.AppendLine("**IMPORTANT:** Return ONLY the JSON array. Do not include markdown code fences, explanations, or any other text.");
        prompt.AppendLine();
        prompt.AppendLine($"Generate the quest steps for {subjectName} now:");

        return prompt.ToString();
    }
}