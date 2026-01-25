# Greg Company Ecosystem - AI Agent Instructions

## Project Architecture

This is a **multi-service business suite** with three core components:

1. **System App** (.NET 8 + React): Primary transactional platform for course management, payments, and user interactions
2. **BI Dashboard** (Python): ETL engine for extracting product metrics and syncing to Rows.com/Notion dashboards
3. **MCP Servers**: Model Context Protocol servers for providing project context to AI agents

## Backend (C# / .NET 8)

### Architecture Pattern: Clean Architecture with Vertical Slice
- **Features-first organization**: All features live under `Features/` directory (Auth, Courses, MercadoPago, Support, Videos, etc.)
- Each feature contains its own: Controllers, Services, Repositories, ViewModels, DTOs
- **Extension-based startup**: `Program.cs` uses extension methods from `Extensions/` for clean dependency registration
  - `AddApplicationServices()` - Registers services/repos using Scrutor scanning
  - `AddPersistence()` - Configures EF Core, Identity, Redis, Hangfire
  - `AddAuth()` - Sets up JWT + Google OAuth
  - `UseAppPipeline()` - Configures middleware chain

### Dependency Injection Convention
Services and repositories are **auto-registered via Scrutor** by namespace scanning:
```csharp
// DependencyInjectionExtensions.cs scans namespaces like:
"MeuCrudCsharp.Features.Courses.Services"
"MeuCrudCsharp.Features.Courses.Repositories"
```
When adding new features: create `Services/` and `Repositories/` folders following this pattern, and they will be auto-discovered.

### Database & ORM
- **Primary DB**: SQL Server via Entity Framework Core
- **DbContext**: `ApiDbContext` extends `IdentityDbContext<Users, Roles, string>`
- **Migrations**: Located in `Migrations/` - run `dotnet ef migrations add <Name>` from backend directory
- **Cache**: Redis for performance (toggled via `USE_REDIS` env var)
- **NoSQL**: MongoDB for flexible document storage (connection string in `appsettings.json`)

### Background Jobs & Webhooks
- **Hangfire** handles all async processing (subscription renewals, payment confirmations)
- **MercadoPago integration**: Webhook processing queues jobs via `IQueueService.EnqueueJobAsync<TJob>()`
- Jobs defined under `Features/MercadoPago/Jobs/`

### Logging
- **Serilog** configured in `Program.cs` with dual output:
  - Console
  - File (`log/log-{date}.txt`) with shared file access for MCP server reads

## Frontend (React + TypeScript + Vite)

### Stack & Key Libraries
- **Build**: Vite for fast development
- **Routing**: React Router v7
- **Forms**: React Hook Form
- **Real-time**: SignalR for live notifications/updates
- **Payments**: MercadoPago SDK React (`@mercadopago/sdk-react`)
- **Styling**: Sass modules

### Project Structure
```
src/
  features/    - Feature-based components (mirrors backend features)
  components/  - Shared/reusable UI components
  pages/       - Route-level page components
  routes/      - Routing configuration
  shared/      - Shared utilities, types, constants
  utils/       - Helper functions
```

### Development Workflow
- Start dev server: `npm run dev` (runs on port 5173)
- Build: `npm run build` (outputs to `dist/`)
- Docker build uses `nginx.conf` for production serving

## BI Dashboard (Python)

### Clean Architecture Pattern
Strict separation of concerns with dependency injection:

```
controllers/  - Orchestrate data flow (DummyJsonController, NotionController)
services/     - Business logic & ETL (DataService - status calc, metrics)
models/       - Data entities (ProductDTO, CleanedProductDTO)
data/         - External integrations (ExcelExporter, RowsExporter)
views/        - Output formatting (TerminalView, ExcelView, RowsView)
interfaces/   - Abstract contracts (IDataService, IProductExporter)
enums/        - Status enums (ProductStatus: OK, CRITICO, ESGOTADO)
```

### Key Workflow
1. Controllers fetch raw data from APIs
2. `DataService.prepare_products()` transforms raw → cleaned DTOs with status logic:
   - `stock == 0` → ESGOTADO
   - `stock < 10` → CRITICO
   - `stock < 20` → REPOR
3. Views format and export to Excel, terminal, or Rows.com API
4. Entry point: `main.py` with interactive CLI menu

### Running BI Dashboard
```bash
cd bi-dashboard
python src/main.py  # Interactive menu for reports/exports
```

## Docker Orchestration

### Services (docker-compose.yml)
- **sql-server**: SQL Server 2022 (port 1433, credentials in env vars)
- **mongodb**: Mongo latest (port 27017)
- **redis**: Redis Alpine (port 6379, AOF persistence)
- **backend**: .NET API (port 5045 → container 8080)
- **frontend**: React app via nginx (port 5173 → container 80)
- **bi-engine**: Python BI dashboard (defined but check config)

### Environment Configuration
- Root `.env` file contains all secrets (MercadoPago keys, DB connection strings, JWT secrets)
- Backend reads from `.env` via `DotNetEnv.Env.Load()` in `Program.cs`
- Connection strings use service names: `mongodb://mongo-db:27017`

### Development Commands
```bash
# Start entire stack
docker-compose up -d

# View logs
docker-compose logs -f backend

# Rebuild specific service
docker-compose up -d --build backend
```

## MCP Servers (Model Context Protocol)

Located in `mcp-servers/`:
- **greg_context_mcp.py**: Provides architecture patterns and project structure to AI agents
- **log_mcp_server.py**: Exposes application logs to AI assistants

These enable AI tools to understand project conventions and debug issues via log analysis.

## Critical Conventions

### Backend Naming
- Namespace follows folder structure: `MeuCrudCsharp.Features.{FeatureName}.{Layer}`
- DTOs suffix: `*ViewModel`, `*Request`, `*Response`
- Interfaces prefix: `I{Name}` (e.g., `IWebhookService`)

### Error Handling
- Validation errors return structured responses
- Serilog captures exceptions to both console and file logs (check `logs/` directory)

### Testing & Debugging
- Backend: Run `dotnet run` from `system-app/backend/`
- Swagger UI available at `/swagger` endpoint
- Frontend: Check browser console and network tab for API errors
- BI: Terminal output shows real-time processing status

### Common Pitfalls
- **Circular dependencies**: Use interfaces in constructors, not concrete types
- **Migration errors**: Always run from backend directory with `dotnet ef`
- **Docker networking**: Services communicate via service names, not localhost
- **Environment variables**: Missing `.env` values cause runtime failures - check logs first

## Adding New Features

### Backend Feature
1. Create folder under `Features/{FeatureName}/`
2. Add subfolders: `Controllers/`, `Services/`, `Repositories/`, `ViewModels/`
3. Services/Repos auto-register if namespace matches pattern in `DependencyInjectionExtensions.cs`
4. Add DbSet to `ApiDbContext.cs` if feature has database entity
5. Create migration: `dotnet ef migrations add {FeatureName}Initial`

### Frontend Feature
1. Create folder under `src/features/{featurename}/`
2. Add components, hooks, types locally to feature
3. Wire up routes in `src/routes/`
4. Use shared API client utilities from `src/utils/`

### Python BI Module
1. Define DTO in `models/`
2. Create interface in `interfaces/`
3. Implement logic in `services/`
4. Create exporter in `data/`
5. Build view in `views/`
6. Wire to `main.py` menu

## Key Files Reference
- Backend entry: [system-app/backend/Program.cs](system-app/backend/Program.cs)
- DI config: [system-app/backend/Extensions/DependencyInjectionExtensions.cs](system-app/backend/Extensions/DependencyInjectionExtensions.cs)
- DB context: [system-app/backend/Data/ApiDbContext.cs](system-app/backend/Data/ApiDbContext.cs)
- Frontend entry: [system-app/frontend/src/main.tsx](system-app/frontend/src/main.tsx)
- BI entry: [bi-dashboard/src/main.py](bi-dashboard/src/main.py)
- BI ETL logic: [bi-dashboard/src/services/data_service.py](bi-dashboard/src/services/data_service.py)
- Docker config: [docker-compose.yml](docker-compose.yml)
