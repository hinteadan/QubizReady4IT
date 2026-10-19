**What changed**

Six abstractions replace the copy-pasted code from Step 0:

| Abstraction | Hides | Today's implementation |
|---|---|---|
| `IPostRepository` | Database, SQL, schema | `SqlitePostRepository` |
| `IFileStorage` | Where and how files live, and how to reach them | `LocalFileStorage` |
| `IUserSession` | How "who am I" is tracked | `InMemoryUserSession` |
| `IContentKindResolver` | How a file is classified for display | `ExtensionContentKindResolver` |
| `ISubjectCatalog` | Where the subject list comes from | `StaticSubjectCatalog` |
| `IClock` | System time | `SystemClock` |

The wiring is in `Program.cs`, which is now the only place that names a concrete class. Connection string, upload folder, and size limit moved to `appsettings.json`.

**Why these count as proper abstractions, not just interfaces**
- **Domain-level contracts.** The interfaces speak in `Post`, `StoredFile`, and `PostQuery`. No `SqliteConnection`, `IBrowserFile`, or disk path crosses a boundary. `SaveAsync` takes a plain `Stream` rather than a Blazor type.
- **Documented behavior.** Each interface says what it promises, such as "returns null if missing", "newest first", and "does nothing if it doesn't exist".
- **No leaking.** `IFileStorage.GetUrl` means the page doesn't know uploads happen to sit under `/uploads`.
- **Side effects fixed.** The N+1 comment query is now one query.