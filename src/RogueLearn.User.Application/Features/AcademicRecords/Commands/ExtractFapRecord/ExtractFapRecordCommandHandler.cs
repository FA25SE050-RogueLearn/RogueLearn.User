// RogueLearn.User/src/RogueLearn.User.Application/Features/AcademicRecords/Commands/ExtractFapRecord/ExtractFapRecordCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using HtmlAgilityPack;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.AcademicRecords.Commands.ExtractFapRecord;

public class ExtractFapRecordCommandHandler : IRequestHandler<ExtractFapRecordCommand, FapRecordData>
{
    private readonly IFapExtractionPlugin _fapPlugin;
    private readonly ILogger<ExtractFapRecordCommandHandler> _logger;

    public ExtractFapRecordCommandHandler(IFapExtractionPlugin fapPlugin, ILogger<ExtractFapRecordCommandHandler> logger)
    {
        _fapPlugin = fapPlugin;
        _logger = logger;
    }

    public async Task<FapRecordData> Handle(ExtractFapRecordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Beginning FAP HTML extraction.");

        var cleanText = PreprocessFapHtml(request.FapHtmlContent);
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            _logger.LogWarning("HTML pre-processing yielded no content.");
            throw new BadRequestException("Could not find a valid grade report table in the provided HTML.");
        }

        var extractedJson = await _fapPlugin.ExtractFapRecordJsonAsync(cleanText, cancellationToken);
        if (string.IsNullOrWhiteSpace(extractedJson))
        {
            _logger.LogError("AI extraction failed to return valid JSON for the academic record.");
            throw new InvalidOperationException("AI extraction failed for the academic record.");
        }

        var fapData = JsonSerializer.Deserialize<FapRecordData>(extractedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (fapData == null)
        {
            _logger.LogError("Failed to deserialize the extracted academic JSON.");
            throw new BadRequestException("Failed to deserialize the extracted academic data.");
        }

        // SAFETY NET: Calculate GPA if AI didn't provide it or returned 0
        if (!fapData.Gpa.HasValue || fapData.Gpa.Value == 0)
        {
            _logger.LogInformation("GPA not calculated by AI or is zero. Computing fallback GPA...");
            fapData.Gpa = CalculateFallbackGpa(fapData.Subjects);
            _logger.LogInformation("Fallback GPA calculated: {Gpa}", fapData.Gpa?.ToString("F2") ?? "null");
        }

        _logger.LogInformation("Successfully extracted academic data with {SubjectCount} subjects and GPA: {Gpa}.",
            fapData.Subjects.Count,
            fapData.Gpa?.ToString("F2") ?? "N/A");

        return fapData;
    }

    private string PreprocessFapHtml(string rawHtml)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(rawHtml);
        var gradeTableNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='Grid']//table[contains(@class, 'table-hover')][1]");

        if (gradeTableNode == null)
        {
            return string.Empty;
        }

        return gradeTableNode.InnerText;
    }

    /// <summary>
    /// Fallback GPA calculation in case the AI doesn't calculate it.
    /// Uses weighted average: GPA = Σ(mark × credits) / Σ(credits)
    /// </summary>
    private double? CalculateFallbackGpa(List<FapSubjectData> subjects)
    {
        var passedSubjects = subjects.Where(s =>
            s.Status.Equals("Passed", StringComparison.OrdinalIgnoreCase) &&
            s.Mark.HasValue &&
            s.Mark.Value > 0).ToList();

        if (!passedSubjects.Any())
        {
            _logger.LogInformation("No passed subjects with valid marks found for GPA calculation.");
            return null;
        }

        double totalWeightedMarks = 0;
        int totalCredits = 0;

        foreach (var subject in passedSubjects)
        {
            int credits = EstimateCredits(subject.SubjectCode);

            // Skip subjects with 0 credits (like TRS601)
            if (credits == 0)
            {
                _logger.LogDebug("Skipping {SubjectCode} from GPA calculation (0 credits)", subject.SubjectCode);
                continue;
            }

            totalWeightedMarks += subject.Mark!.Value * credits;
            totalCredits += credits;

            _logger.LogDebug("GPA calculation: {SubjectCode} = {Mark} × {Credits} credits",
                subject.SubjectCode, subject.Mark.Value, credits);
        }

        if (totalCredits == 0)
        {
            _logger.LogWarning("Total credits is 0 after filtering. Cannot calculate GPA.");
            return null;
        }

        var gpa = Math.Round(totalWeightedMarks / totalCredits, 2);
        _logger.LogInformation("Calculated GPA: {TotalWeightedMarks} / {TotalCredits} = {Gpa}",
            totalWeightedMarks, totalCredits, gpa);

        return gpa;
    }

    /// <summary>
    /// Estimates the credit value for a subject based on its code pattern.
    /// This matches the credit system used in FPT University.
    /// </summary>
    private int EstimateCredits(string subjectCode)
    {
        // Special cases - 0 credits (excluded from GPA)
        if (subjectCode.StartsWith("TRS", StringComparison.OrdinalIgnoreCase))
        {
            return 0; // Excluded from GPA calculation
        }

        // 1 credit subjects
        if (subjectCode.StartsWith("LAB", StringComparison.OrdinalIgnoreCase))
        {
            return 1; // Lab courses
        }

        // 2 credit subjects
        if (subjectCode.StartsWith("VOV", StringComparison.OrdinalIgnoreCase) || // English
            subjectCode.StartsWith("SSL", StringComparison.OrdinalIgnoreCase) || // Soft skills
            subjectCode.StartsWith("SSG", StringComparison.OrdinalIgnoreCase) || // Soft skills
            subjectCode.StartsWith("OTP", StringComparison.OrdinalIgnoreCase))   // Physical education
        {
            return 2;
        }

        // Default: 3 credits for most subjects
        return 3;
    }
}