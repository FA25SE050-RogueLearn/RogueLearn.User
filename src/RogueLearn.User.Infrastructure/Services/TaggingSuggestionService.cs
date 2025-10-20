using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Shared.Extensions; // ToSlug
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Infrastructure.Services;

public class TaggingSuggestionService : ITaggingSuggestionService
{
    private readonly ITagSuggestionPlugin _plugin;
    private readonly ITagRepository _tagRepository;

    public TaggingSuggestionService(ITagSuggestionPlugin plugin, ITagRepository tagRepository)
    {
        _plugin = plugin;
        _tagRepository = tagRepository;
    }

    public async Task<IReadOnlyList<TagSuggestionDto>> SuggestAsync(Guid authUserId, string rawText, int maxTags = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return Array.Empty<TagSuggestionDto>();

        var json = await _plugin.GenerateTagSuggestionsJsonAsync(rawText, maxTags, cancellationToken);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var payload = SafeDeserialize(json, options);
        var items = payload?.Tags ?? new List<TagItem>();

        // Normalize labels and dedupe by slug
        var normalized = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .Select(x => new { Item = x, Slug = x.Label.Trim().ToSlug() })
            .GroupBy(x => x.Slug)
            .Select(g => g.OrderByDescending(x => x.Item.Confidence).First().Item)
            .OrderByDescending(x => x.Confidence)
            .Take(Math.Max(1, Math.Min(10, maxTags)))
            .ToList();

        // Fetch user's tags and map by slug
        var userTags = await _tagRepository.FindAsync(t => t.AuthUserId == authUserId, cancellationToken);
        var tagMap = userTags.ToDictionary(t => t.Name.ToSlug(), t => t);

        var results = new List<TagSuggestionDto>(normalized.Count);
        foreach (var s in normalized)
        {
            var slug = s.Label.Trim().ToSlug();
            if (tagMap.TryGetValue(slug, out var existing))
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
                    Label = s.Label.Trim(),
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
}