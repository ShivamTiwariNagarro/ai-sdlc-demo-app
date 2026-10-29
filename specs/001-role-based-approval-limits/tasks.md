# Tasks: Role-Based Invoice Approval Limits

**Input**: Design documents from `specs/001-role-based-approval-limits/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/approvals-api.md](contracts/approvals-api.md), [quickstart.md](quickstart.md)

**Organization**: Step A delivers single-approver thresholds and eligible-role wiring (US1 and US3). Step B adds the high-value two-approver flow (US2). The final phase handles visible UI errors, Playwright coverage, and the post-implementation mutation run.

## Phase 1: Specification Commit

**Purpose**: Commit the agreed specification artifacts as a distinct commit before implementation.

- [ ] T001 Commit `specs/001-role-based-approval-limits/spec.md`, `specs/001-role-based-approval-limits/plan.md`, and `specs/001-role-based-approval-limits/tasks.md` together as a separate specification-artifact commit. This commit, the Step A commit, and the Step B commit provide at least three feature commits in total.

## Phase 2: Setup

**Purpose**: Capture the pre-change mutation baseline before feature code is modified.

- [ ] T002 Run `dotnet stryker` from `backend/tests/InvoiceApp.Tests/` using dotnet-stryker 4.13.0 before implementation; record the command and baseline score in the repository-root `ASSIGNMENT.md`.

## Phase 3: Foundational

**Purpose**: Provide an atomic repository boundary that both approval steps can use without moving business policy out of Domain.

- [x] T003 Add an atomic invoice-update operation to `backend/src/InvoiceApp.Domain/IInvoiceRepository.cs` and implement its locked read-transition-write behavior in `backend/src/InvoiceApp.Api/InMemoryInvoiceRepository.cs` and `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`.

**Checkpoint**: Repository atomicity is available; approval decisions and validation remain in `ApprovalService`.

## Phase 4: Implementation Step A — User Story 1 and User Story 3 (P1)

**Goal**: Configure Manager and Director demo roles and enforce single-approver limits; only Manager and Director can use Approve, while Manager, Director, Approver, and Admin can use Reject regardless of invoice amount.

**Independent Test**: Unit tests prove Manager approval succeeds through 50,000 inclusive and is refused above it; Director approval succeeds through 500,000 inclusive and is refused above it; Director can approve amounts also within Manager authority; Admin and legacy Approver cannot approve; each refused approval attempt leaves status, decision fields, and approvals unchanged.

### Tests for User Story 1 and User Story 3

- [x] T004 [US1] Add xUnit cases in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for Manager and Director threshold boundaries and valid lower-value Director approvals, including clear reasons and complete invoice-state preservation for over-limit attempts. Include a case proving approval of an already-decided invoice is refused with a clear reason and leaves status, decision fields, and approvals unchanged.
- [x] T005 [US3] Add xUnit cases in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` proving Admin and legacy Approver cannot create or count toward approvals and leave status, decision fields, and approvals unchanged.
- [x] T021 [US3] Add a project reference to `backend/src/InvoiceApp.Api/InvoiceApp.Api.csproj` in `backend/tests/InvoiceApp.Tests/InvoiceApp.Tests.csproj`, then add `backend/tests/InvoiceApp.Tests/ApprovalsControllerAuthorizationTests.cs`. Inspect the `ApprovalsController` class- and action-level `RequireRoleAttribute` metadata and execute the actual filters with an `ActionExecutingContext` containing each role's `UserSession`. Verify Approve allows Manager and Director and denies Approver and Admin without invoking the action or changing invoice state; verify Reject allows Manager, Director, Approver, and Admin for invoices at both low and high amounts, with the authorization outcome independent of invoice amount.

### Implementation for User Story 1 and User Story 3

- [x] T006 [P] [US3] Update `backend/src/InvoiceApp.Api/appsettings.json` with `demo.manager` / Manager and `demo.director` / Director demo users, retaining the existing Admin account.
- [x] T007 [P] [US1] Update `backend/src/InvoiceApp.Domain/ApprovalService.cs` to accept trusted username and role and enforce inclusive 50,000 Manager and 500,000 Director single-approval limits, rejecting ineligible roles before mutation.
- [x] T008 [US3] Update `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs` to pass signed-in username and role to the service; authorize the Approve endpoint for Manager and Director only, and the Reject endpoint for Manager, Director, Approver, and Admin with no invoice amount limit.
- [x] T009 [US1] Run `dotnet test backend/InvoiceApp.sln` and verify Step A threshold, endpoint role-matrix, error-reason, and state-preservation cases pass.
- [ ] T010 [US1] Commit the independently tested Step A changes, including all T003 atomic repository changes, in `backend/src/InvoiceApp.Domain/ApprovalService.cs`, `backend/src/InvoiceApp.Domain/IInvoiceRepository.cs`, `backend/src/InvoiceApp.Api/InMemoryInvoiceRepository.cs`, `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`, `backend/src/InvoiceApp.Api/appsettings.json`, `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`, `backend/tests/InvoiceApp.Tests/InvoiceApp.Tests.csproj`, `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`, and `backend/tests/InvoiceApp.Tests/ApprovalsControllerAuthorizationTests.cs`.

**Checkpoint**: Step A is independently tested and committed with single-approver limits and roles in place.

## Phase 5: Implementation Step B — User Story 2 (P1)

**Goal**: Require two approvals for invoices above 500,000, with distinct identities and a Director as the second approver; invalid attempts leave the invoice unchanged.

**Independent Test**: For an invoice over 500,000, verify a Manager or Director first approval leaves it pending with one recorded approval and no final decision maker; a different Director completes approval; a duplicate user or non-Director second approver receives a clear reason and leaves status, decision fields, and recorded approvals unchanged. Verify concurrent attempts observe the latest invoice state.

### Tests for User Story 2

- [x] T011 [US2] Add xUnit cases in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for both eligible first approver roles, distinct Director completion, same-user refusal, Manager-as-second refusal, state preservation for refused approval attempts, and concurrent same-invoice attempts against the latest state.

### Implementation for User Story 2

- [x] T012 [US2] Add an immutable approval record and ordered approval collection to `backend/src/InvoiceApp.Domain/Invoice.cs`, preserving the data-model constraints: approval username is distinct per invoice, role is Manager or Director, sequence starts at 1, and approval time is recorded.
- [x] T013 [US2] Implement the high-value transitions in `backend/src/InvoiceApp.Domain/ApprovalService.cs`: amounts above 500,000 require a first Manager or Director approval and a second approval from a different Director; only the second approval sets Approved, DecidedBy, and DecidedAt.
- [x] T014 [US2] Route approval validation and state changes through the atomic repository operation implemented in `backend/src/InvoiceApp.Domain/IInvoiceRepository.cs`, `backend/src/InvoiceApp.Api/InMemoryInvoiceRepository.cs`, and `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`; refuse invalid or duplicate approval attempts without changing any invoice field or approval record.
- [x] T015 [US2] Run `dotnet test backend/InvoiceApp.sln` and verify all Step B high-value, role, identity, immutability, and concurrency cases pass.
- [x] T016 [US2] Commit the independently tested Step B changes in `backend/src/InvoiceApp.Domain/Invoice.cs`, `backend/src/InvoiceApp.Domain/ApprovalService.cs`, `backend/src/InvoiceApp.Domain/IInvoiceRepository.cs`, `backend/src/InvoiceApp.Api/InMemoryInvoiceRepository.cs`, and `backend/tests/InvoiceApp.Tests/`.

**Checkpoint**: Step B is independently tested and committed with the two-distinct-approver flow.

## Phase 6: Polish and Cross-Cutting Concerns

**Purpose**: Demonstrate the refused approval attempt reason through the UI and capture end-to-end and post-change mutation evidence.

- [ ] T017 [P] Ensure `backend/src/InvoiceApp.Api/wwwroot/app.js` displays the API reason for a refused approval attempt in the existing accessible decision alert in `backend/src/InvoiceApp.Api/wwwroot/index.html`, without presenting the attempt as success.
- [ ] T018 Add a page object in `e2e/pages/InvoiceApprovalsPage.ts` using Playwright role/label locators (`getByRole`, `getByLabel`) for sign-in, invoice submission, approval, and reading the decision alert.
- [ ] T019 Add one Playwright test in `e2e/tests/approval-limits.spec.ts` that signs in as `demo.manager`, submits an invoice above 50,000, attempts approval, and asserts via the page object that the refused approval attempt reason is shown and the invoice remains PendingApproval.
- [ ] T020 Run `npm test` from `e2e/` and `dotnet test backend/InvoiceApp.sln`; record both results and the post-change `dotnet stryker` score from `backend/tests/InvoiceApp.Tests/` alongside the baseline in repository-root `ASSIGNMENT.md`.

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 commits the specification artifacts before implementation.
- Phase 2 (Setup) follows the specification commit: the Stryker baseline must be captured before feature implementation.
- Phase 3 (Foundational) follows the baseline and provides the atomic repository contract needed for safe approval changes.
- Step A (US1 and US3) follows Phase 3 and must pass before its commit.
- Step B (US2) follows Step A because it extends the role-aware approval service and must pass before its separate commit.
- Phase 6 follows both implementation commits; run the Playwright test after demo role configuration and service behavior are available.

### User Story Dependencies

- **US1 — Single-approver limits (Step A)**: Depends on the atomic repository boundary; independent test is its threshold and state-preservation unit-test set, including refusal of approval for an already-decided invoice.
- **US3 — Eligible roles (Step A)**: Delivered with US1 through demo-user configuration and approval action authorization; legacy Approver and Admin remain ineligible to approve.
- **US2 — Two approvals (Step B)**: Depends on Step A's role-aware service and the atomic repository boundary.

### Parallel Opportunities

- T006 (demo user configuration), T007 (Domain single-approval rules), and T021 (controller/API role-matrix tests) touch separate files and can proceed in parallel after T004 and T005.
- T008 (controller authorization implementation) depends on the role-matrix test task T021; T009 runs the full Step A validation after the implementation tasks.
- The final UI behavior check (T017) can proceed independently from Step B's Domain changes after Step A is committed (T010); the Playwright test (T019) depends on the page object (T018) and Manager configuration (T006).
- Mutation baseline T002 cannot run in parallel with feature implementation because it must reflect the unmodified Domain project.

### Dependency Graph

```text
T001 -> T002 -> T003 -> T004 -> T005 -> (T006 || T007 || T021) -> T008 -> T009 -> T010
                                                                        T010 -> T011 -> T012 -> T013 -> T014 -> T015 -> T016
T017 (after T006 and T010) -> T018 -> T019 (after T016 and T018) -> T020
```

## Implementation Strategy

1. Commit the specification artifacts as a distinct commit.
2. Capture the mutation baseline before changing feature code.
3. Implement Step A as the MVP: atomic repository changes, role accounts, authorization, single-approver limits, and their unit tests; validate and commit it independently.
4. Implement Step B separately: approval records, two-person high-value decisions, identity/role refusals, atomicity, and unit tests; validate and commit it independently.
5. Complete visibility of refused approval attempts and one page-object-driven Playwright test.
6. Run the full validation commands and final mutation analysis; update `ASSIGNMENT.md` with before/after scores and test outcomes.
