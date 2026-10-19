using KnowledgeGram.Models;

namespace KnowledgeGram.Abstractions;

/// <summary>Decides how an uploaded file should be presented.</summary>
public interface IContentKindResolver
{
    /// <summary>Anything unrecognised is <see cref="ContentKind.Document"/> (download link).</summary>
    ContentKind Resolve(string fileName);
}
