// RogueLearn.User/src/RogueLearn.User.Application/Services/QuestStepsPromptBuilder.cs
using System.Text;
using System.Text.Json;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Builds LLM-friendly prompts for generating quest steps based on syllabus content and user context.
/// Generates activities for a SINGLE week (one quest step = one week).
/// Activity types: Reading, KnowledgeCheck, Quiz (NO Coding activities).
/// </summary>
public class QuestStepsPromptBuilder
{
    /// <summary>
    /// Generates a prompt for the LLM to create quest steps for a SINGLE week.
    /// Each step contains 7-10 activities following a structured pattern (Reading, KnowledgeCheck, Quiz only).
    /// This method is called multiple times (once per week) to generate all quest steps.
    /// </summary>
    public string BuildPrompt(
        string syllabusJson,
        string userContext,
        List<Skill> relevantSkills,
        string subjectName,
        string courseDescription,
        int weekNumber,
        int totalWeeks,
        string? errorHint = null)
    {
        var prompt = new StringBuilder();

        // System-level instruction and main goal
        prompt.AppendLine("You are an expert curriculum designer creating gamified weekly learning modules for a university subject.");
        prompt.AppendLine($"Your task is to analyze the syllabus for '{subjectName}' and generate activities for Week {weekNumber} out of {totalWeeks} weeks.");
        prompt.AppendLine();
        prompt.AppendLine($"⚠️ CRITICAL: Generate structured activities for ONLY Week {weekNumber}. Group the appropriate syllabus sessions (typically sessions {(weekNumber - 1) * 5 + 1} through {weekNumber * 5}) and create 7-10 high-quality activities.");
        prompt.AppendLine();
        prompt.AppendLine("---");

        // High-level context
        prompt.AppendLine("## 1. Context");
        prompt.AppendLine($"**Subject:** {subjectName}");
        prompt.AppendLine($"**Course Description:** {courseDescription}");
        prompt.AppendLine($"**Week Number:** {weekNumber} of {totalWeeks}");
        prompt.AppendLine("**Student Context:**");
        prompt.AppendLine(userContext);
        prompt.AppendLine();
        prompt.AppendLine("---");

        // Pre-approved skills
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

        // Syllabus Content
        prompt.AppendLine("## 3. Source of Truth: Complete Syllabus");
        prompt.AppendLine($"The syllabus below contains ALL sessions for the entire course. Extract ONLY the sessions belonging to Week {weekNumber}.");
        prompt.AppendLine("Typically:");
        prompt.AppendLine($"  - Week 1 = Sessions 1-5");
        prompt.AppendLine($"  - Week 2 = Sessions 6-10");
        prompt.AppendLine($"  - Week {weekNumber} = Sessions {(weekNumber - 1) * 5 + 1}-{weekNumber * 5}");
        prompt.AppendLine();
        prompt.AppendLine("Generate activities ONLY for those sessions. Ignore all other sessions.");
        prompt.AppendLine("```json");
        prompt.AppendLine(syllabusJson);
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("---");

        // Output Schema
        prompt.AppendLine("## 4. Your Task: Generate JSON for Week Activities");
        prompt.AppendLine($"Your entire output MUST be a single, valid JSON object containing ONLY an array of activities for Week {weekNumber}.");
        prompt.AppendLine();
        prompt.AppendLine("### MATHEMATICAL NOTATION RULES (PLAIN TEXT ONLY)");
        prompt.AppendLine("- Use PLAIN TEXT for all expressions. NO LaTeX, NO $ delimiters.");
        prompt.AppendLine("- NO backslashes anywhere in math (e.g., sin(x), not \\sin(x))");
        prompt.AppendLine("- Format guide:");
        prompt.AppendLine("  - Operations: + - * /");
        prompt.AppendLine("  - Exponents: x^2, x^3, e^x");
        prompt.AppendLine("  - Fractions: (a + b) / c");
        prompt.AppendLine("  - Square root: sqrt(x)");
        prompt.AppendLine("  - Functions: sin(x), cos(x), ln(x), log(x)");
        prompt.AppendLine("  - Absolute value: |x|");
        prompt.AppendLine("  - Greek letters: alpha, beta, theta, pi");
        prompt.AppendLine("  - Derivatives: f'(x), dy/dx, d^2y/dx^2");
        prompt.AppendLine("  - Integrals: integral from a to b of f(x) dx");
        prompt.AppendLine("  - Limits: lim (x->0) f(x)");
        prompt.AppendLine("  - Summation: sum from i=1 to n of x_i");
        prompt.AppendLine("Examples:");
        prompt.AppendLine("- BAD: The derivative is $\\frac{dy}{dx} = 2x$");
        prompt.AppendLine("- GOOD: The derivative is dy/dx = 2x");
        prompt.AppendLine("- BAD: Using $\\int_{0}^{\\pi} \\sin(x) dx$");
        prompt.AppendLine("- GOOD: Using the integral from 0 to pi of sin(x) dx");
        prompt.AppendLine();
        prompt.AppendLine("**Output Schema:**");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"activities\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"activityId\": \"<UUID>\",");
        prompt.AppendLine("      \"type\": \"Reading | KnowledgeCheck | Quiz\",");
        prompt.AppendLine("      \"payload\": { /* type-specific fields */ }");
        prompt.AppendLine("    }");
        prompt.AppendLine("    // ... more activities (7-10 total per week)");
        prompt.AppendLine("  ]");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        // Activity Payload Schemas
        prompt.AppendLine("### Activity Payload Schemas:");
        prompt.AppendLine();

        // Reading
        prompt.AppendLine("**A. `Reading` Payload:**");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"skillId\": \"<UUID from pre-approved list>\",");
        prompt.AppendLine("  \"experiencePoints\": 15,");
        prompt.AppendLine("  \"articleTitle\": \"<MUST match session topic from syllabus>\",");
        prompt.AppendLine("  \"summary\": \"<2-3 sentence summary of what students will learn>\",");
        prompt.AppendLine("  \"url\": \"<Use 'suggestedUrl' from syllabus session, or empty string if missing>\"");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("⚠️ **READING URL RULE:**");
        prompt.AppendLine("- ALWAYS use the `suggestedUrl` from the corresponding syllabus session");
        prompt.AppendLine("- If `suggestedUrl` is missing or empty, set `url: \"\"`");
        prompt.AppendLine("- Do NOT generate your own URLs");
        prompt.AppendLine();

        // KnowledgeCheck
        prompt.AppendLine("**B. `KnowledgeCheck` Payload (3-5 questions per check):**");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"skillId\": \"<UUID from pre-approved list>\",");
        prompt.AppendLine("  \"experiencePoints\": 35,");
        prompt.AppendLine("  \"topic\": \"<Topic being assessed>\",");
        prompt.AppendLine("  \"questions\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"question\": \"<Clear, specific question testing understanding>\",");
        prompt.AppendLine("      \"options\": [");
        prompt.AppendLine("        \"<Plausible wrong answer>\",");
        prompt.AppendLine("        \"<Correct answer>\",");
        prompt.AppendLine("        \"<Plausible wrong answer>\",");
        prompt.AppendLine("        \"<Plausible wrong answer>\"");
        prompt.AppendLine("      ],");
        prompt.AppendLine("      \"correctAnswer\": \"<Exact match to correct option>\",");
        prompt.AppendLine("      \"explanation\": \"<2-3 sentences explaining why this is correct>\"");
        prompt.AppendLine("    }");
        prompt.AppendLine("    // 2-4 more questions (total 3-5 questions)");
        prompt.AppendLine("  ]");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("⚠️ **KNOWLEDGE CHECK REQUIREMENTS:**");
        prompt.AppendLine("- Each KnowledgeCheck MUST have 3-5 questions (not just 1!)");
        prompt.AppendLine("- Questions should cover key concepts from preceding Reading activities");
        prompt.AppendLine("- All 4 options must be plausible to avoid obvious wrong answers");
        prompt.AppendLine("- Explanations must teach, not just confirm correctness");
        prompt.AppendLine();

        // Quiz
        prompt.AppendLine("**C. `Quiz` Payload (10-15 comprehensive questions):**");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"skillId\": \"<UUID from pre-approved list>\",");
        prompt.AppendLine("  \"experiencePoints\": 50,");
        prompt.AppendLine("  \"questions\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"question\": \"<Comprehensive question covering week's material>\",");
        prompt.AppendLine("      \"options\": [\"<option1>\", \"<option2>\", \"<option3>\", \"<option4>\"],");
        prompt.AppendLine("      \"correctAnswer\": \"<Exact match>\",");
        prompt.AppendLine("      \"explanation\": \"<Detailed 2-4 sentence explanation>\"");
        prompt.AppendLine("    }");
        prompt.AppendLine("    // 9-14 more questions (total 10-15)");
        prompt.AppendLine("  ]");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("⚠️ **QUIZ REQUIREMENTS:**");
        prompt.AppendLine("- Quizzes MUST have 10-15 questions covering ALL topics from the week");
        prompt.AppendLine("- Mix difficulty: 40% basic recall, 40% conceptual, 20% application");
        prompt.AppendLine("- Should be comprehensive end-of-week assessment");
        prompt.AppendLine();

        // CRITICAL RULES
        prompt.AppendLine("### CRITICAL RULES:");
        prompt.AppendLine();
        prompt.AppendLine($"**1. Week {weekNumber} Sessions Only:**");
        prompt.AppendLine($"   - Extract ONLY sessions {(weekNumber - 1) * 5 + 1} through {weekNumber * 5}");
        prompt.AppendLine("   - Ignore all other sessions from the syllabus");
        prompt.AppendLine("   - If the week has fewer than 5 sessions (last week), use all available sessions");
        prompt.AppendLine();
        prompt.AppendLine("**2. Session Filtering (Quality Over Quantity):**");
        prompt.AppendLine("   - Only include sessions with meaningful educational content");
        prompt.AppendLine("   - Skip: duplicate topics, sessions without educational value, trivial content");
        prompt.AppendLine("   - If a session has no suggestedUrl, still include it but set url: \"\"");
        prompt.AppendLine("   - Prioritize sessions that teach new concepts or skills");
        prompt.AppendLine();
        prompt.AppendLine("**3. Activity Pattern Per Week (7-10 activities total):**");
        prompt.AppendLine();
        prompt.AppendLine("   **STANDARD PATTERN:**");
        prompt.AppendLine("   - 2-3 `Reading` activities (from first 2-3 sessions)");
        prompt.AppendLine("   - 1 `KnowledgeCheck` with 3-5 questions (covering those readings)");
        prompt.AppendLine("   - 2-3 more `Reading` activities (remaining sessions)");
        prompt.AppendLine("   - 1 `KnowledgeCheck` with 3-5 questions (covering those readings)");
        prompt.AppendLine("   - 1-2 `Quiz` activities with 10-15 questions each (comprehensive week review)");
        prompt.AppendLine();
        prompt.AppendLine("   **Example Week Structure - 8-9 activities:**");
        prompt.AppendLine("   1. Reading (Session 1)");
        prompt.AppendLine("   2. Reading (Session 2)");
        prompt.AppendLine("   3. KnowledgeCheck (3-5 questions on Sessions 1-2)");
        prompt.AppendLine("   4. Reading (Session 3)");
        prompt.AppendLine("   5. Reading (Session 4)");
        prompt.AppendLine("   6. KnowledgeCheck (3-5 questions on Sessions 3-4)");
        prompt.AppendLine("   7. Reading (Session 5 - optional)");
        prompt.AppendLine("   8. Quiz (10-15 questions covering all sessions)");
        prompt.AppendLine("   9. (Optional) Additional KnowledgeCheck or Reading if beneficial");
        prompt.AppendLine();
        prompt.AppendLine("**4. Activity Count Per Week (STRICT):**");
        prompt.AppendLine("   - Minimum: 6 activities (MUST NOT go below)");
        prompt.AppendLine("   - Typical: 7-10 activities");
        prompt.AppendLine("   - Maximum: 10 activities (MUST NOT exceed)");
        prompt.AppendLine("   - Quality over quantity: Skip obvious or duplicate activities");
        prompt.AppendLine("   - NO Coding activities - only Reading, KnowledgeCheck, and Quiz");
        prompt.AppendLine();
        prompt.AppendLine("**5. UUIDs:**");
        prompt.AppendLine("   - Generate unique UUID v4 for every `activityId`");
        prompt.AppendLine("   - Format: xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx");
        prompt.AppendLine("   - Example: a1b2c3d4-e5f6-4789-0123-456789abcdef");
        prompt.AppendLine();
        prompt.AppendLine("**6. Skill ID Enforcement:**");
        prompt.AppendLine("   - Every activity payload MUST contain valid `skillId` from pre-approved list");
        prompt.AppendLine("   - Select most relevant skill based on activity content");
        prompt.AppendLine("   - Do NOT invent skillIds");
        prompt.AppendLine();
        prompt.AppendLine("**7. Experience Points (FIXED VALUES):**");
        prompt.AppendLine("   - Reading: 15 XP each");
        prompt.AppendLine("   - KnowledgeCheck: 35 XP each (regardless of question count)");
        prompt.AppendLine("   - Quiz: 50 XP each (regardless of question count)");
        prompt.AppendLine("   - Target per week: 250-400 XP total");
        prompt.AppendLine("   - Example: 4 Reading (60) + 2 KnowledgeCheck (70) + 1 Quiz (50) = 180 XP (adjust with more activities)");
        prompt.AppendLine("   - Example: 5 Reading (75) + 2 KnowledgeCheck (70) + 2 Quiz (100) = 245 XP");
        prompt.AppendLine();
        prompt.AppendLine("**8. Quality Standards (DO NOT SKIP):**");
        prompt.AppendLine("   - All questions must have exactly 4 plausible options");
        prompt.AppendLine("   - Explanations must be educational (2-4 sentences, not just confirming)");
        prompt.AppendLine("   - Avoid trivial, obvious, or trick questions");
        prompt.AppendLine("   - KnowledgeChecks test understanding of specific readings");
        prompt.AppendLine("   - Quizzes test comprehensive understanding of entire week");
        prompt.AppendLine("   - Do NOT include activities without educational value");
        prompt.AppendLine();
        prompt.AppendLine("**9. Allowed Activity Types ONLY:**");
        prompt.AppendLine("   - Reading (link to syllabus content)");
        prompt.AppendLine("   - KnowledgeCheck (3-5 questions)");
        prompt.AppendLine("   - Quiz (10-15 questions)");
        prompt.AppendLine("   - NO Coding activities");
        prompt.AppendLine("   - NO other types");
        prompt.AppendLine();
        prompt.AppendLine("**10. JSON Output (MUST BE PERFECT):**");
        prompt.AppendLine("   - Output ONLY the JSON object (no markdown, no commentary, no extra text)");
        prompt.AppendLine("   - Must be valid, parseable JSON");
        prompt.AppendLine("   - Use proper escaping for special characters:");
        prompt.AppendLine("     - Quotes: \\\"");
        prompt.AppendLine("     - Backslashes: \\\\");
        prompt.AppendLine("     - Newlines: \\n");
        prompt.AppendLine("   - Root level MUST contain \"activities\" array (NOT \"weeks\")");
        prompt.AppendLine("   - Every field must have correct type (string, number, array, object)");
        prompt.AppendLine();
        prompt.AppendLine($"Generate the complete JSON object for Week {weekNumber} activities now. Remember: 7-10 activities (Reading, KnowledgeCheck, Quiz ONLY), valid JSON only, no extra text.");

        if (!string.IsNullOrWhiteSpace(errorHint))
        {
            prompt.AppendLine();
            prompt.AppendLine("---");
            prompt.AppendLine("## 5. Correct Previous Errors");
            prompt.AppendLine("Your previous output failed JSON validation. Fix ALL issues and output valid JSON only.");
            prompt.AppendLine("Common fixes:");
            prompt.AppendLine("- Escape backslashes in LaTeX: use \\\\ (e.g., \\\\alpha, \\\\beta, \\\\times)");
            prompt.AppendLine("- Escape quotes in strings: use \\\" inside explanations");
            prompt.AppendLine("- Close all strings; no unclosed quotes");
            prompt.AppendLine("- No trailing commas");
            prompt.AppendLine("- Ensure correct commas between object fields");
            prompt.AppendLine("Validation error details:");
            prompt.AppendLine(errorHint);
        }

        return prompt.ToString();
    }
}
