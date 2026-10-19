# KnowledgeGram - Volatility-based decomposition

Components are named after what is likely to CHANGE. Calls only go downward.

    KnowledgeGram.Web             client: UI technology
        |
    KnowledgeGram.Managers        ContentManager: sequence of use cases
        |
    KnowledgeGram.Engines         PolicyEngine (rules and limits)
        |                         FeedEngine (ordering)
        |                         MediaEngine (file-type handling)
    KnowledgeGram.ResourceAccess  PostAccess (database), FileAccess (storage)
        |                         -> SQLite, a disk folder
    KnowledgeGram.Utilities       Identity (user session), Clock   (usable from any layer)
    KnowledgeGram.Models          plain data shared by everyone

Project references enforce the layering:
  - Engines reference only Models (they cannot reach a database or disk)
  - ResourceAccess references only Models + Utilities
  - Implementations are `internal`; only interfaces and AddXxx() registration methods are public
