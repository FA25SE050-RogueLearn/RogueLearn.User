// RogueLearn.User/src/RogueLearn.User.Application/Services/QuestStepsPromptBuilder.cs
using System.Text;
using System.Text.Json;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Builds LLM-friendly prompts for generating "Master Quest" steps.
/// Generates three parallel difficulty tracks (Standard, Supportive, Challenging) for a single module.
/// </summary>
public class QuestStepsPromptBuilder
{
    public string BuildMasterPrompt(
        QuestStepDefinition module,
        List<Skill> relevantSkills,
        string subjectName,
        string courseDescription,
        Class? userClass = null,
        string? errorHint = null)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("You are an expert curriculum designer creating a MASTER QUEST MODULE.");
        prompt.AppendLine($"**Objective:** Create content for **Module {module.ModuleNumber}: {module.Title}** of '{subjectName}'.");
        prompt.AppendLine();
        prompt.AppendLine("---");

        // 1. Context
        prompt.AppendLine("## 1. Learning Context");
        prompt.AppendLine($"**Subject:** {subjectName}");
        prompt.AppendLine($"**Course Description:** {courseDescription}");
        prompt.AppendLine();

        // 1.1 Career Roadmap Context
        if (userClass != null)
        {
            prompt.AppendLine("### Career Alignment");
            prompt.AppendLine($"**Track:** {userClass.Name}");
            if (!string.IsNullOrEmpty(userClass.RoadmapUrl))
            {
                prompt.AppendLine($"**Roadmap:** {userClass.RoadmapUrl}");
            }
            if (userClass.SkillFocusAreas != null && userClass.SkillFocusAreas.Any())
            {
                prompt.AppendLine($"**Focus Areas:** {string.Join(", ", userClass.SkillFocusAreas)}");
            }
        }

        prompt.AppendLine();
        prompt.AppendLine("## 2. Module Objectives (Topics & Resources)");
        prompt.AppendLine("The content must cover these sessions. Use the provided URLs when available:");

        foreach (var session in module.Sessions)
        {
            prompt.AppendLine($"### Session {session.SessionNumber}: {session.Topic}");

            // Explicitly listing the high-quality URL for the AI to use
            if (!string.IsNullOrWhiteSpace(session.SuggestedUrl))
            {
                prompt.AppendLine($"  ✅ **APPROVED RESOURCE URL:** {session.SuggestedUrl}");
            }
            else
            {
                prompt.AppendLine($"  ⚠️ **NO URL PROVIDED** - Create summary-based reading content");
            }

            if (session.Readings != null && session.Readings.Any())
            {
                prompt.AppendLine($"  📖 **Syllabus Reference:** {string.Join(", ", session.Readings)}");
            }
        }
        prompt.AppendLine();

        // 3. Skills
        prompt.AppendLine("## 3. Skill Mapping");
        prompt.AppendLine("Assign a `skillId` from this list to EVERY activity you create:");
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
        prompt.AppendLine();

        // 4. Activity Types Explained
        prompt.AppendLine("## 4. Activity Types & Purpose");
        prompt.AppendLine("⚠️ **CRITICAL:** You MUST include an integer `experiencePoints` field in the `payload` of EVERY activity.");
        prompt.AppendLine();

        prompt.AppendLine("### 📖 Reading Activities");
        prompt.AppendLine("**Purpose:** Introduce new concepts and knowledge");
        prompt.AppendLine("**XP Value:** 10-20 XP");
        prompt.AppendLine("**Payload Structure:**");
        prompt.AppendLine("- If an APPROVED RESOURCE URL exists → MUST include `url` field");
        prompt.AppendLine("- If NO URL provided → Use `summary` field with detailed content");
        prompt.AppendLine("- Always include `articleTitle`, `summary`, and `experiencePoints`");
        prompt.AppendLine();
        prompt.AppendLine("**Example:**");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"type\": \"Reading\",");
        prompt.AppendLine("  \"activityId\": \"placeholder-id\",");
        prompt.AppendLine("  \"skillId\": \"skill-uuid-here\",");
        prompt.AppendLine("  \"payload\": {");
        prompt.AppendLine("    \"url\": \"https://www.geeksforgeeks.org/c-programming/\",");
        prompt.AppendLine("    \"articleTitle\": \"Introduction to C Programming\",");
        prompt.AppendLine("    \"summary\": \"Learn the basics of C programming...\",");
        prompt.AppendLine("    \"experiencePoints\": 15");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        prompt.AppendLine("### ✅ KnowledgeCheck Activities");
        prompt.AppendLine("**Purpose:** Quick comprehension checks after reading material");
        prompt.AppendLine("**XP Value:** 25-35 XP");
        prompt.AppendLine("**Requirements:**");
        prompt.AppendLine("- **3-4 questions** grouped in a single activity");
        prompt.AppendLine("- Tests understanding of recently covered content");
        prompt.AppendLine("- Provides immediate feedback via explanation");
        prompt.AppendLine("- Place strategically after Reading activities");
        prompt.AppendLine();
        prompt.AppendLine("**Payload Structure (Array of Questions):**");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"type\": \"KnowledgeCheck\",");
        prompt.AppendLine("  \"activityId\": \"placeholder-id\",");
        prompt.AppendLine("  \"skillId\": \"skill-uuid-here\",");
        prompt.AppendLine("  \"payload\": {");
        prompt.AppendLine("    \"experiencePoints\": 30,");
        prompt.AppendLine("    \"questions\": [");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"question\": \"What is a compiler?\",");
        prompt.AppendLine("        \"options\": [\"A\", \"B\", \"C\", \"D\"],");
        prompt.AppendLine("        \"answer\": \"A\",");
        prompt.AppendLine("        \"explanation\": \"...\"");
        prompt.AppendLine("      },");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"question\": \"Question 2...\",");
        prompt.AppendLine("        \"options\": [...],");
        prompt.AppendLine("        \"answer\": \"...\",");
        prompt.AppendLine("        \"explanation\": \"...\"");
        prompt.AppendLine("      }");
        prompt.AppendLine("    ]");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        prompt.AppendLine("### 🎓 Quiz Activities");
        prompt.AppendLine("**Purpose:** Comprehensive assessment of the entire module (MANDATORY FINAL ACTIVITY)");
        prompt.AppendLine("**XP Value:** 50-100 XP (Higher for challenging track)");
        prompt.AppendLine();
        prompt.AppendLine("⚠️ **CRITICAL REQUIREMENT:** EVERY quest step MUST end with a Quiz activity. NO EXCEPTIONS.");
        prompt.AppendLine();
        prompt.AppendLine("**Requirements:**");
        prompt.AppendLine("- **SUPPORTIVE Track:** 7-8 questions (foundational, 70% passing grade)");
        prompt.AppendLine("- **STANDARD Track:** 8-9 questions (balanced difficulty, 70% passing grade)");
        prompt.AppendLine("- **CHALLENGING Track:** 9-10 questions (advanced/application level, 70% passing grade)");
        prompt.AppendLine("- MUST be placed as the LAST activity in the sequence");
        prompt.AppendLine("- Consolidate all questions into a SINGLE Quiz activity");
        prompt.AppendLine("- Cover all major topics from the module comprehensively");
        prompt.AppendLine();
        prompt.AppendLine("**Payload Structure (Array of Questions):**");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"type\": \"Quiz\",");
        prompt.AppendLine("  \"activityId\": \"placeholder-id\",");
        prompt.AppendLine("  \"skillId\": \"skill-uuid-here\",");
        prompt.AppendLine("  \"payload\": {");
        prompt.AppendLine("    \"experiencePoints\": 80,");
        prompt.AppendLine("    \"questions\": [");
        prompt.AppendLine("      {");
        prompt.AppendLine("        \"question\": \"Quiz Question 1?\",");
        prompt.AppendLine("        \"options\": [\"A\", \"B\", \"C\", \"D\"],");
        prompt.AppendLine("        \"answer\": \"Correct answer\",");
        prompt.AppendLine("        \"explanation\": \"Explanation...\"");
        prompt.AppendLine("      },");
        prompt.AppendLine("      // ... 6-9 more questions depending on track");
        prompt.AppendLine("    ]");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        // 5. The "Three-Lane" Requirement
        prompt.AppendLine("## 5. The Three-Lane Architecture");
        prompt.AppendLine("You MUST generate THREE complete, parallel versions of this module.");
        prompt.AppendLine();

        prompt.AppendLine("### 🟢 SUPPORTIVE Track (For struggling students)");
        prompt.AppendLine("**Philosophy:** Extra scaffolding, simpler language, more guidance");
        prompt.AppendLine("- **Reading:** Break complex topics into smaller chunks, use analogies");
        prompt.AppendLine("- **KnowledgeCheck:** 3-4 questions per check, simple recall focus");
        prompt.AppendLine("- **Quiz:** MANDATORY - 7-8 questions, straightforward, foundational understanding");
        prompt.AppendLine("- **Total Activities:** 6-8 activities (including the mandatory Quiz)");
        prompt.AppendLine();

        prompt.AppendLine("### 🟡 STANDARD Track (The balanced path)");
        prompt.AppendLine("**Philosophy:** Comprehensive coverage at appropriate depth");
        prompt.AppendLine("- **Reading:** Cover all learning outcomes with clear explanations");
        prompt.AppendLine("- **KnowledgeCheck:** 3-4 questions per check, comprehension and basic application");
        prompt.AppendLine("- **Quiz:** MANDATORY - 8-9 questions, balanced difficulty, mixed question types");
        prompt.AppendLine("- **Total Activities:** 7-9 activities (including the mandatory Quiz)");
        prompt.AppendLine("- **MUST use APPROVED RESOURCE URLs** when available for Reading activities");
        prompt.AppendLine();

        prompt.AppendLine("### 🔴 CHALLENGING Track (For advanced students)");
        prompt.AppendLine("**Philosophy:** Deeper exploration, edge cases, real-world complexity");
        prompt.AppendLine("- **Reading:** Advanced patterns, industry best practices, performance considerations");
        prompt.AppendLine("- **KnowledgeCheck:** 3-4 questions per check, scenario-based, requires analysis");
        prompt.AppendLine("- **Quiz:** MANDATORY - 9-10 questions, application/analysis level (Bloom's Taxonomy)");
        prompt.AppendLine("- **Total Activities:** 8-10 activities (including the mandatory Quiz)");
        prompt.AppendLine();

        prompt.AppendLine("**CRITICAL:** All three tracks MUST:");
        prompt.AppendLine("1. Cover the same core topics but at different depths");
        prompt.AppendLine("2. End with a Quiz activity (NO EXCEPTIONS)");
        prompt.AppendLine();

        // 6. Activity Flow Pattern
        prompt.AppendLine("## 6. Recommended Activity Flow");
        prompt.AppendLine("For each track, follow this general pattern:");
        prompt.AppendLine("1. Reading (introduce concept)");
        prompt.AppendLine("2. KnowledgeCheck (3-4 questions)");
        prompt.AppendLine("3. Reading (next concept)");
        prompt.AppendLine("4. KnowledgeCheck (3-4 questions)");
        prompt.AppendLine("5. Reading (additional concepts if needed)");
        prompt.AppendLine("6. **Quiz (MANDATORY FINAL ACTIVITY - 7-10 questions)**");
        prompt.AppendLine();
        prompt.AppendLine("**Note:** The Quiz MUST always be the last activity. Adjust the number of Reading and KnowledgeCheck activities based on content complexity.");
        prompt.AppendLine();

        // 7. Output Rules
        prompt.AppendLine("## 7. Output Rules & Constraints");
        prompt.AppendLine();
        prompt.AppendLine("### ✅ MUST DO:");
        prompt.AppendLine("1. Return ONLY valid JSON (no markdown code fences, no preamble)");
        prompt.AppendLine("2. Generate activities for all 3 tracks: `supportive`, `standard`, `challenging`");
        prompt.AppendLine("3. **END EACH TRACK WITH A QUIZ ACTIVITY** (7-10 questions depending on track)");
        prompt.AppendLine("4. Use APPROVED RESOURCE URLs in Reading activities when provided");
        prompt.AppendLine("5. Assign a valid `skillId` to every activity");
        prompt.AppendLine("6. Include `experiencePoints` in EVERY activity payload");
        prompt.AppendLine("7. Group questions into `questions` arrays for KnowledgeCheck and Quiz");
        prompt.AppendLine("8. Ensure Quiz covers all major topics from the module");
        prompt.AppendLine();

        prompt.AppendLine("### ❌ MUST NOT DO:");
        prompt.AppendLine("1. Do NOT generate coding activities or exercises");
        prompt.AppendLine("2. Do NOT use literal escape sequences like `\\n` or `\\t` in strings");
        prompt.AppendLine("3. Do NOT create single-question activities");
        prompt.AppendLine("4. **DO NOT omit the Quiz activity - it is MANDATORY**");
        prompt.AppendLine("5. Do NOT omit `experiencePoints` from payloads");
        prompt.AppendLine("6. Do NOT wrap JSON output in markdown code fences");
        prompt.AppendLine();

        // 8. JSON Schema
        prompt.AppendLine("## 8. Output JSON Schema");
        prompt.AppendLine();
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"supportive\": {");
        prompt.AppendLine("    \"activities\": [");
        prompt.AppendLine("      { \"type\": \"Reading\", \"activityId\": \"placeholder\", \"skillId\": \"...\", \"payload\": { \"experiencePoints\": 10, ... } },");
        prompt.AppendLine("      { \"type\": \"KnowledgeCheck\", \"activityId\": \"placeholder\", \"skillId\": \"...\", \"payload\": { \"experiencePoints\": 25, \"questions\": [...] } },");
        prompt.AppendLine("      { \"type\": \"Reading\", \"activityId\": \"placeholder\", \"skillId\": \"...\", \"payload\": { \"experiencePoints\": 10, ... } },");
        prompt.AppendLine("      { \"type\": \"Quiz\", \"activityId\": \"placeholder\", \"skillId\": \"...\", \"payload\": { \"experiencePoints\": 60, \"questions\": [7-8 questions] } }");
        prompt.AppendLine("    ]");
        prompt.AppendLine("  },");
        prompt.AppendLine("  \"standard\": {");
        prompt.AppendLine("    \"activities\": [");
        prompt.AppendLine("      // ... similar structure ...");
        prompt.AppendLine("      { \"type\": \"Quiz\", \"activityId\": \"placeholder\", \"skillId\": \"...\", \"payload\": { \"experiencePoints\": 80, \"questions\": [8-9 questions] } }");
        prompt.AppendLine("    ]");
        prompt.AppendLine("  },");
        prompt.AppendLine("  \"challenging\": {");
        prompt.AppendLine("    \"activities\": [");
        prompt.AppendLine("      // ... similar structure ...");
        prompt.AppendLine("      { \"type\": \"Quiz\", \"activityId\": \"placeholder\", \"skillId\": \"...\", \"payload\": { \"experiencePoints\": 100, \"questions\": [9-10 questions] } }");
        prompt.AppendLine("    ]");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        // 9. String Formatting Guidelines
        prompt.AppendLine("## 9. String Formatting Guidelines");
        prompt.AppendLine("When writing text for questions, explanations, and summaries:");
        prompt.AppendLine("- Use plain English with normal punctuation");
        prompt.AppendLine("- For line breaks, write complete sentences instead of using escape codes");
        prompt.AppendLine("- For emphasis, use natural language or Markdown (*italic*, **bold**)");
        prompt.AppendLine("- Example: Instead of 'First step\\nSecond step', write 'First step. Second step.'");
        prompt.AppendLine();

        if (!string.IsNullOrWhiteSpace(errorHint))
        {
            prompt.AppendLine("## ⚠️ CORRECTION REQUIRED");
            prompt.AppendLine("Your previous output was invalid. Please fix the following error:");
            prompt.AppendLine("```");
            prompt.AppendLine(errorHint);
            prompt.AppendLine("```");
            prompt.AppendLine();
            prompt.AppendLine("Review the schema and requirements above. Ensure your JSON is valid and complete.");
        }

        // Final reminder with Quiz emphasis
        prompt.AppendLine("---");
        prompt.AppendLine("## FINAL VALIDATION CHECKLIST:");
        prompt.AppendLine("Before submitting, verify:");
        prompt.AppendLine("- [ ] Generated all 3 tracks (supportive, standard, challenging)");
        prompt.AppendLine("- [ ] **EACH TRACK ENDS WITH A QUIZ ACTIVITY (CRITICAL)**");
        prompt.AppendLine("- [ ] Included `experiencePoints` for EVERY activity");
        prompt.AppendLine("- [ ] Quiz has correct question count (Supportive: 7-8, Standard: 8-9, Challenging: 9-10)");
        prompt.AppendLine("- [ ] KnowledgeChecks have 3-4 questions each");
        prompt.AppendLine("- [ ] All Reading activities with URLs use APPROVED RESOURCE URLs");
        prompt.AppendLine("- [ ] Every activity has a valid skillId and activityId");
        prompt.AppendLine("- [ ] Output is pure JSON (no markdown fences, no explanatory text)");
        prompt.AppendLine("- [ ] Questions are grouped in `questions` arrays for KnowledgeCheck and Quiz");

        return prompt.ToString();
    }
}