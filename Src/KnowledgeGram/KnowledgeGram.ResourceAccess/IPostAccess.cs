using KnowledgeGram.Models;

namespace KnowledgeGram.ResourceAccess;

/// <summary>
/// Reaches the place where posts, comments and likes are kept.
/// Callers know nothing about the database, its schema, or its query language,
/// and it makes no decisions about who may do what or how results are ordered.
/// </summary>
public interface IPostAccess
{
    /// <summary>Returns matching posts with their comments. Order is not guaranteed.</summary>
    Task<IReadOnlyList<Post>> FindAsync(PostQuery query);

    /// <summary>Returns the post, or null if it doesn't exist.</summary>
    Task<Post?> GetAsync(int postId);

    Task AddAsync(NewPost post);

    /// <summary>Deletes the post and its comments. Does nothing if it doesn't exist.</summary>
    Task DeleteAsync(int postId);

    Task AddLikeAsync(int postId);

    Task AddCommentAsync(NewComment comment);
}
