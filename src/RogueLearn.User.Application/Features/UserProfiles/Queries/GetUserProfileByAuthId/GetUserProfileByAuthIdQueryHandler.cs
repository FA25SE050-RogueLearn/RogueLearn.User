using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;

public class GetUserProfileByAuthIdQueryHandler : IRequestHandler<GetUserProfileByAuthIdQuery, UserProfileDto?>
{
	private readonly IUserProfileRepository _userProfileRepository;
	private readonly IMapper _mapper;

	public GetUserProfileByAuthIdQueryHandler(IUserProfileRepository userProfileRepository, IMapper mapper)
	{
		_userProfileRepository = userProfileRepository;
		_mapper = mapper;
	}

	public async Task<UserProfileDto?> Handle(GetUserProfileByAuthIdQuery request, CancellationToken cancellationToken)
	{
		var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthId, cancellationToken);

		return _mapper.Map<UserProfileDto>(userProfile);
	}
}