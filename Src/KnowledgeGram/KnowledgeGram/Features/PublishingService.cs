using KnowledgeGram.Abstractions;
using KnowledgeGram.Models;

namespace KnowledgeGram.Features;

/// <summary>FUNCTION: creating and removing posts.</summary>
public interface IPublishingService
{
    long MaxFileSizeBytes { get; }
    Task<OperationResult> PublishAsync(PublishRequest request);
    Task<OperationResult> DeleteAsync(int postId);
}

public sealed class PublishingService : IPublishingService
{
    private readonly IPostRepository _posts;
    private readonly IFileStorage _storage;
    private readonly IUserSession _session;
    private readonly ISubjectCatalog _subjects;

    public PublishingService(IPostRepository posts, IFileStorage storage, IUserSession session, ISubjectCatalog subjects)
    {
        _posts = posts;
        _storage = storage;
        _session = session;
        _subjects = subjects;
    }

    public long MaxFileSizeBytes => _storage.MaxFileSizeBytes;

    public async Task<OperationResult> PublishAsync(PublishRequest request)
    {
        // DUPLICATED RULE: EngagementService repeats this sign-in check.
        if (_session.UserName is not { } user)
            return OperationResult.Fail("Please log in first.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return OperationResult.Fail("Please enter a title.");
        if (string.IsNullOrWhiteSpace(request.Body) && request.File is null)
            return OperationResult.Fail("Write something or attach a file.");
        if (!_subjects.All.Contains(request.Subject))
            return OperationResult.Fail("Please pick a subject.");
        if (request.File is not null && request.File.Size > _storage.MaxFileSizeBytes)
            return OperationResult.Fail("That file is too large.");

        StoredFile? attachment = null;
        if (request.File is not null)
            attachment = await _storage.SaveAsync(request.File.Name, request.File.Content);

        try
        {
            await _posts.AddAsync(new NewPost(
                user, request.Title.Trim(), request.Body.Trim(), request.Subject, attachment));
        }
        catch
        {
            // Don't leave an orphaned file behind if the database write fails.
            if (attachment is not null) await _storage.DeleteAsync(attachment.StoredName);
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

        // DUPLICATED RULE: FeedService decides the same thing to show or hide the Delete button.
        if (post.Author != user)
            return OperationResult.Fail("You can only delete your own posts.");

        await _posts.DeleteAsync(postId);
        if (post.Attachment is not null)
            await _storage.DeleteAsync(post.Attachment.StoredName);

        return OperationResult.Ok();
    }
}
