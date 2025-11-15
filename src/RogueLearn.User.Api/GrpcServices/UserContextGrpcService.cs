using Grpc.Core;
using MediatR;
using RogueLearn.User.Api.Grpc;
using RogueLearn.User.Application.Features.UserContext.Queries.GetUserContextByAuthId;

namespace RogueLearn.User.Api.GrpcServices;

public class UserContextGrpcService : UserContextService.UserContextServiceBase
{
    private readonly IMediator _mediator;

    public UserContextGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<UserContext> GetByAuthId(GetUserContextByAuthIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AuthUserId, out var authUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid auth_user_id"));
        }

        var dto = await _mediator.Send(new GetUserContextByAuthIdQuery(authUserId), context.CancellationToken);
        if (dto is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "user context not found"));
        }

        var ctx = new UserContext
        {
            AuthUserId = dto.AuthUserId.ToString(),
            Username = dto.Username,
            Email = dto.Email,
            DisplayName = dto.DisplayName ?? string.Empty,
            ProfileImageUrl = dto.ProfileImageUrl ?? string.Empty,
            Bio = dto.Bio ?? string.Empty,
            PreferencesJson = dto.PreferencesJson ?? string.Empty,
            Roles = { dto.Roles },
            AchievementsCount = dto.AchievementsCount
        };

        if (dto.Class is not null)
        {
            ctx.Class = new ClassSummary
            {
                Id = dto.Class.Id.ToString(),
                Name = dto.Class.Name,
                RoadmapUrl = dto.Class.RoadmapUrl ?? string.Empty,
                DifficultyLevel = dto.Class.DifficultyLevel,
                SkillFocusAreas = { (dto.Class.SkillFocusAreas ?? Array.Empty<string>()) }
            };
        }

        if (dto.Enrollment is not null)
        {
            ctx.Enrollment = new CurriculumEnrollment
            {
                VersionId = dto.Enrollment.VersionId.ToString(),
                VersionCode = dto.Enrollment.VersionCode,
                EffectiveYear = dto.Enrollment.EffectiveYear,
                Status = dto.Enrollment.Status,
                EnrollmentDate = dto.Enrollment.EnrollmentDate.ToString("yyyy-MM-dd"),
                ExpectedGraduationDate = dto.Enrollment.ExpectedGraduationDate?.ToString("yyyy-MM-dd") ?? string.Empty
            };
        }

        if (dto.Skills is not null)
        {
            var skillSummary = new SkillSummary
            {
                TotalSkills = dto.Skills.TotalSkills,
                TotalExperiencePoints = dto.Skills.TotalExperiencePoints,
                HighestLevel = dto.Skills.HighestLevel,
                AverageLevel = dto.Skills.AverageLevel
            };
            foreach (var s in dto.Skills.TopSkills)
            {
                skillSummary.TopSkills.Add(new UserSkill
                {
                    SkillName = s.SkillName,
                    Level = s.Level,
                    ExperiencePoints = s.ExperiencePoints,
                    LastUpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(s.LastUpdatedAt)
                });
            }
            ctx.Skills = skillSummary;
        }

        return ctx;
    }
}