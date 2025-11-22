// RogueLearn.User/src/RogueLearn.User.Infrastructure/Extensions/ServiceCollectionExtensions.cs
using BuildingBlocks.Shared.Interfaces;
using BuildingBlocks.Shared.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Infrastructure.Messaging;
using RogueLearn.User.Infrastructure.Persistence;
using RogueLearn.User.Infrastructure.Services;
using Supabase;

namespace RogueLearn.User.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Add HttpClientFactory for making HTTP requests reliably.
        services.AddHttpClient();

        // Register Supabase client as SCOPED with proper JWT handling per request.
        // This is the correct, stable, and performant approach.
        services.AddScoped<Client>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();

            var supabaseUrl = config["Supabase:Url"]
                ?? throw new InvalidOperationException("Supabase URL is not configured");
            var supabaseKey = config["Supabase:ApiKey"]
                ?? throw new InvalidOperationException("Supabase API Key is not configured");

            // Get the Authorization header from the current request's context.
            var authHeader = httpContextAccessor.HttpContext?.Request?.Headers["Authorization"].ToString();

            // Configure options. The headers set here will be used for all Postgrest requests
            // made by this specific client instance for the duration of the HTTP request.
            var options = new SupabaseOptions
            {
                AutoConnectRealtime = false, // Set to false to reduce unnecessary connections.
                Headers = new Dictionary<string, string>()
            };

            // Add the user's JWT token to the headers if it's present.
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                options.Headers["Authorization"] = authHeader;
            }

            var client = new Client(supabaseUrl, supabaseKey, options);

            // The blocking `.GetAwaiter().GetResult()` call is removed.
            // Initialization is now handled in a non-blocking, fire-and-forget task,
            // which prevents deadlocks and thread pool starvation that caused the connection errors.
            _ = Task.Run(async () =>
            {
                try
                {
                    await client.InitializeAsync();
                }
                catch
                {
                    // Initialization failures are handled gracefully. The client will attempt to
                    // initialize on first use if this background task fails.
                }
            });

            return client;
        });

        // Register Generic Repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // --- All Repository Registrations are now correctly included ---
        // Register Specific Repositories
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();
        // REMOVED: The IUserQuestProgressRepository is no longer needed.
        // services.AddScoped<IUserQuestProgressRepository, UserQuestProgressRepository>();
        services.AddScoped<IUserSkillRepository, UserSkillRepository>();
        services.AddScoped<IUserSkillRewardRepository, UserSkillRewardRepository>();

        // Academic repositories
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IClassRepository, ClassRepository>();
        services.AddScoped<IClassNodeRepository, ClassNodeRepository>();

        // instance of `ClassSpecializationSubjectRepository` when `IClassSpecializationSubjectRepository` is requested.
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

        // Quest and Learning Path Repositories
        services.AddScoped<ILearningPathRepository, LearningPathRepository>();
        services.AddScoped<IQuestChapterRepository, QuestChapterRepository>();
        services.AddScoped<IQuestRepository, QuestRepository>();
        services.AddScoped<IQuestStepRepository, QuestStepRepository>();
        services.AddScoped<IUserQuestAttemptRepository, UserQuestAttemptRepository>();
        services.AddScoped<IUserQuestStepProgressRepository, UserQuestStepProgressRepository>();


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
        services.AddScoped<INoteSkillRepository, NoteSkillRepository>();
        services.AddScoped<INoteQuestRepository, NoteQuestRepository>();

        // Social/Party repositories
        services.AddScoped<IPartyRepository, PartyRepository>();
        services.AddScoped<IPartyMemberRepository, PartyMemberRepository>();
        services.AddScoped<IPartyInvitationRepository, PartyInvitationRepository>();
        services.AddScoped<IPartyStashItemRepository, PartyStashItemRepository>();

        // Meeting repositories
        services.AddScoped<IMeetingRepository, MeetingRepository>();
        services.AddScoped<IMeetingParticipantRepository, MeetingParticipantRepository>();
        services.AddScoped<IMeetingSummaryRepository, MeetingSummaryRepository>();

        // NEW: Register the shared HTML Cleaning Service
        services.AddScoped<IHtmlCleaningService, HtmlCleaningService>();

        // Storage services
        services.AddScoped<ICurriculumImportStorage, CurriculumImportStorage>();
        services.AddScoped<IRoadmapImportStorage, RoadmapImportStorage>();
        services.AddScoped<IAvatarStorage, AvatarStorage>();
        services.AddScoped<IAchievementImageStorage, AchievementImageStorage>();

        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();

        // User context aggregation service
        services.AddScoped<IUserContextService, UserContextService>();

        // Avatar URL validator
        services.AddScoped<IAvatarUrlValidator, AvatarUrlValidator>();

        // Party notification service
        services.AddScoped<IPartyNotificationService, PartyNotificationService>();

        // Add the new ReadingUrlService for sourcing article URLs
        services.AddScoped<IReadingUrlService, ReadingUrlService>();

        // Add the new UrlValidationService for checking live links
        services.AddScoped<IUrlValidationService, UrlValidationService>();

        return services;
    }
}