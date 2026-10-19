using KnowledgeGram.Models;

namespace KnowledgeGram.Engines;

/// <summary>Understands study materials: what kind of file is this and how should it be presented?</summary>
public interface IMediaEngine
{
    /// <summary>Anything unrecognised is <see cref="ContentKind.Document"/> (download link).</summary>
    ContentKind Resolve(string fileName);
}
