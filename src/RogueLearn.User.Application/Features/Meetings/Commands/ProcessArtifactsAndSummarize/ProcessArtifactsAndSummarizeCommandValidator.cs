using FluentValidation;
using RogueLearn.User.Application.Features.Meetings.DTOs;

namespace RogueLearn.User.Application.Features.Meetings.Commands.ProcessArtifactsAndSummarize;

public class ProcessArtifactsAndSummarizeCommandValidator : AbstractValidator<ProcessArtifactsAndSummarizeCommand>
{
    public ProcessArtifactsAndSummarizeCommandValidator()
    {
        RuleFor(x => x.MeetingId).NotEqual(Guid.Empty);
        RuleFor(x => x.Artifacts).NotNull().NotEmpty();
        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .When(x => x.Artifacts.Any(a => !string.IsNullOrWhiteSpace(a.FileId)));
        RuleForEach(x => x.Artifacts).ChildRules(a =>
        {
            a.RuleFor(y => y.ArtifactType).NotEmpty().MaximumLength(100);
            a.RuleFor(y => y)
                .Must(HasUrlOrFileId)
                .WithMessage("Each artifact must have either a valid absolute Url or a FileId");
            a.When(y => !string.IsNullOrWhiteSpace(y.Url), () =>
            {
                a.RuleFor(y => y.Url)
                    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                    .WithMessage("Url must be a valid absolute URI");
            });
        });
    }

    private static bool HasUrlOrFileId(ArtifactInputDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.FileId)) return true;
        return !string.IsNullOrWhiteSpace(dto.Url) && Uri.TryCreate(dto.Url, UriKind.Absolute, out _);
    }
}
