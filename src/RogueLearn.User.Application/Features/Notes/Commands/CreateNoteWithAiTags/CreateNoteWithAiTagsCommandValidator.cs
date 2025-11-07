using FluentValidation;
using Microsoft.Extensions.Options;
using RogueLearn.User.Application.Options;
using System.IO;

namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNoteWithAiTags;

public class CreateNoteWithAiTagsCommandValidator : AbstractValidator<CreateNoteWithAiTagsCommand>
{
  private readonly AiFileProcessingOptions _options;

  public CreateNoteWithAiTagsCommandValidator(IOptions<AiFileProcessingOptions> options)
  {
    _options = options.Value;

    RuleFor(x => x.AuthUserId).NotEmpty();
    RuleFor(x => x.MaxTags).InclusiveBetween(1, 20);
    RuleFor(x => x.Title).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Title));

    RuleFor(x => x)
        .Must(cmd => !string.IsNullOrWhiteSpace(cmd.RawText) || (cmd.FileStream is not null && (cmd.FileLength ?? 0) > 0))
        .WithMessage("Either rawText or a file must be provided.");

    // File validations (only when a file is provided)
    When(x => x.FileStream is not null && (x.FileLength ?? 0) > 0, () =>
    {
      RuleFor(x => x.FileLength!.Value)
              .LessThanOrEqualTo(_options.MaxFileSizeMB * 1024 * 1024)
              .WithMessage($"File size must be <= {_options.MaxFileSizeMB}MB.");

      RuleFor(x => x)
              .Must(cmd => HasAllowedContentTypeOrExtension(cmd.ContentType, cmd.FileName))
              .WithMessage($"Unsupported content type. Allowed: {string.Join(", ", _options.AllowedMimeTypes)}.");
    });
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