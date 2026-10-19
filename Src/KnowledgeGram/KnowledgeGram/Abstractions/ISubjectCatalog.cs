namespace KnowledgeGram.Abstractions;

/// <summary>The school subjects materials can be filed under.</summary>
public interface ISubjectCatalog
{
    IReadOnlyList<string> All { get; }
}
