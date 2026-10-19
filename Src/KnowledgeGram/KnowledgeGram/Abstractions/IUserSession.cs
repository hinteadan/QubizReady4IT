namespace KnowledgeGram.Abstractions;

/// <summary>Who is using the app right now.</summary>
public interface IUserSession
{
    bool IsSignedIn { get; }

    /// <summary>The signed-in user's name, or null when nobody is signed in.</summary>
    string? UserName { get; }

    /// <exception cref="ArgumentException">If the name is blank.</exception>
    void SignIn(string userName);

    void SignOut();
}
