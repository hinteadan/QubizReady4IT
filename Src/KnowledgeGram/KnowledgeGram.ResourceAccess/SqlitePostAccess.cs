using System.Globalization;
using KnowledgeGram.Models;
using KnowledgeGram.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace KnowledgeGram.ResourceAccess;

/// <summary>Stores posts and comments in a SQLite database.</summary>
internal sealed class SqlitePostAccess : IPostAccess
{
    private const string PostColumns = "Id, Author, Title, Body, Subject, FileName, OriginalName, CreatedAt, Likes";

    private readonly string _connectionString;
    private readonly IClock _clock;

    public SqlitePostAccess(IOptions<DatabaseOptions> options, IClock clock)
    {
        _connectionString = options.Value.ConnectionString;
        _clock = clock;
    }

    public Task<IReadOnlyList<Post>> FindAsync(PostQuery query) =>
        LoadPostsAsync(
            "WHERE (@subject = '' OR Subject = @subject) AND (Title LIKE @text OR Body LIKE @text)",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@subject", query.Subject ?? "");
                cmd.Parameters.AddWithValue("@text", $"%{query.SearchText ?? ""}%");
            });

    public async Task<Post?> GetAsync(int postId)
    {
        var posts = await LoadPostsAsync("WHERE Id = @id", cmd => cmd.Parameters.AddWithValue("@id", postId));
        return posts.FirstOrDefault();
    }

    public async Task AddAsync(NewPost post)
    {
        await using var conn = await OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO Posts (Author, Title, Body, Subject, FileName, OriginalName, CreatedAt, Likes) " +
            "VALUES (@author, @title, @body, @subject, @file, @original, @created, 0)";
        cmd.Parameters.AddWithValue("@author", post.Author);
        cmd.Parameters.AddWithValue("@title", post.Title);
        cmd.Parameters.AddWithValue("@body", post.Body);
        cmd.Parameters.AddWithValue("@subject", post.Subject);
        cmd.Parameters.AddWithValue("@file", post.Attachment?.StoredName ?? "");
        cmd.Parameters.AddWithValue("@original", post.Attachment?.OriginalName ?? "");
        cmd.Parameters.AddWithValue("@created", _clock.Now.ToString("O", CultureInfo.InvariantCulture));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int postId)
    {
        await using var conn = await OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Comments WHERE PostId = @id; DELETE FROM Posts WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", postId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddLikeAsync(int postId)
    {
        await using var conn = await OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Posts SET Likes = Likes + 1 WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", postId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddCommentAsync(NewComment comment)
    {
        await using var conn = await OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO Comments (PostId, Author, Text, CreatedAt) VALUES (@post, @author, @text, @created)";
        cmd.Parameters.AddWithValue("@post", comment.PostId);
        cmd.Parameters.AddWithValue("@author", comment.Author);
        cmd.Parameters.AddWithValue("@text", comment.Text);
        cmd.Parameters.AddWithValue("@created", _clock.Now.ToString("O", CultureInfo.InvariantCulture));
        await cmd.ExecuteNonQueryAsync();
    }

    // ---------- private helpers ----------

    private sealed record PostRow(
        int Id, string Author, string Title, string Body, string Subject,
        string FileName, string OriginalName, DateTime CreatedAt, int Likes);

    private async Task<SqliteConnection> OpenAsync()
    {
        var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }

    private async Task<IReadOnlyList<Post>> LoadPostsAsync(string whereClause, Action<SqliteCommand> bind)
    {
        await using var conn = await OpenAsync();

        var rows = new List<PostRow>();
        await using (var cmd = conn.CreateCommand())
        {
            // No ORDER BY on purpose: how the feed is ordered is the feed engine's decision.
            cmd.CommandText = $"SELECT {PostColumns} FROM Posts {whereClause}";
            bind(cmd);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new PostRow(
                    reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), reader.GetString(6),
                    DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                    reader.GetInt32(8)));
            }
        }

        if (rows.Count == 0) return Array.Empty<Post>();

        var comments = await LoadCommentsAsync(conn, rows.Select(r => r.Id).ToList());

        return rows
            .Select(r => new Post(
                r.Id, r.Author, r.Title, r.Body, r.Subject,
                r.FileName == "" ? null : new StoredFile(r.FileName, r.OriginalName),
                r.CreatedAt, r.Likes,
                comments[r.Id].ToList()))
            .ToList();
    }

    private static async Task<ILookup<int, Comment>> LoadCommentsAsync(SqliteConnection conn, List<int> postIds)
    {
        await using var cmd = conn.CreateCommand();

        var names = postIds.Select((_, i) => $"@p{i}").ToList();
        cmd.CommandText =
            $"SELECT Id, PostId, Author, Text, CreatedAt FROM Comments WHERE PostId IN ({string.Join(",", names)}) ORDER BY Id";
        for (var i = 0; i < postIds.Count; i++)
            cmd.Parameters.AddWithValue(names[i], postIds[i]);

        var comments = new List<Comment>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            comments.Add(new Comment(
                reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),
                DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture)));
        }
        return comments.ToLookup(c => c.PostId);
    }
}
