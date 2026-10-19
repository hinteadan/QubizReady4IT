using KnowledgeGram.Models;

namespace KnowledgeGram.Engines;

/// <summary>Decides what order the feed is shown in. Today: newest first. Tomorrow: whatever helps students most.</summary>
public interface IFeedEngine
{
    IReadOnlyList<Post> Arrange(IEnumerable<Post> posts);
}
