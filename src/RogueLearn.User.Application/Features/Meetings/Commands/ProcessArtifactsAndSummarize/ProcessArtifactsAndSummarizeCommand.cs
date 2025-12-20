using MediatR;
using RogueLearn.User.Application.Features.Meetings.DTOs;

namespace RogueLearn.User.Application.Features.Meetings.Commands.ProcessArtifactsAndSummarize;

public record ProcessArtifactsAndSummarizeCommand(Guid MeetingId, List<ArtifactInputDto> Artifacts, string AccessToken) : IRequest<Unit>;