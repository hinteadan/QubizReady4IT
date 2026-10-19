namespace KnowledgeGram.Abstractions;

/// <summary>The current time. Exists so time can be controlled in tests.</summary>
public interface IClock
{
    DateTime Now { get; }
}
