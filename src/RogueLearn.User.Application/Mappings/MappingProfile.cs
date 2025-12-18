// src/RogueLearn.User.Application/Mappings/MappingProfile.cs
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
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;
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
using RogueLearn.User.Application.Features.QuestFeedback.Queries.GetQuestFeedbackList; // ADDED

namespace RogueLearn.User.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Role mappings
        CreateMap<Role, RoleDto>();
        CreateMap<Role, CreateRoleResponse>();
        CreateMap<Role, UpdateRoleResponse>();

        CreateMap<UserProfile, UserProfileDto>();

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

        // Subject mappings
        CreateMap<Subject, SubjectDto>();
        CreateMap<Subject, CreateSubjectResponse>();
        CreateMap<Subject, UpdateSubjectResponse>();

        // CurriculumProgramDetails mappings
        CreateMap<CurriculumProgram, CurriculumProgramDetailsResponse>();

        // Note mappings
        CreateMap<Note, NoteDto>()
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => ParseJsonContent(src.Content)));
        CreateMap<Note, CreateNoteResponse>()
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => ParseJsonContent(src.Content)));
        CreateMap<Note, UpdateNoteResponse>()
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => ParseJsonContent(src.Content)));

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
            .ForMember(dest => dest.Bio, opt => opt.Ignore());
        CreateMap<PartyInvitation, PartyInvitationDto>()
            .ForCtorParam("JoinLink", opt => opt.MapFrom(_ => (string?)null))
            .ForCtorParam("GameSessionId", opt => opt.MapFrom(_ => (Guid?)null))
            .ForCtorParam("PartyName", opt => opt.MapFrom(_ => string.Empty))
            .ForCtorParam("InviteeName", opt => opt.MapFrom(_ => string.Empty));
        CreateMap<PartyStashItem, PartyStashItemDto>()
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => ParseJsonContent(src.Content)));

        // Meetings mappings
        CreateMap<Meeting, MeetingDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status ?? RogueLearn.User.Domain.Enums.MeetingStatus.Scheduled))
            .ReverseMap();
        CreateMap<MeetingParticipant, MeetingParticipantDto>().ReverseMap();

        // Quest Feedback Mapping
        CreateMap<UserQuestStepFeedback, QuestFeedbackDto>();
    }

    private static object? ParseJsonContent(object? content)
    {
        if (content is null)
            return null;

        // If content is a string, attempt to parse JSON; otherwise return as-is or normalize JsonElement
        if (content is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(s, (JsonSerializerOptions?)null);
                return ConvertJsonElement(jsonElement);
            }
            catch
            {
                // If the content is not valid JSON, return it as a plain string
                return s;
            }
        }

        if (content is JsonElement element)
        {
            return ConvertJsonElement(element);
        }

        // For already structured content (e.g., dictionaries/lists), return as-is
        return content;
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
