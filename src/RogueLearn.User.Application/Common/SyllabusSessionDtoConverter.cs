using System.Text.Json;
using System.Text.Json.Serialization;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Common;

public class SyllabusSessionDtoConverter : JsonConverter<SyllabusSessionDto>
{
    private static bool TryGetPropertyCaseInsensitive(JsonElement root, string name, out JsonElement value)
    {
        if (root.TryGetProperty(name, out value)) return true;

        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    public override SyllabusSessionDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;
            var session = new SyllabusSessionDto();

            // Parse SessionNumber (required)
            if (TryGetPropertyCaseInsensitive(root, "SessionNumber", out var sessionNumberElement))
            {
                if (sessionNumberElement.ValueKind == JsonValueKind.Number)
                {
                    session.SessionNumber = sessionNumberElement.GetInt32();
                }
            }

            // Parse Topic (optional)
            if (TryGetPropertyCaseInsensitive(root, "Topic", out var topicElement) &&
                topicElement.ValueKind != JsonValueKind.Null)
            {
                session.Topic = topicElement.GetString() ?? string.Empty;
            }

            // Parse Activities (optional)
            if (TryGetPropertyCaseInsensitive(root, "Activities", out var activitiesElement) &&
                activitiesElement.ValueKind == JsonValueKind.Array)
            {
                session.Activities = new List<string>();
                foreach (var item in activitiesElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        session.Activities.Add(item.GetString() ?? string.Empty);
                    }
                }
            }

            // Parse Readings (optional)
            if (TryGetPropertyCaseInsensitive(root, "Readings", out var readingsElement) &&
                readingsElement.ValueKind == JsonValueKind.Array)
            {
                session.Readings = new List<string>();
                foreach (var item in readingsElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        session.Readings.Add(item.GetString() ?? string.Empty);
                    }
                }
            }

            // Parse SuggestedUrl (optional) with robust error handling
            if (TryGetPropertyCaseInsensitive(root, "SuggestedUrl", out var suggestedUrlElement))
            {
                if (suggestedUrlElement.ValueKind == JsonValueKind.String)
                {
                    var raw = suggestedUrlElement.GetString();
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        session.SuggestedUrl = raw.Replace("`", string.Empty).Trim();
                    }
                }
                else if (suggestedUrlElement.ValueKind == JsonValueKind.Object)
                {
                    session.SuggestedUrl = null;
                }
            }

            return session;
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        SyllabusSessionDto value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}