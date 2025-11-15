// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestProgress/UpdateQuestProgressCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestProgress;

public class UpdateQuestProgressCommandHandler : IRequestHandler<UpdateQuestProgressCommand>
{
    private readonly IUserQuestProgressRepository _progressRepository;
    private readonly ILogger<UpdateQuestProgressCommandHandler> _logger;

    public UpdateQuestProgressCommandHandler(IUserQuestProgressRepository progressRepository, ILogger<UpdateQuestProgressCommandHandler> logger)
    {
        _progressRepository = progressRepository;
        _logger = logger;
    }

    public async Task Handle(UpdateQuestProgressCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to update quest progress for User {AuthUserId}, Quest {QuestId} to status {Status}",
            request.AuthUserId, request.QuestId, request.Status);

        var progress = await _progressRepository.FirstOrDefaultAsync(
            p => p.AuthUserId == request.AuthUserId && p.QuestId == request.QuestId,
            cancellationToken);

        if (progress == null)
        {
            // If no progress record exists, create one. This handles the case where a user
            // updates progress on a quest for the first time.
            _logger.LogInformation("No existing progress found. Creating new UserQuestProgress record for User {AuthUserId}, Quest {QuestId}",
                request.AuthUserId, request.QuestId);
            progress = new Domain.Entities.UserQuestProgress
            {
                AuthUserId = request.AuthUserId,
                QuestId = request.QuestId,
                Status = request.Status, // Set to the requested status directly
                LastUpdatedAt = DateTimeOffset.UtcNow
            };

            if (request.Status == QuestStatus.Completed)
            {
                progress.CompletedAt = DateTimeOffset.UtcNow;
            }

            await _progressRepository.AddAsync(progress, cancellationToken);

            _logger.LogInformation("Successfully created and set quest progress for User {AuthUserId}, Quest {QuestId} to status {Status}",
                request.AuthUserId, request.QuestId, request.Status);

            return; // End execution here as the record is now created with the correct status
        }

        // If a record already exists, update it as before.
        progress.Status = request.Status;
        progress.LastUpdatedAt = DateTimeOffset.UtcNow;

        if (request.Status == QuestStatus.Completed)
        {
            progress.CompletedAt ??= DateTimeOffset.UtcNow;
        }
        else
        {
            progress.CompletedAt = null;
        }

        await _progressRepository.UpdateAsync(progress, cancellationToken);

        _logger.LogInformation("Successfully updated quest progress for User {AuthUserId}, Quest {QuestId} to status {Status}",
            request.AuthUserId, request.QuestId, request.Status);
    }
}