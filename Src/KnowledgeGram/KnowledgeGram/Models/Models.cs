namespace KnowledgeGram.Models;

// ----- data that travels between layers -----

/// <summary>A file that has already been persisted by file storage.</summary>
/// <param name="StoredName">Opaque key the storage understands. Never show it to users.</param>
/// <param name="OriginalName">The name the author uploaded it with. Safe to display.</param>
public record StoredFile(string StoredName, string OriginalName);

public record Comment(int Id, int PostId, string Author, string Text, DateTime CreatedAt);

public record Post(
    int Id,
    string Author,
    string Title,
    string Body,
    string Subject,
    StoredFile? Attachment,
    DateTime CreatedAt,
    int Likes,
    IReadOnlyList<Comment> Comments);

public record NewPost(string Author, string Title, string Body, string Subject, StoredFile? Attachment);

public record NewComment(int PostId, string Author, string Text);

/// <summary>Null or empty values mean "don't filter on this".</summary>
public record PostQuery(string? Subject = null, string? SearchText = null);

public enum ContentKind { Image, Video, Audio, Document }

// ----- what the UI sends in and gets back -----

public record UploadedFile(string Name, Stream Content, long Size);

public record PublishRequest(string Title, string Body, string Subject, UploadedFile? File);

public record AttachmentView(string Url, string DisplayName, ContentKind Kind);

/// <summary>A post exactly as the UI needs to draw it: no storage keys, no rules left to evaluate.</summary>
public record PostView(
    int Id,
    string Author,
    string Title,
    string Body,
    string Subject,
    DateTime CreatedAt,
    int Likes,
    AttachmentView? Attachment,
    bool CanDelete,
    IReadOnlyList<Comment> Comments);

public record OperationResult(bool Succeeded, string? Error = null)
{
    public static OperationResult Ok() => new(true);
    public static OperationResult Fail(string error) => new(false, error);
}
