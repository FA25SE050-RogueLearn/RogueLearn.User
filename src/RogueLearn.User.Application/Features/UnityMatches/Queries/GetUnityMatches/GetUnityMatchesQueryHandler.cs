using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UnityMatches.Queries.GetUnityMatches;

public class GetUnityMatchesQueryHandler : IRequestHandler<GetUnityMatchesQuery, string>
{
    private readonly IMatchResultRepository _matchResultRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IMatchPlayerSummaryRepository _matchPlayerSummaryRepository;
    private readonly IUserSkillRewardRepository _userSkillRewardRepository;
    private readonly ILogger<GetUnityMatchesQueryHandler> _logger;

    public GetUnityMatchesQueryHandler(
        IMatchResultRepository matchResultRepository,
        IGameSessionRepository gameSessionRepository,
        IMatchPlayerSummaryRepository matchPlayerSummaryRepository,
        IUserSkillRewardRepository userSkillRewardRepository,
        ILogger<GetUnityMatchesQueryHandler> logger)
    {
        _matchResultRepository = matchResultRepository;
        _gameSessionRepository = gameSessionRepository;
        _matchPlayerSummaryRepository = matchPlayerSummaryRepository;
        _userSkillRewardRepository = userSkillRewardRepository;
        _logger = logger;
    }

    public async Task<string> Handle(GetUnityMatchesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            Guid? userIdGuid = null;
            if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var parsedUserId))
            {
                userIdGuid = parsedUserId;
            }

            List<MatchResult> matchResults;
            if (userIdGuid.HasValue)
            {
                var matchResultIds = await _matchPlayerSummaryRepository.GetRecentMatchResultIdsByUserAsync(userIdGuid.Value, request.Limit);
                var list = new List<MatchResult>();
                foreach (var id in matchResultIds)
                {
                    var mr = await _matchResultRepository.GetByIdAsync(id, cancellationToken);
                    if (mr != null) list.Add(mr);
                }

                if (list.Count == 0)
                {
                    list = await _matchResultRepository.GetMatchesByUserAsync(userIdGuid.Value, request.Limit, cancellationToken);
                }

                matchResults = list
                    .OrderByDescending(m => m.StartUtc)
                    .Take(request.Limit)
                    .ToList();
            }
            else
            {
                matchResults = await _matchResultRepository.GetRecentMatchesAsync(request.Limit, cancellationToken);
            }

            var matchesJson = new List<string>();
            foreach (var m in matchResults.Where(m => !string.IsNullOrWhiteSpace(m.MatchDataJson)))
            {
                var matchData = m.MatchDataJson!;
                bool needsQuestions = false;

                try
                {
                    var node = JsonNode.Parse(matchData) as JsonObject;
                    needsQuestions = node == null || QuestionsMissing(node) || node["questionPack"] == null;
                }
                catch
                {
                    needsQuestions = true;
                }

                if (needsQuestions)
                {
                    GameSession? session = null;

                    if (m.Id != Guid.Empty)
                    {
                        session = await _gameSessionRepository.GetByMatchResultIdAsync(m.Id, cancellationToken);
                    }

                    if (session == null && Guid.TryParse(m.MatchId, out var sessionGuid))
                    {
                        session = await _gameSessionRepository.GetBySessionIdAsync(sessionGuid, cancellationToken);
                    }

                    if (session == null && m.UserId.HasValue)
                    {
                        var recentSessions = await _gameSessionRepository.GetRecentSessionsByUserAsync(m.UserId.Value, 10, cancellationToken);
                        session = recentSessions.FirstOrDefault();
                    }

                    if (session != null && !string.IsNullOrWhiteSpace(session.QuestionPackJson))
                    {
                        matchData = MergeQuestionPackIntoMatchData(matchData, session.QuestionPackJson, session.SessionId);
                    }
                }

                Guid? rewardUserId = null;
                if (userIdGuid.HasValue)
                {
                    rewardUserId = userIdGuid.Value;
                }
                else if (m.UserId.HasValue)
                {
                    rewardUserId = m.UserId.Value;
                }

                if (rewardUserId.HasValue)
                {
                    try
                    {
                        var rewards = await _userSkillRewardRepository.GetBySourceAllAsync(
                            rewardUserId.Value,
                            "UserService",
                            m.Id,
                            cancellationToken: cancellationToken);

                        if (rewards != null && rewards.Any())
                        {
                            matchData = AttachRewards(matchData, rewards);
                        }
                        else
                        {
                            matchData = AttachFallbackComputedXp(matchData, rewardUserId.Value, m.TotalPlayers);
                        }
                    }
                    catch (Exception xpEx)
                    {
                        _logger.LogWarning(xpEx, "[Stats] Failed to enrich match {MatchId} with XP rewards for user {UserId}", m.MatchId, userIdGuid);
                    }
                }

                matchesJson.Add(matchData);
            }

            var responseJson = "{\"matches\":[" + string.Join(",", matchesJson) + "]}";
            return responseJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Stats] Failed to read Unity matches from database");
            throw;
        }
    }

    private static bool QuestionsMissing(JsonObject matchNode)
    {
        if (!matchNode.TryGetPropertyValue("questions", out var questionsNode)) return true;
        if (questionsNode is JsonArray arr) return arr.Count == 0;
        return true;
    }

    private static string AttachRewards(string matchData, IEnumerable<UserSkillReward> rewards)
    {
        var node = JsonNode.Parse(string.IsNullOrWhiteSpace(matchData) ? "{}" : matchData) as JsonObject ?? new JsonObject();
        var xpArr = new JsonArray();
        foreach (var r in rewards)
        {
            var obj = new JsonObject
            {
                ["skillId"] = r.SkillId.ToString(),
                ["skillName"] = r.SkillName,
                ["pointsAwarded"] = r.PointsAwarded
            };
            xpArr.Add(obj);
        }
        node["xpRewards"] = xpArr;
        node["xpTotal"] = rewards.Sum(r => r.PointsAwarded);
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    // Fallback: compute XP purely from match data topics when no persisted rewards exist.
    private static string AttachFallbackComputedXp(string matchData, Guid rewardUserId, int totalPlayers)
    {
        try
        {
            var root = JsonNode.Parse(string.IsNullOrWhiteSpace(matchData) ? "{}" : matchData) as JsonObject ?? new JsonObject();
            var summaries = root["playerSummaries"] as JsonArray;
            if (summaries == null || summaries.Count == 0) return matchData;

            var targetSummary = summaries
                .OfType<JsonObject>()
                .FirstOrDefault(s => string.Equals(s["userId"]?.ToString(), rewardUserId.ToString(), StringComparison.OrdinalIgnoreCase));

            if (targetSummary == null) return matchData;

            var totalQuestions = ToInt(targetSummary["totalQuestions"]);
            var correctAnswers = ToInt(targetSummary["correctAnswers"]);
            var baseXp = (totalQuestions * 5) + (correctAnswers * 5);
            var resultStr = root["result"]?.ToString()?.Trim().ToLowerInvariant();
            var resultFactor = resultStr == "win" ? 1.15 : 0.9;
            var teamFactor = 1 + Math.Min(0.05 * Math.Max(totalPlayers - 1, 0), 0.2);
            var totalXp = (int)Math.Round(baseXp * resultFactor * teamFactor, MidpointRounding.AwayFromZero);
            if (totalXp <= 0) return matchData;

            var topics = targetSummary["topicBreakdown"] as JsonArray;
            if (topics == null || topics.Count == 0) return matchData;

            var topicRows = topics
                .OfType<JsonObject>()
                .Select(t => new
                {
                    topic = t["topic"]?.ToString() ?? "Unknown",
                    total = ToInt(t["total"])
                })
                .Where(t => t.total > 0)
                .ToList();

            var topicTotal = topicRows.Sum(t => t.total);
            if (topicTotal <= 0) return matchData;

            var topicPortion = (int)Math.Round(totalXp * 0.6, MidpointRounding.AwayFromZero);
            var xpArr = new JsonArray();
            int awardedTotal = 0;

            foreach (var row in topicRows)
            {
                var share = Math.Clamp((double)row.total / topicTotal, 0, 1);
                var topicXp = (int)Math.Round(topicPortion * share, MidpointRounding.AwayFromZero);
                if (topicXp <= 0) continue;
                awardedTotal += topicXp;
                xpArr.Add(new JsonObject
                {
                    ["skillName"] = row.topic,
                    ["pointsAwarded"] = topicXp
                });
            }

            if (xpArr.Count > 0)
            {
                root["xpRewards"] = xpArr;
                root["xpTotal"] = awardedTotal;
                return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            }
        }
        catch
        {
            // ignore fallback errors
        }

        return matchData;
    }

    private static int ToInt(JsonNode? node)
    {
        if (node == null) return 0;
        if (node is JsonValue v && v.TryGetValue<int>(out var val)) return val;
        if (int.TryParse(node.ToString(), out var parsed)) return parsed;
        return 0;
    }

    private static string MergeQuestionPackIntoMatchData(string? matchDataJson, string? questionPackJson, Guid sessionId)
    {
        var matchNode = JsonNode.Parse(string.IsNullOrWhiteSpace(matchDataJson) ? "{}" : matchDataJson) as JsonObject ?? new JsonObject();
        matchNode["sessionId"] = sessionId.ToString();

        if (!string.IsNullOrWhiteSpace(questionPackJson))
        {
            var packNode = JsonNode.Parse(questionPackJson);
            if (packNode is JsonObject packObj)
            {
                if (matchNode["questionPack"] == null)
                {
                    matchNode["questionPack"] = packObj.DeepClone();
                }

                if (QuestionsMissing(matchNode) && packObj["questions"] is JsonArray qArr)
                {
                    matchNode["questions"] = qArr.DeepClone();
                }
            }
        }

        return matchNode.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }
}
