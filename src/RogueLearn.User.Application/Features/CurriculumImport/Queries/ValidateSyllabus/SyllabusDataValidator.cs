using FluentValidation;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

public class SyllabusDataValidator : AbstractValidator<SyllabusData>
{
    public SyllabusDataValidator()
    {
        RuleFor(x => x.SubjectCode)
            .NotEmpty()
            .WithMessage("Subject code is required.")
            .MaximumLength(20)
            .WithMessage("Subject code cannot exceed 20 characters.")
            .Matches("^[A-Z0-9_-]+$")
            .WithMessage("Subject code can only contain uppercase letters, numbers, underscores, and hyphens.");

        RuleFor(x => x.VersionNumber)
            .GreaterThan(0)
            .WithMessage("Version number must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("Version number cannot exceed 100.");

        RuleFor(x => x.Content)
            .NotNull()
            .WithMessage("Syllabus content is required.")
            .SetValidator(new SyllabusContentValidator());

        RuleFor(x => x.EffectiveDate)
            .Must(date => date == null || date >= DateOnly.FromDateTime(DateTime.Now.AddYears(-10)))
            .WithMessage("Effective date cannot be more than 10 years in the past.")
            .Must(date => date == null || date <= DateOnly.FromDateTime(DateTime.Now.AddYears(5)))
            .WithMessage("Effective date cannot be more than 5 years in the future.")
            .When(x => x.EffectiveDate.HasValue);
    }
}

public class SyllabusContentValidator : AbstractValidator<SyllabusContent>
{
    public SyllabusContentValidator()
    {
        RuleFor(x => x.CourseDescription)
            .MaximumLength(2000)
            .WithMessage("Course description cannot exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.CourseDescription));

        RuleFor(x => x.LearningOutcomes)
            .Must(outcomes => outcomes == null || outcomes.All(o => !string.IsNullOrWhiteSpace(o)))
            .WithMessage("Learning outcomes cannot contain empty or whitespace-only values.")
            .When(x => x.LearningOutcomes != null);

        RuleForEach(x => x.LearningOutcomes)
            .MaximumLength(500)
            .WithMessage("Each learning outcome cannot exceed 500 characters.")
            .When(x => x.LearningOutcomes != null);

        RuleForEach(x => x.WeeklySchedule)
            .SetValidator(new SyllabusWeekValidator())
            .When(x => x.WeeklySchedule != null);

        RuleForEach(x => x.Assessments)
            .SetValidator(new AssessmentItemValidator())
            .When(x => x.Assessments != null);

        RuleFor(x => x.RequiredTexts)
            .Must(texts => texts == null || texts.All(t => !string.IsNullOrWhiteSpace(t)))
            .WithMessage("Required texts cannot contain empty or whitespace-only values.")
            .When(x => x.RequiredTexts != null);

        RuleForEach(x => x.RequiredTexts)
            .MaximumLength(300)
            .WithMessage("Each required text cannot exceed 300 characters.")
            .When(x => x.RequiredTexts != null);

        RuleFor(x => x.RecommendedTexts)
            .Must(texts => texts == null || texts.All(t => !string.IsNullOrWhiteSpace(t)))
            .WithMessage("Recommended texts cannot contain empty or whitespace-only values.")
            .When(x => x.RecommendedTexts != null);

        RuleForEach(x => x.RecommendedTexts)
            .MaximumLength(300)
            .WithMessage("Each recommended text cannot exceed 300 characters.")
            .When(x => x.RecommendedTexts != null);

        RuleFor(x => x.GradingPolicy)
            .MaximumLength(1000)
            .WithMessage("Grading policy cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.GradingPolicy));

        RuleFor(x => x.AttendancePolicy)
            .MaximumLength(1000)
            .WithMessage("Attendance policy cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.AttendancePolicy));
    }
}

public class SyllabusWeekValidator : AbstractValidator<SyllabusWeek>
{
    public SyllabusWeekValidator()
    {
        RuleFor(x => x.WeekNumber)
            .GreaterThan(0)
            .WithMessage("Week number must be greater than 0.")
            .LessThanOrEqualTo(52)
            .WithMessage("Week number cannot exceed 52.");

        RuleFor(x => x.Topic)
            .NotEmpty()
            .WithMessage("Week topic is required.")
            .MaximumLength(200)
            .WithMessage("Week topic cannot exceed 200 characters.");

        RuleFor(x => x.Activities)
            .Must(activities => activities == null || activities.All(a => !string.IsNullOrWhiteSpace(a)))
            .WithMessage("Activities cannot contain empty or whitespace-only values.")
            .When(x => x.Activities != null);

        RuleForEach(x => x.Activities)
            .MaximumLength(300)
            .WithMessage("Each activity cannot exceed 300 characters.")
            .When(x => x.Activities != null);

        RuleFor(x => x.Readings)
            .Must(materials => materials == null || materials.All(m => !string.IsNullOrWhiteSpace(m)))
            .WithMessage("Reading materials cannot contain empty or whitespace-only values.")
            .When(x => x.Readings != null);

        RuleForEach(x => x.Readings)
            .MaximumLength(300)
            .WithMessage("Each reading material cannot exceed 300 characters.")
            .When(x => x.Readings != null);
    }
}

public class AssessmentItemValidator : AbstractValidator<AssessmentItem>
{
    public AssessmentItemValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Assessment type is required.")
            .MaximumLength(50)
            .WithMessage("Assessment type cannot exceed 50 characters.");

        RuleFor(x => x.WeightPercentage)
            .GreaterThan(0)
            .WithMessage("Weight percentage must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("Weight percentage cannot exceed 100.")
            .When(x => x.WeightPercentage.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Assessment description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}