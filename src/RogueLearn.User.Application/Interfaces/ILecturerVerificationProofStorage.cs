namespace RogueLearn.User.Application.Interfaces;

public interface ILecturerVerificationProofStorage
{
    Task<string> UploadAsync(Guid authUserId, byte[] content, string? contentType, string? fileName, CancellationToken cancellationToken = default);
}