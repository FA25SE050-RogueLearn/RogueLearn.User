using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.QuestFeedback.Commands.SubmitQuestStepFeedback;

public class SubmitQuestStepFeedbackCommandHandler : IRequestHandler<SubmitQuestStepFeedbackCommand, Guid>
{
    private readonly IUserQuestStepFeedbackRepository _feedbackRepository;
    private readonly IQuestStepRepository _stepRepository;
    private readonly IQuestRepository _questRepository;
    private readonly ILogger<SubmitQuestStepFeedbackCommandHandler> _logger;

    public SubmitQuestStepFeedbackCommandHandler(
        IUserQuestStepFeedbackRepository feedbackRepository,
        IQuestStepRepository stepRepository,
        IQuestRepository questRepository,
        ILogger<SubmitQuestStepFeedbackCommandHandler> logger)
    {
        _feedbackRepository = feedbackRepository;
        _stepRepository = stepRepository;
        _questRepository = questRepository;
        _logger = logger;
    }

    public async Task<Guid> Handle(SubmitQuestStepFeedbackCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Step exists
        var step = await _stepRepository.GetByIdAsync(request.StepId, cancellationToken)
            ?? throw new NotFoundException("QuestStep", request.StepId);

        if (step.QuestId != request.QuestId)
        {
            throw new BadRequestException("Step does not belong to the specified Quest.");
        }

        // 2. Fetch Quest to get the SubjectId
        // This is the critical step to allow aggregating feedback by Subject later.
        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (quest.SubjectId == null)
        {
            // Edge case: Quest not linked to a subject (e.g., custom event quests).
            // For now, we require a subject link for feedback to support the analytics requirement.
            _logger.LogWarning("Feedback submitted for non-subject quest {QuestId}", request.QuestId);
            throw new BadRequestException("Cannot submit feedback for this quest type (missing linked subject).");
        }

        // 3. Create Feedback with SubjectId
        var feedback = new UserQuestStepFeedback
        {
            AuthUserId = request.AuthUserId,
            QuestId = request.QuestId,
            StepId = request.StepId,
            SubjectId = quest.SubjectId.Value, // Link to the master subject for admin aggregation
            Rating = request.Rating,
            Category = request.Category,
            Comment = request.Comment,
            IsResolved = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var created = await _feedbackRepository.AddAsync(feedback, cancellationToken);

        _logger.LogInformation(
            "Feedback submitted for Subject {SubjectId} (Quest {QuestId}, Step {StepId}). Category: {Category}, Rating: {Rating}",
            quest.SubjectId, request.QuestId, request.StepId, request.Category, request.Rating);

        return created.Id;
    }
}