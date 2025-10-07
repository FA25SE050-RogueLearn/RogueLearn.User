using AutoMapper;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Mappings;

public class MappingProfile : Profile
{
  public MappingProfile()
  {
    CreateMap<UserProfile, UserProfileDto>();
  }
}