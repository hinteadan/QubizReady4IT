using KnowledgeGram.Models;

namespace KnowledgeGram.Engines;

internal sealed class MediaEngine : IMediaEngine
{
    private static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

    private static readonly HashSet<string> Videos = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".webm", ".mov" };

    private static readonly HashSet<string> Audio = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".wav" };

    public ContentKind Resolve(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (Images.Contains(ext)) return ContentKind.Image;
        if (Videos.Contains(ext)) return ContentKind.Video;
        if (Audio.Contains(ext)) return ContentKind.Audio;
        return ContentKind.Document;
    }
}
