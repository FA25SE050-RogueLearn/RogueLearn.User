using Microsoft.Extensions.Diagnostics.HealthChecks;
using RogueLearn.User.Domain.Entities;
using Supabase;

namespace RogueLearn.User.Api.HealthChecks;

public class SupabaseHealthCheck : IHealthCheck
{
    private readonly Client _supabaseClient;
    private readonly ILogger<SupabaseHealthCheck> _logger;

    public SupabaseHealthCheck(
        Client supabaseClient, 
        ILogger<SupabaseHealthCheck> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Simple query to keep connection warm
            await _supabaseClient
                .From<UserProfile>()
                .Select("id")
                .Limit(1)
                .Get(cancellationToken);
            
            stopwatch.Stop();
            
            _logger.LogDebug(
                "Supabase health check succeeded in {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);
            
            return HealthCheckResult.Healthy(
                $"Supabase connected ({stopwatch.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Supabase health check failed");
            return HealthCheckResult.Unhealthy(
                "Supabase connection failed", 
                ex);
        }
    }
}