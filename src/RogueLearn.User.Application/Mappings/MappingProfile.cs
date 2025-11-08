using AutoMapper;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;
using RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllRoutes;
// MODIFICATION: Commented out the using statement for a missing query to resolve compilation error.
// using RogueLearn.User.Application.Features.Onboarding.Queries.GetOnboardingVersionsByProgram;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Features.Roles.Queries.GetAllRoles;
using RogueLearn.User.Application.Features.Roles.Commands.CreateRole;
using RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;
using RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.CreateCurriculumProgram;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.UpdateCurriculumProgram;
using GetAllCurriculumProgramDto = RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms.CurriculumProgramDto;
using GetByIdCurriculumProgramDto = RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms.CurriculumProgramDto;
// MODIFICATION: Commented out unused using statements related to obsolete entities.
// using RogueLearn.User.Application.Features.CurriculumVersions.Commands.CreateCurriculumVersion;
// using CurriculumVersionDto = RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumVersionsByProgram.CurriculumVersionDto;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;
// using RogueLearn.User.Application.Features.CurriculumStructure.Commands.AddSubjectToCurriculum;
// using RogueLearn.User.Application.Features.CurriculumStructure.Commands.UpdateCurriculumStructure;
// using RogueLearn.User.Application.Features.SyllabusVersions.Queries.GetSyllabusVersionsBySubject;
// using RogueLearn.User.Application.Features.SyllabusVersions.Commands.CreateSyllabusVersion;
// using RogueLearn.User.Application.Features.SyllabusVersions.Commands.UpdateSyllabusVersion;
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
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Features.Meetings.DTOs;

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

        // MODIFICATION: Commented out mappings for obsolete entities.
        // CreateMap<CurriculumVersion, CurriculumVersionDto>();
        // CreateMap<CurriculumVersion, CreateCurriculumVersionResponse>();

        // MODIFICATION: Commented out mapping for a non-existent DTO to resolve compilation error.
        // CreateMap<CurriculumVersion, OnboardingVersionDto>();


        // Subject mappings
        CreateMap<Subject, SubjectDto>();
        CreateMap<Subject, CreateSubjectResponse>();
        CreateMap<Subject, UpdateSubjectResponse>();

        // MODIFICATION: Commented out mappings for obsolete entities.
        // CreateMap<CurriculumStructure, AddSubjectToCurriculumResponse>();
        // CreateMap<CurriculumStructure, UpdateCurriculumStructureResponse>();

        // CreateMap<SyllabusVersion, SyllabusVersionDto>()
        //     .ForMember(dest => dest.Content, opt => opt.MapFrom(src =>
        //         src.Content != null ? JsonSerializer.Serialize(src.Content, (JsonSerializerOptions)null!) : string.Empty));
        // CreateMap<SyllabusVersion, CreateSyllabusVersionResponse>()
        //     .ForMember(dest => dest.Content, opt => opt.MapFrom(src =>
        //         src.Content != null ? JsonSerializer.Serialize(src.Content, (JsonSerializerOptions)null!) : string.Empty));
        // CreateMap<SyllabusVersion, UpdateSyllabusVersionResponse>()
        //     .ForMember(dest => dest.Content, opt => opt.MapFrom(src =>
        //         src.Content != null ? JsonSerializer.Serialize(src.Content, (JsonSerializerOptions)null!) : string.Empty));

        // CurriculumProgramDetails mappings
        CreateMap<CurriculumProgram, CurriculumProgramDetailsResponse>();
        // CreateMap<CurriculumVersion, CurriculumVersionDetailsDto>();

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

        CreateMap<QuestStep, QuestStepDto>()
    .ForMember(dest => dest.Content, opt => opt.MapFrom(src => ParseJsonContent(src.Content)));

        // Mapping for GenerateQuestSteps command (returns GeneratedQuestStepDto):
        CreateMap<QuestStep, GeneratedQuestStepDto>()
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => ParseJsonContent(src.Content)));
        // Parties feature mappings
        CreateMap<Party, PartyDto>();
        CreateMap<PartyMember, PartyMemberDto>()
            .ForMember(dest => dest.Username, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.FirstName, opt => opt.Ignore())
            .ForMember(dest => dest.LastName, opt => opt.Ignore())
            .ForMember(dest => dest.ProfileImageUrl, opt => opt.Ignore())
            .ForMember(dest => dest.Level, opt => opt.Ignore())
            .ForMember(dest => dest.ExperiencePoints, opt => opt.Ignore())
            .ForMember(dest => dest.Bio, opt => opt.Ignore());
        CreateMap<PartyInvitation, PartyInvitationDto>();
        CreateMap<PartyStashItem, PartyStashItemDto>();

        // Meetings mappings
        CreateMap<Meeting, MeetingDto>().ReverseMap();
        CreateMap<MeetingParticipant, MeetingParticipantDto>().ReverseMap();
    }
    private static object? ParseJsonContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var element = JsonSerializer.Deserialize<JsonElement>(content, (JsonSerializerOptions?)null);
        return ConvertJsonElement(element);
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElement)
                .ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intValue)
                ? (object)intValue
                : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}
