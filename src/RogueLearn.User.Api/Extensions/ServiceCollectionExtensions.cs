// RogueLearn.User/src/RogueLearn.User.Api/Extensions/ServiceCollectionExtensions.cs
using FluentValidation;
using MediatR;
using Microsoft.OpenApi.Models;
using RogueLearn.User.Application.Behaviours;
using RogueLearn.User.Application.Features.UserProfiles.Commands.LogNewUser;
using RogueLearn.User.Application.Mappings;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Infrastructure.Persistence;
using System.Reflection;
using RogueLearn.User.Infrastructure.Services;

namespace RogueLearn.User.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Options (feature flags / constraints)
        services.AddOptions<Application.Options.AiFileProcessingOptions>();

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(LogNewUserCommand).Assembly));

        // Add AutoMapper
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(LogNewUserCommand).Assembly);

        // Pipeline Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));

        // CRITICAL FIX: Read configuration ONCE outside the lambda
        var googleSearchApiKey = configuration["GoogleSearch:ApiKey"]
            ?? throw new InvalidOperationException("GoogleSearch:ApiKey is not configured.");
        var googleSearchEngineId = configuration["GoogleSearch:SearchEngineId"]
            ?? throw new InvalidOperationException("GoogleSearch:SearchEngineId is not configured.");

        // Configure HttpClient for Google Search
        services.AddHttpClient("GoogleSearchClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "RogueLearn-EducationPlatform/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        });

        // Register GoogleWebSearchService
        services.AddSingleton<IWebSearchService>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("GoogleSearchClient");
            var logger = serviceProvider.GetService<ILogger<GoogleWebSearchService>>();

            // Use the variables captured from outer scope - NO duplicate config reading
            return new GoogleWebSearchService(
                googleSearchApiKey,
                googleSearchEngineId,
                httpClient,
                logger);
        });

        // Register extraction plugins
        // MODIFICATION: Swapped CurriculumExtractionPlugin for HtmlCurriculumExtractionService (Non-AI)
        services.AddScoped<ICurriculumExtractionPlugin, HtmlCurriculumExtractionService>();

        services.AddScoped<ISyllabusExtractionPlugin, SyllabusExtractionPlugin>();
        // Register Roadmap SK plugin
        services.AddScoped<IRoadmapExtractionPlugin, RoadmapExtractionPlugin>();
        // Register Tagging SK plugin
        services.AddScoped<ITagSuggestionPlugin, TagSuggestionPlugin>();
        services.AddScoped<IFileTagSuggestionPlugin, FileTagSuggestionPlugin>();
        // Register TaggingSuggestionService
        services.AddScoped<ITaggingSuggestionService, TaggingSuggestionService>();
        // Register summarization plugins (text and file)
        services.AddScoped<ISummarizationPlugin, ContentSummarizationPlugin>();
        services.AddScoped<IFileSummarizationPlugin, ContentSummarizationPlugin>();
        services.AddScoped<IFapExtractionPlugin, FapExtractionPlugin>();
        services.AddScoped<ISubjectExtractionPlugin, SubjectExtractionPlugin>();
        // ADDED: Register the new plugin for quest step generation.
        services.AddScoped<IQuestGenerationPlugin, QuestGenerationPlugin>();
        services.AddScoped<ISkillDependencyAnalysisPlugin, SkillDependencyAnalysisPlugin>();
        services.AddScoped<IConstructiveQuestionGenerationPlugin, ConstructiveQuestionGenerationPlugin>();
        // ADDED: Register Academic Analysis Plugin
        services.AddScoped<IAcademicAnalysisPlugin, AcademicAnalysisPlugin>();
        // Register prompt builders
        services.AddScoped<IPromptBuilder, UserContextPromptBuilder>();
        services.AddScoped<QuestStepsPromptBuilder>();
        services.AddScoped<IAcademicContextBuilder, AcademicContextBuilder>();
        services.AddScoped<IQuestDifficultyResolver, QuestDifficultyResolver>();
        services.AddScoped<IGradeExperienceCalculator, GradeExperienceCalculator>();
        // ADDED: Topic Grouper Service
        services.AddScoped<ITopicGrouperService, TopicGrouperService>();

        // ADDED: Subject Import Service (Background Job)
        services.AddScoped<ISubjectImportService, SubjectImportService>();

        services.AddSingleton<IMemoryStore, InMemoryStore>();
        services.AddScoped<IAiQueryClassificationService, AiQueryClassificationService>();

        return services;
    }

    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "RogueLearn.User API", Version = "v1" });

            c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Please enter your JWT with Bearer into field. Example: \"Bearer {token}\"",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
        // Register the repository for match results.
        services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        services.AddScoped<IMatchResultRepository, MatchResultRepository>();
        services.AddScoped<IMatchPlayerSummaryRepository, MatchPlayerSummaryRepository>();

        return services;
    }
}