using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;

public class GetUserProfileByAuthIdQueryHandler : IRequestHandler<GetUserProfileByAuthIdQuery, UserProfileDto?>
{
	private readonly IUserProfileRepository _userProfileRepository;
	private readonly IUserRoleRepository _userRoleRepository;
	private readonly IRoleRepository _roleRepository;
	private readonly IMapper _mapper;
	private readonly ILogger<GetUserProfileByAuthIdQueryHandler> _logger;

	public GetUserProfileByAuthIdQueryHandler(
		IUserProfileRepository userProfileRepository,
		IUserRoleRepository userRoleRepository,
		IRoleRepository roleRepository,
		IMapper mapper,
		ILogger<GetUserProfileByAuthIdQueryHandler> logger)
	{
		_userProfileRepository = userProfileRepository;
		_userRoleRepository = userRoleRepository;
		_roleRepository = roleRepository;
		_mapper = mapper;
		_logger = logger;
	}

	/// <summary>
	/// Retrieves a user's profile by their authentication ID, including mapped role names.
	/// </summary>
	/// <param name="request">The request containing the AuthId.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The user's profile DTO, or null if not found.</returns>
	public async Task<UserProfileDto?> Handle(GetUserProfileByAuthIdQuery request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Fetching user profile by auth id {AuthId}", request.AuthId);
		var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthId, cancellationToken);

		if (userProfile is null)
		{
			_logger.LogInformation("No user profile found for auth id {AuthId}", request.AuthId);
			return null;
		}

		var dto = _mapper.Map<UserProfileDto>(userProfile);

		var userRoles = await _userRoleRepository.GetRolesForUserAsync(userProfile.AuthUserId, cancellationToken) 
			?? Enumerable.Empty<Domain.Entities.UserRole>();
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

		_logger.LogInformation("Retrieved profile for auth user {AuthUserId} with {RoleCount} roles", userProfile.AuthUserId, dto.Roles.Count);

		return dto;
	}
}