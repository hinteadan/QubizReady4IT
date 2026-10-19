using KnowledgeGram.Models;

namespace KnowledgeGram.Abstractions;

/// <summary>
/// Stores and retrieves posts and their comments.
/// Callers know nothing about the underlying database, its schema, or its query language.
/// </summary>
public interface IPostRepository
{
    /// <summary>Returns matching posts, newest first, each with its comments (oldest first).</summary>
    Task<IReadOnlyList<Post>> FindAsync(PostQuery query);

    /// <summary>Returns the post, or null if it doesn't exist.</summary>
    Task<Post?> GetAsync(int postId);

    Task AddAsync(NewPost post);

    /// <summary>Deletes the post and its comments. Does nothing if it doesn't exist.</summary>
    Task DeleteAsync(int postId);

    Task AddLikeAsync(int postId);

    Task AddCommentAsync(NewComment comment);
}
