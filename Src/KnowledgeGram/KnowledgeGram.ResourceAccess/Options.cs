namespace KnowledgeGram.ResourceAccess;

public class DatabaseOptions
{
    public const string SectionName = "Database";
    public string ConnectionString { get; set; } = "Data Source=knowledgegram.db";
}

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Relative paths are resolved against the app's content root.</summary>
    public string RootFolder { get; set; } = "wwwroot/uploads";

    /// <summary>URL prefix under which RootFolder is reachable by browsers.</summary>
    public string PublicUrlPrefix { get; set; } = "/uploads";
}
