// RogueLearn.User/src/RogueLearn.User.Infrastructure/Services/CurriculumImportStorage.cs
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

/*
 * ==========================================================================================
 * SUPABASE STORAGE BUCKET SETUP INSTRUCTIONS
 * ==========================================================================================
 * This service requires a Supabase Storage bucket to cache AI-processed data.
 *
 * 1. BUCKET NAME:
 *    Create a bucket named: `curriculum-imports`
 *
 * 2. PUBLIC BUCKET:
 *    Make the bucket PUBLIC. This is necessary for retrieving cached JSON files via URL.
 *    The contents are non-sensitive JSON caches of public curriculum/syllabus data.
 *
 * 3. POLICIES:
 *    The following policies allow authenticated users to perform all necessary operations (CRUD).
 *    Navigate to `Storage -> Policies` and create these policies for the `curriculum-imports` bucket.
 *
 *    -- Policy: Allow authenticated select (read)
 *    CREATE POLICY "Allow authenticated read access"
 *    ON storage.objects FOR SELECT
 *    TO authenticated
 *    USING (bucket_id = 'curriculum-imports');
 *
 *    -- Policy: Allow authenticated insert (create)
 *    CREATE POLICY "Allow authenticated insert access"
 *    ON storage.objects FOR INSERT
 *    TO authenticated
 *    WITH CHECK (bucket_id = 'curriculum-imports');
 *
 *    -- Policy: Allow authenticated update
 *    CREATE POLICY "Allow authenticated update access"
 *    ON storage.objects FOR UPDATE
 *    TO authenticated
 *    USING (bucket_id = 'curriculum-imports');
 *
 *    -- Policy: Allow authenticated delete
 *    CREATE POLICY "Allow authenticated delete access"
 *    ON storage.objects FOR DELETE
 *    TO authenticated
 *    USING (bucket_id = 'curriculum-imports');
 * ==========================================================================================
 */
namespace RogueLearn.User.Infrastructure.Services;

public class CurriculumImportStorage : ICurriculumImportStorage
{
    private readonly Supabase.Client _client;
    private readonly ILogger<CurriculumImportStorage> _logger;

    public CurriculumImportStorage(Supabase.Client client, ILogger<CurriculumImportStorage> logger)
    {
        _client = client;
        _logger = logger;
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

    #region Syllabus Storage Methods

    public async Task SaveSyllabusDataAsync(
        string subjectCode,
        int version,
        SyllabusData syllabusData,
        string extractedJson,
        string inputHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string bucketName = "curriculum-imports"; // Use same bucket but different folder

            if (string.IsNullOrEmpty(subjectCode))
            {
                _logger.LogWarning("Subject code is empty, using fallback storage key");
                // Fallback to temporary key if subject code is not available
                var tempKey = $"syllabus/_temp/validation_{inputHash}";
                await SaveSyllabusLatestAsync(
                    bucketName,
                    "_temp",
                    $"validation_{inputHash}",
                    extractedJson,
                    string.Empty,
                    inputHash,
                    cancellationToken);

                _logger.LogInformation("Saved syllabus validation data with temporary key: {Key}", tempKey);
                return;
            }

            // Create syllabus folder structure alongside curriculum folders
            await SaveSyllabusLatestAsync(
                bucketName,
                subjectCode,
                version.ToString(),
                extractedJson,
                string.Empty, // No raw text content for syllabus
                inputHash,
                cancellationToken);

            _logger.LogInformation(
                "Saved syllabus data in syllabus/{SubjectCode}/{Version}/ for subject: {SubjectCode} version: {Version}",
                subjectCode, version, subjectCode, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to save syllabus data for subject: {SubjectCode} version: {Version}",
                subjectCode, version);
            throw;
        }
    }

    public async Task<string?> TryGetCachedSyllabusDataAsync(
        string inputHash,
        CancellationToken cancellationToken = default)
    {
        var byHashJsonPath = $"syllabus/_hashes/{inputHash}.json";
        var storage = _client.Storage.From("curriculum-imports");
        try
        {
            // This is the corrected line. It explicitly provides null for the optional parameters,
            // resolving the CS0121 ambiguity error.
            var bytes = await storage.Download(byHashJsonPath, (Supabase.Storage.TransformOptions?)null, (EventHandler<float>?)null);
            return bytes is null ? null : Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            // Supabase client throws an exception for 404s, which is expected for cache misses.
            return null;
        }
    }

    public async Task<string?> GetSyllabusDataAsync(
        string subjectCode,
        int version,
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string bucketName = "curriculum-imports";
            // Version folder naming for syllabus follows the raw version number
            var data = await TryGetLatestJsonAsync(
                bucketName,
                $"syllabus/{subjectCode}",
                version.ToString(),
                cancellationToken);

            if (data != null)
            {
                _logger.LogInformation(
                    "Retrieved syllabus data for subject: {SubjectCode} version: {Version}",
                    subjectCode, version);
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve syllabus data for subject: {SubjectCode} version: {Version}",
                subjectCode, version);
            return null;
        }
    }

    public async Task<bool> ClearCachedSyllabusDataAsync(
        string inputHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string bucketName = "curriculum-imports";
            var result = await ClearCacheByHashAsync(
                bucketName,
                inputHash,
                cancellationToken);

            if (result)
            {
                _logger.LogInformation("Cleared cached syllabus data for hash: {Hash}", inputHash);
            }
            else
            {
                _logger.LogWarning("Failed to clear cached syllabus data for hash: {Hash}", inputHash);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cached syllabus data for hash: {Hash}", inputHash);
            return false;
        }
    }

    private async Task SaveSyllabusLatestAsync(
        string bucketName,
        string subjectCode,
        string versionCode,
        string jsonContent,
        string rawTextContent,
        string rawTextHash,
        CancellationToken cancellationToken = default)
    {
        // Paths for syllabus folder structure
        var safeSubject = subjectCode.Trim();
        var safeVersion = versionCode.Trim();
        var prefix = $"syllabus/{safeSubject}/{safeVersion}/";
        var latestJsonPath = prefix + "latest.json";
        var latestMetaPath = prefix + "latest.meta.json";
        var byHashJsonPath = $"syllabus/_hashes/{rawTextHash}.json";
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

        // Upload JSON versioned under syllabus/subject/version keyed by hash (historical/reference)
        await storage.Upload(jsonBytes, versionByHashJsonPath, new Supabase.Storage.FileOptions
        {
            ContentType = "application/json",
            Upsert = true
        });

        // Upload metadata (includes hash)
        var metaObj = new
        {
            rawTextHash = rawTextHash,
            lastUpdated = DateTime.UtcNow.ToString("O"),
            subjectCode = safeSubject,
            version = safeVersion
        };
        var metaJson = System.Text.Json.JsonSerializer.Serialize(metaObj);
        var metaBytes = Encoding.UTF8.GetBytes(metaJson);
        await storage.Upload(metaBytes, latestMetaPath, new Supabase.Storage.FileOptions
        {
            ContentType = "application/json",
            Upsert = true
        });

        _logger.LogInformation("Saved syllabus data to {Prefix} with hash {Hash}", prefix, rawTextHash);
    }

    #endregion
}