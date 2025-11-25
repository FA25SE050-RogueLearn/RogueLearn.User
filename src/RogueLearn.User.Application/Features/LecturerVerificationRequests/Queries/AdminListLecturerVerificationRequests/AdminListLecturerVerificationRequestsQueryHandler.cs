using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminListLecturerVerificationRequests;

public class AdminListLecturerVerificationRequestsQueryHandler : IRequestHandler<AdminListLecturerVerificationRequestsQuery, AdminListLecturerVerificationRequestsResponse>
{
    private readonly ILecturerVerificationRequestRepository _repository;

    public AdminListLecturerVerificationRequestsQueryHandler(ILecturerVerificationRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<AdminListLecturerVerificationRequestsResponse> Handle(AdminListLecturerVerificationRequestsQuery request, CancellationToken cancellationToken)
    {
        Func<RogueLearn.User.Domain.Entities.LecturerVerificationRequest, bool> filter = r => true;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusNorm = request.Status.Trim().ToLowerInvariant();
            filter = r => (statusNorm == "declined" ? VerificationStatus.Rejected : Enum.Parse<VerificationStatus>(request.Status, true)) == r.Status;
        }
        if (request.UserId.HasValue)
        {
            var prev = filter;
            var uid = request.UserId.Value;
            filter = r => prev(r) && r.AuthUserId == uid;
        }

        var all = await _repository.GetAllAsync(cancellationToken);
        var filtered = all.Where(filter);
        var total = filtered.Count();
        var page = request.Page <= 0 ? 1 : request.Page;
        var size = request.Size <= 0 ? 20 : request.Size;
        var items = filtered
            .Skip((page - 1) * size)
            .Take(size)
            .Select(r => new AdminLecturerVerificationItem
            {
                Id = r.Id,
                UserId = r.AuthUserId,
                Status = r.Status == VerificationStatus.Rejected ? "declined" : r.Status.ToString().ToLowerInvariant(),
                Institution = r.Documents != null && r.Documents.TryGetValue("institution", out var inst) ? inst?.ToString() : null
            })
            .ToList();

        return new AdminListLecturerVerificationRequestsResponse
        {
            Items = items,
            Page = page,
            Size = size,
            Total = total
        };
    }
}