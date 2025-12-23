using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.GuildMasterActions;

public class PinGuildPostCommandHandler : IRequestHandler<PinGuildPostCommand, Unit>
{
    private readonly IGuildPostRepository _repo;
    public PinGuildPostCommandHandler(IGuildPostRepository repo) => _repo = repo;
    public async Task<Unit> Handle(PinGuildPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());
        post.IsPinned = true;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(post, cancellationToken);
        return Unit.Value;
    }
}

public class UnpinGuildPostCommandHandler : IRequestHandler<UnpinGuildPostCommand, Unit>
{
    private readonly IGuildPostRepository _repo;
    public UnpinGuildPostCommandHandler(IGuildPostRepository repo) => _repo = repo;
    public async Task<Unit> Handle(UnpinGuildPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());
        post.IsPinned = false;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(post, cancellationToken);
        return Unit.Value;
    }
}

public class LockGuildPostCommandHandler : IRequestHandler<LockGuildPostCommand, Unit>
{
    private readonly IGuildPostRepository _repo;
    public LockGuildPostCommandHandler(IGuildPostRepository repo) => _repo = repo;
    public async Task<Unit> Handle(LockGuildPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());
        post.IsLocked = true;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(post, cancellationToken);
        return Unit.Value;
    }
}

public class UnlockGuildPostCommandHandler : IRequestHandler<UnlockGuildPostCommand, Unit>
{
    private readonly IGuildPostRepository _repo;
    public UnlockGuildPostCommandHandler(IGuildPostRepository repo) => _repo = repo;
    public async Task<Unit> Handle(UnlockGuildPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());
        post.IsLocked = false;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(post, cancellationToken);
        return Unit.Value;
    }
}

public class SetAnnouncementPostCommandHandler : IRequestHandler<SetAnnouncementPostCommand, Unit>
{
    private readonly IGuildPostRepository _repo;
    public SetAnnouncementPostCommandHandler(IGuildPostRepository repo) => _repo = repo;
    public async Task<Unit> Handle(SetAnnouncementPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());
        post.IsAnnouncement = true;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(post, cancellationToken);
        return Unit.Value;
    }
}

public class UnsetAnnouncementPostCommandHandler : IRequestHandler<UnsetAnnouncementPostCommand, Unit>
{
    private readonly IGuildPostRepository _repo;
    public UnsetAnnouncementPostCommandHandler(IGuildPostRepository repo) => _repo = repo;
    public async Task<Unit> Handle(UnsetAnnouncementPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());
        post.IsAnnouncement = false;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(post, cancellationToken);
        return Unit.Value;
    }
}