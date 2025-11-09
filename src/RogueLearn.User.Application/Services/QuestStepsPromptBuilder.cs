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
    /// <returns>A structured prompt optimized for LLM consumption.</returns>
    public string BuildPrompt(string syllabusJson, string userContext, List<Skill> relevantSkills)
    {
        var prompt = new StringBuilder();

        // Header
        prompt.AppendLine("# Quest Step Generation Task");
        prompt.AppendLine();
        prompt.AppendLine("You are an expert educational content designer creating gamified learning experiences.");
        prompt.AppendLine("Your task is to analyze syllabus content and generate engaging, progressive quest steps.");
        prompt.AppendLine();

        // User Context Section
        prompt.AppendLine("---");
        prompt.AppendLine();
        prompt.AppendLine("## Student Context");
        prompt.AppendLine();
        prompt.AppendLine(userContext);
        prompt.AppendLine();

        // Skills Section
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

            // Also provide JSON format for easier parsing by LLM
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

        // Syllabus Section
        prompt.AppendLine("---");
        prompt.AppendLine();
        prompt.AppendLine("## Syllabus Content");
        prompt.AppendLine();
        prompt.AppendLine("Use this syllabus content as the foundation for creating quest steps:");
        prompt.AppendLine();
        prompt.AppendLine("```json");
        prompt.AppendLine(syllabusJson);
        prompt.AppendLine("```");
        prompt.AppendLine();

        // Instructions Section
        prompt.AppendLine("---");
        prompt.AppendLine();
        prompt.AppendLine("## Generation Instructions");
        prompt.AppendLine();
        prompt.AppendLine("### Requirements");
        prompt.AppendLine();
        prompt.AppendLine("1. **Quantity:** Generate 10 steps, each step represents 1 week.");
        prompt.AppendLine("2. **Progression:** Steps should follow a logical learning progression (easy to hard)");
        prompt.AppendLine("3. **Variety:** Use diverse step types to maintain engagement");
        prompt.AppendLine("4. **Personalization:** Consider the student's context, level, and class information");
        prompt.AppendLine("5. **Skill Mapping:** Each step must target exactly ONE skill from the pre-approved list");
        prompt.AppendLine();

        prompt.AppendLine("### Step Types and Content Schemas");
        prompt.AppendLine();
        prompt.AppendLine("Each quest step must conform to one of these exact types:");
        prompt.AppendLine();

        prompt.AppendLine("#### 1. Reading");
        prompt.AppendLine("Used for: Foundational knowledge, articles, documentation");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 1,");
        prompt.AppendLine("  \"title\": \"Introduction to Topic\",");
        prompt.AppendLine("  \"description\": \"Read about the fundamental concepts\",");
        prompt.AppendLine("  \"stepType\": \"Reading\",");
        prompt.AppendLine("  \"experiencePoints\": 15,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine("    \"articleTitle\": \"Title of the reading material\",");
        prompt.AppendLine("    \"summary\": \"Brief overview of what will be learned\",");
        prompt.AppendLine("    \"url\": \"https://example.com/resource\"");
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
        prompt.AppendLine("Used for: Programming exercises, algorithm implementation");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 4,");
        prompt.AppendLine("  \"title\": \"Implement the Solution\",");
        prompt.AppendLine("  \"description\": \"Write code to solve the problem\",");
        prompt.AppendLine("  \"stepType\": \"Coding\",");
        prompt.AppendLine("  \"experiencePoints\": 40,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine("    \"challenge\": \"Problem description and requirements\",");
        prompt.AppendLine("    \"template\": \"// Starter code\\nfunction solve() {\\n  // Your code here\\n}\",");
        prompt.AppendLine("    \"expectedOutput\": \"Description or example of expected output\"");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();

        /*
        prompt.AppendLine("#### 5. Submission");
        prompt.AppendLine("Used for: Projects, essays, creative work requiring review");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 5,");
        prompt.AppendLine("  \"title\": \"Submit Your Work\",");
        prompt.AppendLine("  \"description\": \"Create and submit your project\",");
        prompt.AppendLine("  \"stepType\": \"Submission\",");
        prompt.AppendLine("  \"experiencePoints\": 50,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine("    \"challenge\": \"Project requirements and guidelines\",");
        prompt.AppendLine("    \"submissionFormat\": \"Description of required format (e.g., PDF, ZIP, GitHub link)\"");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        */

        /*
        prompt.AppendLine("#### 6. Reflection");
        prompt.AppendLine("Used for: Metacognition, learning review, concept synthesis");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"stepNumber\": 6,");
        prompt.AppendLine("  \"title\": \"Reflect on Learning\",");
        prompt.AppendLine("  \"description\": \"Think critically about what you've learned\",");
        prompt.AppendLine("  \"stepType\": \"Reflection\",");
        prompt.AppendLine("  \"experiencePoints\": 20,");
        prompt.AppendLine("  \"content\": {");
        prompt.AppendLine("    \"skillId\": \"<guid-from-approved-list>\",");
        prompt.AppendLine("    \"challenge\": \"Reflection task description\",");
        prompt.AppendLine("    \"reflectionPrompt\": \"Questions to guide reflection\",");
        prompt.AppendLine("    \"expectedOutcome\": \"What insights should the student gain\"");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");
        prompt.AppendLine();
        */

        // Critical Rules
        prompt.AppendLine("### Critical Rules");
        prompt.AppendLine();
        // Remove Submission and Reflection for now
        prompt.AppendLine("- **MUST** use only skillIds from the Pre-Approved Skills list");
        prompt.AppendLine("- **MUST** use exact stepType values: `Reading`, `Interactive`, `Quiz`, `Coding`");
        prompt.AppendLine("- **MUST** include all required fields for each content schema");
        prompt.AppendLine("- **MUST** set experiencePoints between 10-50 based on difficulty");
        prompt.AppendLine("- **MUST** return ONLY valid JSON array, no markdown formatting");
        prompt.AppendLine("- **NEVER** invent new skillIds or use IDs not in the approved list");
        prompt.AppendLine("- **NEVER** create custom stepTypes");
        prompt.AppendLine("- **NEVER** omit the skillId field from content objects");
        prompt.AppendLine();

        // Best Practices
        prompt.AppendLine("### Best Practices");
        prompt.AppendLine();
        prompt.AppendLine("- Start with foundational Reading or Interactive steps");
        prompt.AppendLine("- Include Quiz steps after introducing new concepts");
        prompt.AppendLine("- Use Coding steps for practical application (when relevant)");
        /*
        prompt.AppendLine("- Reserve Submission steps for comprehensive projects");
        prompt.AppendLine("- End with Reflection to consolidate learning");
        */
        prompt.AppendLine("- Vary difficulty progressively (easy → medium → hard)");
        prompt.AppendLine("- Keep titles concise and engaging (3-6 words)");
        prompt.AppendLine("- Make descriptions clear and motivating (10-20 words)");
        prompt.AppendLine();

        // Output Format
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
        // remove submission and reflection type for now
        prompt.AppendLine("    \"stepType\": \"Reading|Interactive|Quiz|Coding\",");
        prompt.AppendLine("    \"experiencePoints\": 10-50,");
        prompt.AppendLine("    \"content\": { /* schema based on stepType */ }");
        prompt.AppendLine("  }");
        prompt.AppendLine("]");
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("**IMPORTANT:** Return ONLY the JSON array. Do not include markdown code fences, explanations, or any other text.");

        return prompt.ToString();
    }
}
