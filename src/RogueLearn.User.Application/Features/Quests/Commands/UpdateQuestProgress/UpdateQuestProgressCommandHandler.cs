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
            _logger.LogWarning("User quest progress record not found for User {AuthUserId} and Quest {QuestId}",
                request.AuthUserId, request.QuestId);
            throw new NotFoundException("Quest progress not found for this user.");
        }

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