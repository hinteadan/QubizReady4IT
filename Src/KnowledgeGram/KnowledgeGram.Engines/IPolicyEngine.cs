using KnowledgeGram.Models;

namespace KnowledgeGram.Engines;

/// <summary>
/// The rules of the community: what may be posted, how big it can be, who may remove what.
/// Pure logic. It touches no database, no disk and no session.
/// </summary>
public interface IPolicyEngine
{
    IReadOnlyList<string> Subjects { get; }

    long MaxUploadBytes { get; }

    OperationResult ValidatePost(PublishRequest request);

    OperationResult ValidateComment(string text);

    /// <param name="userName">The acting user, or null if nobody is signed in.</param>
    bool CanDelete(string? userName, string postAuthor);
}
