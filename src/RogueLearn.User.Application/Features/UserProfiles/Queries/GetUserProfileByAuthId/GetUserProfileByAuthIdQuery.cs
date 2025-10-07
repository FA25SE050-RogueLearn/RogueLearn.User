using MediatR;

namespace RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;

public class GetUserProfileByAuthIdQuery : IRequest<UserProfileDto?>
{
	public Guid AuthId { get; set; }

	public GetUserProfileByAuthIdQuery(Guid authId)
	{
		AuthId = authId;
	}
}