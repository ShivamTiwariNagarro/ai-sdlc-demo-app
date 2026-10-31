# Implementation Plan: Role-Based Invoice Approval Limits

**Branch**: `001-role-based-approval-limits` | **Date**: 2026-10-29 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-role-based-approval-limits/spec.md` and implementation constraints supplied for the assignment.

## Summary

Add Manager and Director approval limits and preserve approval state in the Domain `Invoice` and `ApprovalService`. Managers may single-approve invoices through 50,000; Directors through 500,000. Higher-value invoices remain pending after a first eligible approval and are approved only after a distinct Director approves. Only Manager and Director may use the Approve endpoint; Manager, Director, Approver, and Admin may use the Reject endpoint regardless of invoice amount. Role and username come from the signed-in session. Refused approval attempts return a clear service error and do not mutate the invoice; this is distinct from using the Reject endpoint. The repository provides atomic per-invoice updates so concurrent approvals are evaluated against current state. The API continues to use `Handle(...)`, and the static UI displays the returned approval refusal reason.

Commit the specification artifacts (`spec.md`, `plan.md`, and `tasks.md`) separately before implementation. Implementation is split into at least two independently tested commits: first, single-approver caps, role wiring, and the atomic repository boundary; second, recorded approvals and the two-approver high-value path. Together with the specification-artifact commit, feature delivery must have at least three commits. Add the requested UI end-to-end coverage and collect Domain mutation scores using Stryker 4.13.0.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core 8; existing xUnit 2.9.2 test project; existing Playwright test setup; Stryker.NET 4.13.0 for mutation testing

**Storage**: Existing singleton in-memory `IInvoiceRepository`; no database or schema migration

**Testing**: `dotnet test backend/InvoiceApp.sln`; Playwright via `cd e2e; npm test`; Stryker against `backend/src/InvoiceApp.Domain`

**Target Platform**: ASP.NET Core web API and static UI running on .NET 8

**Project Type**: Layered web application (Domain, API, xUnit tests, static UI, Playwright E2E)

**Performance Goals**: No latency or throughput target is specified; retain the demo application's current scale and synchronous request behavior.

**Constraints**: All approval business rules and recorded approvals belong in `InvoiceApp.Domain`. Controllers pass the trusted session username and role, use the existing `Handle(...)` error mapping, and contain no approval rules. Only Manager and Director may use the Approve endpoint; Manager, Director, Approver, and Admin may use the Reject endpoint without an amount limit. Admin and the legacy Approver role are excluded from approval decisions. Same-invoice approval attempts must be serialized by the repository atomic-update contract; the current in-memory implementation guarantees this within one process. Add `demo.manager` and `demo.director` to `appsettings.json`. The UI must display the reason for a refused approval attempt. End-to-end tests use page objects and accessible role locators.

**Scale/Scope**: One demo application instance and its in-memory invoice repository; one feature affecting approval lifecycle, role configuration, API authorization, visible UI errors, unit and end-to-end tests, and mutation-testing evidence.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / constraint | Gate | Plan |
|---|---|---|
| Domain-owned business rules | PASS | `ApprovalService` and `Invoice` own role limits, validation, approval records, and lifecycle transitions. |
| Rule-level unit tests | PASS | Use `FakeInvoiceRepository`; cover both thresholds, role eligibility, distinct identities, final and pending states, clear errors, and state preservation for refused approval attempts. |
| Validate before mutating state | PASS | Validate status, role, amount, and identity before committing any change; atomic repository update serializes same-invoice attempts. |
| Credential safety | PASS | Add only the repository's existing demo-only password convention; review interaction logs and artifacts for sensitive data before sharing. |
| Repository conventions | PASS | Follow API/controller patterns, role filters, xUnit naming, accessible Playwright locators, and `AGENTS.md`. |
| Domain/API layering | PASS | Keep storage coordination behind `IInvoiceRepository`; do not move approval decisions into the repository, controller, or `Program.cs`. |
| Assignment delivery expectations | PASS | Keep implementation in separately testable steps; include UI E2E and before/after mutation scores in assignment evidence. |

No constitution violations are planned.

**Post-design gate: PASS.** The atomic repository boundary coordinates access without owning approval policy; failure paths validate before mutation; tests are specified for positive, negative, boundary, and concurrent cases; no additional project or real credential is introduced.

## Project Structure

### Documentation (this feature)

```text
specs/001-role-based-approval-limits/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── approvals-api.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code

```text
backend/
├── src/
│   ├── InvoiceApp.Domain/
│   │   ├── ApprovalService.cs
│   │   ├── Invoice.cs
│   │   └── IInvoiceRepository.cs
│   └── InvoiceApp.Api/
│       ├── Controllers/ApprovalsController.cs
│       ├── InMemoryInvoiceRepository.cs
│       ├── appsettings.json
│       └── wwwroot/
│           ├── app.js
│           └── index.html
└── tests/InvoiceApp.Tests/
    ├── ApprovalServiceTests.cs
    └── FakeInvoiceRepository.cs
e2e/
├── pages/
└── tests/
```

**Structure Decision**: Extend existing projects and conventions without adding projects or frameworks. Keep API behavior in the current `ApprovalsController`, domain changes in the existing Domain types and repository interface, and E2E coverage in the existing Playwright folders.

## Design Decisions

- The approval service passes a state transition to an atomic repository operation. The in-memory repository performs the read, transition, and update under one lock, preventing two service instances from evaluating the same stale invoice concurrently. The repository coordinates atomicity only; domain validation and transitions remain in `ApprovalService`.
- `IInvoiceRepository`'s atomic operation is implemented by `FakeInvoiceRepository` too, so unit tests exercise the coordination contract. A future persistent or multi-instance implementation must use its transaction/concurrency mechanism rather than assume this process-local lock is sufficient.
- The Approve endpoint is authorized only for Manager and Director. The Reject endpoint is authorized for Manager, Director, Approver, and Admin, with no amount limit. Approval-rule refusals leave approval state unchanged and are distinct from a successful call to the Reject endpoint.
- `DecidedBy` for a high-value approval is the second (final) Director. Both approval records remain associated with the invoice; a valid first approval leaves status PendingApproval and `DecidedBy` unset.

## Complexity Tracking

No constitution violations or additional projects are proposed.
