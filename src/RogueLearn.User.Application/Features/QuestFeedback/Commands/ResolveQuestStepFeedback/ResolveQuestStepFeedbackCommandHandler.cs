// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestFeedback/Commands/ResolveQuestStepFeedback/ResolveQuestStepFeedbackCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.QuestFeedback.Commands.ResolveQuestStepFeedback;

public class ResolveQuestStepFeedbackCommandHandler : IRequestHandler<ResolveQuestStepFeedbackCommand>
{
    private readonly IUserQuestStepFeedbackRepository _feedbackRepository;
    private readonly ILogger<ResolveQuestStepFeedbackCommandHandler> _logger;

    public ResolveQuestStepFeedbackCommandHandler(
        IUserQuestStepFeedbackRepository feedbackRepository,
        ILogger<ResolveQuestStepFeedbackCommandHandler> logger)
    {
        _feedbackRepository = feedbackRepository;
        _logger = logger;
    }

    public async Task Handle(ResolveQuestStepFeedbackCommand request, CancellationToken cancellationToken)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(request.FeedbackId, cancellationToken);

        if (feedback == null)
        {
            throw new NotFoundException("Feedback", request.FeedbackId);
        }

        feedback.IsResolved = request.IsResolved;
        feedback.AdminNotes = request.AdminNotes;
        feedback.UpdatedAt = DateTimeOffset.UtcNow;

        await _feedbackRepository.UpdateAsync(feedback, cancellationToken);

        _logger.LogInformation("Feedback {FeedbackId} updated by admin. Resolved: {IsResolved}",
            request.FeedbackId, request.IsResolved);
    }
}