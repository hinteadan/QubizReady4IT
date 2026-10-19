using KnowledgeGram.Models;

namespace KnowledgeGram.Abstractions;

/// <summary>
/// Stores uploaded study materials somewhere (disk, cloud, ...) and tells callers how to reach them.
/// </summary>
public interface IFileStorage
{
    /// <summary>Largest file this storage accepts, in bytes.</summary>
    long MaxFileSizeBytes { get; }

    /// <summary>Persists the stream and returns a handle to the stored file.</summary>
    Task<StoredFile> SaveAsync(string originalName, Stream content);

    /// <summary>Removes the file. Does nothing if it doesn't exist.</summary>
    Task DeleteAsync(string storedName);

    /// <summary>The URL a browser can use to display or download the file.</summary>
    string GetUrl(string storedName);
}
