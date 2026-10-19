using KnowledgeGram.Abstractions;
using KnowledgeGram.Models;

namespace KnowledgeGram.Features;

/// <summary>FUNCTION: reacting to posts with likes and comments.</summary>
public interface IEngagementService
{
    Task LikeAsync(int postId);
    Task<OperationResult> CommentAsync(int postId, string text);
}

public sealed class EngagementService : IEngagementService
{
    private readonly IPostRepository _posts;
    private readonly IUserSession _session;

    public EngagementService(IPostRepository posts, IUserSession session)
    {
        _posts = posts;
        _session = session;
    }

    public Task LikeAsync(int postId) => _posts.AddLikeAsync(postId);

    public async Task<OperationResult> CommentAsync(int postId, string text)
    {
        // DUPLICATED RULE: PublishingService repeats this sign-in check.
        if (_session.UserName is not { } user)
            return OperationResult.Fail("Please log in first.");
        if (string.IsNullOrWhiteSpace(text))
            return OperationResult.Fail("Write a comment first.");

        await _posts.AddCommentAsync(new NewComment(postId, user, text.Trim()));
        return OperationResult.Ok();
    }
}
