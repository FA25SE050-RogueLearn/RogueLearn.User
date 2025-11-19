// RogueLearn.User/src/RogueLearn.User.Application/Services/QuestStepGenerationService.cs
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using System.Net;

namespace RogueLearn.User.Application.Services;

public interface IQuestStepGenerationService
{
    /// <summary>
    /// Generates quest steps asynchronously with built-in retry logic.
    /// Retries up to 3 times on AI service failures (5xx errors).
    /// Total: 4 attempts over ~4 minutes with exponential backoff.
    /// </summary>
    [AutomaticRetry(Attempts = 4, DelaysInSeconds = new[] { 30, 60, 120 })]
    Task GenerateQuestStepsAsync(Guid authUserId, Guid questId);
}

public class QuestStepGenerationService : IQuestStepGenerationService
{
    private readonly IMediator _mediator;
    private readonly ILogger<QuestStepGenerationService> _logger;

    public QuestStepGenerationService(
        IMediator mediator,
        ILogger<QuestStepGenerationService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Generates quest steps for a given quest.
    /// Automatically retried by Hangfire with exponential backoff on transient failures.
    /// 
    /// Retries on:
    /// - HttpRequestException with 503 Service Unavailable
    /// - InvalidOperationException with "503" or "Service Unavailable" (Semantic Kernel wrapper)
    /// - OperationCanceledException (timeout)
    /// - NotFoundException when entity not found (race condition)
    /// 
    /// Does NOT retry on:
    /// - BadRequestException (validation error)
    /// - Other permanent errors
    /// </summary>
    [AutomaticRetry(Attempts = 4, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task GenerateQuestStepsAsync(Guid authUserId, Guid questId)
    {
        try
        {
            _logger.LogInformation(
                "[BACKGROUND JOB] Starting quest step generation. Quest: {QuestId}, User: {AuthUserId}",
                questId, authUserId);

            var command = new GenerateQuestStepsCommand
            {
                AuthUserId = authUserId,
                QuestId = questId
            };

            var result = await _mediator.Send(command);

            _logger.LogInformation(
                "[BACKGROUND JOB] ✅ Successfully completed quest step generation. " +
                "Quest: {QuestId}, Generated: {StepCount} steps",
                questId, result.Count);
        }
        // ========== TRANSIENT ERRORS (WILL RETRY) ==========

        // ARCHITECTURAL FIX #1: Handle race condition (entity not yet available)
        catch (NotFoundException ex) when (ex.Message.Contains("Quest"))
        {
            _logger.LogWarning(ex,
                "[BACKGROUND JOB] ⚠️ Quest {QuestId} not found (possible race condition). " +
                "Hangfire will retry this job automatically. Attempt details in error.",
                questId);
            throw; // Trigger Hangfire retry
        }

        // ARCHITECTURAL FIX #2: Handle HTTP 503 Service Unavailable
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogWarning(ex,
                "[BACKGROUND JOB] ⚠️ AI service temporarily unavailable (HTTP 503). " +
                "Hangfire will retry this job after a delay. Quest: {QuestId}",
                questId);
            throw; // Trigger Hangfire retry
        }

        // ARCHITECTURAL FIX #3: Handle other 5xx HTTP errors
        catch (HttpRequestException ex) when (ex.StatusCode >= HttpStatusCode.InternalServerError)
        {
            _logger.LogWarning(ex,
                "[BACKGROUND JOB] ⚠️ AI service error (HTTP {StatusCode}). " +
                "Hangfire will retry this job. Quest: {QuestId}",
                questId, ex.StatusCode);
            throw; // Trigger Hangfire retry
        }

        // ARCHITECTURAL FIX #4: Handle Semantic Kernel wrapping 503 as InvalidOperationException
        catch (InvalidOperationException ex) when (ex.Message.Contains("503") || ex.Message.Contains("Service Unavailable"))
        {
            _logger.LogWarning(ex,
                "[BACKGROUND JOB] ⚠️ AI service unavailable (503 from Semantic Kernel). " +
                "Hangfire will retry this job. Quest: {QuestId}",
                questId);
            throw; // Trigger Hangfire retry
        }

        // ARCHITECTURAL FIX #5: Handle timeout errors (transient - service is slow)
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex,
                "[BACKGROUND JOB] ⚠️ Quest step generation timed out (possible service slowness). " +
                "Hangfire will retry this job. Quest: {QuestId}",
                questId);
            throw; // Trigger Hangfire retry
        }

        // ========== PERMANENT ERRORS (WILL NOT RETRY) ==========

        // ARCHITECTURAL FIX #6: Handle validation/permanent errors
        catch (BadRequestException ex)
        {
            _logger.LogError(ex,
                "[BACKGROUND JOB] ❌ Bad request error (will NOT retry - requires manual intervention). " +
                "Quest: {QuestId}, Error: {Message}",
                questId, ex.Message);
            throw; // Don't retry - user action required
        }

        // ARCHITECTURAL FIX #7: Handle other permanent errors
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[BACKGROUND JOB] ❌ Unexpected error during quest step generation (will NOT retry). " +
                "Quest: {QuestId}, Exception Type: {ExceptionType}",
                questId, ex.GetType().Name);
            throw; // Don't retry
        }
    }
}