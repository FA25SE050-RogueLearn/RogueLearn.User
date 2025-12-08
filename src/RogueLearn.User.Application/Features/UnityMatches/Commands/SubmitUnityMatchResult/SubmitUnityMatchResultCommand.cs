using System.Text.Json;
using MediatR;

namespace RogueLearn.User.Application.Features.UnityMatches.Commands.SubmitUnityMatchResult;

public sealed class SubmitUnityMatchResultCommand : IRequest<SubmitUnityMatchResultResponse>
{
    public string MatchId { get; init; } = string.Empty;
    public string? Result { get; init; }
    public string? JoinCode { get; init; }
    public string? Scene { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public int TotalPlayers { get; init; }
    public Guid? UserId { get; init; }
    public string RawJson { get; init; } = string.Empty;

    public static SubmitUnityMatchResultCommand FromPayload(JsonElement payload)
    {
        var hasObject = payload.ValueKind == JsonValueKind.Object;
        string rawJson;
        try { rawJson = payload.GetRawText(); } catch { rawJson = "{}"; }

        string? GetString(string name)
        {
            if (!hasObject) return null;
            return payload.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
                ? el.GetString()
                : null;
        }

        DateTime GetDate(string name, DateTime fallback)
        {
            if (hasObject && payload.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(el.GetString(), out var parsed))
            {
                return parsed;
            }
            return fallback;
        }

        int GetInt(string name, int fallback)
        {
            if (hasObject && payload.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var val))
            {
                return val;
            }
            return fallback;
        }

        Guid? GetGuid(string name)
        {
            var str = GetString(name);
            if (!string.IsNullOrWhiteSpace(str) && Guid.TryParse(str, out var guid))
            {
                return guid;
            }
            return null;
        }

        var start = GetDate("startUtc", DateTime.UtcNow.AddMinutes(-5));
        var end = GetDate("endUtc", DateTime.UtcNow);
        var totalPlayers = GetInt("totalPlayers", 0);
        if (totalPlayers == 0 && hasObject)
        {
            try
            {
                if (payload.TryGetProperty("per_player", out var pp) && pp.ValueKind == JsonValueKind.Array)
                {
                    totalPlayers = pp.GetArrayLength();
                }
                else if (payload.TryGetProperty("playerSummaries", out var ps) && ps.ValueKind == JsonValueKind.Array)
                {
                    totalPlayers = ps.GetArrayLength();
                }
            }
            catch
            {
                totalPlayers = 0;
            }
        }

        return new SubmitUnityMatchResultCommand
        {
            MatchId = GetString("matchId") ?? Guid.NewGuid().ToString(),
            Result = GetString("result") ?? "lose",
            JoinCode = GetString("joinCode"),
            Scene = GetString("scene") ?? "unknown",
            StartUtc = start,
            EndUtc = end,
            TotalPlayers = totalPlayers,
            UserId = GetGuid("userId"),
            RawJson = rawJson
        };
    }
}
