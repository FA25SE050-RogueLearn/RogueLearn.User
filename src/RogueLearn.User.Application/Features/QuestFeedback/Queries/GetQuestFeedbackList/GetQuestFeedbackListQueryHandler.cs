// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestFeedback/Queries/GetQuestFeedbackList/GetQuestFeedbackListQueryHandler.cs
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

        if (request.SubjectId.HasValue)
        {
            // Aggregate ALL feedback for this subject, regardless of who took the quest
            feedbackList = await _feedbackRepository.GetBySubjectIdAsync(request.SubjectId.Value, cancellationToken);
        }
        else if (request.QuestId.HasValue)
        {
            // Specific user's quest instance history
            feedbackList = await _feedbackRepository.GetByQuestIdAsync(request.QuestId.Value, cancellationToken);
        }
        else if (request.UnresolvedOnly)
        {
            // Global triage list (show me everything broken across the system)
            feedbackList = await _feedbackRepository.GetUnresolvedAsync(cancellationToken);
        }
        else
        {
            // Fallback: Fetch all (careful on large datasets, usually strictly controlled by controller)
            feedbackList = await _feedbackRepository.GetAllAsync(cancellationToken);
        }

        // Apply "Unresolved" filter in memory if we fetched by Subject/Quest 
        // (since the specific repository methods return history which might include resolved items)
        if (request.UnresolvedOnly && (request.SubjectId.HasValue || request.QuestId.HasValue))
        {
            feedbackList = feedbackList.Where(f => !f.IsResolved);
        }

        return _mapper.Map<List<QuestFeedbackDto>>(feedbackList);
    }
}