# Project Rules for TicketHub

## Proposal-First Workflow Trigger Rule
For requests involving code modification, UI design, refactoring, or feature implementation in this workspace:

1. **Conditional Activation ("پیشنهاد"):**
   - **IF** the user request starts with the word **"پیشنهاد"** (or explicitly asks for proposals/options):
     - **Step 1 (Proposal Phase):** Provide analysis, implementation methods/options (روش‌های پیاده‌سازی), recommendations, and advice **WITHOUT modifying any code**, and wait for user approval.
     - **Step 2 (Implementation Phase):** Implement changes only after receiving explicit user approval.
   - **OTHERWISE:**
     - Execute the implementation, fix, or enhancement directly.

2. **Completion Workflow:**
   - After completing code modifications, always clean/rebuild (`dotnet build`), run bUnit tests (`dotnet test`), update `Journey.md`, and restart `TicketHub.Web`.

## UI & Browser Testing
- For UI/UX visual validation, browser rendering inspection, and interactive testing, you can use Chrome DevTools MCP tools (`chrome-devtools-mcp`).
