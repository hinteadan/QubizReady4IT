using KnowledgeGram.Abstractions;
using KnowledgeGram.Components;
using KnowledgeGram.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Configuration
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.SectionName));

// The ONE place that knows which implementation backs each abstraction.
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ISubjectCatalog, StaticSubjectCatalog>();
builder.Services.AddSingleton<IContentKindResolver, ExtensionContentKindResolver>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<IPostRepository, SqlitePostRepository>();
builder.Services.AddScoped<IUserSession, InMemoryUserSession>();   // scoped = one per browser connection
builder.Services.AddSingleton<SqliteDatabaseInitializer>();

var app = builder.Build();

app.Services.GetRequiredService<SqliteDatabaseInitializer>().EnsureCreated();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
