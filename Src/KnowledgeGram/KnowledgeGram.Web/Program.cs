using KnowledgeGram.Engines;
using KnowledgeGram.Managers;
using KnowledgeGram.ResourceAccess;
using KnowledgeGram.Utilities;
using KnowledgeGram.Web.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Composition root: each layer registers itself. The web client only ever uses IContentManager and IUserSession.
builder.Services.AddUtilities();
builder.Services.AddResourceAccess(builder.Configuration);
builder.Services.AddEngines(builder.Configuration);
builder.Services.AddManagers();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
