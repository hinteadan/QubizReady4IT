using KnowledgeGram.Abstractions;

namespace KnowledgeGram.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}
