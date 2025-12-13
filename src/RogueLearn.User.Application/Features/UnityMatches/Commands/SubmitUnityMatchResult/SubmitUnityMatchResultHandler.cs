using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase.Postgrest.Exceptions;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.UnityMatches.Commands.SubmitUnityMatchResult;

public sealed class SubmitUnityMatchResultHandler : IRequestHandler<SubmitUnityMatchResultCommand, SubmitUnityMatchResultResponse>
{
    private readonly IMatchResultRepository _matchResultRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IMatchPlayerSummaryRepository _matchPlayerSummaryRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<SubmitUnityMatchResultHandler> _logger;

    public SubmitUnityMatchResultHandler(
        IMatchResultRepository matchResultRepository,
        IGameSessionRepository gameSessionRepository,
        IMatchPlayerSummaryRepository matchPlayerSummaryRepository,
        ISkillRepository skillRepository,
        ISubjectRepository subjectRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        IMediator mediator,
        ILogger<SubmitUnityMatchResultHandler> logger)
    {
        _matchResultRepository = matchResultRepository;
        _gameSessionRepository = gameSessionRepository;
        _matchPlayerSummaryRepository = matchPlayerSummaryRepository;
        _skillRepository = skillRepository;
        _subjectRepository = subjectRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<SubmitUnityMatchResultResponse> Handle(SubmitUnityMatchResultCommand command, CancellationToken cancellationToken)
    {
        var matchId = string.IsNullOrWhiteSpace(command.MatchId) ? Guid.NewGuid().ToString() : command.MatchId;
        var result = NormalizeResult(command.Result);
        var scene = string.IsNullOrWhiteSpace(command.Scene) ? "unknown" : command.Scene!;
        var normalizedStart = NormalizeUtc(command.StartUtc == default ? DateTime.UtcNow.AddMinutes(-5) : command.StartUtc);
        var normalizedEnd = NormalizeUtc(command.EndUtc == default ? DateTime.UtcNow : command.EndUtc);

        GameSession? resolvedSession = null;
        MatchResult savedMatch;
        try
        {
            resolvedSession = await ResolveSessionForMatchAsync(matchId, command.UserId, command.JoinCode, normalizedEnd, cancellationToken);
            var incomingMatchData = resolvedSession != null
                ? MergeQuestionPackIntoMatchData(command.RawJson, resolvedSession.QuestionPackJson, resolvedSession.SessionId)
                : command.RawJson;

            MatchResult? existingMatch = null;
            if (!string.IsNullOrWhiteSpace(matchId))
            {
                try
                {
                    existingMatch = await _matchResultRepository.GetByMatchIdAsync(matchId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Unity Match] Lookup by matchId {MatchId} failed", matchId);
                }
            }

            var matchResult = new MatchResult
            {
                MatchId = matchId,
                StartUtc = normalizedStart,
                EndUtc = normalizedEnd,
                Result = result,
                Scene = scene,
                TotalPlayers = command.TotalPlayers,
                UserId = command.UserId,
                MatchDataJson = string.IsNullOrWhiteSpace(incomingMatchData) ? "{}" : incomingMatchData
            };

            if (existingMatch != null)
            {
                _logger.LogInformation("[Unity Match] Match {MatchId} already exists - merging player summaries", matchResult.MatchId);
                existingMatch.MatchDataJson = MergeMatchData(existingMatch.MatchDataJson, incomingMatchData);
                existingMatch.TotalPlayers = Math.Max(existingMatch.TotalPlayers, command.TotalPlayers);
                existingMatch.StartUtc = existingMatch.StartUtc == default
                    ? normalizedStart
                    : (existingMatch.StartUtc <= normalizedStart ? existingMatch.StartUtc : normalizedStart);
                existingMatch.EndUtc = existingMatch.EndUtc == default
                    ? normalizedEnd
                    : (existingMatch.EndUtc >= normalizedEnd ? existingMatch.EndUtc : normalizedEnd);
                existingMatch.Result = string.IsNullOrWhiteSpace(result) ? existingMatch.Result : result;
                existingMatch.Scene = string.IsNullOrWhiteSpace(scene) ? existingMatch.Scene : scene;
                if (!existingMatch.UserId.HasValue && command.UserId.HasValue)
                {
                    existingMatch.UserId = command.UserId;
                }
                savedMatch = await _matchResultRepository.UpdateAsync(existingMatch, cancellationToken);
            }
            else
            {
                try
                {
                    savedMatch = await _matchResultRepository.AddAsync(matchResult, cancellationToken);
                }
                catch (Exception ex) when (IsDuplicateMatchIdError(ex))
                {
                    _logger.LogWarning(ex, "[Unity Match] Duplicate match_id {MatchId} detected, loading existing and merging", matchResult.MatchId);
                    var existing = await _matchResultRepository.GetByMatchIdAsync(matchResult.MatchId, cancellationToken);
                    if (existing != null)
                    {
                        existing.MatchDataJson = MergeMatchData(existing.MatchDataJson, incomingMatchData);
                        existing.TotalPlayers = Math.Max(existing.TotalPlayers, command.TotalPlayers);
                        existing.StartUtc = existing.StartUtc == default
                            ? normalizedStart
                            : (existing.StartUtc <= normalizedStart ? existing.StartUtc : normalizedStart);
                        existing.EndUtc = existing.EndUtc == default
                            ? normalizedEnd
                            : (existing.EndUtc >= normalizedEnd ? existing.EndUtc : normalizedEnd);
                        existing.Result = string.IsNullOrWhiteSpace(result) ? existing.Result : result;
                        existing.Scene = string.IsNullOrWhiteSpace(scene) ? existing.Scene : scene;
                        if (!existing.UserId.HasValue && command.UserId.HasValue)
                        {
                            existing.UserId = command.UserId;
                        }
                        savedMatch = await _matchResultRepository.UpdateAsync(existing, cancellationToken);
                    }
                    else
                    {
                        savedMatch = matchResult;
                        _logger.LogWarning("[Unity Match] Insert failed and existing row not found for match_id {MatchId}; using incoming payload as fallback", matchResult.MatchId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Unity Match] Failed to upsert match {MatchId}", matchId);
            throw;
        }

        _logger.LogInformation("[Unity Match] Attempting to link match result {MatchResultId} to game session", savedMatch.Id);
        _logger.LogInformation("[Unity Match] MatchId: {MatchId}", savedMatch.MatchId);

        GameSession? linkedSession = null;
        try
        {
            GameSession? session = resolvedSession;

            if (session == null && Guid.TryParse(savedMatch.MatchId, out var sessionGuid))
            {
                _logger.LogInformation("[Unity Match] Parsed sessionGuid: {SessionGuid}", sessionGuid);
                session = await _gameSessionRepository.GetBySessionIdAsync(sessionGuid, cancellationToken);
                if (session != null)
                {
                    _logger.LogInformation("[Unity Match] Found game session by exact GUID");
                }
            }
            else if (session == null)
            {
                _logger.LogInformation("[Unity Match] Could not parse MatchId '{MatchId}' as GUID", savedMatch.MatchId);
            }

            if (session == null && savedMatch.UserId.HasValue)
            {
                _logger.LogInformation("[Unity Match] Looking for most recent uncompleted session for user {UserId}", savedMatch.UserId.Value);
                var recentSessions = await _gameSessionRepository.GetRecentSessionsByUserAsync(savedMatch.UserId.Value, 20, cancellationToken);
                session = recentSessions
                    .Where(s => (s.Status != "completed" || s.MatchResultId == null))
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefault();

                if (session != null)
                {
                    _logger.LogInformation("[Unity Match] Found uncompleted session {SessionId} for user", session.Id);
                }
            }

            if (session == null)
            {
                var sessionIdFromPayload = ExtractSessionIdFromMatchData(savedMatch.MatchDataJson);
                if (sessionIdFromPayload.HasValue)
                {
                    _logger.LogInformation("[Unity Match] Trying sessionId from payload: {SessionId}", sessionIdFromPayload.Value);
                    session = await _gameSessionRepository.GetBySessionIdAsync(sessionIdFromPayload.Value, cancellationToken);
                }
            }

            if (session == null)
            {
                _logger.LogInformation("[Unity Match] Looking for closest recent unlinked session by timestamp");
                var recentSessions = await _gameSessionRepository.GetRecentSessionsAsync(30, cancellationToken);
                var target = normalizedEnd;
                session = recentSessions
                    .Where(s => s.MatchResultId == null)
                    .OrderBy(s => Math.Abs((s.CreatedAt.UtcDateTime - target).TotalMinutes))
                    .FirstOrDefault();

                if (session != null)
                {
                    _logger.LogInformation("[Unity Match] Found nearest unlinked session {SessionId} (created_at {CreatedAt})", session.Id, session.CreatedAt);
                }
            }

            if (session == null && resolvedSession != null)
            {
                session = resolvedSession;
            }

            if (session != null)
            {
                _logger.LogInformation("[Unity Match] Updating game session {SessionId} with match_result_id", session.Id);
                session.MatchResultId = savedMatch.Id;
                session.CompletedAt = session.CompletedAt ?? DateTimeOffset.UtcNow;
                session.Status = "completed";
                if (savedMatch.UserId.HasValue && !session.UserId.HasValue)
                {
                    session.UserId = savedMatch.UserId;
                }
                await _gameSessionRepository.UpdateAsync(session, cancellationToken);
                _logger.LogInformation("[Unity Match] Successfully linked match result to game session");
                linkedSession = session;
            }
            else
            {
                _logger.LogWarning("[Unity Match] No game session found to link - neither by GUID nor by user");
            }
        }
        catch (Exception linkEx)
        {
            _logger.LogWarning(linkEx, "[Unity Match] Saved match {MatchId} but failed to link to a game session", savedMatch.MatchId);
        }

        try
        {
            var sessionIdForSummaries = linkedSession?.SessionId
                ?? resolvedSession?.SessionId
                ?? ExtractSessionIdFromMatchData(savedMatch.MatchDataJson);

            if (!sessionIdForSummaries.HasValue && Guid.TryParse(savedMatch.MatchId, out var parsed))
            {
                sessionIdForSummaries = parsed;
            }

            var playerSummaries = ExtractPlayerSummaries(command.RawJson, savedMatch.Id, sessionIdForSummaries, command.UserId);

            await _matchPlayerSummaryRepository.DeleteByMatchResultIdAsync(savedMatch.Id, cancellationToken);
            if (playerSummaries.Count > 0)
            {
                await _matchPlayerSummaryRepository.AddRangeAsync(playerSummaries, cancellationToken);
            }

            await AwardSkillXpAsync(savedMatch, resolvedSession, command, playerSummaries, cancellationToken);
        }
        catch (Exception summaryEx)
        {
            _logger.LogWarning(summaryEx, "[Unity Match] Saved match {MatchId} but failed to upsert player summaries", savedMatch.MatchId);
        }

        return new SubmitUnityMatchResultResponse
        {
            Success = true,
            MatchId = savedMatch.MatchId,
            SessionId = linkedSession?.SessionId
        };
    }

    private async Task<GameSession?> ResolveSessionForMatchAsync(string? matchId, Guid? userId, string? joinCode, DateTime endUtc, CancellationToken cancellationToken)
    {
        GameSession? session = null;

        if (Guid.TryParse(matchId, out var sessionGuid))
        {
            session = await _gameSessionRepository.GetBySessionIdAsync(sessionGuid, cancellationToken);
            if (session != null) return session;
        }

        if (!string.IsNullOrWhiteSpace(joinCode))
        {
            session = await _gameSessionRepository.GetByJoinCodeAsync(joinCode.Trim(), cancellationToken);
            if (session != null) return session;
        }

        if (userId.HasValue)
        {
            var recentSessions = await _gameSessionRepository.GetRecentSessionsByUserAsync(userId.Value, 20, cancellationToken);
            var targetTime = endUtc == default ? DateTime.UtcNow : endUtc.ToUniversalTime();
            session = recentSessions
                .OrderBy(s =>
                {
                    var pivot = (s.CompletedAt ?? s.CreatedAt).UtcDateTime;
                    return Math.Abs((pivot - targetTime).TotalMinutes);
                })
                .FirstOrDefault();
        }

        return session;
    }

    private static DateTime NormalizeUtc(DateTime dt)
    {
        return dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();
    }

    private static string NormalizeResult(string? raw)
    {
        var val = string.IsNullOrWhiteSpace(raw) ? "lose" : raw!.Trim().ToLowerInvariant();
        return val == "win" ? "win" : "lose";
    }

    private static Guid? ExtractSessionIdFromMatchData(string? matchDataJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(matchDataJson)) return null;
            var node = JsonNode.Parse(matchDataJson) as JsonObject;
            var val = node?["sessionId"]?.ToString();
            if (!string.IsNullOrWhiteSpace(val) && Guid.TryParse(val, out var guid))
            {
                return guid;
            }
        }
        catch { }
        return null;
    }

    private static bool QuestionsMissing(JsonObject matchNode)
    {
        if (!matchNode.TryGetPropertyValue("questions", out var questionsNode)) return true;
        if (questionsNode is JsonArray arr) return arr.Count == 0;
        return true;
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

    private static JsonArray? GetArray(JsonObject obj, string propertyName)
    {
        if (obj.TryGetPropertyValue(propertyName, out var node) && node is JsonArray arr)
        {
            return arr;
        }

        return null;
    }

    private static int GetIntFromNode(JsonObject obj, string propertyName)
    {
        if (obj.TryGetPropertyValue(propertyName, out var node))
        {
            if (node is JsonValue jv && jv.TryGetValue<int>(out var intVal))
            {
                return intVal;
            }

            if (int.TryParse(node?.ToString(), out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static DateTime? GetDateTimeFromNode(JsonObject obj, string propertyName)
    {
        if (obj.TryGetPropertyValue(propertyName, out var node))
        {
            if (DateTime.TryParse(node?.ToString(), out var parsed))
            {
                return parsed.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc) : parsed.ToUniversalTime();
            }
        }

        return null;
    }

    private static void MergePlayerClientIds(JsonObject target, JsonObject incoming)
    {
        var ids = new HashSet<long>();

        void Collect(JsonArray? arr)
        {
            if (arr == null) return;
            foreach (var node in arr)
            {
                if (node == null) continue;
                if (long.TryParse(node.ToString(), out var id))
                {
                    ids.Add(id);
                }
            }
        }

        Collect(GetArray(target, "playerClientIds"));
        Collect(GetArray(incoming, "playerClientIds"));

        if (ids.Count > 0)
        {
            var merged = new JsonArray();
            foreach (var id in ids.OrderBy(x => x))
            {
                merged.Add(JsonValue.Create(id));
            }
            target["playerClientIds"] = merged;
        }
    }

    private static void MergePlayerSummaries(JsonObject target, JsonObject incoming)
    {
        var merged = new Dictionary<string, JsonObject>();

        void Add(JsonArray? arr)
        {
            if (arr == null) return;
            foreach (var node in arr)
            {
                if (node is not JsonObject obj) continue;
                var key = obj.TryGetPropertyValue("playerId", out var pidNode)
                    ? pidNode?.ToString()
                    : null;
                key = string.IsNullOrWhiteSpace(key) ? Guid.NewGuid().ToString() : key!;
                merged[key] = obj.DeepClone() as JsonObject ?? new JsonObject(obj);
            }
        }

        Add(GetArray(target, "playerSummaries"));
        Add(GetArray(incoming, "playerSummaries"));

        if (merged.Count > 0)
        {
            var mergedArr = new JsonArray();
            foreach (var summary in merged.OrderBy(x => x.Key).Select(x => x.Value))
            {
                mergedArr.Add(summary);
            }
            target["playerSummaries"] = mergedArr;
        }
    }

    private static string MergeMatchData(string? existingJson, string incomingJson)
    {
        var existingNode = JsonNode.Parse(string.IsNullOrWhiteSpace(existingJson) ? "{}" : existingJson) as JsonObject ?? new JsonObject();
        var incomingNode = JsonNode.Parse(string.IsNullOrWhiteSpace(incomingJson) ? "{}" : incomingJson) as JsonObject ?? new JsonObject();

        MergePlayerSummaries(existingNode, incomingNode);
        MergePlayerClientIds(existingNode, incomingNode);

        var existingQuestions = GetArray(existingNode, "questions");
        var incomingQuestions = GetArray(incomingNode, "questions");
        if ((existingQuestions == null || existingQuestions.Count == 0) && incomingQuestions != null)
        {
            existingNode["questions"] = incomingQuestions.DeepClone();
        }
        else if (incomingQuestions != null && existingQuestions != null && incomingQuestions.Count > existingQuestions.Count)
        {
            existingNode["questions"] = incomingQuestions.DeepClone();
        }

        if (existingNode["questionPack"] == null && incomingNode["questionPack"] != null)
        {
            existingNode["questionPack"] = incomingNode["questionPack"]?.DeepClone();
        }

        existingNode["matchId"] = CloneNode(incomingNode["matchId"]) ?? existingNode["matchId"];
        existingNode["result"] = CloneNode(incomingNode["result"]) ?? existingNode["result"];
        existingNode["scene"] = CloneNode(incomingNode["scene"]) ?? existingNode["scene"];
        existingNode["relayRegion"] = CloneNode(incomingNode["relayRegion"]) ?? existingNode["relayRegion"];
        existingNode["joinCode"] = CloneNode(incomingNode["joinCode"]) ?? existingNode["joinCode"];
        existingNode["hostClientId"] = CloneNode(incomingNode["hostClientId"]) ?? existingNode["hostClientId"];
        existingNode["userId"] = CloneNode(incomingNode["userId"]) ?? existingNode["userId"];

        var totalPlayers = Math.Max(GetIntFromNode(existingNode, "totalPlayers"), GetIntFromNode(incomingNode, "totalPlayers"));
        if (totalPlayers > 0)
        {
            existingNode["totalPlayers"] = totalPlayers;
        }

        var start = GetDateTimeFromNode(existingNode, "startUtc");
        var incomingStart = GetDateTimeFromNode(incomingNode, "startUtc");
        if (incomingStart.HasValue)
        {
            start = !start.HasValue || incomingStart.Value < start.Value ? incomingStart : start;
        }
        if (start.HasValue)
        {
            existingNode["startUtc"] = start.Value.ToString("o");
        }

        var end = GetDateTimeFromNode(existingNode, "endUtc");
        var incomingEnd = GetDateTimeFromNode(incomingNode, "endUtc");
        if (incomingEnd.HasValue)
        {
            end = !end.HasValue || incomingEnd.Value > end.Value ? incomingEnd : end;
        }
        if (end.HasValue)
        {
            existingNode["endUtc"] = end.Value.ToString("o");
        }

        return existingNode.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static JsonNode? CloneNode(JsonNode? node)
    {
        return node?.DeepClone();
    }

    private async Task AwardSkillXpAsync(
        MatchResult match,
        GameSession? linkedSession,
        SubmitUnityMatchResultCommand command,
        List<MatchPlayerSummary> summaries,
        CancellationToken cancellationToken)
    {
        if (summaries.Count == 0) return;

        // Require a subject-bound skill mapping; if missing, we skip XP awards
        if (linkedSession == null || string.IsNullOrWhiteSpace(linkedSession.Subject))
        {
            _logger.LogWarning("[Unity Match] No session/subject context for match {MatchId}; skipping skill XP", match.MatchId);
            return;
        }

        var subject = await _subjectRepository.GetByCodeAsync(linkedSession.Subject, cancellationToken);
        if (subject == null)
        {
            _logger.LogWarning("[Unity Match] Subject {Subject} not found for match {MatchId}; skipping skill XP", linkedSession.Subject, match.MatchId);
            return;
        }

        var mappings = (await _subjectSkillMappingRepository.GetMappingsBySubjectIdsAsync(new[] { subject.Id }, cancellationToken)).ToList();
        if (!mappings.Any())
        {
            _logger.LogInformation("[Unity Match] No skill mappings for subject {Subject} ({SubjectId}); skipping skill XP", linkedSession.Subject, subject.Id);
            return;
        }

        var mappedSkillIds = mappings.Select(m => m.SkillId).ToHashSet();
        var skills = (await _skillRepository.GetAllAsync(cancellationToken)).Where(s => mappedSkillIds.Contains(s.Id)).ToList();
        var skillsById = skills.ToDictionary(s => s.Id, s => s);

        var totalPlayers = Math.Max(command.TotalPlayers, summaries.Count);

        foreach (var summary in summaries)
        {
            if (!summary.UserId.HasValue) continue;

            var baseXp = (summary.TotalQuestions * 5) + (summary.CorrectAnswers * 5);
            var resultFactor = command.Result?.Trim().ToLowerInvariant() == "win" ? 1.15 : 0.9;
            var teamFactor = 1 + Math.Min(0.05 * Math.Max(totalPlayers - 1, 0), 0.2);

            var totalXp = (int)Math.Round(baseXp * resultFactor * teamFactor, MidpointRounding.AwayFromZero);
            if (totalXp <= 0) continue;

            // Distribute XP across subject-bound skills using relevance weights
            var totalWeight = mappings.Sum(m => m.RelevanceWeight);
            if (totalWeight <= 0) totalWeight = mappings.Count;

            foreach (var mapping in mappings)
            {
                if (!skillsById.TryGetValue(mapping.SkillId, out var skill)) continue;

                var portion = (double)mapping.RelevanceWeight / (double)totalWeight;
                var points = (int)Math.Round(totalXp * portion, MidpointRounding.AwayFromZero);
                if (points <= 0) continue;

                var ingestCmd = new IngestXpEventCommand
                {
                    AuthUserId = summary.UserId.Value,
                    SourceService = "UserService",
                    SourceType = SkillRewardSourceType.BossFight.ToString(),
                    SourceId = match.Id,
                    SkillId = skill.Id,
                    Points = points,
                    Reason = $"Boss fight: {linkedSession.Subject}",
                    OccurredAt = DateTimeOffset.UtcNow
                };

                try
                {
                    await _mediator.Send(ingestCmd, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Unity Match] Failed to award XP for skill {Skill} user {UserId}", skill.Id, summary.UserId);
                }
            }
        }
    }

    private static (List<TopicRow> Topics, int TotalQuestions) ParseTopics(string? topicJson)
    {
        var list = new List<TopicRow>();
        if (string.IsNullOrWhiteSpace(topicJson)) return (list, 0);
        try
        {
            var arr = JsonNode.Parse(topicJson) as JsonArray;
            if (arr == null) return (list, 0);
            foreach (var node in arr.OfType<JsonObject>())
            {
                var topic = node["topic"]?.ToString();
                var total = ParseInt(node["total"]);
                var correct = ParseInt(node["correct"]);
                if (string.IsNullOrWhiteSpace(topic)) continue;
                list.Add(new TopicRow(topic, total, correct));
            }
        }
        catch
        {
            // ignore malformed topic breakdown
        }
        var totalQuestions = list.Sum(x => x.Total);
        return (list, totalQuestions);
    }

    private static int ParseInt(JsonNode? node)
    {
        if (node == null) return 0;
        if (node is JsonValue jv && jv.TryGetValue<int>(out var v)) return v;
        if (int.TryParse(node.ToString(), out var parsed)) return parsed;
        return 0;
    }

    private record TopicRow(string Topic, int Total, int Correct);

    private static List<MatchPlayerSummary> ExtractPlayerSummaries(string rawJson, Guid matchResultId, Guid? sessionId, Guid? defaultUserId)
    {
        var results = new List<MatchPlayerSummary>();
        if (string.IsNullOrWhiteSpace(rawJson)) return results;

        JsonObject? root = null;
        try
        {
            root = JsonNode.Parse(rawJson) as JsonObject;
        }
        catch
        {
            return results;
        }

        var rootUserId = ParseGuid(root?["userId"]);

        if (root != null && root.TryGetPropertyValue("per_player", out var perPlayerNode) && perPlayerNode is JsonArray perPlayers)
        {
            foreach (var node in perPlayers.OfType<JsonObject>())
            {
                var summaryNode = node["summary"] as JsonObject;
                var topics = summaryNode?["topics"] as JsonArray;
                var (totalQuestions, correctAnswers) = SumTopics(topics);

                var entity = new MatchPlayerSummary
                {
                    MatchResultId = matchResultId,
                    SessionId = sessionId,
                    UserId = ParseGuid(node["user_id"]) ?? defaultUserId ?? rootUserId,
                    ClientId = ParseLong(node["client_id"]),
                    TotalQuestions = totalQuestions,
                    CorrectAnswers = correctAnswers,
                    AverageTime = null,
                    TopicBreakdownJson = topics?.ToJsonString(),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                results.Add(entity);
            }
        }

        if (root != null && root.TryGetPropertyValue("playerSummaries", out var summariesNode) && summariesNode is JsonArray summariesArr)
        {
            foreach (var node in summariesArr.OfType<JsonObject>())
            {
                var entity = new MatchPlayerSummary
                {
                    MatchResultId = matchResultId,
                    SessionId = sessionId,
                    ClientId = ParseLong(node["playerId"]),
                    UserId = ParseGuid(node["userId"]) ?? defaultUserId ?? rootUserId,
                    TotalQuestions = GetIntFromNode(node, "totalQuestions"),
                    CorrectAnswers = GetIntFromNode(node, "correctAnswers"),
                    AverageTime = ParseDouble(node["averageTime"]),
                    TopicBreakdownJson = node["topicBreakdown"]?.ToJsonString(),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                if (entity.TotalQuestions == 0 && entity.CorrectAnswers == 0 && node["topicBreakdown"] is JsonArray breakdownTopics)
                {
                    var (totalQ, correct) = SumTopics(breakdownTopics);
                    entity.TotalQuestions = totalQ;
                    entity.CorrectAnswers = correct;
                }

                results.Add(entity);
            }
        }

        if (results.Count == 0)
        {
            results.Add(new MatchPlayerSummary
            {
                MatchResultId = matchResultId,
                SessionId = sessionId,
                UserId = defaultUserId ?? rootUserId,
                ClientId = null,
                TotalQuestions = 0,
                CorrectAnswers = 0,
                AverageTime = null,
                TopicBreakdownJson = null,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return results;
    }

    private static (int total, int correct) SumTopics(JsonArray? topics)
    {
        var total = 0;
        var correct = 0;
        if (topics != null)
        {
            foreach (var t in topics.OfType<JsonObject>())
            {
                total += GetIntFromNode(t, "total");
                correct += GetIntFromNode(t, "correct");
            }
        }
        return (total, correct);
    }

    private static Guid? ParseGuid(JsonNode? node)
    {
        var str = node?.ToString();
        if (!string.IsNullOrWhiteSpace(str) && Guid.TryParse(str, out var guid))
        {
            return guid;
        }
        return null;
    }

    private static long? ParseLong(JsonNode? node)
    {
        if (node == null) return null;
        if (node is JsonValue val && val.TryGetValue<long>(out var parsed))
        {
            return parsed;
        }

        if (long.TryParse(node.ToString(), out var parsedFromString))
        {
            return parsedFromString;
        }

        return null;
    }

    private static double? ParseDouble(JsonNode? node)
    {
        if (node == null) return null;
        if (node is JsonValue val && val.TryGetValue<double>(out var parsed))
        {
            return parsed;
        }

        if (double.TryParse(node.ToString(), out var parsedFromString))
        {
            return parsedFromString;
        }

        return null;
    }

    private static bool IsDuplicateMatchIdError(Exception ex)
    {
        var message = ex.Message ?? string.Empty;
        return message.IndexOf("match_results_match_id_key", StringComparison.OrdinalIgnoreCase) >= 0
               || message.IndexOf("duplicate key value", StringComparison.OrdinalIgnoreCase) >= 0
               || message.IndexOf("match_id", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
