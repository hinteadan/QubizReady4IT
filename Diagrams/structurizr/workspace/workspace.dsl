workspace "KnowledgeGram" "C4 model for the KnowledgeGram platform" {

    model {
        client = person "Client" "A user who accesses and contributes knowledge content"
        administrator = person "Administrator" "A user who administers the KnowledgeGram platform"

        entra = softwareSystem "Microsoft Entra ID" "Shared identity provider for clients and administrators" "Existing System,External System"
        notificationService = softwareSystem "Notification Service" "External service for delivering KnowledgeGram notifications" "Planned System,External System"
        microsoftLearn = softwareSystem "Microsoft Learn" "Purchased external knowledge and learning solution" "Existing System,External System"

        knowledgeGram = softwareSystem "KnowledgeGram Platform" "A knowledge-sharing platform where clients access and contribute learning content, and administrators manage the platform" {
            webApp = container "KnowledgeGram Web App" "Blazor Server application providing the user interface and feed, publishing, and engagement workflows" "ASP.NET Core / Blazor Server" {
                homePage = component "Home Page" "Displays the feed and handles search, publishing, likes, comments, and local sign-in" "Blazor / Home.razor"
                feedService = component "Feed Service" "Browses, searches, and filters posts; builds attachment views and determines delete-button visibility" "C# / IFeedService / FeedService"
                publishingService = component "Publishing Service" "Validates and publishes posts and attachments; deletes posts owned by the current user" "C# / IPublishingService / PublishingService"
                engagementService = component "Engagement Service" "Adds likes and validates and adds comments" "C# / IEngagementService / EngagementService"
                postRepository = component "Post Repository" "Persists and queries posts, comments, and like counts" "C# / IPostRepository / SqlitePostRepository"
                fileStorageAccess = component "File Storage Access" "Saves and deletes uploaded files and provides their public URLs" "C# / IFileStorage / LocalFileStorage"
                userSession = component "User Session" "Stores the chosen username for the browser connection; not Entra authentication" "C# / IUserSession / InMemoryUserSession"
                contentKindResolver = component "Content Kind Resolver" "Classifies attachments as images, video, audio, or documents using their file extensions" "C# / IContentKindResolver / ExtensionContentKindResolver"
                subjectCatalog = component "Subject Catalog" "Provides the fixed list of subjects used by the UI and publishing validation" "C# / ISubjectCatalog / StaticSubjectCatalog"
                clock = component "Clock" "Provides the current time for post and comment timestamps" "C# / IClock / SystemClock"
            }
            contentDatabase = container "Content Database" "Stores knowledge posts, comments, and engagement data" "SQLite" "Database"
            fileStorage = container "Local File Storage" "Stores uploaded learning materials on the web server's disk" "File system"
            contentNotificationFunction = container "Content Notification Function" "Planned: checks for new content matching user preferences and requests notifications when matches are found" "Azure Functions" "Planned System,Content Checker"
            notificationBus = container "Notification Bus" "Planned: queues notification requests from the Web App and Content Notification Function" "Message bus" "Planned System,Async Alternative,Message Bus"
            notificationProcessor = container "Notification Processor Function" "Planned: consumes queued notification requests and invokes the Notification Service" "Azure Functions" "Planned System,Async Alternative,Message Processor"
            learningBus = container "Learning Integration Bus" "Planned: queues requests to retrieve content from Microsoft Learn" "Message bus" "Planned System,Async Alternative,Message Bus"
            learningProcessor = container "Learning Integration Function" "Planned: consumes learning requests, retrieves Microsoft Learn content, and submits results to the Web App for persistence" "Azure Functions" "Planned System,Async Alternative,Message Processor"
        }

        client -> knowledgeGram "Uses to access and contribute knowledge" "HTTPS"
        administrator -> knowledgeGram "Uses to administer the platform" "HTTPS"

        client -> entra "Will authenticate through" "" "Planned"
        administrator -> entra "Will authenticate through" "" "Planned"
        knowledgeGram -> entra "Will use for sign-in" "" "Planned"
        knowledgeGram -> notificationService "Will send notifications through" "" "Planned"
        knowledgeGram -> microsoftLearn "Will integrate with purchased learning content" "" "Planned"

        client -> webApp "Uses" "HTTPS"
        administrator -> webApp "Uses" "HTTPS"
        webApp -> contentDatabase "Reads and writes posts, comments, and engagement data" "SQLite"
        webApp -> fileStorage "Stores and retrieves uploaded learning materials" "File I/O"
        webApp -> entra "Will use for sign-in" "" "Planned"
        webApp -> notificationService "Will send notifications through" "" "Planned,Synchronous"
        webApp -> microsoftLearn "Will integrate with purchased learning content" "" "Planned,Synchronous"

        contentNotificationFunction -> webApp "Will retrieve user preferences and check for matching new content via" "" "Planned"
        contentNotificationFunction -> notificationService "Will send notifications about matching new content through" "" "Planned,Synchronous"

        webApp -> notificationBus "Will enqueue notification requests on" "" "Planned"
        contentNotificationFunction -> notificationBus "Will enqueue notifications about matching new content on" "" "Planned"
        notificationProcessor -> notificationBus "Will consume notification requests from" "" "Planned"
        notificationProcessor -> notificationService "Will invoke to deliver queued notifications" "" "Planned"

        webApp -> learningBus "Will enqueue learning content requests on" "" "Planned"
        learningProcessor -> learningBus "Will consume learning content requests from" "" "Planned"
        learningProcessor -> microsoftLearn "Will retrieve requested learning content from" "" "Planned"
        learningProcessor -> webApp "Will submit results for storage in the Content Database or Local File Storage" "" "Planned"

        client -> homePage "Browses and contributes knowledge through" "HTTPS / Blazor"
        homePage -> feedService "Loads the feed through"
        homePage -> publishingService "Publishes and deletes posts through"
        homePage -> engagementService "Likes and comments through"
        homePage -> userSession "Signs in, signs out, and reads the current user"
        homePage -> subjectCatalog "Loads subjects from"

        feedService -> postRepository "Queries posts and comments through"
        feedService -> fileStorageAccess "Gets attachment URLs from"
        feedService -> contentKindResolver "Classifies attachments through"
        feedService -> userSession "Reads the current user to determine delete-button visibility"

        publishingService -> postRepository "Creates, retrieves, and deletes posts through"
        publishingService -> fileStorageAccess "Checks upload size and saves and deletes attachments through"
        publishingService -> userSession "Checks sign-in and post ownership using"
        publishingService -> subjectCatalog "Validates subjects against"

        engagementService -> postRepository "Updates likes and adds comments through"
        engagementService -> userSession "Checks sign-in and identifies comment authors using"

        postRepository -> clock "Gets post and comment timestamps from"
        postRepository -> contentDatabase "Reads and writes posts, comments, and engagement data" "SQLite"
        fileStorageAccess -> fileStorage "Stores and retrieves uploaded learning materials" "File I/O"
    }

    views {
        systemContext knowledgeGram "SystemContext" "System Context diagram for KnowledgeGram" {
            include *
            autoLayout
        }

        container knowledgeGram "Containers" "Container diagram for KnowledgeGram (synchronous alternative)" {
            include *
            exclude "element.tag==Async Alternative"
            autoLayout
        }

        container knowledgeGram "ContainersAsync" "Container diagram for KnowledgeGram (asynchronous alternative)" {
            include *
            exclude "relationship.tag==Synchronous"
            autoLayout
        }

        component webApp "WebAppComponents" "Component diagram for KnowledgeGram Web App (current runtime implementation)" {
            include *
            exclude "element.tag==Async Alternative"
            autoLayout
        }

        styles {
            element "External System" {
                background #999999
                color #ffffff
            }
            element "Content Checker" {
                background #ffffff
                color #333333
                stroke #999999
            }
            element "Message Bus" {
                background #3b82c4
                color #ffffff
            }
            element "Message Processor" {
                background #8064a2
                color #ffffff
            }
            element "Database" {
                shape Cylinder
            }
            element "Person" {
                shape Person
                background #08427b
                color #ffffff
            }
            relationship "Planned" {
                color #777777
                style dashed
            }
        }
    }
}
