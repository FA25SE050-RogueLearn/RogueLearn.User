using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Shared.Extensions; // ToSlug
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Infrastructure.Services;

public class TaggingSuggestionService : ITaggingSuggestionService
{
    private readonly ITagSuggestionPlugin _plugin;
    private readonly IFileTagSuggestionPlugin _filePlugin;
    private readonly ITagRepository _tagRepository;
    private readonly ILogger<TaggingSuggestionService> _logger;

    public TaggingSuggestionService(ITagSuggestionPlugin plugin, IFileTagSuggestionPlugin filePlugin, ITagRepository tagRepository, ILogger<TaggingSuggestionService> logger)
    {
        _plugin = plugin;
        _filePlugin = filePlugin;
        _tagRepository = tagRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TagSuggestionDto>> SuggestAsync(Guid authUserId, string rawText, int maxTags = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return Array.Empty<TagSuggestionDto>();

        var json = await _plugin.GenerateTagSuggestionsJsonAsync(rawText, maxTags, cancellationToken);
        return await MapToSuggestionsAsync(authUserId, json, maxTags, cancellationToken);
    }

    public async Task<IReadOnlyList<TagSuggestionDto>> SuggestFromFileAsync(Guid authUserId, AiFileAttachment attachment, int maxTags = 10, CancellationToken cancellationToken = default)
    {
        if (attachment == null || ((attachment.Bytes == null || attachment.Bytes.Length == 0) && attachment.Stream is null))
            return Array.Empty<TagSuggestionDto>();

        var userTags = await _tagRepository.FindAsync(t => t.AuthUserId == authUserId, cancellationToken);
        var knownNames = userTags.Select(t => t.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var json = await _filePlugin.GenerateTagSuggestionsJsonAsync(attachment, knownNames, maxTags, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogWarning("File-based tag suggestion returned empty JSON. FileName={FileName}, ContentType={ContentType}", attachment.FileName, attachment.ContentType);
            return Array.Empty<TagSuggestionDto>();
        }
        return await MapToSuggestionsAsync(authUserId, json, maxTags, cancellationToken);
    }

    private async Task<IReadOnlyList<TagSuggestionDto>> MapToSuggestionsAsync(Guid authUserId, string json, int maxTags, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var payload = SafeDeserialize(json, options);
        var items = payload?.Tags ?? new List<TagItem>();

        // Normalize labels (singularize, trim) and dedupe by normalized slug
        var normalized = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .Select(x => new { Item = x, Slug = NormalizeLabel(x.Label).ToSlug() })
            .GroupBy(x => x.Slug)
            .Select(g => g.OrderByDescending(x => x.Item.Confidence).First().Item)
            .OrderByDescending(x => x.Confidence)
            .Take(Math.Max(1, Math.Min(10, maxTags)))
            .ToList();

        var userTags = await _tagRepository.FindAsync(t => t.AuthUserId == authUserId, cancellationToken);
        var tagMap = userTags.ToDictionary(t => NormalizeLabel(t.Name).ToSlug(), t => t);

        var results = new List<TagSuggestionDto>(normalized.Count);
        const double matchThreshold = 0.8;
        foreach (var s in normalized)
        {
            var slug = NormalizeLabel(s.Label).ToSlug();
            if (tagMap.TryGetValue(slug, out var existing) && s.Confidence >= matchThreshold)
            {
                results.Add(new TagSuggestionDto
                {
                    Label = existing.Name,
                    Confidence = s.Confidence,
                    Reason = s.Reason ?? string.Empty,
                    MatchedTagId = existing.Id,
                    MatchedTagName = existing.Name
                });
            }
            else
            {
                results.Add(new TagSuggestionDto
                {
                    Label = NormalizeLabel(s.Label).Trim().ToPascalCase(),
                    Confidence = s.Confidence,
                    Reason = s.Reason ?? string.Empty
                });
            }
        }

        return results;
    }

    private static TagPayload? SafeDeserialize(string json, JsonSerializerOptions options)
    {
        try
        {
            return JsonSerializer.Deserialize<TagPayload>(json, options);
        }
        catch
        {
            // Attempt to unwrap raw array fallback
            try
            {
                var arr = JsonSerializer.Deserialize<List<TagItem>>(json, options) ?? new();
                return new TagPayload { Tags = arr };
            }
            catch
            {
                return null;
            }
        }
    }

    private sealed class TagPayload
    {
        [JsonPropertyName("tags")] public List<TagItem> Tags { get; set; } = new();
    }

    private sealed class TagItem
    {
        [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
        [JsonPropertyName("confidence")] public double Confidence { get; set; } = 0.0;
        [JsonPropertyName("reason")] public string? Reason { get; set; }
    }

    private static string NormalizeLabel(string label)
    {
        var s = (label ?? string.Empty).Trim();
        if (s.Length == 0) return s;
        var lower = s.ToLowerInvariant();
        if (lower.EndsWith("ies") && lower.Length > 3)
            lower = lower[..^3] + "y";
        else if (lower.EndsWith("es") && lower.Length > 4)
            lower = lower[..^2];
        else if (lower.EndsWith("s") && lower.Length > 3)
            lower = lower[..^1];

        lower = lower.Replace(".net", "dotnet");
        lower = lower.Replace("c#", "csharp");

        return lower;
    }
}