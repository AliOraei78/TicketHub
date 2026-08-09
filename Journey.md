# Architecture & Infrastructure

## Test Infrastructure
- **Activities**: Fixed WorkflowsE2ETests drag/drop. Appended empty form tests for Projects, Users, Workflows. Implemented complete 3->1->2 plural/singular `BulkActions_Scenario` tests for Workflows and Settings.
- **Notes**: Fixed UI overlay pointer interception in Playwright with `Force = true`. Deleting single items must re-instantiate `HashSet` rather than `Clear()` to trigger Blazor `BulkActionToolbar` re-render. Checkbox elements within `label` might not register state changes if they're hidden, click the wrapper label instead. Filtering lists via search guarantees items appear on the first page.

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
- **Activities**: Fixed `UsersE2ETests` locating strategies (Playwright text/label specificity) and timeout issues. Verified client-side pagination fallback during Bulk Delete works perfectly.
- **Notes**: When clicking action buttons in grids, ensure `title` attributes exist to avoid fragile DOM selectors. When paginating, if `CurrentPage > TotalPages` after bulk delete, frontend must fallback to `maxPage`.

## Workflows & Data Seeding
- **Activities**: Integrated `SlideSelect` in `TransitionSidebar` for Field Types. Updated `WorkflowsE2ETests` to handle custom UI overlay clicks. Seeded field types.
- **Notes**: Playwright needs explicit `.WaitForAsync(Visible)` and `Force = true` for custom dropdowns utilizing overlays. DbInitializer exhaustively seeds transition field types.

## Security & Permissions
- **Activities**: Fixed "Admin" vs "ادمین" role mapping bug in `DbInitializer`. Seeded 12 core system permissions and assigned them to the "ادمین" role to activate RBAC in `PermissionService`. Standardized `ConfirmDeleteModal` description text and synchronized success toasts in E2E tests to correctly pass `PermissionsSettings_BulkActions_Scenario`.
- **Notes**: Permissions enforce RBAC using `relativePath` as `ResourceKey` in `PermissionRouteGuard` and `SecuredView`. If a permission doesn't exist in DB, `PermissionService` defaults to allowing access, so default permissions must be seeded for security. When testing toast notifications for bulk actions, ensure spacing and pluralization exactly matches the `Store` state templates.
