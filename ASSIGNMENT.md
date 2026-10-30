# AI in SDLC Assignment

## Feature

Implemented role-based invoice approval limits in the ASP.NET Core invoice demo:

- Managers may approve invoices up to and including 50,000.
- Directors may approve invoices up to and including 500,000.
- Invoices above 500,000 require two distinct approvers; the second approver must be a Director.
- Admin and the legacy Approver role cannot approve or count toward an approval.
- Rejected approval attempts return a clear reason and preserve invoice state.

## Pull request

- [PR #1: Add role-based invoice approval limits](https://github.com/ShivamTiwariNagarro/ai-sdlc-demo-app/pull/1)
- The feature branch contains more than the required three commits, including separate specification, single-approver, and two-approver implementation commits.

## Context and spec-driven workflow

- [AGENTS.md](AGENTS.md) documents the domain layering, approval rules, tests, and review expectations.
- Specification and planning artifacts are in [specs/001-role-based-approval-limits](specs/001-role-based-approval-limits/): `spec.md`, `plan.md`, `tasks.md`, `research.md`, `data-model.md`, `quickstart.md`, the API contract, and requirements checklist.
- Approval and PR review guidance is in `.github/skills/approval-rules/` and `.github/skills/review-pr/`. The read-only reviewer agent is `.github/agents/reviewer.agent.md`.
- Structured interaction logging is configured in `.github/hooks/`; the log is [logs/ai-interactions.jsonl](logs/ai-interactions.jsonl). Review this log for sensitive content before sharing it.

## Tests and mutation testing

| Check | Result |
|---|---|
| `dotnet test backend\InvoiceApp.sln` | Passed: 46 tests |
| `cd e2e; npm test` | Passed: 1 Playwright test |
| Stryker baseline, before implementation | 30.77% |
| Stryker final, after implementation and focused tests | 82.83% |

The Playwright test submits a 75,000 invoice as a Manager, attempts approval, checks the exact refusal message, and verifies the invoice remains pending.

Stryker was run with dotnet-stryker 4.13.0 against `InvoiceApp.Domain`. The baseline recorded 12 killed and 10 survived mutants. The final run recorded 82 killed and 0 survived mutants; 17 were NoCoverage, 31 ignored, and 6 had compile errors. The final run had no surviving or NoCoverage mutants in `ApprovalService.cs` or `Invoice.cs`.

- Baseline output: [docs/mutation-before.txt](docs/mutation-before.txt)
- Final output: [docs/mutation-final.txt](docs/mutation-final.txt)

## Review findings and verdicts

- **fixed** — Updated `AGENTS.md` to specify that only Manager and Director may approve; Admin and the legacy Approver role cannot approve.
- **fixed** — Standardized review verdict wording as “fixed,” “dismissed with a reason,” or “deferred with a reason.”
- **fixed** — Added an explicit “Admin does not count as an approver” check to the PR review skill and reviewer agent.

## Demo video

**TODO:** Upload the 8–10 minute demo video and replace this line with an accessible link. The recording should show a live test, mutation run, or end-to-end test.
