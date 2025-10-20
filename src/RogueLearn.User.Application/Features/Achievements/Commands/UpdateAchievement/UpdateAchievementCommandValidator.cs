using FluentValidation;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;

public class UpdateAchievementCommandValidator : AbstractValidator<UpdateAchievementCommand>
{
    private static readonly string[] AllowedRuleTypes = new[] { "threshold", "streak", "composite" };
    private static readonly string[] AllowedCategories = new[] { "core", "progression", "quests", "codebattle", "study", "skills" };

    public UpdateAchievementCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .NotEmpty();

        RuleFor(x => x.SourceService)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.IconUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("IconUrl must be a valid absolute URL if provided.");

        RuleFor(x => x.RuleType)
            .Must(rt => string.IsNullOrWhiteSpace(rt) || AllowedRuleTypes.Contains(rt))
            .WithMessage($"RuleType must be one of: {string.Join(", ", AllowedRuleTypes)} if provided.");

        RuleFor(x => x.RuleConfig)
            .Must(IsValidJsonOrEmpty)
            .WithMessage("RuleConfig must be valid JSON if provided.");

        RuleFor(x => x.Category)
            .Must(cat => string.IsNullOrWhiteSpace(cat) || AllowedCategories.Contains(cat))
            .WithMessage($"Category must be one of: {string.Join(", ", AllowedCategories)} if provided.");

        RuleFor(x => x.Version)
            .GreaterThanOrEqualTo(1);
    }

    private static bool IsValidJsonOrEmpty(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}