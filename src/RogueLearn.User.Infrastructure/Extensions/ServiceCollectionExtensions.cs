using BuildingBlocks.Shared.Interfaces;
using BuildingBlocks.Shared.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RogueLearn.User.Application.Features.Quests.Services;
using RogueLearn.User.Application.Features.QuestSubmissions.Services;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Infrastructure.Persistence;
using RogueLearn.User.Infrastructure.Services;
using Supabase;

namespace RogueLearn.User.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddHttpClient();

        services.AddScoped<Client>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();

            var supabaseUrl = config["Supabase:Url"]
                ?? throw new InvalidOperationException("Supabase URL is not configured");
            var supabaseKey = config["Supabase:ApiKey"]
                ?? throw new InvalidOperationException("Supabase API Key is not configured");
            var authHeader = httpContextAccessor.HttpContext?.Request?.Headers["Authorization"].ToString();
            var options = new SupabaseOptions
            {
                AutoConnectRealtime = false,
                Headers = new Dictionary<string, string>()
            };
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                options.Headers["Authorization"] = authHeader;
            }

            var client = new Client(supabaseUrl, supabaseKey, options);
            _ = Task.Run(async () =>
            {
                try
                {
                    await client.InitializeAsync();
                }
                catch
                {

                }
            });

            return client;
        });

        // Generic Repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // Users Repositories
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();
        services.AddScoped<IUserSkillRepository, UserSkillRepository>();
        services.AddScoped<IUserSkillRewardRepository, UserSkillRewardRepository>();

        // Academic repositories
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IClassRepository, ClassRepository>();
        services.AddScoped<IClassSpecializationSubjectRepository, ClassSpecializationSubjectRepository>();
        services.AddScoped<IStudentSemesterSubjectRepository, StudentSemesterSubjectRepository>();
        services.AddScoped<IStudentEnrollmentRepository, StudentEnrollmentRepository>();
        services.AddScoped<ILecturerVerificationRequestRepository, LecturerVerificationRequestRepository>();

        // Curriculum repositories
        services.AddScoped<ICurriculumProgramRepository, CurriculumProgramRepository>();
        services.AddScoped<ICurriculumProgramSubjectRepository, CurriculumProgramSubjectRepository>();
        // Register the repository for the new subject_skill_mappings table.
        services.AddScoped<ISubjectSkillMappingRepository, SubjectSkillMappingRepository>();

        // Gamification repositories
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<ISkillDependencyRepository, SkillDependencyRepository>();

        // Quests Repositories
        services.AddScoped<IQuestRepository, QuestRepository>();
        services.AddScoped<IQuestStepRepository, QuestStepRepository>();
        services.AddScoped<IUserQuestAttemptRepository, UserQuestAttemptRepository>();
        services.AddScoped<IUserQuestStepProgressRepository, UserQuestStepProgressRepository>();
        services.AddScoped<IQuestSubmissionRepository, QuestSubmissionRepository>();

        // Game session repositories (Unity boss fight multiplayer)
        services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        services.AddScoped<IMatchResultRepository, MatchResultRepository>();
        services.AddScoped<IMatchPlayerSummaryRepository, MatchPlayerSummaryRepository>();
        services.AddScoped<IUserQuestStepFeedbackRepository, UserQuestStepFeedbackRepository>();

        // Guild repositories
        services.AddScoped<IGuildRepository, GuildRepository>();
        services.AddScoped<IGuildMemberRepository, GuildMemberRepository>();
        services.AddScoped<IGuildInvitationRepository, GuildInvitationRepository>();
        services.AddScoped<IGuildJoinRequestRepository, GuildJoinRequestRepository>();
        // Guild posts repository
        services.AddScoped<IGuildPostRepository, GuildPostRepository>();
        services.AddScoped<IGuildPostCommentRepository, GuildPostCommentRepository>();
        services.AddScoped<IGuildPostLikeRepository, GuildPostLikeRepository>();

        // System repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<INoteTagRepository, NoteTagRepository>();

        // Social/Party repositories
        services.AddScoped<IPartyRepository, PartyRepository>();
        services.AddScoped<IPartyMemberRepository, PartyMemberRepository>();
        services.AddScoped<IPartyInvitationRepository, PartyInvitationRepository>();
        services.AddScoped<IPartyStashItemRepository, PartyStashItemRepository>();

        // Meeting repositories
        services.AddScoped<IMeetingRepository, MeetingRepository>();
        services.AddScoped<IMeetingParticipantRepository, MeetingParticipantRepository>();
        services.AddScoped<IMeetingSummaryRepository, MeetingSummaryRepository>();

        services.AddScoped<IHtmlCleaningService, HtmlCleaningService>();

        // Storage services
        services.AddScoped<ICurriculumImportStorage, CurriculumImportStorage>();
        services.AddScoped<IRoadmapImportStorage, RoadmapImportStorage>();
        services.AddScoped<IAvatarStorage, AvatarStorage>();
        services.AddScoped<IAchievementImageStorage, AchievementImageStorage>();
        services.AddScoped<IGuildPostImageStorage, GuildPostImageStorage>();
        services.AddScoped<ILecturerVerificationProofStorage, LecturerVerificationProofStorage>();

        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();

        // User context aggregation service
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IRpcFullUserInfoService, RpcFullUserInfoService>();

        // Avatar URL validator
        services.AddScoped<IAvatarUrlValidator, AvatarUrlValidator>();

        // Notification services
        services.AddScoped<IPartyNotificationService, PartyNotificationService>();
        services.AddScoped<IGuildNotificationService, GuildNotificationService>();
        services.AddScoped<ILecturerNotificationService, LecturerNotificationService>();
        services.AddScoped<IMeetingNotificationService, MeetingNotificationService>();

        // ReadingUrlService for sourcing article URLs
        services.AddScoped<IReadingUrlService, ReadingUrlService>();

        // UrlValidationService for checking live links
        services.AddScoped<IUrlValidationService, UrlValidationService>();
        services.AddScoped<ActivityValidationService>();
        services.AddScoped<IQuizValidationService, QuizValidationService>();
        services.AddScoped<IKnowledgeCheckValidationService, KnowledgeCheckValidationService>();
        services.AddScoped<ICodingValidationService, CodingValidationService>();

        return services;
    }
}