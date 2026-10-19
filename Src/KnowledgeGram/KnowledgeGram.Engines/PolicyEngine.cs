using KnowledgeGram.Models;
using Microsoft.Extensions.Options;

namespace KnowledgeGram.Engines;

public class PolicyOptions
{
    public const string SectionName = "Policy";
    public long MaxUploadBytes { get; set; } = 100 * 1024 * 1024;
}

internal sealed class PolicyEngine : IPolicyEngine
{
    private readonly PolicyOptions _options;

    public PolicyEngine(IOptions<PolicyOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<string> Subjects { get; } =
        new[] { "Math", "Physics", "Chemistry", "Biology", "History", "Programming", "Other" };

    public long MaxUploadBytes => _options.MaxUploadBytes;

    public OperationResult ValidatePost(PublishRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return OperationResult.Fail("Please enter a title.");
        if (string.IsNullOrWhiteSpace(request.Body) && request.File is null)
            return OperationResult.Fail("Write something or attach a file.");
        if (!Subjects.Contains(request.Subject))
            return OperationResult.Fail("Please pick a subject.");
        if (request.File is not null && request.File.Size > MaxUploadBytes)
            return OperationResult.Fail("That file is too large.");
        return OperationResult.Ok();
    }

    public OperationResult ValidateComment(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? OperationResult.Fail("Write a comment first.")
            : OperationResult.Ok();

    public bool CanDelete(string? userName, string postAuthor) =>
        userName is not null && userName == postAuthor;
}
