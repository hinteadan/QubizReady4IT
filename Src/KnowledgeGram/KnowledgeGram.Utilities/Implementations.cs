namespace KnowledgeGram.Utilities;

internal sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}

/// <summary>Remembers the user for the lifetime of the browser connection. No passwords yet.</summary>
internal sealed class InMemoryUserSession : IUserSession
{
    public string? UserName { get; private set; }

    public bool IsSignedIn => UserName is not null;

    public void SignIn(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("User name can't be blank.", nameof(userName));
        UserName = userName.Trim();
    }

    public void SignOut() => UserName = null;
}
