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
    /// <summary>
    /// Saves syllabus data with proper categorization by subject and version in syllabus folder
    /// </summary>
    /// <param name="subjectCode">The subject code for categorization</param>
    /// <param name="version">The version number</param>
    /// <param name="syllabusData">The syllabus data to save</param>
    /// <param name="extractedJson">The extracted JSON content</param>
    /// <param name="inputHash">The hash of the input text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveSyllabusDataAsync(
        string subjectCode,
        int version,
        SyllabusData syllabusData,
        string extractedJson,
        string inputHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to get cached syllabus data by input hash from syllabus folder
    /// </summary>
    /// <param name="inputHash">The hash of the input text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached JSON data if found, null otherwise</returns>
    Task<string?> TryGetCachedSyllabusDataAsync(
        string inputHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets syllabus data for a specific subject and version from syllabus folder
    /// </summary>
    /// <param name="subjectCode">The subject code</param>
    /// <param name="version">The version number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The syllabus JSON data if found, null otherwise</returns>
    Task<string?> GetSyllabusDataAsync(
        string subjectCode,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears cached syllabus data for a specific input hash from syllabus folder
    /// </summary>
    /// <param name="inputHash">The hash of the input text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cleared successfully, false otherwise</returns>
    Task<bool> ClearCachedSyllabusDataAsync(
        string inputHash,
        CancellationToken cancellationToken = default);
}