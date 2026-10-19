using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace KnowledgeGram.Infrastructure;

/// <summary>Creates the tables if they aren't there yet. Called once at startup.</summary>
public sealed class SqliteDatabaseInitializer
{
    private readonly string _connectionString;

    public SqliteDatabaseInitializer(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public void EnsureCreated()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "CREATE TABLE IF NOT EXISTS Posts (Id INTEGER PRIMARY KEY AUTOINCREMENT, Author TEXT, Title TEXT, Body TEXT, Subject TEXT, FileName TEXT, OriginalName TEXT, CreatedAt TEXT, Likes INTEGER);" +
            "CREATE TABLE IF NOT EXISTS Comments (Id INTEGER PRIMARY KEY AUTOINCREMENT, PostId INTEGER, Author TEXT, Text TEXT, CreatedAt TEXT);";
        cmd.ExecuteNonQuery();
    }
}
