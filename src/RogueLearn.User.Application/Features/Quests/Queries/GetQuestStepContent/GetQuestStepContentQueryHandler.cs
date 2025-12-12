// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetQuestStepContent/GetQuestStepContentQueryHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewtonsoftJson = Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestStepContent;

public class GetQuestStepContentQueryHandler : IRequestHandler<GetQuestStepContentQuery, QuestStepContentResponse>
{
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ILogger<GetQuestStepContentQueryHandler> _logger;

    public GetQuestStepContentQueryHandler(
        IQuestStepRepository questStepRepository,
        ILogger<GetQuestStepContentQueryHandler> logger)
    {
        _questStepRepository = questStepRepository;
        _logger = logger;
    }

    public async Task<QuestStepContentResponse> Handle(
        GetQuestStepContentQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching content for QuestStep {QuestStepId}", request.QuestStepId);

        var questStep = await _questStepRepository.GetByIdAsync(request.QuestStepId, cancellationToken);

        if (questStep == null)
        {
            _logger.LogError("QuestStep with ID {QuestStepId} not found", request.QuestStepId);
            throw new NotFoundException("QuestStep", request.QuestStepId);
        }

        if (questStep.Content == null)
        {
            _logger.LogWarning("QuestStep {QuestStepId} has no content", request.QuestStepId);
            return new QuestStepContentResponse();
        }

        try
        {
            string jsonString;

            // Handle case where content is stored as a JSON string in the database
            if (questStep.Content is string contentString)
            {
                jsonString = contentString;
            }
            else if (questStep.Content is JToken jToken)
            {
                // If it's already a JToken (from Newtonsoft), convert to string
                jsonString = jToken.ToString(NewtonsoftJson.Formatting.None);
            }
            else
            {
                // For dictionary or other object types, serialize with Newtonsoft
                jsonString = NewtonsoftJson.JsonConvert.SerializeObject(questStep.Content);
            }

            _logger.LogInformation("QuestStep content JSON string ({Length} bytes)", jsonString.Length);

            // Parse using Newtonsoft to handle the activities array properly
            var contentObj = NewtonsoftJson.JsonConvert.DeserializeObject<JObject>(jsonString);

            if (contentObj == null)
            {
                _logger.LogError("Deserialization resulted in null object for QuestStep {QuestStepId}", request.QuestStepId);
                return new QuestStepContentResponse();
            }

            var response = new QuestStepContentResponse();

            if (contentObj.TryGetValue("activities", out var activitiesToken) && activitiesToken is JArray activitiesArray)
            {
                foreach (var activityToken in activitiesArray)
                {
                    if (activityToken is JObject activityObj)
                    {
                        var activity = new QuestStepActivity
                        {
                            ActivityId = activityObj["activityId"]?.ToString() ?? string.Empty,
                            Type = activityObj["type"]?.ToString() ?? string.Empty,
                            SkillId = activityObj["skillId"]?.ToString(),
                            // Convert payload JToken to a Dictionary that serializes properly
                            Payload = activityObj["payload"]?.ToObject<Dictionary<string, object>>(
                                new NewtonsoftJson.JsonSerializer
                                {
                                    Converters = { new NestedObjectConverter() }
                                })
                        };
                        response.Activities.Add(activity);
                    }
                }
            }

            _logger.LogInformation(
                "Successfully parsed QuestStep content: {ActivityCount} activities",
                response.Activities.Count);

            return response;
        }
        catch (NewtonsoftJson.JsonException njEx)
        {
            _logger.LogError(njEx, "JSON parsing failed for QuestStep {QuestStepId}", request.QuestStepId);
            throw new InvalidOperationException($"Failed to parse quest step content: {njEx.Message}", njEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing QuestStep {QuestStepId} content", request.QuestStepId);
            throw;
        }
    }

    /// <summary>
    /// Custom converter to handle nested JToken objects and convert them to proper .NET types
    /// </summary>
    private class NestedObjectConverter : NewtonsoftJson.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(object);
        }

        public override object? ReadJson(NewtonsoftJson.JsonReader reader, Type objectType, object? existingValue, NewtonsoftJson.JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            return ConvertToken(token);
        }

        private object? ConvertToken(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Object => ((JObject)token).Properties()
                    .ToDictionary(p => p.Name, p => ConvertToken(p.Value)),
                JTokenType.Array => ((JArray)token).Select(ConvertToken).ToList(),
                JTokenType.Integer => token.Value<long>(),
                JTokenType.Float => token.Value<double>(),
                JTokenType.String => token.Value<string>(),
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Null => null,
                _ => token.ToString()
            };
        }

        public override void WriteJson(NewtonsoftJson.JsonWriter writer, object? value, NewtonsoftJson.JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}