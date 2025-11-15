using Grpc.Core;
using MediatR;
using RogueLearn.User.Api.Grpc;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildById;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildInvitations;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildJoinRequests;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMemberRoles;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMyGuild;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMyJoinRequests;
using RogueLearn.User.Application.Features.Guilds.Queries.GetAllGuilds;

namespace RogueLearn.User.Api.GrpcServices;

public class GuildsGrpcService : GuildsService.GuildsServiceBase
{
    private readonly IMediator _mediator;

    public GuildsGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<Guild> GetGuildById(GetGuildByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid guild_id"));
        }

        var dto = await _mediator.Send(new GetGuildByIdQuery(guildId), context.CancellationToken);
        if (dto is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "guild not found"));
        }

        return new Guild
        {
            Id = dto.Id.ToString(),
            Name = dto.Name,
            Description = dto.Description,
            IsPublic = dto.IsPublic,
            MaxMembers = dto.MaxMembers,
            CreatedBy = dto.CreatedBy.ToString(),
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(dto.CreatedAt),
            MemberCount = dto.MemberCount
        };
    }

    public override async Task<GuildList> GetAllGuilds(GetAllGuildsRequest request, ServerCallContext context)
    {
        var res = await _mediator.Send(new GetAllGuildsQuery(request.IncludePrivate), context.CancellationToken);
        var list = new GuildList();
        foreach (var g in res)
        {
            list.Items.Add(new Guild
            {
                Id = g.Id.ToString(),
                Name = g.Name,
                Description = g.Description,
                IsPublic = g.IsPublic,
                MaxMembers = g.MaxMembers,
                CreatedBy = g.CreatedBy.ToString(),
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(g.CreatedAt),
                MemberCount = g.MemberCount
            });
        }
        return list;
    }

    public override async Task<GuildInvitationList> GetInvitations(GetGuildInvitationsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid guild_id"));
        }
        var res = await _mediator.Send(new GetGuildInvitationsQuery(guildId), context.CancellationToken);
        var list = new GuildInvitationList();
        foreach (var i in res)
        {
            list.Items.Add(new GuildInvitation
            {
                Id = i.Id.ToString(),
                GuildId = i.GuildId.ToString(),
                InviteeId = i.InviteeId.ToString(),
                Message = i.Message ?? string.Empty,
                ExpiresAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(i.ExpiresAt),
                InvitationId = i.InvitationId.ToString(),
                InviterAuthUserId = i.InviterAuthUserId.ToString(),
                TargetUserId = i.TargetUserId?.ToString() ?? string.Empty,
                TargetEmail = i.TargetEmail ?? string.Empty,
                Status = i.Status.ToString(),
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(i.CreatedAt),
                RespondedAt = i.RespondedAt.HasValue ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(i.RespondedAt.Value) : null
            });
        }
        return list;
    }

    public override async Task<GuildJoinRequestList> GetJoinRequests(GetGuildJoinRequestsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid guild_id"));
        }
        var res = await _mediator.Send(new GetGuildJoinRequestsQuery(guildId, request.PendingOnly), context.CancellationToken);
        var list = new GuildJoinRequestList();
        foreach (var r in res)
        {
            list.Items.Add(new GuildJoinRequest
            {
                Id = r.Id.ToString(),
                GuildId = r.GuildId.ToString(),
                RequesterId = r.RequesterId.ToString(),
                Status = r.Status.ToString(),
                Message = r.Message ?? string.Empty,
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(r.CreatedAt),
                RespondedAt = r.RespondedAt.HasValue ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(r.RespondedAt.Value) : null,
                ExpiresAt = r.ExpiresAt.HasValue ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(r.ExpiresAt.Value) : null
            });
        }
        return list;
    }

    public override async Task<GuildMemberRoleList> GetMemberRoles(GetGuildMemberRolesRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId) || !Guid.TryParse(request.MemberAuthUserId, out var memberAuth))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid ids"));
        }
        var res = await _mediator.Send(new GetGuildMemberRolesQuery(guildId, memberAuth), context.CancellationToken);
        var list = new GuildMemberRoleList();
        foreach (var role in res)
        {
            list.Roles.Add(role.ToString());
        }
        return list;
    }

    public override async Task<Guild> GetMyGuild(GetMyGuildRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AuthUserId, out var authUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid auth_user_id"));
        }
        var dto = await _mediator.Send(new GetMyGuildQuery(authUserId), context.CancellationToken);
        if (dto is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "guild not found"));
        }
        return new Guild
        {
            Id = dto.Id.ToString(),
            Name = dto.Name,
            Description = dto.Description,
            IsPublic = dto.IsPublic,
            MaxMembers = dto.MaxMembers,
            CreatedBy = dto.CreatedBy.ToString(),
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(dto.CreatedAt),
            MemberCount = dto.MemberCount
        };
    }

    public override async Task<GuildJoinRequestList> GetMyJoinRequests(GetMyJoinRequestsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AuthUserId, out var authUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid auth_user_id"));
        }
        var res = await _mediator.Send(new GetMyJoinRequestsQuery(authUserId, request.PendingOnly), context.CancellationToken);
        var list = new GuildJoinRequestList();
        foreach (var r in res)
        {
            list.Items.Add(new GuildJoinRequest
            {
                Id = r.Id.ToString(),
                GuildId = r.GuildId.ToString(),
                RequesterId = r.RequesterId.ToString(),
                Status = r.Status.ToString(),
                Message = r.Message ?? string.Empty,
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(r.CreatedAt),
                RespondedAt = r.RespondedAt.HasValue ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(r.RespondedAt.Value) : null,
                ExpiresAt = r.ExpiresAt.HasValue ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(r.ExpiresAt.Value) : null
            });
        }
        return list;
    }
}