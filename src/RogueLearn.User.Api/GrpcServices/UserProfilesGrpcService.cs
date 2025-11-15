using Grpc.Core;
using MediatR;
using RogueLearn.User.Api.Grpc;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Features.UserProfiles.Commands.UpdateMyProfile;
using RogueLearn.User.Application.Features.UserProfiles.Commands.LogNewUser;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;

namespace RogueLearn.User.Api.GrpcServices;

public class UserProfilesGrpcService : UserProfilesService.UserProfilesServiceBase
{
    private readonly IMediator _mediator;

    public UserProfilesGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<UserProfile> GetByAuthId(GetUserProfileByAuthIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AuthUserId, out var authUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid auth_user_id"));
        }

        var result = await _mediator.Send(new GetUserProfileByAuthIdQuery(authUserId), context.CancellationToken);
        if (result is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "user profile not found"));
        }

        return new UserProfile
        {
            Id = result.Id.ToString(),
            AuthUserId = result.AuthUserId.ToString(),
            Username = result.Username,
            Email = result.Email,
            FirstName = result.FirstName ?? string.Empty,
            LastName = result.LastName ?? string.Empty,
            Level = result.Level,
            ExperiencePoints = result.ExperiencePoints,
            OnboardingCompleted = result.OnboardingCompleted,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(result.CreatedAt),
            ProfileImageUrl = result.ProfileImageUrl ?? string.Empty,
            Bio = result.Bio ?? string.Empty,
            PreferencesJson = result.PreferencesJson ?? string.Empty,
            Roles = { result.Roles },
            ClassId = result.ClassId?.ToString() ?? string.Empty,
            RouteId = result.RouteId?.ToString() ?? string.Empty
        };
    }

    public override async Task<UserProfile> UpdateMyProfile(UpdateMyProfileRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AuthUserId, out var authUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid auth_user_id"));
        }
        var cmd = new UpdateMyProfileCommand
        {
            AuthUserId = authUserId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            ProfileImageUrl = request.ProfileImageUrl,
            Bio = request.Bio,
            PreferencesJson = request.PreferencesJson
        };
        var result = await _mediator.Send(cmd, context.CancellationToken);
        return new UserProfile
        {
            Id = result.Id.ToString(),
            AuthUserId = result.AuthUserId.ToString(),
            Username = result.Username,
            Email = result.Email,
            FirstName = result.FirstName ?? string.Empty,
            LastName = result.LastName ?? string.Empty,
            Level = result.Level,
            ExperiencePoints = result.ExperiencePoints,
            OnboardingCompleted = result.OnboardingCompleted,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(result.CreatedAt),
            ProfileImageUrl = result.ProfileImageUrl ?? string.Empty,
            Bio = result.Bio ?? string.Empty,
            PreferencesJson = result.PreferencesJson ?? string.Empty,
            Roles = { result.Roles },
            ClassId = result.ClassId?.ToString() ?? string.Empty,
            RouteId = result.RouteId?.ToString() ?? string.Empty
        };
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> LogNewUser(LogNewUserRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AuthUserId, out var authUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid auth_user_id"));
        }
        var cmd = new LogNewUserCommand
        {
            AuthUserId = authUserId,
            Email = request.Email,
            Username = request.Username
        };
        await _mediator.Send(cmd, context.CancellationToken);
        return new Google.Protobuf.WellKnownTypes.Empty();
    }

    public override async Task<UserProfileList> GetAll(GetAllUserProfilesRequest request, ServerCallContext context)
    {
        var response = await _mediator.Send(new GetAllUserProfilesQuery(), context.CancellationToken);
        var list = new UserProfileList();
        foreach (var result in response.UserProfiles)
        {
            list.Items.Add(new UserProfile
            {
                Id = result.Id.ToString(),
                AuthUserId = result.AuthUserId.ToString(),
                Username = result.Username,
                Email = result.Email,
                FirstName = result.FirstName ?? string.Empty,
                LastName = result.LastName ?? string.Empty,
                Level = result.Level,
                ExperiencePoints = result.ExperiencePoints,
                OnboardingCompleted = result.OnboardingCompleted,
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(result.CreatedAt),
                ProfileImageUrl = result.ProfileImageUrl ?? string.Empty,
                Bio = result.Bio ?? string.Empty,
                PreferencesJson = result.PreferencesJson ?? string.Empty,
                Roles = { result.Roles },
                ClassId = result.ClassId?.ToString() ?? string.Empty,
                RouteId = result.RouteId?.ToString() ?? string.Empty
            });
        }
        return list;
    }
}