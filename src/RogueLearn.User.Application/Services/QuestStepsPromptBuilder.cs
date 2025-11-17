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
    /// </summary>
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

        // ===== NEW: STEP TYPE DISTRIBUTION ENFORCEMENT (MOVED TO TOP) =====
        prompt.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
        prompt.AppendLine("║          🚨 CRITICAL: STEP TYPE DISTRIBUTION 🚨               ║");
        prompt.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
        prompt.AppendLine();
        prompt.AppendLine("**MANDATORY REQUIREMENT: You MUST generate exactly 10 steps with this EXACT distribution:**");
        prompt.AppendLine();
        prompt.AppendLine("┌─────────────┬──────────┬────────────────────────────────────┐");
        prompt.AppendLine("│ Step Type   │ Quantity │ Purpose                            │");
        prompt.AppendLine("├─────────────┼──────────┼────────────────────────────────────┤");
        prompt.AppendLine("│ Reading     │ 4 steps  │ Foundational knowledge             │");
        prompt.AppendLine("│ Interactive │ 2 steps  │ Hands-on practice                  │");
        prompt.AppendLine("│ Quiz        │ 2 steps  │ Knowledge assessment               │");
        prompt.AppendLine("│ Coding      │ 2 steps  │ Practical programming (MANDATORY!) │");
        prompt.AppendLine("└─────────────┴──────────┴────────────────────────────────────┘");
        prompt.AppendLine();
        prompt.AppendLine("⛔ VALIDATION CHECK: Before submitting, count your step types!");
        prompt.AppendLine("   - If you have 7+ Reading steps → REJECTED");
        prompt.AppendLine("   - If you have 0 Coding steps → REJECTED");
        prompt.AppendLine("   - If total ≠ 10 steps → REJECTED");
        prompt.AppendLine();
        prompt.AppendLine("✅ CORRECT distribution: 4 Reading + 2 Interactive + 2 Quiz + 2 Coding = 10 steps");
        prompt.AppendLine();
        prompt.AppendLine("---");
        prompt.AppendLine();

        prompt.AppendLine("### ABSOLUTE RULES (VIOLATION = STEP REJECTION):");
        prompt.AppendLine();
        prompt.AppendLine($"1. ✓ ONLY create content about **{subjectName}**");
        prompt.AppendLine("2. ✗ NEVER generate content about different technologies or subjects");
        prompt.AppendLine("3. ✓ Every Reading step MUST use topics from the provided syllabus SessionSchedule");
        prompt.AppendLine("4. ✓ Reading step titles MUST align with session Topics from the syllabus");
        prompt.AppendLine("5. ✓ Use the 'suggestedUrl' from the syllabus for Reading steps. If it's an empty string, the 'url' in your output must also be an empty string.");
        prompt.AppendLine($"6. ✓ Stay within the subject domain: {subjectName}");
        prompt.AppendLine("7. ✓ MUST include exactly 2 Coding steps (NON-NEGOTIABLE!)");
        prompt.AppendLine();

        prompt.AppendLine("---");
        prompt.AppendLine();
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

            // FIX: Use AppendLine for each JSON line to avoid code fence issues
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
        prompt.AppendLine("- The 'suggestedUrl' field is pre-validated and MUST be used for the 'url' in Reading steps.");
        prompt.AppendLine("- The 'Readings' array may contain additional reference materials.");
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
        prompt.AppendLine("1. **Quantity:** Generate exactly 10 steps (4 Reading + 2 Interactive + 2 Quiz + 2 Coding)");
        prompt.AppendLine("2. **Progression:** Steps should follow a logical learning progression (easy to hard)");
        prompt.AppendLine("3. **Variety:** MANDATORY mix: you CANNOT skip Coding steps!");
        prompt.AppendLine("4. **Personalization:** Consider the student's context, level, and class information");
        prompt.AppendLine("5. **Skill Mapping:** Each step must target exactly ONE skill from the pre-approved list");
        prompt.AppendLine($"6. **Topic Alignment:** Every step MUST be about {subjectName} from the syllabus");
        prompt.AppendLine("7. **URL Usage:** For Reading steps, COPY the suggestedUrl directly. If empty, check Readings for URLs.");
        prompt.AppendLine();

        prompt.AppendLine("### Step Types and Content Schemas");
        prompt.AppendLine();

        // ===== SECTION 1: READING SCHEMA =====
        prompt.AppendLine("#### 1. Reading (EXACTLY 4 steps required)");
        prompt.AppendLine("Used for: Foundational knowledge, articles, documentation");
        prompt.AppendLine();
        prompt.AppendLine("**⚠️  CRITICAL: URL EXTRACTION PROCESS ⚠️**");
        prompt.AppendLine();
        prompt.AppendLine("For EVERY Reading step:");
        prompt.AppendLine("1. Match the reading topic to a session in SessionSchedule");
        prompt.AppendLine("2. Copy the 'suggestedUrl' field value EXACTLY to 'url'");
        prompt.AppendLine("3. If 'suggestedUrl' is empty/null, check 'Readings' array for URLs");
        prompt.AppendLine("4. If still no URL found, use empty string \"\"");
        prompt.AppendLine();
        prompt.AppendLine("**Schema:**");

        // FIX: Split JSON schema into lines using AppendLine instead of inline code fence
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 1,");
        prompt.AppendLine("  \"title\": \"Introduction to Topic\",");
        prompt.AppendLine("  \"description\": \"Read about the fundamental concepts\",");
        prompt.AppendLine("  \"stepType\": \"Reading\",");
        prompt.AppendLine("  \"experiencePoints\": 15,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine("    \"articleTitle\": \"<MUST match session Topic>\",");
        prompt.AppendLine("    \"summary\": \"<reference Readings from syllabus>\",");
        prompt.AppendLine("    \"url\": \"<COPY suggestedUrl here>\"");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        // ===== SECTION 2: INTERACTIVE SCHEMA =====
        prompt.AppendLine("#### 2. Interactive (EXACTLY 2 steps required)");
        prompt.AppendLine("Used for: Hands-on exploration, experimentation, guided practice");

        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 2,");
        prompt.AppendLine("  \"title\": \"Practice the Concept\",");
        prompt.AppendLine("  \"description\": \"Apply what you've learned\",");
        prompt.AppendLine("  \"stepType\": \"Interactive\",");
        prompt.AppendLine("  \"experiencePoints\": 25,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine("    \"challenge\": \"Description of the challenge\",");
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

        // ===== SECTION 3: QUIZ SCHEMA =====
        prompt.AppendLine("#### 3. Quiz (EXACTLY 2 steps required)");
        prompt.AppendLine("Used for: Knowledge assessment, concept verification");

        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 3,");
        prompt.AppendLine("  \"title\": \"Test Your Knowledge\",");
        prompt.AppendLine("  \"description\": \"Verify your understanding\",");
        prompt.AppendLine("  \"stepType\": \"Quiz\",");
        prompt.AppendLine("  \"experiencePoints\": 20,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine("    \"questions\": [");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"question\": \"What is the correct definition?\",");
        prompt.AppendLine("        \"options\": [\"Option 1\", \"Option 2\", \"Option 3\", \"Option 4\"],");
        prompt.AppendLine("        \"correctAnswer\": \"Option 1\",");
        prompt.AppendLine("        \"explanation\": \"Why this is correct\"");
        prompt.AppendLine("      }");
        prompt.AppendLine("    ]");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        // ===== SECTION 4: CODING SCHEMA (CRITICAL!) =====
        prompt.AppendLine("#### 4. Coding (EXACTLY 2 steps required - NON-NEGOTIABLE!)");
        prompt.AppendLine();
        prompt.AppendLine("🚨 **CRITICAL**: You MUST include 2 Coding steps. This is NOT optional!");
        prompt.AppendLine();
        prompt.AppendLine("Used for: Practical programming challenges");
        prompt.AppendLine();
        prompt.AppendLine("**How to create Coding steps:**");
        prompt.AppendLine("1. Identify 2 programming topics from the syllabus SessionSchedule");
        prompt.AppendLine("2. Create practical coding challenges based on those topics");
        prompt.AppendLine($"3. Use appropriate language for {subjectName} (e.g., Java/Kotlin for Android, C# for ASP.NET)");
        prompt.AppendLine("4. Set difficulty based on step progression (Beginner → Intermediate)");
        prompt.AppendLine();
        prompt.AppendLine("**Examples for Android subject:**");
        prompt.AppendLine("- \"Create a Button click listener\"");
        prompt.AppendLine("- \"Implement a RecyclerView adapter\"");
        prompt.AppendLine("- \"Build a custom View with ConstraintLayout\"");
        prompt.AppendLine();
        prompt.AppendLine("**Schema:**");

        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 4,");
        prompt.AppendLine("  \"title\": \"Coding Challenge: [Topic]\",");
        prompt.AppendLine("  \"description\": \"Implement a solution to practice the concept\",");
        prompt.AppendLine("  \"stepType\": \"Coding\",");
        prompt.AppendLine("  \"experiencePoints\": 40,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine($"    \"language\": \"[Java|Kotlin|C#|etc. - appropriate for {subjectName}]\",");
        prompt.AppendLine("    \"difficulty\": \"Beginner|Intermediate|Advanced\",");
        prompt.AppendLine($"    \"topic\": \"[specific topic from {subjectName} syllabus]\"");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        // ===== CRITICAL RULES SECTION =====
        prompt.AppendLine("### Critical Rules");
        prompt.AppendLine();
        prompt.AppendLine("- **MUST** use only skillIds from the Pre-Approved Skills list");
        prompt.AppendLine("- **MUST** include exactly 2 Coding steps (this is checked and enforced!)");
        prompt.AppendLine("- For Reading steps, **MUST** copy suggestedUrl from syllabus");
        prompt.AppendLine("- **MUST** use exact stepType values: Reading, Interactive, Quiz, Coding");
        prompt.AppendLine("- **MUST** return ONLY valid JSON array");
        prompt.AppendLine($"- **NEVER** generate content about technologies not in {subjectName}");
        prompt.AppendLine();

        // ===== OUTPUT FORMAT SECTION =====
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
        prompt.AppendLine("    \"content\": { }");
        prompt.AppendLine("  }");
        prompt.AppendLine("]");
        prompt.AppendLine("```");
        prompt.AppendLine();

        // ===== FINAL VALIDATION CHECKLIST =====
        prompt.AppendLine("---");
        prompt.AppendLine();
        prompt.AppendLine("## FINAL VALIDATION CHECKLIST");
        prompt.AppendLine();
        prompt.AppendLine("Before submitting, verify EVERY requirement:");
        prompt.AppendLine();
        prompt.AppendLine("☑ Total steps = 10");
        prompt.AppendLine("☑ Reading steps = 4");
        prompt.AppendLine("☑ Interactive steps = 2");
        prompt.AppendLine("☑ Quiz steps = 2");
        prompt.AppendLine("☑ Coding steps = 2 (MANDATORY!)");
        prompt.AppendLine($"☑ All steps are about {subjectName}");
        prompt.AppendLine("☑ All Reading steps have URLs from syllabus (or empty string if not available)");
        prompt.AppendLine("☑ All skillIds are from Pre-Approved Skills list");
        prompt.AppendLine("☑ Output is valid JSON only (no markdown, no explanations)");
        prompt.AppendLine();
        prompt.AppendLine("**IMPORTANT:** Return ONLY the JSON array.");
        prompt.AppendLine();
        prompt.AppendLine($"Generate the 10 quest steps for {subjectName} now (4 Reading + 2 Interactive + 2 Quiz + 2 Coding):");

        return prompt.ToString();
    }
}
