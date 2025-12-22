using FluentValidation;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStepContent;

public class UpdateQuestStepContentValidator : AbstractValidator<UpdateQuestStepContentCommand>
{
    public UpdateQuestStepContentValidator()
    {
        RuleFor(x => x.QuestStepId).NotEmpty();
        RuleFor(x => x.Activities).NotNull();
        RuleForEach(x => x.Activities).SetValidator(new QuestStepActivityValidator());
    }
}

public class QuestStepActivityValidator : AbstractValidator<UpdateQuestStepActivityDto>
{
    public QuestStepActivityValidator()
    {
        RuleFor(x => x.ActivityId).NotEmpty();
        RuleFor(x => x.Type).NotEmpty().Must(BeValidType).WithMessage("Type must be Reading, KnowledgeCheck, or Quiz.");
        RuleFor(x => x.Payload).NotNull().WithMessage("Payload is required.");

        RuleFor(x => x).Custom((activity, context) =>
        {
            if (activity.Payload == null) return;

            // Validate ExperiencePoints exists
            if (!activity.Payload.ContainsKey("experiencePoints") || !IsNumber(activity.Payload["experiencePoints"]))
            {
                context.AddFailure("Payload", "Payload must contain numeric 'experiencePoints'.");
            }

            switch (activity.Type)
            {
                case "Reading":
                    ValidateReading(activity.Payload, context);
                    break;
                case "KnowledgeCheck":
                case "Quiz":
                    ValidateAssessment(activity.Payload, context);
                    break;
            }
        });
    }

    private bool BeValidType(string type)
    {
        return type == "Reading" || type == "KnowledgeCheck" || type == "Quiz";
    }

    private bool IsNumber(object value)
    {
        return value is int || value is long || value is double || value is decimal ||
               (value is JValue jv && (jv.Type == JTokenType.Integer || jv.Type == JTokenType.Float));
    }

    private void ValidateReading(Dictionary<string, object> payload, ValidationContext<UpdateQuestStepActivityDto> context)
    {
        if (!payload.ContainsKey("url") || string.IsNullOrWhiteSpace(payload["url"]?.ToString()))
            context.AddFailure("Payload", "Reading activity must have a 'url'.");

        if (!payload.ContainsKey("summary") || string.IsNullOrWhiteSpace(payload["summary"]?.ToString()))
            context.AddFailure("Payload", "Reading activity must have a 'summary'.");

        if (!payload.ContainsKey("articleTitle") || string.IsNullOrWhiteSpace(payload["articleTitle"]?.ToString()))
            context.AddFailure("Payload", "Reading activity must have an 'articleTitle'.");
    }

    private void ValidateAssessment(Dictionary<string, object> payload, ValidationContext<UpdateQuestStepActivityDto> context)
    {
        if (!payload.ContainsKey("questions"))
        {
            context.AddFailure("Payload", "Assessment activity must have 'questions'.");
            return;
        }

        // Convert to JArray if it's a JArray, or try casting list
        var questions = payload["questions"];
        if (questions is JArray jArray)
        {
            if (jArray.Count == 0) context.AddFailure("Payload", "Questions array cannot be empty.");

            foreach (var item in jArray)
            {
                ValidateQuestionObject(item, context);
            }
        }
        else if (questions is IEnumerable<object> list)
        {
            if (!list.Any()) context.AddFailure("Payload", "Questions array cannot be empty.");
            // Manual validation if it deserialized to List<Dictionary> or similar
            // This path usually handled by JArray in Newtonsoft context, but covering bases
        }
    }

    private void ValidateQuestionObject(JToken item, ValidationContext<UpdateQuestStepActivityDto> context)
    {
        if (item is not JObject qObj)
        {
            context.AddFailure("Payload", "Question item must be an object.");
            return;
        }

        if (string.IsNullOrWhiteSpace(qObj["question"]?.ToString()))
            context.AddFailure("Payload", "Question text is required.");

        if (string.IsNullOrWhiteSpace(qObj["answer"]?.ToString()))
            context.AddFailure("Payload", "Correct answer is required.");

        if (qObj["options"] is not JArray options || options.Count < 2)
            context.AddFailure("Payload", "Question must have at least 2 options.");
    }
}