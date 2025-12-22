using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Interfaces;

public interface ICurriculumImportStorage
{
    Task SaveLatestAsync(
        string bucketName,
        string programCode,
        string versionCode,
        string jsonContent,
        string rawTextContent,
        string rawTextHash,
        CancellationToken cancellationToken = default);

    Task<string?> TryGetLatestJsonAsync(
        string bucketName,
        string programCode,
        string versionCode,
        CancellationToken cancellationToken = default);

    Task<string?> TryGetLatestMetaJsonAsync(
        string bucketName,
        string programCode,
        string versionCode,
        CancellationToken cancellationToken = default);

    // Retrieve cached curriculum JSON by raw text hash to avoid AI extraction when identical
    Task<string?> TryGetByHashJsonAsync(
        string bucketName,
        string rawTextHash,
        CancellationToken cancellationToken = default);

    // Retrieve versioned curriculum JSON under program/version by raw text hash
    Task<string?> TryGetVersionedByHashJsonAsync(
        string bucketName,
        string programCode,
        string versionCode,
        string rawTextHash,
        CancellationToken cancellationToken = default);

    // Clear cached data by hash
    Task<bool> ClearCacheByHashAsync(
        string bucketName,
        string rawTextHash,
        CancellationToken cancellationToken = default);

    // Clear all cached data for a program/version
    Task<bool> ClearCacheForProgramVersionAsync(
        string bucketName,
        string programCode,
        string versionCode,
        CancellationToken cancellationToken = default);

    // Syllabus-specific methods using syllabus folder organization
    Task SaveSyllabusDataAsync(
        string subjectCode,
        int version,
        SyllabusData syllabusData,
        string extractedJson,
        string inputHash,
        CancellationToken cancellationToken = default);

    Task<string?> TryGetCachedSyllabusDataAsync(
        string inputHash,
        CancellationToken cancellationToken = default);

    Task<string?> GetSyllabusDataAsync(
        string subjectCode,
        int version,
        CancellationToken cancellationToken = default);

    Task<bool> ClearCachedSyllabusDataAsync(
        string inputHash,
        CancellationToken cancellationToken = default);

    // ==========================================================
    // NEW: User Academic Analysis Storage
    // ==========================================================

    /// <summary>
    /// Saves the AI-generated academic analysis report for a specific user.
    /// Path: user-data/{authUserId}/academic-analysis.json
    /// </summary>
    Task SaveUserAnalysisAsync(
        Guid authUserId,
        AcademicAnalysisReport report,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the cached academic analysis report for a user.
    /// </summary>
    Task<AcademicAnalysisReport?> GetUserAnalysisAsync(
        Guid authUserId,
        CancellationToken cancellationToken = default);
}