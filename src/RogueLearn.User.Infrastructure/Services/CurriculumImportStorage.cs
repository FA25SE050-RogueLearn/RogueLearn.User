using RogueLearn.User.Application.Interfaces;
using System.Text;
using System.Text.Json;

namespace RogueLearn.User.Infrastructure.Services;

public class CurriculumImportStorage : ICurriculumImportStorage
{
    private readonly Supabase.Client _client;

    public CurriculumImportStorage(Supabase.Client client)
    {
        _client = client;
    }

    public async Task SaveLatestAsync(
        string bucketName,
        string programCode,
        string versionCode,
        string jsonContent,
        string rawTextContent,
        string rawTextHash,
        CancellationToken cancellationToken = default)
    {
        // Paths
        var safeProgram = programCode.Trim();
        var safeVersion = versionCode.Trim();
        var prefix = $"curriculum/{safeProgram}/{safeVersion}/";
        var latestJsonPath = prefix + "latest.json";
        var latestMetaPath = prefix + "latest.meta.json";
        var rawTextPath = prefix + "raw/latest.txt";
        var byHashJsonPath = $"curriculum/_hashes/{rawTextHash}.json";
        var versionByHashJsonPath = prefix + $"versions/{rawTextHash}.json";

        // Get bucket
        var storage = _client.Storage.From(bucketName);

        // Upload JSON (overwrite)
        var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
        await storage.Upload(jsonBytes, latestJsonPath, new Supabase.Storage.FileOptions
        {
            ContentType = "application/json",
            Upsert = true
        });

        // Upload JSON by hash (cache), overwrite so latest known content for this raw text is available
        await storage.Upload(jsonBytes, byHashJsonPath, new Supabase.Storage.FileOptions
        {
            ContentType = "application/json",
            Upsert = true
        });

        // Upload JSON versioned under program/version keyed by hash (historical/reference)
        await storage.Upload(jsonBytes, versionByHashJsonPath, new Supabase.Storage.FileOptions
        {
            ContentType = "application/json",
            Upsert = true
        });

        // Upload raw text (overwrite)
        var rawBytes = Encoding.UTF8.GetBytes(rawTextContent);
        await storage.Upload(rawBytes, rawTextPath, new Supabase.Storage.FileOptions
        {
            ContentType = "text/plain",
            Upsert = true
        });

        // Upload metadata (includes hash)
        var metaObj = new
        {
            rawTextHash = rawTextHash,
            programCode = safeProgram,
            versionCode = safeVersion
        };
        var metaJson = JsonSerializer.Serialize(metaObj);
        var metaBytes = Encoding.UTF8.GetBytes(metaJson);
        await storage.Upload(metaBytes, latestMetaPath, new Supabase.Storage.FileOptions
        {
            ContentType = "application/json",
            Upsert = true
        });
    }

    public async Task<string?> TryGetLatestJsonAsync(
        string bucketName,
        string programCode,
        string versionCode,
        CancellationToken cancellationToken = default)
    {
        var safeProgram = programCode.Trim();
        var safeVersion = versionCode.Trim();
        var prefix = $"curriculum/{safeProgram}/{safeVersion}/";
        var latestJsonPath = prefix + "latest.json";

        var storage = _client.Storage.From(bucketName);
        try
        {
            var bytes = await storage.Download(latestJsonPath, (Supabase.Storage.TransformOptions?)null, (EventHandler<float>?)null);
            return bytes is null ? null : Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> TryGetLatestMetaJsonAsync(
        string bucketName,
        string programCode,
        string versionCode,
        CancellationToken cancellationToken = default)
    {
        var safeProgram = programCode.Trim();
        var safeVersion = versionCode.Trim();
        var prefix = $"curriculum/{safeProgram}/{safeVersion}/";
        var latestMetaPath = prefix + "latest.meta.json";

        var storage = _client.Storage.From(bucketName);
        try
        {
            var bytes = await storage.Download(latestMetaPath, (Supabase.Storage.TransformOptions?)null, (EventHandler<float>?)null);
            return bytes is null ? null : Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> TryGetByHashJsonAsync(
        string bucketName,
        string rawTextHash,
        CancellationToken cancellationToken = default)
    {
        var byHashJsonPath = $"curriculum/_hashes/{rawTextHash}.json";
        var storage = _client.Storage.From(bucketName);
        try
        {
            var bytes = await storage.Download(byHashJsonPath, (Supabase.Storage.TransformOptions?)null, (EventHandler<float>?)null);
            return bytes is null ? null : Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> TryGetVersionedByHashJsonAsync(
        string bucketName,
        string programCode,
        string versionCode,
        string rawTextHash,
        CancellationToken cancellationToken = default)
    {
        var safeProgram = programCode.Trim();
        var safeVersion = versionCode.Trim();
        var prefix = $"curriculum/{safeProgram}/{safeVersion}/";
        var versionByHashJsonPath = prefix + $"versions/{rawTextHash}.json";

        var storage = _client.Storage.From(bucketName);
        try
        {
            var bytes = await storage.Download(versionByHashJsonPath, (Supabase.Storage.TransformOptions?)null, (EventHandler<float>?)null);
            return bytes is null ? null : Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ClearCacheByHashAsync(
        string bucketName,
        string rawTextHash,
        CancellationToken cancellationToken = default)
    {
        var storage = _client.Storage.From(bucketName);
        var byHashJsonPath = $"curriculum/_hashes/{rawTextHash}.json";

        try
        {
            await storage.Remove(new List<string> { byHashJsonPath });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ClearCacheForProgramVersionAsync(
        string bucketName,
        string programCode,
        string versionCode,
        CancellationToken cancellationToken = default)
    {
        var safeProgram = programCode.Trim();
        var safeVersion = versionCode.Trim();
        var prefix = $"curriculum/{safeProgram}/{safeVersion}/";

        var storage = _client.Storage.From(bucketName);

        try
        {
            // List all files in the program/version directory
            var files = await storage.List(prefix);
            if (files?.Any() == true)
            {
                var filePaths = files.Select(f => prefix + f.Name).ToList();
                await storage.Remove(filePaths);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}