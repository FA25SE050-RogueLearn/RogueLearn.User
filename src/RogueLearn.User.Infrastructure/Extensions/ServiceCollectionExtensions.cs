// RogueLearn.User/src/RogueLearn.User.Infrastructure/Extensions/ServiceCollectionExtensions.cs
using BuildingBlocks.Shared.Interfaces;
using BuildingBlocks.Shared.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Infrastructure.Persistence;
using RogueLearn.User.Infrastructure.Services;
using Supabase;

namespace RogueLearn.User.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static async Task<IServiceCollection> AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Supabase client
        var supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL is not configured");
        var supabaseKey = configuration["Supabase:ApiKey"] ?? throw new InvalidOperationException("Supabase API Key is not configured");

        var options = new SupabaseOptions
        {
            AutoConnectRealtime = true
        };

        var supabase = new Client(supabaseUrl, supabaseKey, options);
        // Properly await the initialization
        await supabase.InitializeAsync();

        // Register Supabase client as a singleton for the application lifetime
        services.AddSingleton(supabase);

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
        services.AddScoped<IStudentTermSubjectRepository, StudentTermSubjectRepository>();
        services.AddScoped<IStudentEnrollmentRepository, StudentEnrollmentRepository>();
        services.AddScoped<ILecturerVerificationRequestRepository, LecturerVerificationRequestRepository>();

        // Curriculum repositories
        services.AddScoped<ICurriculumProgramRepository, CurriculumProgramRepository>();
        services.AddScoped<ICurriculumVersionRepository, CurriculumVersionRepository>();
        services.AddScoped<ICurriculumVersionActivationRepository, CurriculumVersionActivationRepository>();
        services.AddScoped<ICurriculumStructureRepository, CurriculumStructureRepository>();
        services.AddScoped<ISyllabusVersionRepository, SyllabusVersionRepository>();
        services.AddScoped<ICurriculumImportJobRepository, CurriculumImportJobRepository>();
        services.AddScoped<IElectivePackRepository, ElectivePackRepository>();
        services.AddScoped<IElectiveSourceRepository, ElectiveSourceRepository>();

        // Gamification repositories
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<ISkillDependencyRepository, SkillDependencyRepository>();

        // System repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Storage services
        services.AddScoped<ICurriculumImportStorage, CurriculumImportStorage>();

        // Register Message Bus (commented out per MVP scope)
        //services.AddScoped<IMessageBus, MassTransitMessageBus>();

        return services;
    }
}