using FluentValidation;
using System.Text.Json;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.UserProfiles.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    private readonly IAvatarUrlValidator _avatarUrlValidator;

    public UpdateMyProfileCommandValidator(IAvatarUrlValidator avatarUrlValidator)
    {
        _avatarUrlValidator = avatarUrlValidator;

        RuleFor(x => x.AuthUserId)
            .NotEmpty();

        RuleFor(x => x.FirstName)
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .MaximumLength(100);

        RuleFor(x => x.Bio)
            .MaximumLength(4000);

        // Either URL or file upload, but not both
        RuleFor(x => x)
            .Must(x => !(x.ProfileImageBytes != null && !string.IsNullOrWhiteSpace(x.ProfileImageUrl)))
            .WithMessage("Provide either ProfileImageUrl or an uploaded profileImage file, not both.");

        RuleFor(x => x.ProfileImageUrl)
            .Must(BeValidAvatarUrl).WithMessage("ProfileImageUrl must be a valid and allowed avatar URL")
            .When(x => !string.IsNullOrWhiteSpace(x.ProfileImageUrl));

        // Validate uploaded image constraints when bytes are provided
        When(x => x.ProfileImageBytes != null && x.ProfileImageBytes.Length > 0, () =>
        {
            RuleFor(x => x.ProfileImageBytes!)
                .Must(b => b.Length <= 5 * 1024 * 1024) // 5MB
                .WithMessage("Image file too large (max 5MB).");

            RuleFor(x => x)
                .Must(HaveAllowedContentTypeOrExtension)
                .WithMessage("Unsupported image type. Allowed: png, jpg/jpeg, webp, gif.");
        });

        RuleFor(x => x.PreferencesJson)
            .Must(BeValidJsonObject).WithMessage("PreferencesJson must be a valid JSON object")
            .When(x => !string.IsNullOrWhiteSpace(x.PreferencesJson));
    }

    private bool HaveAllowedContentTypeOrExtension(UpdateMyProfileCommand cmd)
    {
        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif"
        };
        var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "png", "jpg", "jpeg", "webp", "gif"
        };

        if (!string.IsNullOrWhiteSpace(cmd.ProfileImageContentType))
        {
            if (allowedContentTypes.Contains(cmd.ProfileImageContentType))
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(cmd.ProfileImageFileName))
        {
            var ext = Path.GetExtension(cmd.ProfileImageFileName).TrimStart('.').ToLowerInvariant();
            return allowedExts.Contains(ext);
        }

        return false; // neither valid content type nor file extension
    }

    private bool BeValidAvatarUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return _avatarUrlValidator.IsValid(url);
    }

    private static bool BeValidJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }
}