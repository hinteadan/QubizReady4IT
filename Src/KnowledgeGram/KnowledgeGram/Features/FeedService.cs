using KnowledgeGram.Abstractions;
using KnowledgeGram.Models;

namespace KnowledgeGram.Features;

/// <summary>FUNCTION: browsing, searching and filtering what has been shared.</summary>
public interface IFeedService
{
    Task<IReadOnlyList<PostView>> GetFeedAsync(PostQuery query);
}

public sealed class FeedService : IFeedService
{
    private readonly IPostRepository _posts;
    private readonly IFileStorage _storage;
    private readonly IContentKindResolver _kinds;
    private readonly IUserSession _session;

    public FeedService(IPostRepository posts, IFileStorage storage, IContentKindResolver kinds, IUserSession session)
    {
        _posts = posts;
        _storage = storage;
        _kinds = kinds;
        _session = session;
    }

    public async Task<IReadOnlyList<PostView>> GetFeedAsync(PostQuery query)
    {
        var found = await _posts.FindAsync(query);       // knows the database abstraction
        return found.Select(ToView).ToList();
    }

    private PostView ToView(Post post)
    {
        AttachmentView? attachment = null;
        if (post.Attachment is not null)
        {
            attachment = new AttachmentView(                       // knows the file-storage abstraction
                _storage.GetUrl(post.Attachment.StoredName),
                post.Attachment.OriginalName,
                _kinds.Resolve(post.Attachment.OriginalName));
        }

        // DUPLICATED RULE: PublishingService.DeleteAsync enforces this same "only the author" rule.
        var canDelete = post.Author == _session.UserName;         // knows the identity abstraction

        return new PostView(
            post.Id, post.Author, post.Title, post.Body, post.Subject,
            post.CreatedAt, post.Likes, attachment, canDelete, post.Comments);
    }
}
