// RogueLearn.User/src/RogueLearn.User.Application/Features/AcademicRecords/Commands/ExtractFapRecord/ExtractFapRecordCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using HtmlAgilityPack;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.AcademicRecords.Commands.ExtractFapRecord;

// This handler is responsible for the business logic of extracting data from FAP HTML.
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

        // Pre-process the HTML to isolate the relevant table content.
        var cleanText = PreprocessFapHtml(request.FapHtmlContent);
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            _logger.LogWarning("HTML pre-processing yielded no content.");
            throw new BadRequestException("Could not find a valid grade report table in the provided HTML.");
        }

        // Invoke the AI plugin to convert the text to structured JSON.
        var extractedJson = await _fapPlugin.ExtractFapRecordJsonAsync(cleanText, cancellationToken);
        if (string.IsNullOrWhiteSpace(extractedJson))
        {
            _logger.LogError("AI extraction failed to return valid JSON for the academic record.");
            throw new InvalidOperationException("AI extraction failed for the academic record.");
        }

        // Deserialize the JSON into our strongly-typed object.
        var fapData = JsonSerializer.Deserialize<FapRecordData>(extractedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (fapData == null)
        {
            _logger.LogError("Failed to deserialize the extracted academic JSON.");
            throw new BadRequestException("Failed to deserialize the extracted academic data.");
        }

        _logger.LogInformation("Successfully extracted academic data with {SubjectCount} subjects.", fapData.Subjects.Count);
        return fapData;
    }

    // This helper method uses HtmlAgilityPack to parse the HTML and find the specific grades table.
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
}