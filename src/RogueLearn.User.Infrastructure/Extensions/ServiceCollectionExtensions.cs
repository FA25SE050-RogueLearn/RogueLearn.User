// RogueLearn.User/src/RogueLearn.User.Infrastructure/Extensions/ServiceCollectionExtensions.cs
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
        // Register Supabase client as a scoped service so we can attach the caller's Authorization header per-request.
        services.AddScoped<Client>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();

            // Configure Supabase client
            var supabaseUrl = config["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL is not configured");
            var supabaseKey = config["Supabase:ApiKey"] ?? throw new InvalidOperationException("Supabase API Key is not configured");

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true,
                Headers = []
            };

            // Forward the incoming Authorization header (Bearer <jwt>) to Supabase
            var authHeader = httpContextAccessor.HttpContext?.Request?.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                options.Headers["Authorization"] = authHeader!;
            }

            var client = new Client(supabaseUrl, supabaseKey, options);

            client.InitializeAsync().GetAwaiter().GetResult();
            return client;
        });

        // MassTransit configuration remains commented out as per MVP scope.
        //services.AddMassTransit(busConfig => { ... });

        // Register Generic Repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // --- All Repository Registrations are now correctly included ---
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
        // ADDED: Register the new repository for specialization subjects
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

        // ADDED: Quest and Learning Path Repositories
        services.AddScoped<ILearningPathRepository, LearningPathRepository>();
        services.AddScoped<IQuestChapterRepository, QuestChapterRepository>();
        services.AddScoped<IQuestRepository, QuestRepository>();
        services.AddScoped<ILearningPathQuestRepository, LearningPathQuestRepository>();
        services.AddScoped<IQuestStepRepository, QuestStepRepository>(); 

        // System repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<INoteTagRepository, NoteTagRepository>();
        services.AddScoped<INoteSkillRepository, NoteSkillRepository>();
        services.AddScoped<INoteQuestRepository, NoteQuestRepository>();

        // Storage services
        services.AddScoped<ICurriculumImportStorage, CurriculumImportStorage>();
        services.AddScoped<IRoadmapImportStorage, RoadmapImportStorage>();
        services.AddScoped<IAvatarStorage, AvatarStorage>();
        services.AddScoped<IAchievementImageStorage, AchievementImageStorage>();

        // Register Message Bus (commented out per MVP scope)
        //services.AddScoped<IMessageBus, MassTransitMessageBus>();

        // PDF text extraction service
        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
        // General file text extractor (PDF, TXT, DOCX)
        services.AddScoped<IFileTextExtractor, DocumentTextExtractor>();

        // User context aggregation service
        services.AddScoped<IUserContextService, UserContextService>();

        // Avatar URL validator
        services.AddScoped<IAvatarUrlValidator, AvatarUrlValidator>();

        return services;
    }
}