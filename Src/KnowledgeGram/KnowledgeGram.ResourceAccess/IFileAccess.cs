using KnowledgeGram.Models;

namespace KnowledgeGram.ResourceAccess;

/// <summary>Reaches the place where uploaded study materials are kept (disk, cloud, ...).</summary>
public interface IFileAccess
{
    /// <summary>Persists the stream and returns a handle to the stored file.</summary>
    Task<StoredFile> SaveAsync(string originalName, Stream content);

    /// <summary>Removes the file. Does nothing if it doesn't exist.</summary>
    Task DeleteAsync(string storedName);

    /// <summary>The URL a browser can use to display or download the file.</summary>
    string GetUrl(string storedName);
}
