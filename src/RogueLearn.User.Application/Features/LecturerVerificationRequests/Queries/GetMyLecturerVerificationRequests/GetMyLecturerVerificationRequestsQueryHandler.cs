using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.GetMyLecturerVerificationRequests;

public class GetMyLecturerVerificationRequestsQueryHandler : IRequestHandler<GetMyLecturerVerificationRequestsQuery, IReadOnlyList<MyLecturerVerificationRequestDto>>
{
    private readonly ILecturerVerificationRequestRepository _repository;

    public GetMyLecturerVerificationRequestsQueryHandler(ILecturerVerificationRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<MyLecturerVerificationRequestDto>> Handle(GetMyLecturerVerificationRequestsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.FindAsync(r => r.AuthUserId == request.AuthUserId, cancellationToken);
        var list = items.Select(r => new MyLecturerVerificationRequestDto
        {
            Id = r.Id,
            Status = r.Status == RogueLearn.User.Domain.Enums.VerificationStatus.Rejected ? "declined" : r.Status.ToString().ToLowerInvariant(),
            Reason = r.ReviewNotes,
            SubmittedAt = r.SubmittedAt,
            ReviewedAt = r.ReviewedAt,
            ScreenshotUrl = r.Documents != null && r.Documents.TryGetValue("screenshotUrl", out var urlObj) ? urlObj?.ToString() : null,
            Documents = r.Documents
        }).OrderByDescending(x => x.SubmittedAt).ToList();
        return list;
    }
}