It's a six-project solution matching the layered diagram:

| Project | Contents |
|---|---|
| `Web` | UI only; talks to `IContentManager` and `IUserSession` |
| `Managers` | `ContentManager`, the sequence of each use case |
| `Engines` | `PolicyEngine` (rules, subjects, upload limit), `FeedEngine` (ordering), `MediaEngine` (file kinds) |
| `ResourceAccess` | `IPostAccess` (SQLite) and `IFileAccess` (disk), with the schema initializer running automatically at startup |
| `Utilities` | `IUserSession`, `IClock` |
| `Models` | Plain shared data |

Three things in **Volatile** that **Functional** doesn't have:
- **Enforced layering.** Engines reference only Models, so they can't reach a database or disk. Implementations are `internal`, so the web client can't instantiate them. This isn't airtight yet, since Web can still use the lower-layer interfaces. An architecture test could close that gap in Step 3.
- **Rules moved out of orchestration.** Each rule now has exactly one home. The upload limit moved from file storage to `PolicyEngine`, because it's a community rule and not a storage property.
- **Ordering moved out of SQL.** The `ORDER BY` is gone from the database layer, and `FeedEngine` owns it.

A fair counter-point: for a three-feature app, Volatile has more files and ceremony, and Functional is easier to read on day one.
The difference shows up when requirements change.