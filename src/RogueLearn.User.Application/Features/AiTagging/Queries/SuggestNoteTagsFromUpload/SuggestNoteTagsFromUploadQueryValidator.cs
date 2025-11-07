using FluentValidation;
using Microsoft.Extensions.Options;
using RogueLearn.User.Application.Options;
using System.IO;

namespace RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;

public class SuggestNoteTagsFromUploadQueryValidator : AbstractValidator<SuggestNoteTagsFromUploadQuery>
{
    private readonly AiFileProcessingOptions _options;

    public SuggestNoteTagsFromUploadQueryValidator(IOptions<AiFileProcessingOptions> options)
    {
        _options = options.Value;

        RuleFor(x => x.AuthUserId).NotEmpty();
        RuleFor(x => x.FileStream).NotNull().WithMessage("File stream must not be null.");
        RuleFor(x => x.FileLength).NotNull().Must(l => l > 0).WithMessage("File length must be greater than 0.");
        RuleFor(x => x.MaxTags).InclusiveBetween(1, 20);

        // Centralized validation for file-first processing
        RuleFor(x => x.FileLength).LessThanOrEqualTo(_options.MaxFileSizeMB * 1024 * 1024)
            .WithMessage($"File size must be <= {_options.MaxFileSizeMB}MB.");

        RuleFor(x => x)
            .Must(cmd => HasAllowedContentTypeOrExtension(cmd.ContentType, cmd.FileName))
            .WithMessage($"Unsupported content type. Allowed: {string.Join(", ", _options.AllowedMimeTypes)}.");
    }

    private bool HasAllowedContentTypeOrExtension(string? contentType, string? fileName)
    {
        var allowedTypes = new HashSet<string>(_options.AllowedMimeTypes, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(contentType) && allowedTypes.Contains(contentType))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
            var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "pdf", "txt", "docx", "pptx"
            };
            return allowedExts.Contains(ext);
        }

        return false;
    }
}