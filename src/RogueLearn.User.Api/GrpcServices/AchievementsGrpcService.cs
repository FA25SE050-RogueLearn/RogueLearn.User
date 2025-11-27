using Grpc.Core;
using MediatR;
using RogueLearn.User.Api.Grpc;
using RogueLearn.User.Application.Features.Achievements.Queries.GetAllAchievements;
using RogueLearn.User.Application.Features.Achievements.Commands.CreateAchievement;
using RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;
using RogueLearn.User.Application.Features.Achievements.Commands.DeleteAchievement;
using RogueLearn.User.Application.Features.Achievements.Commands.AwardAchievementToUser;
using RogueLearn.User.Application.Features.Achievements.Commands.RevokeAchievementFromUser;
using RogueLearn.User.Application.Features.Achievements.Queries.GetUserAchievementsByAuthId;
using GrantAchievementsResponse = RogueLearn.User.Api.Grpc.GrantAchievementsResponse;

namespace RogueLearn.User.Api.GrpcServices;

public class AchievementsGrpcService : AchievementsService.AchievementsServiceBase
{
    private readonly IMediator _mediator;

    public AchievementsGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<AchievementList> GetAll(GetAllRequest request, ServerCallContext context)
    {
        var response = await _mediator.Send(new GetAllAchievementsQuery(), context.CancellationToken);
        var list = new AchievementList();
        foreach (var a in response.Achievements)
        {
            list.Achievements.Add(new Achievement
            {
                Id = a.Id.ToString(),
                Key = a.Key,
                Name = a.Name,
                Description = a.Description,
                RuleType = a.RuleType ?? string.Empty,
                RuleConfig = a.RuleConfig ?? string.Empty,
                Category = a.Category ?? string.Empty,
                Icon = a.Icon ?? string.Empty,
                IconUrl = a.IconUrl ?? string.Empty,
                Version = a.Version,
                IsActive = a.IsActive,
                SourceService = a.SourceService,
                IsMedal = a.IsMedal
            });
        }
        return list;
    }

    public override async Task<Achievement> Create(CreateAchievementRequest request, ServerCallContext context)
    {
        var cmd = new CreateAchievementCommand
        {
            Key = request.Key,
            Name = request.Name,
            Description = request.Description,
            RuleType = string.IsNullOrWhiteSpace(request.RuleType) ? null : request.RuleType,
            RuleConfig = string.IsNullOrWhiteSpace(request.RuleConfig) ? null : request.RuleConfig,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category,
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon,
            IconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl,
            Version = request.Version,
            IsActive = request.IsActive,
            SourceService = request.SourceService
        };
        var res = await _mediator.Send(cmd, context.CancellationToken);
        return new Achievement
        {
            Id = res.Id.ToString(),
            Key = res.Key,
            Name = res.Name,
            Description = res.Description,
            RuleType = res.RuleType ?? string.Empty,
            RuleConfig = res.RuleConfig ?? string.Empty,
            Category = res.Category ?? string.Empty,
            Icon = res.Icon ?? string.Empty,
            IconUrl = res.IconUrl ?? string.Empty,
            Version = res.Version,
            IsActive = res.IsActive,
            SourceService = res.SourceService
        };
    }

    public override async Task<Achievement> Update(UpdateAchievementRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid id"));
        }
        var cmd = new UpdateAchievementCommand
        {
            Id = id,
            Key = request.Key,
            Name = request.Name,
            Description = request.Description,
            RuleType = string.IsNullOrWhiteSpace(request.RuleType) ? null : request.RuleType,
            RuleConfig = string.IsNullOrWhiteSpace(request.RuleConfig) ? null : request.RuleConfig,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category,
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon,
            IconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl,
            Version = request.Version,
            IsActive = request.IsActive,
            SourceService = request.SourceService
        };
        var res = await _mediator.Send(cmd, context.CancellationToken);
        return new Achievement
        {
            Id = res.Id.ToString(),
            Key = res.Key,
            Name = res.Name,
            Description = res.Description,
            RuleType = res.RuleType ?? string.Empty,
            RuleConfig = res.RuleConfig ?? string.Empty,
            Category = res.Category ?? string.Empty,
            Icon = res.Icon ?? string.Empty,
            IconUrl = res.IconUrl ?? string.Empty,
            Version = res.Version,
            IsActive = res.IsActive,
            SourceService = res.SourceService
        };
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> Delete(DeleteAchievementRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid id"));
        }
        await _mediator.Send(new DeleteAchievementCommand { Id = id }, context.CancellationToken);
        return new Google.Protobuf.WellKnownTypes.Empty();
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> AwardToUser(AwardAchievementToUserRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId) || !Guid.TryParse(request.AchievementId, out var achievementId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid ids"));
        }
        var cmd = new AwardAchievementToUserCommand
        {
            UserId = userId,
            AchievementId = achievementId,
            Context = string.IsNullOrWhiteSpace(request.Context) ? null : request.Context
        };
        await _mediator.Send(cmd, context.CancellationToken);
        return new Google.Protobuf.WellKnownTypes.Empty();
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> RevokeFromUser(RevokeAchievementFromUserRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId) || !Guid.TryParse(request.AchievementId, out var achievementId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid ids"));
        }
        var cmd = new RevokeAchievementFromUserCommand
        {
            UserId = userId,
            AchievementId = achievementId
        };
        await _mediator.Send(cmd, context.CancellationToken);
        return new Google.Protobuf.WellKnownTypes.Empty();
    }

    public override async Task<GrantAchievementsResponse> GrantAchievements(GrantAchievementsRequest request, ServerCallContext context)
    {
        var cmd = new Application.Features.Achievements.Commands.GrantAchievements.GrantAchievementsCommand
        {
            Entries = request.UserAchievements
                .Select(x =>
                {
                    Guid.TryParse(x.UserId, out var authUserId);
                    return new Application.Features.Achievements.Commands.GrantAchievements.GrantAchievementEntry
                    {
                        AuthUserId = authUserId,
                        AchievementKey = x.AchievementKey,
                        Context = string.IsNullOrWhiteSpace(x.Context) ? null : x.Context
                    };
                })
                .ToList()
        };

        var res = await _mediator.Send(cmd, context.CancellationToken);
        var reply = new GrantAchievementsResponse
        {
            GrantedCount = res.GrantedCount
        };
        reply.Errors.AddRange(res.Errors);
        return reply;
    }

    public override async Task<UserAchievementList> GetByAuthUserId(GetUserAchievementsByAuthUserIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AuthUserId, out var authUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid auth_user_id"));
        }
        var res = await _mediator.Send(new GetUserAchievementsByAuthIdQuery { AuthUserId = authUserId }, context.CancellationToken);
        var list = new UserAchievementList();
        foreach (var ua in res.Achievements)
        {
            list.Achievements.Add(new UserAchievement
            {
                AchievementId = ua.AchievementId.ToString(),
                Key = ua.Key,
                Name = ua.Name,
                Description = ua.Description,
                IconUrl = ua.IconUrl ?? string.Empty,
                SourceService = ua.SourceService,
                EarnedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(ua.EarnedAt),
                Context = ua.Context ?? string.Empty
            });
        }
        return list;
    }
}