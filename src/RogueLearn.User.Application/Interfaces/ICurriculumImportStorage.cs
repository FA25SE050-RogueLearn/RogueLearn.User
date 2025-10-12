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
}