using AutoMapper;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Features.Roles.Queries.GetAllRoles;
using RogueLearn.User.Application.Features.Roles.Commands.CreateRole;
using RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;
using RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.CreateCurriculumProgram;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.UpdateCurriculumProgram;
using GetAllCurriculumProgramDto = RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms.CurriculumProgramDto;
using GetByIdCurriculumProgramDto = RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms.CurriculumProgramDto;
using RogueLearn.User.Application.Features.CurriculumVersions.Commands.CreateCurriculumVersion;
using CurriculumVersionDto = RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumVersionsByProgram.CurriculumVersionDto;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Mappings;

public class MappingProfile : Profile
{
  public MappingProfile()
  {
    // UserProfile mappings
    CreateMap<UserProfile, UserProfileDto>();
    
    // Role mappings
    CreateMap<Role, RoleDto>();
    CreateMap<Role, CreateRoleResponse>();
    CreateMap<Role, UpdateRoleResponse>();
    
    // UserRole mappings - custom mapping for UserRoleDto
    CreateMap<UserRole, UserRoleDto>()
      .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId))
      .ForMember(dest => dest.AssignedAt, opt => opt.MapFrom(src => src.AssignedAt))
      .ForMember(dest => dest.RoleName, opt => opt.Ignore()) // Will be set manually
      .ForMember(dest => dest.Description, opt => opt.Ignore()); // Will be set manually

    // CurriculumProgram mappings
    CreateMap<CurriculumProgram, GetAllCurriculumProgramDto>();
    CreateMap<CurriculumProgram, GetByIdCurriculumProgramDto>();
    CreateMap<CurriculumProgram, CreateCurriculumProgramResponse>();
    CreateMap<CurriculumProgram, UpdateCurriculumProgramResponse>();

    // CurriculumVersion mappings
    CreateMap<CurriculumVersion, CurriculumVersionDto>();
    CreateMap<CurriculumVersion, CreateCurriculumVersionResponse>();
  }
}