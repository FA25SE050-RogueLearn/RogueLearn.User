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
        AcademicContext academicContext,
        string? errorHint = null)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("You are an expert curriculum designer creating a personalized, gamified weekly learning module.");
        prompt.AppendLine($"**Objective:** Create a tailored learning path for Week {weekContext.WeekNumber} of {weekContext.TotalWeeks} for '{subjectName}'.");
        prompt.AppendLine();
        prompt.AppendLine("---");

        // 1. Context
        prompt.AppendLine("## 1. Learning Context");
        prompt.AppendLine($"**Subject:** {subjectName}");
        prompt.AppendLine($"**Course Description:** {courseDescription}");
        prompt.AppendLine("**Student Profile:");
        prompt.AppendLine(userContext);
        prompt.AppendLine();

        prompt.AppendLine("## 2. Academic Performance Context");
        prompt.AppendLine("**IMPORTANT:** Use this information to personalize difficulty, pacing, and focus areas.");
        prompt.AppendLine();
        prompt.AppendLine($"**Overall GPA:** {academicContext.CurrentGpa:F2}/10.0");
        prompt.AppendLine($"**Quest Attempt Status:** {GetAttemptReasonDescription(academicContext.AttemptReason)}");
        if (academicContext.PreviousAttempts > 0)
        {
            prompt.AppendLine($"**Previous Attempts:** {academicContext.PreviousAttempts} time(s)");
        }
        if (academicContext.PrerequisiteHistory.Any())
        {
            prompt.AppendLine();
            prompt.AppendLine("**Prerequisite Subject Performance:**");
            foreach (var prereq in academicContext.PrerequisiteHistory.Take(5))
            {
                prompt.AppendLine($"- {prereq.SubjectCode} ({prereq.SubjectName}): {prereq.PerformanceLevel} - Grade: {prereq.Grade ?? "N/A"}");
            }
            var weakPrereqs = academicContext.PrerequisiteHistory.Where(p => p.PerformanceLevel == "Weak").ToList();
            if (weakPrereqs.Any())
            {
                prompt.AppendLine();
                prompt.AppendLine("⚠️ **Foundation Gaps Detected:**");
                foreach (var weak in weakPrereqs.Take(3))
                {
                    prompt.AppendLine($"   - Struggled with {weak.SubjectCode}: Include additional foundational review");
                }
            }
        }
        if (academicContext.RelatedSubjects.Any())
        {
            prompt.AppendLine();
            prompt.AppendLine("**Related Subject Grades:**");
            foreach (var related in academicContext.RelatedSubjects.Take(5))
            {
                prompt.AppendLine($"- {related.SubjectCode}: {related.Grade ?? "N/A"}/10.0");
            }
        }
        if (academicContext.StrengthAreas.Any())
        {
            prompt.AppendLine();
            prompt.AppendLine("✅ **Proven Strengths:**");
            foreach (var strength in academicContext.StrengthAreas)
            {
                prompt.AppendLine($"   - {strength}");
            }
            prompt.AppendLine("   → Leverage these areas for confidence-building");
        }
        if (academicContext.ImprovementAreas.Any())
        {
            prompt.AppendLine();
            prompt.AppendLine("📈 **Areas Needing Support:**");
            foreach (var area in academicContext.ImprovementAreas)
            {
                prompt.AppendLine($"   - {area}");
            }
            prompt.AppendLine("   → Provide extra scaffolding, examples, and practice in these areas");
        }

        prompt.AppendLine();
        prompt.AppendLine("### Personalization Directives:");
        prompt.AppendLine(GetPersonalizationInstructions(academicContext));
        prompt.AppendLine();
        prompt.AppendLine("---");

        // 2. Week Objectives (The "What")
        prompt.AppendLine("## 3. Week Learning Objectives (Topics to Cover)");
        prompt.AppendLine("You must cover the following topics. Group related topics into logical activities.");
        foreach (var topic in weekContext.TopicsToCover)
        {
            prompt.AppendLine($"- {topic}");
        }
        prompt.AppendLine();

        // 3. Approved Resources (The "How")
        prompt.AppendLine("## 4. Approved Resource Pool");
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
            prompt.AppendLine("**No External URLs Available:**");
            prompt.AppendLine("- Do NOT create any Reading activities");
            prompt.AppendLine("- Generate only KnowledgeCheck and Quiz activities");
            prompt.AppendLine("- Use clear, plain-text math and examples derived from Topics");
        }
        prompt.AppendLine();

        // 4. Skills
        prompt.AppendLine("## 5. Skill Mapping");
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
        prompt.AppendLine("## 6. Construction Rules");
        prompt.AppendLine("1. **Group Topics:** Combine 2-3 related topics into each `Reading` activity.");
        prompt.AppendLine("2. **Distribute Resources:** Use the Approved Resource Pool when available. If limited URLs exist, reuse strong sources across grouped topics.");
        prompt.AppendLine("3. **Activity Count:** Generate **6 to 9** activities total.");
        if (weekContext.AvailableResources.Any())
        {
            prompt.AppendLine("   - 0-3 `Reading` activities (use only approved URLs; do not invent or duplicate)");
            prompt.AppendLine("   - 2-4 `KnowledgeCheck` activities (3-5 questions each)");
            prompt.AppendLine("   - 1 `Quiz` (10-15 questions)");
        }
        else
        {
            prompt.AppendLine("   - 0 `Reading` activities (no URLs provided)");
            prompt.AppendLine("   - 3-5 `KnowledgeCheck` activities (3-5 questions each)");
            prompt.AppendLine("   - 1-2 `Quiz` activities (10-15 questions each)");
        }
        prompt.AppendLine("4. **No Coding:** Do not generate coding activities.");
        prompt.AppendLine("5. **JSON Only:** Output valid JSON matching the schema below. No markdown.");
        prompt.AppendLine();
        prompt.AppendLine("### CRITICAL: HOW TO HANDLE C ESCAPE SEQUENCES IN JSON");
        prompt.AppendLine();
        prompt.AppendLine("**PROBLEM:** C escape sequences like \\n, \\t, \\0 will break JSON parsing.");
        prompt.AppendLine();
        prompt.AppendLine("**SOLUTION:** Never write literal backslash characters in your JSON strings.");
        prompt.AppendLine();
        prompt.AppendLine("When discussing C escape sequences in questions, explanations, or options:");
        prompt.AppendLine();
        prompt.AppendLine("❌ WRONG (will cause JSON errors):");
        prompt.AppendLine("  \"question\": \"What does \\n represent?\"");
        prompt.AppendLine("  \"explanation\": \"The \\0 character marks...\"");
        prompt.AppendLine();
        prompt.AppendLine("✅ CORRECT (use descriptive text):");
        prompt.AppendLine("  \"question\": \"What does the newline escape sequence represent?\"");
        prompt.AppendLine("  \"explanation\": \"The null character (backslash-zero) marks...\"");
        prompt.AppendLine();
        prompt.AppendLine("Reference:");
        prompt.AppendLine("- \\n → 'newline' or 'backslash-n'");
        prompt.AppendLine("- \\t → 'tab' or 'backslash-t'");
        prompt.AppendLine("- \\0 → 'null character' or 'backslash-zero'");
        prompt.AppendLine("- \\r → 'carriage return' or 'backslash-r'");
        prompt.AppendLine("- \\\\ → 'backslash' or 'single backslash'");
        prompt.AppendLine("- \\\" → 'double quote' or 'backslash-quote'");
        prompt.AppendLine();
        prompt.AppendLine("**NEVER USE ACTUAL BACKSLASHES IN YOUR CONTENT**");
        prompt.AppendLine("### CRITICAL JSON STRING ENCODING RULES");
        prompt.AppendLine("1) Double backslashes in JSON strings: \\\\n, \\\\t, \\\\0, \\\\\\\\ ");
        prompt.AppendLine("2) If showing C code with escapes, double ALL backslashes");
        prompt.AppendLine("3) Escape double quotes as \\\" inside strings");
        prompt.AppendLine("4) Prefer describing escapes in words if unsure");
        prompt.AppendLine("### STRING AND CODE REPRESENTATION RULES (ESCAPES IN JSON)");
        prompt.AppendLine("- You may use C escape sequences in text (\\0, \\n, \\t, \\r, \\a, \\v, \\e)");
        prompt.AppendLine("- IMPORTANT: In JSON strings, backslashes MUST be doubled: \\0, \\n, \\t, \\r, \\a, \\v, \\e");
        prompt.AppendLine("- Keep code examples concise; avoid markdown code fences; output JSON only");
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

    private string GetAttemptReasonDescription(QuestAttemptReason reason)
    {
        return reason switch
        {
            QuestAttemptReason.FirstTime => "First Attempt (Standard pace)",
            QuestAttemptReason.Retake => "Retaking after failure (Needs reinforcement)",
            QuestAttemptReason.CurrentlyStudying => "Currently enrolled (Real-time support)",
            QuestAttemptReason.Advancement => "Already passed (Advanced enrichment)",
            _ => "Unknown"
        };
    }

    private string GetPersonalizationInstructions(AcademicContext context)
    {
        var instructions = new StringBuilder();
        if (context.CurrentGpa >= 8.5)
        {
            instructions.AppendLine("- **High Achiever (GPA ≥ 8.5):** Include challenging questions with minimal scaffolding. Add extension questions.");
        }
        else if (context.CurrentGpa >= 7.0)
        {
            instructions.AppendLine("- **Good Performance (GPA 7.0-8.5):** Balance difficulty with moderate guidance.");
        }
        else if (context.CurrentGpa > 0)
        {
            instructions.AppendLine("- **Needs Support (GPA < 7.0):** Provide step-by-step explanations. Break complex concepts into smaller parts. Include more worked examples.");
        }
        switch (context.AttemptReason)
        {
            case QuestAttemptReason.Retake:
                instructions.AppendLine("- **Retake Student:** Focus on common failure points. Add prerequisite review questions. Use varied question formats to address different learning gaps.");
                break;
            case QuestAttemptReason.CurrentlyStudying:
                instructions.AppendLine("- **Current Student:** Align with typical semester pacing. Include practical application questions relevant to assignments.");
                break;
            case QuestAttemptReason.Advancement:
                instructions.AppendLine("- **Already Passed:** Focus on advanced applications, deeper insights, and connections to upcoming topics.");
                break;
        }
        if (context.PrerequisiteHistory.Any(p => p.PerformanceLevel == "Weak"))
        {
            instructions.AppendLine("- **Foundation Gaps:** Include brief refreshers on prerequisite concepts before introducing new material. Link new concepts explicitly to prior knowledge.");
        }
        if (context.ImprovementAreas.Any())
        {
            instructions.AppendLine($"- **Remediation Focus:** Pay special attention to {string.Join(", ", context.ImprovementAreas.Take(2))}. Provide extra practice and concrete examples in these areas.");
        }
        if (context.StrengthAreas.Any())
        {
            instructions.AppendLine($"- **Leverage Strengths:** Connect new material to their strong areas ({string.Join(", ", context.StrengthAreas.Take(2))}) to build confidence.");
        }
        return instructions.ToString();
    }
}
