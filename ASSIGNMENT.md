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
| `dotnet test backend\InvoiceApp.sln` | Passed: 48 tests |
| `cd e2e; npm test` | Passed: 1 Playwright test |
| Stryker baseline, before implementation | 30.77% |
| Stryker final, after implementation and focused tests | 82.83% |

The Playwright test submits a 75,000 invoice as a Manager, attempts approval, checks the exact refusal message, and verifies the invoice remains pending.

Stryker was run with dotnet-stryker 4.13.0 against `InvoiceApp.Domain`. The baseline recorded 12 killed and 10 survived mutants. The final run recorded 82 killed and 0 survived mutants; 17 were NoCoverage, 31 ignored, and 6 had compile errors. The final run had no surviving or NoCoverage mutants in `ApprovalService.cs` or `Invoice.cs`.

- Baseline output: [docs/mutation-before.txt](docs/mutation-before.txt)
- Final output: [docs/mutation-final.txt](docs/mutation-final.txt)

## Review findings and verdicts

| # | Finding | Verdict | Reason or commit |
|---|---|---|---|
| 1 | `tasks.md` items unchecked | Fixed | `a88d416`, `3e3a411` |
| 2 | `AGENTS.md` demo accounts omitted Manager and Director | Fixed | `a88d416` |
| 3 | Seed invoice `INV-1003` attributed to `demo.approver` | Dismissed | Historical sample data; it affects no rule. |
| 4 | Defensive branches in `ApprovalService` that normal flows cannot reach | Dismissed | They reject malformed approval state before anything changes. |
| 5 | Admin approving an already-decided invoice gets the “pending” reason | Dismissed | The pending check runs first by design. It is still refused with a clear reason and nothing changes. |
| 6 | Reject after the first approval untested | Fixed | `2db4e11` (approval kept as audit history) |
| 7 | No negative role test for Reject | Fixed | `2db4e11` (Clerk role gets 403, invoice unchanged) |

### Setup-phase review by the reviewer sub-agent

- **Fixed** — Updated `AGENTS.md` to make Manager and Director the only approval roles and clarify that Admin and legacy Approver cannot approve.
- **Fixed** — Standardized review verdict wording as “fixed,” “dismissed with a reason,” or “deferred with a reason.”
- **Fixed** — Added an explicit Admin-does-not-count-as-an-approver check to the `review-pr` skill and reviewer agent.

## Agents and sub-agents

Four workspace agents were used at their corresponding stages:

| Agent | Stage and contribution |
|---|---|
| `spec-writer` | Specification stage: turned the approval rules into testable requirements and edge cases. |
| `implementer` | Implementation and UI verification stages: implemented the approval flow and wrote the Playwright test for the Manager refusal experience. |
| `test-writer` | Test and mutation-analysis stages: added and refined xUnit approval-rule tests against the mutation results. |
| `reviewer` | Review stage: ran as a sub-agent, inspected the changes read-only, and refused edits as instructed by its agent configuration. |

## Skills and hooks

The three project-specific skills were `approval-rules`, `review-pr`, and `write-unit-tests`. The repository also contains Spec Kit skills `speckit-constitution`, `speckit-specify`, `speckit-clarify`, `speckit-plan`, `speckit-tasks`, `speckit-implement`, `speckit-analyze`, `speckit-checklist`, and `speckit-converge`; `speckit-taskstoissues` is deprecated. The recorded workflow used clarification, planning, task generation, and analysis guidance. The `/speckit-` commands initially fell back to the corresponding manual skill steps until the VS Code window was reloaded.

The `.github/hooks/` logging hook records submitted prompts, tool use, and agent stop events in [logs/ai-interactions.jsonl](logs/ai-interactions.jsonl), including the event payload when available. It is an automatic activity log, not a complete project journal: human decisions and verified review and test evidence were added manually because the hook cannot infer them.

## Model

The hook did not record the model. Copilot was used in Auto model mode, so the model could vary between requests. The user reports that the first chat displayed GPT-6 Luna; this is user-observed, not hook-recorded.

## What worked and what did not

- The spec-driven workflow provided useful artifacts and separated the two approval implementation steps. The slash commands were not initially available in the active VS Code window, so their manual skill workflows were used until the window was reloaded.
- The first review pass was thin. Follow-up review guidance and a findings table made the role rules, verdicts, and Admin exclusion more explicit.
- The baseline mutation run (30.77%) and final run (82.83%) exercised different code: the baseline was captured before the feature, and the final run included the implementation and its tests. The score change is not a controlled comparison of the same mutants.
- The final run reported 17 `NoCoverage` mutants in `backend/src/InvoiceApp.Domain/DiscountCalculator.cs`. That calculator is unrelated to approval limits and was not changed; those mutants were left out of scope rather than adding unrelated tests or production changes. The final run also reported 31 ignored mutants and 6 compile-error mutants.
- The automatic hook did not capture the model, human decisions, or a complete record of skill and review/test outcomes. Those gaps are stated rather than filled with guesses.

## Demo video

**TODO:** Upload the 8–10 minute demo video and replace this line with an accessible link. The recording should show a live test, mutation run, or end-to-end test.
