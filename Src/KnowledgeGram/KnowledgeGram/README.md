# KnowledgeGram - Step 2A: functional decomposition

Components are named after what the app DOES:

    Home.razor (UI)
      |-- FeedService         browse / search / filter
      |-- PublishingService   create / delete posts
      |-- EngagementService   likes / comments
            |
            v   each one talks to the Step 1 abstractions directly
      IPostRepository, IFileStorage, IUserSession, IContentKindResolver, ISubjectCatalog

