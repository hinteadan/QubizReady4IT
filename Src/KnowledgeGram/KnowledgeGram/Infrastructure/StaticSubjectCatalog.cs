using KnowledgeGram.Abstractions;

namespace KnowledgeGram.Infrastructure;

public sealed class StaticSubjectCatalog : ISubjectCatalog
{
    public IReadOnlyList<string> All { get; } =
        new[] { "Math", "Physics", "Chemistry", "Biology", "History", "Programming", "Other" };
}
