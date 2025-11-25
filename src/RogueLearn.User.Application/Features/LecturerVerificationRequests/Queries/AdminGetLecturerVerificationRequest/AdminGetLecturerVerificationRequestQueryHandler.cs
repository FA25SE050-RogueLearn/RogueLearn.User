using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminGetLecturerVerificationRequest;

public class AdminGetLecturerVerificationRequestQueryHandler : IRequestHandler<AdminGetLecturerVerificationRequestQuery, AdminLecturerVerificationRequestDetail?>
{
    private readonly ILecturerVerificationRequestRepository _repository;

    public AdminGetLecturerVerificationRequestQueryHandler(ILecturerVerificationRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<AdminLecturerVerificationRequestDetail?> Handle(AdminGetLecturerVerificationRequestQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.RequestId, cancellationToken);
        if (entity is null) return null;
        return new AdminLecturerVerificationRequestDetail
        {
            Id = entity.Id,
            AuthUserId = entity.AuthUserId,
            Email = entity.Documents != null && entity.Documents.TryGetValue("email", out var e) ? e?.ToString() ?? string.Empty : string.Empty,
            StaffId = entity.Documents != null && entity.Documents.TryGetValue("staffId", out var s) ? s?.ToString() ?? string.Empty : string.Empty,
            ScreenshotUrl = entity.Documents != null && entity.Documents.TryGetValue("screenshotUrl", out var u) ? u?.ToString() : null,
            Status = entity.Status == RogueLearn.User.Domain.Enums.VerificationStatus.Rejected ? "declined" : entity.Status.ToString().ToLowerInvariant(),
            SubmittedAt = entity.SubmittedAt
        };
    }
}