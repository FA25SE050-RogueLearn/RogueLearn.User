using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Meetings.Commands.CreateOrUpdateSummary;

public class CreateOrUpdateSummaryCommandHandler : IRequestHandler<CreateOrUpdateSummaryCommand, Unit>
{
    private readonly IMeetingSummaryRepository _summaryRepo;

    public CreateOrUpdateSummaryCommandHandler(IMeetingSummaryRepository summaryRepo)
    {
        _summaryRepo = summaryRepo;
    }

    public async Task<Unit> Handle(CreateOrUpdateSummaryCommand request, CancellationToken cancellationToken)
    {
        var existing = await _summaryRepo.GetByMeetingAsync(request.MeetingId, cancellationToken);
        if (existing != null)
        {
            existing.SummaryText = request.Content;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            await _summaryRepo.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var entity = new MeetingSummary
            {
                MeetingId = request.MeetingId,
                SummaryText = request.Content,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _summaryRepo.AddAsync(entity, cancellationToken);
        }

        return Unit.Value;
    }
}