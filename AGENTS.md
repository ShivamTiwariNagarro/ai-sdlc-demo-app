# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected).
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `docs/use-cases`: short briefs for features we may build.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only, approvals are Approver or Admin.
- Business rules go in the Domain project, never in controllers.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chromium && npm test`

## Demo accounts (not real credentials)

- `demo.approver` / `ChangeMe123!` (role Approver)
- `demo.admin` / `ChangeMe123!` (role Admin)

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.

## Approval limits assignment

- The assignment's approval business roles are Manager and Director. Do not treat the API's existing `Approver` role as a synonym for either; Admin must not count as an approver. If the API only exposes `Approver` and `Admin`, update identity/role flow and authorization as part of implementing this feature rather than inventing a mapping.
- A Manager may single-approve an invoice up to and including 50,000.
- A Director may single-approve an invoice up to and including 500,000, including invoices below 50,000.
- An invoice above 500,000 requires two distinct approvers. The first approver may be a Manager or Director; the second approver must be a Director.
- The same user must never count as both approvers for one invoice. An Admin does not count as an approver for these limits.
- Validate the approver's role, the invoice amount, and any existing approval before changing invoice state. A rejected attempt must return a clear reason and leave the invoice status and recorded approvals unchanged.
- Keep these rules in `InvoiceApp.Domain`. Controllers pass the signed-in user's identity and role to the service; they do not implement approval-limit business rules.
- Test the exact 50,000 and 500,000 boundaries and amounts just above them. For every rule, assert successful and rejected outcomes, including status, approvals, and error messages. Include tests for duplicate approvers and Admin not counting.

## Assignment workflow and AI interaction logging

- Before starting each phase of the assignment, update `logs/current-stage.txt` to that phase. Do not leave it at `setup` after setup is complete. Use clear stage names such as `setup`, `specification`, `single-approver-limits`, `two-approver-rule`, `verification`, `review`, and `hand-in`.
- Keep the Copilot hook configuration and logger in `.github/hooks/`. Hook events append JSONL records to `logs/ai-interactions.jsonl`; include the original event payload and promote useful event details (session, tool, tool arguments/results, and stop reason) to top-level fields. `stage_source` identifies whether the stage came from the event payload or `logs/current-stage.txt`.
- Hooks can only capture fields present in their event payload. Keep unavailable evidence explicitly marked; do not invent model, skill, human-decision, review, or test details. Add those details manually only when verified.
- Review interaction logs for secrets or sensitive data before sharing or committing them.
- Run `dotnet test backend/InvoiceApp.sln` before every commit.
- In the pull request description, give every review finding exactly one verdict: fixed, dismissed with a reason, or deferred with a reason.
