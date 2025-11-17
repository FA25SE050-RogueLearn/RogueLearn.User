using DotNetEnv;

namespace RogueLearn.User.Api.Utilities;

public class EnvironmentHelper 
{
    public static void LoadConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        Console.WriteLine("App Environment: " + environment);
        
        if (environment == "Development")
        {
            LoadDevelopmentConfig();
        }
        else
        {
            LoadProductionConfig();
        }
    }
    
    private static void LoadDevelopmentConfig()
    {
        // Load from .env file in development
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
            Console.WriteLine("Loaded configuration from .env file");
        }
        else
        {
            Console.WriteLine("Warning: .env file not found");
        }
    }
    
    private static void LoadProductionConfig()
    {
        Console.WriteLine("Loading Docker Secrets...");
        LoadDockerSecrets();
        
    }
    
    private static void LoadDockerSecrets()
    {
        var secretsPath = "/run/secrets";
        
        if (!Directory.Exists(secretsPath))
        {
            Console.WriteLine("⚠ Warning: /run/secrets directory not found");
            return;
        }
        
        var secretFiles = Directory.GetFiles(secretsPath);
        
        if (secretFiles.Length == 0)
        {
            Console.WriteLine("⚠ Warning: No secrets found in /run/secrets");
            return;
        }
        
        foreach (var secretFile in secretFiles)
        {
            try
            {
                var secretName = Path.GetFileName(secretFile);
                var secretValue = File.ReadAllText(secretFile).Trim();
                
                Environment.SetEnvironmentVariable(secretName, secretValue);
                
                // Testing purpose, delete later
                Console.WriteLine($"✓ Loaded secret: {secretName}, value = {secretValue}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error loading secret {secretFile}: {ex.Message}");
            }
        }
    }
    
    // Helper method to get configuration values
    public static string? GetValue(string key, string? defaultValue = null)
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }
    
    public static string GetRequiredValue(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Required configuration '{key}' is missing");
        }
        return value;
    }
}