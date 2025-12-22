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
            .WithMessage("Version number must be greater than 0.");

        RuleFor(x => x.Content)
            .NotNull()
            .WithMessage("Syllabus content is required.")
            .SetValidator(new SyllabusContentValidator());

        RuleFor(x => x.ApprovedDate)
            .Must(date => date == null || date >= DateOnly.FromDateTime(DateTime.Now.AddYears(-10)))
            .WithMessage("Effective date cannot be more than 10 years in the past.")
            .Must(date => date == null || date <= DateOnly.FromDateTime(DateTime.Now.AddYears(5)))
            .WithMessage("Effective date cannot be more than 5 years in the future.")
            .When(x => x.ApprovedDate.HasValue);
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

        // Course Learning Outcomes
        RuleForEach(x => x.CourseLearningOutcomes)
            .SetValidator(new CourseLearningOutcomeValidator())
            .When(x => x.CourseLearningOutcomes != null);

        RuleForEach(x => x.SessionSchedule)
            .SetValidator(new SyllabusSessionValidator())
            .When(x => x.SessionSchedule != null);

        RuleForEach(x => x.ConstructiveQuestions)
            .SetValidator(new ConstructiveQuestionValidator())
            .When(x => x.ConstructiveQuestions != null);
    }
}

public class SyllabusSessionValidator : AbstractValidator<SyllabusSessionDto>
{
    public SyllabusSessionValidator()
    {
        RuleFor(x => x.SessionNumber)
            .GreaterThan(0)
            .WithMessage("Session number must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("Session number cannot exceed 100.");

        RuleFor(x => x.Topic)
            .NotEmpty()
            .WithMessage("Session topic is required.")
            .MaximumLength(500)
            .WithMessage("Session topic cannot exceed 500 characters.");

        RuleFor(x => x.Activities)
            .Must(activities => activities == null || activities.All(a => !string.IsNullOrWhiteSpace(a)))
            .WithMessage("Activities cannot contain empty or whitespace-only values.")
            .When(x => x.Activities != null);

        RuleForEach(x => x.Activities)
            .MaximumLength(1000)
            .WithMessage("Each activity cannot exceed 1000 characters.")
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

public class ConstructiveQuestionValidator : AbstractValidator<ConstructiveQuestion>
{
    public ConstructiveQuestionValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty()
            .WithMessage("Question is required.")
            .MaximumLength(2000);

        RuleFor(x => x.Name)
            .MaximumLength(100);

        RuleFor(x => x.SessionNumber)
            .GreaterThan(0)
            .WithMessage("Session number must be greater than 0.")
            .When(x => x.SessionNumber.HasValue);
    }
}

public class CourseLearningOutcomeValidator : AbstractValidator<CourseLearningOutcome>
{
    public CourseLearningOutcomeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("CLO Id is required.")
            .MaximumLength(50);

        RuleFor(x => x.Details)
            .NotEmpty()
            .WithMessage("CLO details are required.")
            .MaximumLength(2000);
    }
}