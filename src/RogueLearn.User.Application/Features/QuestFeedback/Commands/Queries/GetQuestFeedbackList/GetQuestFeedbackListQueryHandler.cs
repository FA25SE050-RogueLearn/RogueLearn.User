// src/RogueLearn.User.Application/Features/QuestFeedback/Queries/GetQuestFeedbackList/GetQuestFeedbackListQueryHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.QuestFeedback.Queries.GetQuestFeedbackList;

public class GetQuestFeedbackListQueryHandler : IRequestHandler<GetQuestFeedbackListQuery, List<QuestFeedbackDto>>
{
    private readonly IUserQuestStepFeedbackRepository _feedbackRepository;
    private readonly IMapper _mapper;

    public GetQuestFeedbackListQueryHandler(IUserQuestStepFeedbackRepository feedbackRepository, IMapper mapper)
    {
        _feedbackRepository = feedbackRepository;
        _mapper = mapper;
    }

    public async Task<List<QuestFeedbackDto>> Handle(GetQuestFeedbackListQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<Domain.Entities.UserQuestStepFeedback> feedbackList;

        if (request.QuestId.HasValue)
        {
            feedbackList = await _feedbackRepository.GetByQuestIdAsync(request.QuestId.Value, cancellationToken);
            if (request.UnresolvedOnly)
            {
                feedbackList = feedbackList.Where(f => !f.IsResolved);
            }
        }
        else if (request.UnresolvedOnly)
        {
            feedbackList = await _feedbackRepository.GetUnresolvedAsync(cancellationToken);
        }
        else
        {
            feedbackList = await _feedbackRepository.GetAllAsync(cancellationToken);
        }

        return _mapper.Map<List<QuestFeedbackDto>>(feedbackList);
    }
}