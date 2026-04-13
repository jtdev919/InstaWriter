# InstaWriter - Social Content Orchestration Platform (SCOP)

AI-powered content operations platform for Instagram marketing. Automates content lifecycle from idea capture through publishing and analytics, with human-in-the-loop approval for health/brand-sensitive content.

## Tech stack

- **Backend**: .NET 10 (preview) Web API, Azure Functions, Durable Functions
- **Frontend**: React
- **Database**: Azure SQL
- **Storage**: Azure Blob Storage
- **Messaging**: Azure Service Bus
- **AI**: Azure AI Foundry + Azure OpenAI (GPT-4o via Azure.AI.OpenAI SDK)
- **Secrets**: Azure Key Vault
- **Telemetry**: Application Insights
- **External APIs**: Instagram Graph API / Instagram Platform (professional accounts only)

## Project structure

```
InstaWriter.slnx
docs/
  architecture.md                # Full PRD and system design
src/
  InstaWriter.Api/               # ASP.NET Web API (minimal APIs)
    Program.cs                   # App entry point, service registration
    Endpoints/
      ContentIdeaEndpoints.cs
      ContentDraftEndpoints.cs
      PublishJobEndpoints.cs
      TaskItemEndpoints.cs
      ChannelAccountEndpoints.cs
      ContentGenerationEndpoints.cs
  InstaWriter.Core/              # Domain models, enums, shared logic
    Entities/
      ContentIdea.cs
      ContentDraft.cs
      PublishJob.cs
      TaskItem.cs
      ChannelAccount.cs
    Services/
      IInstagramPublisher.cs     # Interface for Instagram publishing
      IContentGenerator.cs       # Interface for AI content generation
    Workflow/
      StatusTransitions.cs       # State machine for all entity status changes
  InstaWriter.Infrastructure/    # EF Core, DbContext, data access, external APIs
    Data/
      AppDbContext.cs
      DesignTimeDbContextFactory.cs
      Configurations/
    Instagram/
      InstagramPublisher.cs      # Instagram Graph API client
    AI/
      AzureOpenAIContentGenerator.cs  # Azure OpenAI content generation
  InstaWriter.Api.Tests/         # Integration tests (xUnit v3 + WebApplicationFactory)
```

## Build / run / test

```bash
dotnet build InstaWriter.slnx
dotnet run --project src/InstaWriter.Api
dotnet test InstaWriter.slnx
```

### EF Core migrations

```bash
# From repo root — generate a migration
dotnet ef migrations add <MigrationName> --project src/InstaWriter.Infrastructure --startup-project src/InstaWriter.Api

# Apply migrations
dotnet ef database update --project src/InstaWriter.Infrastructure --startup-project src/InstaWriter.Api
```

### Local database

- SQL Server LocalDB: `(localdb)\MSSQLLocalDB`, database `InstaWriter`
- Auto-migrates on startup in Development environment

### API endpoints

- OpenAPI doc: https://localhost:7201/openapi/v1.json
- Health check: GET /api/health
- Content ideas: GET, POST /api/content/ideas | GET, PUT, DELETE /api/content/ideas/{id}
- Content drafts: GET, POST /api/content/drafts | GET, PUT, DELETE /api/content/drafts/{id}
- Publish jobs: GET, POST /api/publish/jobs | GET, DELETE /api/publish/jobs/{id} | GET /api/publish/jobs/{id}/status | POST /api/publish/jobs/{id}/execute
- Tasks: GET, POST /api/tasks | GET, PUT, DELETE /api/tasks/{id} | POST /api/tasks/{id}/complete
- Channel accounts: GET, POST /api/channels | GET, DELETE /api/channels/{id} | PUT /api/channels/{id}/token
- AI generation: POST /api/content/drafts/generate | POST /api/content/drafts/{id}/regenerate-caption | POST /api/content/drafts/{id}/score-compliance
- Transitions: POST /api/{entity}/{id}/transition (all entities support workflow state transitions)

## Key concepts

- **Three lanes**: fully automated (low-risk), human-approved (brand/health-sensitive), manual capture (requires original media)
- **Workflow states**: IdeaCaptured -> BriefReady -> DraftGenerated -> AwaitingReview -> Approved -> Scheduled -> Published -> InsightsCollected
- **Instagram limits**: 25 API-published posts per 24-hour rolling window for business accounts
- **Health compliance**: content touching biomarkers, supplements, hormones, or diagnoses must be flagged for manual review before publishing

## Coding conventions

> Establish and document conventions here as the codebase takes shape. Expected patterns:
> - C# / .NET conventions for backend
> - ESLint + Prettier for React frontend
> - Entity Framework for data access
> - xUnit for testing

## Architecture reference

See [docs/architecture.md](docs/architecture.md) for the full system design including:
- Operating model and content lanes
- Core service modules
- Data model and entity definitions
- API design
- Workflow specifications
- Phased implementation plan
