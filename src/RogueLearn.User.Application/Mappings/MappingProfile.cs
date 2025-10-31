// RogueLearn.User/src/RogueLearn.User.Application/Mappings/MappingProfile.cs
using AutoMapper;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;
using RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllRoutes;
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
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;
using RogueLearn.User.Application.Features.CurriculumStructure.Commands.AddSubjectToCurriculum;
using RogueLearn.User.Application.Features.CurriculumStructure.Commands.UpdateCurriculumStructure;
using RogueLearn.User.Application.Features.SyllabusVersions.Queries.GetSyllabusVersionsBySubject;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.CreateSyllabusVersion;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.UpdateSyllabusVersion;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;
using RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;
using System.Text.Json;
using RogueLearn.User.Application.Features.Achievements.Queries.GetAllAchievements;
using RogueLearn.User.Application.Features.Achievements.Commands.CreateAchievement;
using RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

namespace RogueLearn.User.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // UserProfile mappings
        CreateMap<UserProfile, UserProfileDto>()
          .ForMember(dest => dest.PreferencesJson, opt => opt.MapFrom(src => src.Preferences != null ? JsonSerializer.Serialize(src.Preferences, (JsonSerializerOptions)null!) : null));

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

        // Subject mappings
        CreateMap<Subject, SubjectDto>();
        CreateMap<Subject, CreateSubjectResponse>();
        CreateMap<Subject, UpdateSubjectResponse>();

        // CurriculumStructure mappings
        CreateMap<CurriculumStructure, AddSubjectToCurriculumResponse>();
        CreateMap<CurriculumStructure, UpdateCurriculumStructureResponse>();

        // SyllabusVersion mappings
        CreateMap<SyllabusVersion, SyllabusVersionDto>();
        CreateMap<SyllabusVersion, CreateSyllabusVersionResponse>();
        CreateMap<SyllabusVersion, UpdateSyllabusVersionResponse>();

        // CurriculumProgramDetails mappings
        CreateMap<CurriculumProgram, CurriculumProgramDetailsResponse>();
        CreateMap<CurriculumVersion, CurriculumVersionDetailsDto>();

        // Note mappings
        CreateMap<Note, NoteDto>();
        CreateMap<Note, CreateNoteResponse>();
        CreateMap<Note, UpdateNoteResponse>();

        // Skill Catalog mapping
        //CreateMap<Skill, SkillDto>();

        // Class Specialization mappings
        CreateMap<ClassSpecializationSubject, SpecializationSubjectDto>();
        CreateMap<AddSpecializationSubjectCommand, ClassSpecializationSubject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        //  Mappings for the Onboarding feature
        CreateMap<CurriculumProgram, RouteDto>();
        CreateMap<Class, ClassDto>();

        // Achievement mappings
        CreateMap<Achievement, AchievementDto>()
            .ForMember(dest => dest.RuleConfig, opt => opt.Ignore())
            .AfterMap((src, dest) => { dest.RuleConfig = src.RuleConfig != null ? JsonSerializer.Serialize(src.RuleConfig) : null; });
        CreateMap<Achievement, CreateAchievementResponse>()
            .ForMember(dest => dest.RuleConfig, opt => opt.Ignore())
            .AfterMap((src, dest) => { dest.RuleConfig = src.RuleConfig != null ? JsonSerializer.Serialize(src.RuleConfig) : null; });
        CreateMap<Achievement, UpdateAchievementResponse>()
            .ForMember(dest => dest.RuleConfig, opt => opt.Ignore())
            .AfterMap((src, dest) => { dest.RuleConfig = src.RuleConfig != null ? JsonSerializer.Serialize(src.RuleConfig) : null; });

        // Mappings for Quest Details
        CreateMap<Quest, QuestDetailsDto>();
        CreateMap<QuestStep, QuestStepDto>();

        // MODIFIED: Explicitly provide the default value for the optional parameter to satisfy the expression tree compiler.
        CreateMap<QuestStep, GeneratedQuestStepDto>()
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src =>
                !string.IsNullOrWhiteSpace(src.Content) ? JsonDocument.Parse(src.Content, default) : null));
    }
}