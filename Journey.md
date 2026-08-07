# Architecture & Infrastructure

## Test Infrastructure
- **Activities**: Integrated bUnit (Component) & Playwright (E2E) w/ Testcontainers (SQL Server) & Mock Auth. Created BUnitComponentTestBase & PlaywrightTestBase. Phase 1 tests implemented and passing.
- **Notes**: Playwright needs local Docker up. Testcontainers replaces DB connection string at runtime. Fluxor state/dispatcher mocked in bUnit using Moq.

## Web Application (Blazor)
- **Activities**: Uses Fluxor for state management. Uses TailwindCSS for styling. Uses FluentValidation for form validation.
- **Notes**: Fluxor requires specific setup for component tests to avoid loading full app state.

## Concurrency
- **Activities**: Handled `dotnet build` file lock errors by killing running `TicketHub.Web` processes before building.
- **Notes**: Always clean/rebuild/run after changes. Blazor Server locks DLLs.

## Entity Framework & Blazor
- **Activities**: Resolved EF Core concurrency error in Blazor Server forms caused by `Blazored.FluentValidation`.
- **Notes**: Avoid injecting scoped DbContext/Repositories into FluentValidation validators. Use `IServiceScopeFactory` to create a fresh scope in `MustAsync` to prevent DbContext collisions on the single SignalR circuit.

## Users Management & E2E
- **Activities**: Fixed `UsersE2ETests` locating strategies (Playwright text/label specificity) and fixed timeout issues for row deletion assertions.
- **Notes**: When clicking action buttons in grids, ensure `title` attributes exist to avoid fragile DOM selectors.

## Workflows & Data Seeding
- **Activities**: Updated `DbInitializer` to include `MultipleDropdown`, `Checkbox`, and `ColorPicker` field types in the seeded transition fields for completeness.
- **Notes**: DbInitializer seeds roles, default users, projects, workflow statuses (including identical named statuses for multiple stages), and exhaustive transition field types to test all UI components.

## Security & Permissions
- **Activities**: Fixed "Admin" vs "ادمین" role mapping bug in `DbInitializer`. Seeded 12 core system permissions and assigned them to the "ادمین" role to activate RBAC in `PermissionService`.
- **Notes**: Permissions enforce RBAC using `relativePath` as `ResourceKey` in `PermissionRouteGuard` and `SecuredView`. If a permission doesn't exist in DB, `PermissionService` defaults to allowing access, so default permissions must be seeded for security.
