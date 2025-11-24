using System.Diagnostics;
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
		var stopWatch = System.Diagnostics.Stopwatch.StartNew();

		// 1. Fetch Profile
		var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthId, cancellationToken);

		if (userProfile is null)
		{
			_logger.LogInformation("No user profile found for auth id {AuthId}", request.AuthId);
			return null;
		}

		var dto = _mapper.Map<UserProfileDto>(userProfile);

		// 2. Fetch User Roles
		var userRoles = await _userRoleRepository.GetRolesForUserAsync(userProfile.AuthUserId, cancellationToken)
		                ?? Enumerable.Empty<Domain.Entities.UserRole>();

		// 3. OPTIMIZED: Batch Fetch Roles
		var roleIds = userRoles.Select(ur => ur.RoleId).Distinct().ToList();

		if (roleIds.Any())
		{
			// This is now 1 request regardless of how many roles the user has
			var roles = await _roleRepository.GetByIdsAsync(roleIds, cancellationToken);

			dto.Roles = roles
				.Where(r => !string.IsNullOrWhiteSpace(r.Name))
				.Select(r => r.Name)
				.ToList();
		}
		else
		{
			dto.Roles = new List<string>();
		}
		stopWatch.Stop();
		_logger.LogInformation($"Handler took: {stopWatch.Elapsed.TotalSeconds}s");
		_logger.LogInformation("Retrieved profile for auth user {AuthUserId} with {RoleCount} roles",
			userProfile.AuthUserId, dto.Roles.Count);

		return dto;
	}
}