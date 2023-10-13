
namespace Benchmark

module RandomGraphGenerator =
    open System
    //open System.Collections.Generic
    open DCRGraph.DCRGraphDTO

    let gl_roleNameList = [
        "Customer";
        "Inventory";
        "Shipping";
        "Notification";
        "Authentication";
        "Recommendation";
        "Analytics";
        "Pricing";
        "Search";
        "Order";
        "Messaging";
        "Review";
        "Subscription";
        "Content";
        "Tracking";
        "Billing";
        "Campaign";
        "Feedback";
        "Reward";
        "Location";
        "Document";
        "Integration";
        "Rating";
        "Support";
        "Workflow";
        "DataProcessing";
        "Image";
        "Translation";
        "Email";
        "File";
        "Compliance";
        "Reporting";
        "Loyalty";
        "FraudDetection";
        "Social";
        "Monitoring";
        "Calendar";
        "Chat";
        "Tax";
        "Survey";
        "Event";
        "NotificationDelivery";
        "Validation";
        "RecommendationEngine";
        "Audit";
        "InventoryManagement";
        "ErrorHandling";
        "CreditCard";
        "PaymentGateway";
        "AssetManagement";
        "FeedbackAnalysis";
        "ContentModeration";
        "GeoLocation";
        "Tagging";
        "SubscriptionManagement";
        "RecommendationSystem";
        "RateLimiting";
        "Scheduling";
        "AuditLog";
        "VideoProcessing";
        "KnowledgeBase";
        "WorkflowAutomation";
        "ComplianceManagement";
        "AnalyticsDashboard";
        "ImageRecognition";
        "TranslationEngine";
        "EmailDelivery";
        "FileStorage";
        "FraudPrevention";
        "SocialMediaIntegration";
        "PerformanceMonitoring";
        "CalendarIntegration";
        "Chatbot";
        "TaxCalculation";
        "SurveyAnalytics";
        "EventRegistration";
        "PushNotification";
        "DataValidation";
        "Personalization";
        "ErrorReporting";
        "PaymentProcessing";
        "AssetTracking";
        "FeedbackManagement";
        "ContentDelivery";
        "GeoFencing";
        "TagManagement";
        "SubscriptionRenewal";
        "RecommendationAlgorithm";
        "RateLimit";
        "SchedulingAutomation";
        "AuditTrail";
        "VideoStreaming";
        "KnowledgeGraph";
        "WorkflowOrchestration";
        "ComplianceReporting";
        "AnalyticsVisualization";
        "ImageOptimization";
        "LanguageTranslation";
        "EmailAutomation";
        "FileSharing"
    ]

    let random = Random()

    type RelationTypes = 
        {
            RelationTypes: List<string>
            RandomGenerator: Random
        }
        static member Create() =
            {
                RelationTypes = ["Condition"; "Exclusion"; "Inclusion"; "Milestone"; "Response"];
                RandomGenerator = Random()
            }
        member this.Random() =
            let randomIndex = this.RandomGenerator.Next(0, this.RelationTypes.Length)
            this.RelationTypes.Item randomIndex

    let relationTypes = RelationTypes.Create()

    type GenerationContext = 
        { NumRoles: int
          NumEvents: int
          NumRels: int 
        }

    let createEvent (role: string) : DcrEventDTO = 
        let initiator = role
        let action = ((Guid.NewGuid()).ToString("N")).Substring(0, 8)

        {
            Id = sprintf "%s%s" initiator action;
            Initiator = initiator;
            Action = action;
            Receivers = [];
        }

    type CircularList<'T when 'T : equality > = 
        {
            mutable CurrentList: List<'T>
            OriginalList: List<'T>
            RandomGenerator: Random
        } 
        static member Create (lst: List<'T>) =
            {
                CurrentList = lst;
                OriginalList = lst;
                RandomGenerator = Random()
            }
        member this.NextRandom () =
            if this.CurrentList.Length = 0 then
                this.CurrentList <- this.OriginalList
            let randomIndex = this.RandomGenerator.Next(0, this.CurrentList.Length)
            let elm = this.CurrentList.Item randomIndex
            this.CurrentList <- List.filter (fun e -> e <> elm) this.CurrentList
            elm

    let createEventsForRoles (gCtx: GenerationContext) (roles: List<string>) =
        let rolesCircular = CircularList.Create roles
        let mutable events : List<DcrEventDTO> = []
        for i in 0 .. gCtx.NumEvents do
            let role = rolesCircular.NextRandom()
            events <- events @ [createEvent role]

        events

    let createRelationships (gCtx: GenerationContext) (events: List<DcrEventDTO>) =
        let eventsCircular = CircularList.Create events
        let mutable relations : List<DcrRelationshipDTO> = List.Empty
        for i in 0 .. gCtx.NumRels do
            let event1 = eventsCircular.NextRandom()
            let event2 = eventsCircular.NextRandom()

            relations <- relations @ [{
                From = event1.Id;
                To = event2.Id;
                Relations = [{RelationType = relationTypes.Random()}]
            }]

        relations

    let createMarkingsForEvent (event: DcrEventDTO) : DcrMarkingsDTO =
        {
            EventId = event.Id;
            Marking = {
                Executed = false;
                Included = false;
                Pending = false
            }
        }

    let generateRandomGraph (gCtx: GenerationContext) =

        let roles =
            [for l in 1 .. gCtx.NumRoles do yield sprintf "Role%s" (gl_roleNameList.Item l)]

        let events = createEventsForRoles gCtx roles

        let relationships = createRelationships gCtx events

        let markings = List.map createMarkingsForEvent events

        {
            Events = events;
            Nestings = List.empty;
            Relationships = relationships;
            Markings = markings
        }
