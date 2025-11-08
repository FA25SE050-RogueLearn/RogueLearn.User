using FluentValidation;

namespace RogueLearn.User.Application.Features.Meetings.Commands.ProcessArtifactsAndSummarize;

public class ProcessArtifactsAndSummarizeCommandValidator : AbstractValidator<ProcessArtifactsAndSummarizeCommand>
{
    public ProcessArtifactsAndSummarizeCommandValidator()
    {
        RuleFor(x => x.MeetingId).NotEqual(Guid.Empty);
        RuleFor(x => x.Artifacts).NotNull().NotEmpty();
        RuleForEach(x => x.Artifacts).ChildRules(a =>
        {
            a.RuleFor(y => y.ArtifactType).NotEmpty().MaximumLength(100);
            a.RuleFor(y => y.Url)
                .NotEmpty()
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("Url must be a valid absolute URI");
        });
    }
}