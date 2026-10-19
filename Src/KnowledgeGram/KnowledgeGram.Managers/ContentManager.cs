using KnowledgeGram.Engines;
using KnowledgeGram.Models;
using KnowledgeGram.ResourceAccess;
using KnowledgeGram.Utilities;

namespace KnowledgeGram.Managers;

/// <summary>
/// Owns the SEQUENCE of each use case. Contains no rules of its own (engines do that)
/// and no knowledge of databases or disks (resource access does that).
/// </summary>
internal sealed class ContentManager : IContentManager
{
    private readonly IUserSession _session;
    private readonly IPolicyEngine _policy;
    private readonly IFeedEngine _feed;
    private readonly IMediaEngine _media;
    private readonly IPostAccess _posts;
    private readonly IFileAccess _files;

    public ContentManager(
        IUserSession session, IPolicyEngine policy, IFeedEngine feed, IMediaEngine media,
        IPostAccess posts, IFileAccess files)
    {
        _session = session;
        _policy = policy;
        _feed = feed;
        _media = media;
        _posts = posts;
        _files = files;
    }

    public IReadOnlyList<string> Subjects => _policy.Subjects;

    public long UploadLimitBytes => _policy.MaxUploadBytes;

    public async Task<IReadOnlyList<PostView>> BrowseAsync(PostQuery query)
    {
        var found = await _posts.FindAsync(query);
        var arranged = _feed.Arrange(found);
        return arranged.Select(ToView).ToList();
    }

    public async Task<OperationResult> PublishAsync(PublishRequest request)
    {
        if (_session.UserName is not { } user)
            return OperationResult.Fail("Please log in first.");

        var verdict = _policy.ValidatePost(request);
        if (!verdict.Succeeded) return verdict;

        StoredFile? attachment = null;
        if (request.File is not null)
            attachment = await _files.SaveAsync(request.File.Name, request.File.Content);

        try
        {
            await _posts.AddAsync(new NewPost(
                user, request.Title.Trim(), request.Body.Trim(), request.Subject, attachment));
        }
        catch
        {
            // Don't leave an orphaned file behind if the database write fails.
            if (attachment is not null) await _files.DeleteAsync(attachment.StoredName);
            throw;
        }

        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteAsync(int postId)
    {
        if (_session.UserName is not { } user)
            return OperationResult.Fail("Please log in first.");

        var post = await _posts.GetAsync(postId);
        if (post is null)
            return OperationResult.Fail("That post no longer exists.");

        if (!_policy.CanDelete(user, post.Author))
            return OperationResult.Fail("You can only delete your own posts.");

        await _posts.DeleteAsync(postId);
        if (post.Attachment is not null)
            await _files.DeleteAsync(post.Attachment.StoredName);

        return OperationResult.Ok();
    }

    public Task LikeAsync(int postId) => _posts.AddLikeAsync(postId);

    public async Task<OperationResult> CommentAsync(int postId, string text)
    {
        if (_session.UserName is not { } user)
            return OperationResult.Fail("Please log in first.");

        var verdict = _policy.ValidateComment(text);
        if (!verdict.Succeeded) return verdict;

        await _posts.AddCommentAsync(new NewComment(postId, user, text.Trim()));
        return OperationResult.Ok();
    }

    private PostView ToView(Post post)
    {
        AttachmentView? attachment = null;
        if (post.Attachment is not null)
        {
            attachment = new AttachmentView(
                _files.GetUrl(post.Attachment.StoredName),
                post.Attachment.OriginalName,
                _media.Resolve(post.Attachment.OriginalName));
        }

        return new PostView(
            post.Id, post.Author, post.Title, post.Body, post.Subject,
            post.CreatedAt, post.Likes, attachment,
            _policy.CanDelete(_session.UserName, post.Author),
            post.Comments);
    }
}
