using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using KnowledgeGram.Models;

namespace KnowledgeGram.ResourceAccess;

/// <summary>Keeps files in a folder on the web server's disk.</summary>
internal sealed class LocalFileAccess : IFileAccess
{
    private readonly FileStorageOptions _options;
    private readonly string _rootPath;

    public LocalFileAccess(IOptions<FileStorageOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _rootPath = Path.IsPathRooted(_options.RootFolder)
            ? _options.RootFolder
            : Path.Combine(environment.ContentRootPath, _options.RootFolder);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredFile> SaveAsync(string originalName, Stream content)
    {
        var safeOriginalName = Path.GetFileName(originalName);
        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(safeOriginalName)}";

        await using var target = new FileStream(
            Path.Combine(_rootPath, storedName), FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(target);

        return new StoredFile(storedName, safeOriginalName);
    }

    public Task DeleteAsync(string storedName)
    {
        // GetFileName stops callers from escaping the storage folder with "../".
        var path = Path.Combine(_rootPath, Path.GetFileName(storedName));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public string GetUrl(string storedName) =>
        $"{_options.PublicUrlPrefix.TrimEnd('/')}/{Uri.EscapeDataString(storedName)}";
}
