using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Features.Guilds.Commands.CreateGuild;

public record CreateGuildCommand : IRequest<CreateGuildResponse>
{
    public Guid CreatorAuthUserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Privacy { get; init; } = "public"; // "invite_only" | "public"
    public int MaxMembers { get; init; } = 50;
}

public record CreateGuildResponse
{
    public Guid GuildId { get; init; }
    public string RoleGranted { get; init; } = "GuildMaster";
    public GuildDto Guild { get; init; } = new();
}