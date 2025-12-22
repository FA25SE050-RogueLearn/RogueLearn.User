using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RogueLearn.User.Application.Models;
using System.Text.Json;
using System.Text;

namespace RogueLearn.User.Application.Plugins;

public class AcademicAnalysisPlugin : IAcademicAnalysisPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<AcademicAnalysisPlugin> _logger;

    public AcademicAnalysisPlugin(Kernel kernel, ILogger<AcademicAnalysisPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<AcademicAnalysisReport> AnalyzePerformanceAsync(
        List<FapSubjectData> extractedGrades,
        Dictionary<string, string> subjectNames,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze the following student transcript and identify their technical strengths, weaknesses, and a 'Student Persona'.");
        sb.AppendLine();
        sb.AppendLine("## TRANSCRIPT DATA");

        foreach (var grade in extractedGrades)
        {
            var name = subjectNames.TryGetValue(grade.SubjectCode, out var n) ? n : "Unknown Subject";
            var score = grade.Mark.HasValue ? grade.Mark.Value.ToString("F1") : "N/A";
            var status = grade.Status;

            sb.AppendLine($"- {grade.SubjectCode}: {name} | Grade: {score} | Status: {status}");
        }

        sb.AppendLine();
        sb.AppendLine("## INSTRUCTIONS");
        sb.AppendLine("1. **Student Persona:** Create a professional title for this student based on their strong subjects (e.g., 'Backend Specialist', 'Data Science Enthusiast', 'Generalist Developer').");
        sb.AppendLine("2. **Strong Areas:** Identify 2-4 areas/skills where the student excels (High grades). Look at the subject names to infer skills (e.g., High grade in 'Java Web' -> Strong in 'Backend Development').");
        sb.AppendLine("3. **Weak Areas:** Identify 2-4 areas where the student struggles (Low grades, Failed, or Not Started if they should have started).");
        sb.AppendLine("4. **Recommendations:** Give 1-2 sentence advice on what to focus on next.");
        sb.AppendLine();
        sb.AppendLine("## OUTPUT FORMAT (JSON ONLY)");
        sb.AppendLine("{");
        sb.AppendLine("  \"studentPersona\": \"string\",");
        sb.AppendLine("  \"strongAreas\": [\"string\", \"string\"],");
        sb.AppendLine("  \"weakAreas\": [\"string\", \"string\"],");
        sb.AppendLine("  \"recommendations\": \"string\"");
        sb.AppendLine("}");

        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddUserMessage(sb.ToString());

            var result = await chatService.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
            var json = CleanJson(result?.Content ?? "{}");

            var report = JsonSerializer.Deserialize<AcademicAnalysisReport>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return report ?? new AcademicAnalysisReport();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze academic performance.");
            return new AcademicAnalysisReport
            {
                StudentPersona = "Student",
                Recommendations = "Complete more courses to generate a personalized analysis."
            };
        }
    }

    private string CleanJson(string raw)
    {
        var cleaned = raw.Trim();
        if (cleaned.StartsWith("```json")) cleaned = cleaned.Substring(7);
        if (cleaned.StartsWith("```")) cleaned = cleaned.Substring(3);
        if (cleaned.EndsWith("```")) cleaned = cleaned.Substring(0, cleaned.Length - 3);
        return cleaned.Trim();
    }
}