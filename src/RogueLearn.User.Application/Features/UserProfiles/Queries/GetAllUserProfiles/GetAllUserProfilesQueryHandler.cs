using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;

public class GetAllUserProfilesQueryHandler : IRequestHandler<GetAllUserProfilesQuery, GetAllUserProfilesResponse>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllUserProfilesQueryHandler> _logger;

    public GetAllUserProfilesQueryHandler(
        IUserProfileRepository userProfileRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IMapper mapper,
        ILogger<GetAllUserProfilesQueryHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<GetAllUserProfilesResponse> Handle(GetAllUserProfilesQuery request, CancellationToken cancellationToken)
    {
        var userProfiles = await _userProfileRepository.GetAllAsync(cancellationToken);
        var userProfileDtos = new List<UserProfileDto>();

        foreach (var userProfile in userProfiles)
        {
            var dto = _mapper.Map<UserProfileDto>(userProfile);

            // Get roles for each user
            var userRoles = await _userRoleRepository.GetRolesForUserAsync(userProfile.AuthUserId, cancellationToken);
            var roleNames = new List<string>();
            
            foreach (var userRole in userRoles)
            {
                var role = await _roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);
                if (role != null && !string.IsNullOrWhiteSpace(role.Name))
                {
                    roleNames.Add(role.Name);
                }
            }

            dto.Roles = roleNames.Distinct().ToList();
            userProfileDtos.Add(dto);
        }

        _logger.LogInformation("Retrieved {UserProfileCount} user profiles", userProfileDtos.Count);

        return new GetAllUserProfilesResponse
        {
            UserProfiles = userProfileDtos
        };
    }
}