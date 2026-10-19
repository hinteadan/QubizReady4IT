using KnowledgeGram.Models;

namespace KnowledgeGram.Engines;

internal sealed class FeedEngine : IFeedEngine
{
    public IReadOnlyList<Post> Arrange(IEnumerable<Post> posts) =>
        posts.OrderByDescending(p => p.CreatedAt)
             .ThenByDescending(p => p.Id)
             .ToList();
}
