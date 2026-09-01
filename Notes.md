# TicketHub Project Notes

## Context & Architecture
**Architecture Style:** Clean Architecture (Onion Architecture)
**Tech Stack:** .NET 10, C#, Entity Framework Core 10 (PostgreSQL / Npgsql), Blazor (Fluxor, Bit.BlazorUI, Tailwind CSS)

### Layers Breakdown
1. **TicketHub.Core (Domain Layer)**
   - Contains the core business entities and interfaces.
   - **Key Concepts:**
     - **Ticketing:** `Ticket`, `Comment`, `Attachment`, `TicketHistory`.
     - **Dynamic Workflows:** `Workflow`, `WorkflowStatus`, `Transition`, `TransitionField`. Provides a dynamic state machine for ticket transitions rather than hardcoded statuses.
     - **Custom Fields:** `FieldType`, `FieldCategory`, `TicketField`, `TicketFieldValue` indicating that tickets can have dynamically assigned fields based on category or workflow.
     - **Access Control:** `User`, `Role`, `UserRole`, `Permission`, `RolePermission`, `Project`, `RoleProject`.

2. **TicketHub.Application (Application Layer)**
   - Houses the business logic, use cases, and service contracts.
   - **Libraries:** `FluentValidation` for validation rules, `Mapster` for object mapping between Entities and DTOs.
   - **Structure:** `DTOs`, `Models`, `Services`, `Validations`, `Mapping`.

3. **TicketHub.Infrastructure (Data Access Layer)**
   - Database context and migrations.
   - **EF Core:** Uses `AppDbContext` with PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`).
   - **Data Seeding:** `DbInitializer` heavily seeds the database with default statuses (Open, In Progress, Closed, etc.), default roles (Admin, Support, User), priorities, and constructs a comprehensive default "General" (عمومی) workflow complete with nodes, transitions, required fields, and role permissions for each transition.

4. **TicketHub.Web (Presentation Layer)**
   - Blazor Web App serving as the frontend.
   - **Libraries:** `Bit.BlazorUI` for components, `Fluxor` for Redux-style state management, `TailwindCSS` for styling, `Serilog` for logging.
   - **Notables:** Includes a `WorkflowEditor` for visual manipulation of the ticket workflows.

## Journey & Activity Log

### [2026-08-07] - Initial Project Analysis
- **Action Done:** Analyzed the entire project structure across all layers (Core, Application, Infrastructure, Web).
- **Findings:** The project is a highly dynamic ticketing system with a configurable workflow engine built-in. It relies on Blazor for the UI and Fluxor for state management. Mappings are handled via Mapster and validation via FluentValidation.
- **Outcome:** Created this `Notes.md` file to keep track of architectural details and journey logs as requested.

### [2026-08-07] - Fixed DbContext Concurrency Issue in Validations
- **Action Done:** Refactored `TicketFieldDtoValidator` to use `IRepository<TicketField>` instead of the scoped `IAppDbContext`.
- **Findings:** Blazor Server / Fluxor effects can run concurrently, leading to EF Core's `DbContext` multi-threading issue when using the shared scoped context. By utilizing the `GenericRepository` which internally leverages `IDbContextFactory`, DbContext accesses are now fully isolated and thread-safe.
