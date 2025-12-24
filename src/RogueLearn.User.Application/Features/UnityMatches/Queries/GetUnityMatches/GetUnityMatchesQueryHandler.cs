using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums; // For SkillRewardSourceType
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UnityMatches.Queries.GetUnityMatches;

public class GetUnityMatchesQueryHandler : IRequestHandler<GetUnityMatchesQuery, string>
{
    private readonly IMatchResultRepository _matchResultRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IMatchPlayerSummaryRepository _matchPlayerSummaryRepository;
    private readonly IUserSkillRewardRepository _userSkillRewardRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly ILogger<GetUnityMatchesQueryHandler> _logger;

    public GetUnityMatchesQueryHandler(
        IMatchResultRepository matchResultRepository,
        IGameSessionRepository gameSessionRepository,
        IMatchPlayerSummaryRepository matchPlayerSummaryRepository,
        IUserSkillRewardRepository userSkillRewardRepository,
        ISkillRepository skillRepository,
        ISubjectRepository subjectRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        ILogger<GetUnityMatchesQueryHandler> logger)
    {
        _matchResultRepository = matchResultRepository;
        _gameSessionRepository = gameSessionRepository;
        _matchPlayerSummaryRepository = matchPlayerSummaryRepository;
        _userSkillRewardRepository = userSkillRewardRepository;
        _skillRepository = skillRepository;
        _subjectRepository = subjectRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
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

                GameSession? session = null;

                if (needsQuestions)
                {
                    session = await GetSessionForMatchAsync(m, cancellationToken);

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
                        // Use CodeBattle enum because Unity matches are CodeBattle
                        var rewards = await _userSkillRewardRepository.GetBySourceAllAsync(
                            rewardUserId.Value,
                            SkillRewardSourceType.CodeBattle,
                            m.Id,
                            cancellationToken: cancellationToken);

                        if (rewards != null && rewards.Any())
                        {
                            // Fetch skill names needed for these rewards
                            var skillIds = rewards.Select(r => r.SkillId).Distinct().ToList();
                            // In a real optimized scenario, we'd use GetByIdsAsync, but assuming GetAllAsync is cached/fast or we iterate.
                            // Since we don't have GetByIdsAsync in ISkillRepository interface based on context, we might fetch one by one or assume small set.
                            // But usually GenericRepository has GetAll. Let's use GetAll and filter in memory if GetByIds isn't available.
                            var allSkills = await _skillRepository.GetAllAsync(cancellationToken);
                            var skillMap = allSkills.Where(s => skillIds.Contains(s.Id)).ToDictionary(s => s.Id, s => s.Name);

                            matchData = AttachRewards(matchData, rewards, skillMap);
                        }
                        else
                        {
                            if (session == null)
                            {
                                session = await GetSessionForMatchAsync(m, cancellationToken);
                            }
                            matchData = await AttachFallbackComputedXpAsync(matchData, rewardUserId.Value, m.TotalPlayers, session, cancellationToken);
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

    private static string AttachRewards(string matchData, IEnumerable<UserSkillReward> rewards, Dictionary<Guid, string> skillMap)
    {
        var node = JsonNode.Parse(string.IsNullOrWhiteSpace(matchData) ? "{}" : matchData) as JsonObject ?? new JsonObject();
        var xpArr = new JsonArray();
        foreach (var r in rewards)
        {
            var skillName = skillMap.TryGetValue(r.SkillId, out var name) ? name : "Unknown Skill";
            var obj = new JsonObject
            {
                ["skillId"] = r.SkillId.ToString(),
                ["skillName"] = skillName,
                ["pointsAwarded"] = r.PointsAwarded
            };
            xpArr.Add(obj);
        }
        node["xpRewards"] = xpArr;
        node["xpTotal"] = rewards.Sum(r => r.PointsAwarded);
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    // Fallback: compute XP purely from match data topics when no persisted rewards exist.
    private async Task<string> AttachFallbackComputedXpAsync(string matchData, Guid rewardUserId, int totalPlayers, GameSession? session, CancellationToken cancellationToken)
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

            // Attempt to map using session subject first
            if (session != null && !string.IsNullOrWhiteSpace(session.Subject))
            {
                var subject = await _subjectRepository.GetByCodeAsync(session.Subject, cancellationToken);
                if (subject != null)
                {
                    var mappings = await _subjectSkillMappingRepository.GetMappingsBySubjectIdsAsync(new[] { subject.Id }, cancellationToken);
                    if (mappings.Any())
                    {
                        var allSkills = await _skillRepository.GetAllAsync(cancellationToken);
                        var skillMap = allSkills.ToDictionary(s => s.Id, s => s.Name);

                        var xpArr = new JsonArray();
                        var totalWeight = mappings.Sum(m => m.RelevanceWeight);
                        if (totalWeight <= 0) totalWeight = mappings.Count();

                        int awardedTotal = 0;
                        foreach (var mapping in mappings)
                        {
                            if (!skillMap.TryGetValue(mapping.SkillId, out var skillName)) continue;
                            var portion = (double)mapping.RelevanceWeight / (double)totalWeight;
                            var points = (int)Math.Round(totalXp * portion, MidpointRounding.AwayFromZero);
                            if (points <= 0) continue;

                            awardedTotal += points;
                            xpArr.Add(new JsonObject
                            {
                                ["skillName"] = skillName,
                                ["pointsAwarded"] = points
                            });
                        }

                        if (xpArr.Count > 0)
                        {
                            root["xpRewards"] = xpArr;
                            root["xpTotal"] = awardedTotal;
                            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
                        }
                    }
                }
            }

            // If no mappings found, return matchData without "fake" topic rewards.
            return matchData;
        }
        catch
        {
            // ignore fallback errors
        }

        return matchData;
    }

    private async Task<GameSession?> GetSessionForMatchAsync(MatchResult m, CancellationToken cancellationToken)
    {
        GameSession? session = null;

        if (m.Id != Guid.Empty)
        {
            session = await _gameSessionRepository.GetByMatchResultIdAsync(m.Id, cancellationToken);
        }

        if (session == null && m.MatchId != Guid.Empty)
        {
            session = await _gameSessionRepository.GetBySessionIdAsync(m.MatchId, cancellationToken);
        }

        if (session == null && m.UserId.HasValue)
        {
            var recentSessions = await _gameSessionRepository.GetRecentSessionsByUserAsync(m.UserId.Value, 10, cancellationToken);
            session = recentSessions.FirstOrDefault();
        }

        return session;
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