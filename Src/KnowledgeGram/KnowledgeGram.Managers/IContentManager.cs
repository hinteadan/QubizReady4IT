using KnowledgeGram.Models;

namespace KnowledgeGram.Managers;

/// <summary>
/// Everything a user can DO with KnowledgeGram, as use cases.
/// Any client (web, mobile, API) talks to this and nothing below it.
/// The acting user is taken from the session, never passed in by the client.
/// </summary>
public interface IContentManager
{
    IReadOnlyList<string> Subjects { get; }

    /// <summary>Largest upload accepted, so clients can refuse early.</summary>
    long UploadLimitBytes { get; }

    Task<IReadOnlyList<PostView>> BrowseAsync(PostQuery query);

    Task<OperationResult> PublishAsync(PublishRequest request);

    Task<OperationResult> DeleteAsync(int postId);

    Task LikeAsync(int postId);

    Task<OperationResult> CommentAsync(int postId, string text);
}
