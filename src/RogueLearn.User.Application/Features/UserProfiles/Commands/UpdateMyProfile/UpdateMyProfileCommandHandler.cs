using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.UserProfiles.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, UserProfileDto>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAvatarStorage _avatarStorage;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateMyProfileCommandHandler> _logger;

    // ADDED: Repositories for validation
    private readonly IClassRepository _classRepository;
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;

    public UpdateMyProfileCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IAvatarStorage avatarStorage,
        IMapper mapper,
        ILogger<UpdateMyProfileCommandHandler> logger,
        IClassRepository classRepository,
        ICurriculumProgramRepository curriculumProgramRepository)
    {
        _userProfileRepository = userProfileRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _avatarStorage = avatarStorage;
        _mapper = mapper;
        _logger = logger;
        _classRepository = classRepository;
        _curriculumProgramRepository = curriculumProgramRepository;
    }

    public async Task<UserProfileDto> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (profile is null)
        {
            throw new NotFoundException("UserProfile", request.AuthUserId);
        }

        // If an image upload is provided, upload to storage and set the profile image URL
        if (request.ProfileImageBytes is not null && request.ProfileImageBytes.Length > 0)
        {
            try
            {
                var publicUrl = await _avatarStorage.SaveAvatarAsync(
                    request.AuthUserId,
                    request.ProfileImageBytes,
                    request.ProfileImageContentType,
                    request.ProfileImageFileName,
                    cancellationToken);

                profile.ProfileImageUrl = publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload avatar for auth user {AuthUserId}", request.AuthUserId);
                throw; // Bubble up to be handled by global exception handler
            }
        }
        else if (request.ProfileImageUrl != null)
        {
            // PATCH semantics: update only provided non-null values
            profile.ProfileImageUrl = string.IsNullOrWhiteSpace(request.ProfileImageUrl) ? null : request.ProfileImageUrl;
        }

        // Apply other allowed updates
        if (request.FirstName != null) profile.FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName;
        if (request.LastName != null) profile.LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName;
        if (request.Bio != null) profile.Bio = request.Bio; // allow empty string to clear bio
        if (request.PreferencesJson != null)
        {
            // Preferences are stored as JSONB; deserialize safe JSON object to dictionary
            profile.Preferences = string.IsNullOrWhiteSpace(request.PreferencesJson)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, object>>(request.PreferencesJson);
        }

        // ADDED: Academic path updates with validation
        if (request.ClassId.HasValue)
        {
            var classExists = await _classRepository.ExistsAsync(request.ClassId.Value, cancellationToken);
            if (!classExists)
            {
                throw new NotFoundException("Class", request.ClassId.Value);
            }
            profile.ClassId = request.ClassId.Value;
        }

        if (request.RouteId.HasValue)
        {
            var routeExists = await _curriculumProgramRepository.ExistsAsync(request.RouteId.Value, cancellationToken);
            if (!routeExists)
            {
                throw new NotFoundException("CurriculumProgram", request.RouteId.Value);
            }
            profile.RouteId = request.RouteId.Value;
        }

        profile.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await _userProfileRepository.UpdateAsync(profile, cancellationToken);
        var dto = _mapper.Map<UserProfileDto>(updated);

        // hydrate roles
        var userRoles = await _userRoleRepository.GetRolesForUserAsync(updated.AuthUserId, cancellationToken);
        var roleNames = new List<string>();
        foreach (var ur in userRoles)
        {
            var role = await _roleRepository.GetByIdAsync(ur.RoleId, cancellationToken);
            if (role != null && !string.IsNullOrWhiteSpace(role.Name))
            {
                roleNames.Add(role.Name);
            }
        }
        dto.Roles = roleNames.Distinct().ToList();

        _logger.LogInformation("Updated profile for auth user {AuthUserId}", updated.AuthUserId);

        return dto;
    }
}