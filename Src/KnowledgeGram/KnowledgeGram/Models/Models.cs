namespace KnowledgeGram.Models;

/// <summary>A file that has already been persisted by an <c>IFileStorage</c>.</summary>
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
