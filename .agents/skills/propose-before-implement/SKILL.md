---
name: propose-before-implement
description: Proposal-first workflow triggered ONLY when the user's request starts with "پیشنهاد". Analyzes requirements, presents technical implementation methods, options, recommendations, and advice WITHOUT modifying code, and waits for user approval. Otherwise, executes directly.
---

# Proposal-First & Method Recommendation Workflow for TicketHub

## Core Mandate & Trigger Rule

### 1. Trigger Condition:
- **IF** the user's prompt starts with the word **"پیشنهاد"** (or explicitly requests proposals/options):
  - You **MUST** follow the 2-step interaction pattern below (Step 1: Analyze & Propose without touching code, Step 2: Implement upon approval).
- **OTHERWISE** (for all standard requests not starting with "پیشنهاد"):
  - Proceed directly with normal implementation, bug fixes, or enhancements without waiting for a separate proposal turn.

---

### Step 1: Analyze, Recommend & Propose (NO Code Changes)
*Triggered ONLY when user prompt begins with "پیشنهاد"*

1. **Detailed Technical & Design Analysis:** Evaluate the request against Clean Code, Clean Architecture, and existing components.
2. **Implementation Methods & Options (روش‌های پیاده‌سازی):**
   - Present the available technical approaches or implementation methods.
   - Highlight the recommended approach along with its pros, trade-offs, or UX impact.
3. **Recommendations & Advice (توصیه‌ها و پیشنهادات):**
   - Offer expert advice on aesthetics, performance, and maintainability.
   - Outline a clear, concise action plan.
4. **STRICT RULE:** Do NOT edit, touch, or create any source code files during Step 1.
5. **Request User Approval:** End your turn by asking the user for feedback or approval on the proposed methods.

---

### Step 2: Implement & Test (AFTER User Approval or for Direct Requests)
1. **Implementation:** Modify the files according to the agreed proposal or direct request.
2. **Build & Verify:** Clean/rebuild (`dotnet build`), run bUnit tests (`dotnet test`), update `Journey.md`, and restart `TicketHub.Web`.
