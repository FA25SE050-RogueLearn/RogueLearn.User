using BuildingBlocks.Shared.Interfaces;
using BuildingBlocks.Shared.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Infrastructure.Persistence;
using RogueLearn.User.Infrastructure.Messaging;
using RogueLearn.User.Infrastructure.Services;
using Supabase;

namespace RogueLearn.User.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register Supabase client as SCOPED with proper JWT handling per request
        services.AddScoped<Client>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();

            var supabaseUrl = config["Supabase:Url"]
                ?? throw new InvalidOperationException("Supabase URL is not configured");
            var supabaseKey = config["Supabase:ApiKey"]
                ?? throw new InvalidOperationException("Supabase API Key is not configured");

            // Get the Authorization header from the current request
            var authHeader = httpContextAccessor.HttpContext?.Request?.Headers["Authorization"].ToString();

            // Configure options with the Authorization header
            var options = new SupabaseOptions
            {
                AutoConnectRealtime = false, // Disable realtime to reduce overhead
                AutoRefreshToken = false,     // Client-side handles token refresh
                Headers = new Dictionary<string, string>
                {
                    { "apikey", supabaseKey }
                }
            };

            // Add the user's JWT token if present
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                options.Headers["Authorization"] = authHeader;
            }

            // Create and return the client
            // The headers set in options will be used for all Postgrest requests
            var client = new Client(supabaseUrl, supabaseKey, options);

            // Fire and forget initialization (non-blocking)
            // The client will initialize on first use if needed
            _ = Task.Run(async () =>
            {
                try
                {
                    await client.InitializeAsync();
                }
                catch
                {
                    // Initialization failures will be handled on actual use
                }
            });

            return client;
        });

        // Register Generic Repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // Register Specific Repositories
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();
        services.AddScoped<IUserQuestProgressRepository, UserQuestProgressRepository>();
        services.AddScoped<IUserSkillRepository, UserSkillRepository>();
        services.AddScoped<IUserSkillRewardRepository, UserSkillRewardRepository>();

        // Academic repositories
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IClassRepository, ClassRepository>();
        services.AddScoped<IClassNodeRepository, ClassNodeRepository>();
        services.AddScoped<IClassSpecializationSubjectRepository, ClassSpecializationSubjectRepository>();
        services.AddScoped<IStudentSemesterSubjectRepository, StudentSemesterSubjectRepository>();
        services.AddScoped<IStudentEnrollmentRepository, StudentEnrollmentRepository>();
        services.AddScoped<ILecturerVerificationRequestRepository, LecturerVerificationRequestRepository>();

        // Curriculum repositories
        services.AddScoped<ICurriculumProgramRepository, CurriculumProgramRepository>();
        services.AddScoped<ICurriculumVersionRepository, CurriculumVersionRepository>();
        services.AddScoped<ICurriculumVersionActivationRepository, CurriculumVersionActivationRepository>();
        services.AddScoped<ICurriculumStructureRepository, CurriculumStructureRepository>();
        services.AddScoped<ISyllabusVersionRepository, SyllabusVersionRepository>();

        // Gamification repositories
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<ISkillDependencyRepository, SkillDependencyRepository>();

        // Quest and Learning Path Repositories
        services.AddScoped<ILearningPathRepository, LearningPathRepository>();
        services.AddScoped<IQuestChapterRepository, QuestChapterRepository>();
        services.AddScoped<IQuestRepository, QuestRepository>();
        services.AddScoped<ILearningPathQuestRepository, LearningPathQuestRepository>();
        services.AddScoped<IQuestStepRepository, QuestStepRepository>();
        services.AddScoped<IUserQuestAttemptRepository, UserQuestAttemptRepository>();
        services.AddScoped<IUserQuestStepProgressRepository, UserQuestStepProgressRepository>();

        // Guild repositories
        services.AddScoped<IGuildRepository, GuildRepository>();
        services.AddScoped<IGuildMemberRepository, GuildMemberRepository>();
        services.AddScoped<IGuildInvitationRepository, GuildInvitationRepository>();
        services.AddScoped<IGuildPostRepository, GuildPostRepository>();

        // System repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<INoteTagRepository, NoteTagRepository>();
        services.AddScoped<INoteSkillRepository, NoteSkillRepository>();
        services.AddScoped<INoteQuestRepository, NoteQuestRepository>();

        // Social/Party repositories
        services.AddScoped<IPartyRepository, PartyRepository>();
        services.AddScoped<IPartyMemberRepository, PartyMemberRepository>();
        services.AddScoped<IPartyInvitationRepository, PartyInvitationRepository>();
        services.AddScoped<IPartyStashItemRepository, PartyStashItemRepository>();

        // Storage services
        services.AddScoped<ICurriculumImportStorage, CurriculumImportStorage>();
        services.AddScoped<IRoadmapImportStorage, RoadmapImportStorage>();
        services.AddScoped<IAvatarStorage, AvatarStorage>();
        services.AddScoped<IAchievementImageStorage, AchievementImageStorage>();

        // PDF text extraction service
        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
        services.AddScoped<IFileTextExtractor, DocumentTextExtractor>();

        // User context aggregation service
        services.AddScoped<IUserContextService, UserContextService>();

        // Avatar URL validator
        services.AddScoped<IAvatarUrlValidator, AvatarUrlValidator>();

        // Party notification service
        services.AddScoped<IPartyNotificationService, PartyNotificationService>();

        return services;
    }
}