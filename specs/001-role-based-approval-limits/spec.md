# Feature Specification: Role-Based Invoice Approval Limits

**Feature Branch**: `Not created (no before_specify hook is configured)`

**Created**: 2026-10-29

**Status**: Draft

**Input**: User description: "Add approval limits by role to the invoice approval service. A Manager may approve invoices up to and including 50,000. A Director may approve up to and including 500,000, including small invoices. An invoice above 500,000 needs two distinct approvers, and the second approver must be a Director; the first may be a Manager or a Director. The same user can never count as both approvers on one invoice. A refused approval attempt (an approval that breaks the limit rules) returns a clear reason and leaves the invoice completely unchanged (status, decided-by and recorded approvals). Admin does not count as an approver. The old Approver role no longer approves invoices."

## Clarifications

### Session 2026-10-29

- Q: Should concurrent approval attempts for one invoice be processed atomically against the latest invoice state? → A: Yes; process attempts atomically per invoice so only valid distinct approvals are recorded.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Approve within role limits (Priority: P1)

A Manager or Director reviews a pending invoice and approves it when its amount is within that role's single-approval limit. Directors may approve invoices of any positive amount up to and including 500,000.

**Why this priority**: This supports routine invoice decisions while applying the correct authority for the amount.

**Independent Test**: Submit pending invoices at, below, and above each single-approval threshold; verify permitted approvals complete and disallowed approvals explain the reason without changing the invoice.

**Acceptance Scenarios**:

1. **Given** a pending invoice for 50,000, **When** a Manager approves it, **Then** it becomes Approved and records that Manager as the decision maker and approver.
2. **Given** a pending invoice for 50,000.01, **When** a Manager attempts approval, **Then** the approval attempt is refused with a clear reason and the status, decided-by value, and recorded approvals remain unchanged.
3. **Given** a pending invoice for 500,000, **When** a Director approves it, **Then** it becomes Approved and records the Director's approval.
4. **Given** a pending invoice for 500,000.01, **When** a Director attempts to approve it as a single approver, **Then** it remains pending and the attempt explains that a second approval is required.
5. **Given** a pending invoice for 50,000 or less, **When** a Director approves it, **Then** it becomes Approved.
6. **Given** an invoice that has already been decided, **When** a Manager or Director attempts to approve it, **Then** the approval attempt is refused with a clear reason and its status, decision fields, and recorded approvals remain unchanged.

---

### User Story 2 - Complete two approvals for a high-value invoice (Priority: P1)

For an invoice above 500,000, a first Manager or Director records an approval and a different Director provides the second approval to complete the decision.

**Why this priority**: High-value invoices require additional oversight and independent approval.

**Independent Test**: Use an invoice above 500,000 and verify both valid first-approver paths, successful completion by a different Director, and preservation of the first approval when a second approval attempt is refused.

**Acceptance Scenarios**:

1. **Given** a pending invoice above 500,000, **When** a Manager provides the first approval, **Then** the invoice remains pending and records that approval without setting a final decided-by value.
2. **Given** a pending invoice above 500,000, **When** a Director provides the first approval, **Then** the invoice remains pending and records that approval without setting a final decided-by value.
3. **Given** a high-value invoice with one valid approval, **When** a different Director provides the second approval, **Then** the invoice becomes Approved and both distinct approvals are recorded.
4. **Given** a high-value invoice with one recorded approval, **When** the same user attempts the second approval, **Then** the approval attempt is refused with a clear reason and the invoice's status, decided-by value, and recorded approvals remain exactly as they were before the attempt.
5. **Given** a high-value invoice with one Manager approval, **When** another Manager attempts the second approval, **Then** the approval attempt is refused because the second approver must be a Director, and the invoice remains unchanged.

---

### User Story 3 - Enforce endpoint role access (Priority: P1)

The Approve endpoint is available only to Manager and Director roles. The Reject endpoint is available to Manager, Director, Approver, and Admin roles, with no invoice amount limit. Admin and the legacy Approver role cannot approve or count toward either approval requirement.

**Why this priority**: Role eligibility is necessary to enforce the authority limits and ensure high-value approvals are valid.

**Independent Test**: Attempt approval as Admin and as a user with the legacy Approver role at both single-approval and two-approval stages; verify each attempt is refused without changing invoice state or approval records. Verify that Manager, Director, Approver, and Admin can use the Reject endpoint regardless of invoice amount.

**Acceptance Scenarios**:

1. **Given** a pending invoice, **When** an Admin attempts to call Approve, **Then** endpoint authorization denies the request and the invoice remains unchanged.
2. **Given** a pending invoice, **When** a user with the legacy Approver role attempts to call Approve, **Then** endpoint authorization denies the request and the invoice remains unchanged.
3. **Given** a high-value invoice with a first approval, **When** an Admin or legacy Approver attempts to call Approve for the second approval, **Then** endpoint authorization denies the request and the existing approval remains unchanged.
4. **Given** an invoice of any amount, **When** a Manager, Director, Approver, or Admin calls the Reject endpoint, **Then** the endpoint is authorized for that role.

### Edge Cases

- At exactly 50,000, a Manager may approve; any greater amount requires a Director or the high-value two-approval path.
- At exactly 500,000, a Director may approve in one step; any greater amount requires two distinct approvers.
- A Director may single-approve invoices below 50,000, including amounts a Manager may also approve.
- An invalid second approval must not remove, replace, or add to the first recorded approval.
- Repeating an approval after an invoice has been decided must be refused without changing its decision or approvals.
- An Admin or legacy Approver must not be accepted as either approver, regardless of invoice amount.
- Concurrent approval attempts for one invoice must be processed atomically against the latest state; attempts made invalid by an earlier completed attempt must be refused without changing the invoice.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST determine approval eligibility from the signed-in user's actual role and identity; it MUST NOT treat Admin or the legacy Approver role as Manager or Director.
- **FR-002**: A Manager MUST be able to single-approve a pending invoice with a positive amount up to and including 50,000.
- **FR-003**: A Director MUST be able to single-approve a pending invoice with a positive amount up to and including 500,000, including amounts at or below 50,000.
- **FR-004**: A Manager MUST NOT single-approve an invoice above 50,000, and a Director MUST NOT single-approve an invoice above 500,000.
- **FR-005**: An invoice above 500,000 MUST require two approvals from distinct users; the first approver MUST be a Manager or Director, and the second approver MUST be a Director.
- **FR-006**: The system MUST compare approver identities to ensure the same user cannot fulfill both approval requirements.
- **FR-007**: Admin and the legacy Approver role MUST NOT create or count toward an invoice approval, whether as a first, second, or sole approver.
- **FR-008**: A refused approval attempt (an authorized Approve endpoint request that fails an applicable role-eligibility, amount-limit, approval-sequence, or invoice-state rule) MUST provide a clear reason and leave the invoice status, decision fields (including decided-by and decision time), and recorded approvals exactly as they were before that attempt. Requests denied by endpoint authorization are distinct from refused approval attempts.
- **FR-009**: An invoice MUST be marked Approved only after its applicable single- or two-approval requirements are satisfied.
- **FR-010**: An approval attempt for an invoice that is not pending MUST be refused with a clear reason and MUST leave the invoice status, decision fields (including decided-by and decision time), and recorded approvals exactly as they were before the attempt.
- **FR-011**: The system MUST process approval attempts for the same invoice atomically against the latest invoice state, recording only approvals that remain valid when processed.
- **FR-012**: The Approve endpoint MUST be authorized only for users with the Manager or Director role.
- **FR-013**: The Reject endpoint MUST be authorized for users with the Manager, Director, Approver, or Admin role, with no invoice amount limit.

### Key Entities *(include if feature involves data)*

- **Invoice**: An invoice under review, including its amount, current status, final decision maker, and any approvals recorded so far.
- **Approval**: A recorded approval associated with one invoice and one distinct user, including the user's role and whether it is the first or second approval.
- **Approver role**: The user's assigned business role, limited to Manager or Director for this feature; Admin and legacy Approver are ineligible.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested Manager decisions at 50,000 or less are accepted, and 100% of tested Manager decisions above 50,000 are refused without invoice changes.
- **SC-002**: 100% of tested Director decisions at or below 500,000 are accepted, and 100% of tested Director single-approval attempts above 500,000 are refused without invoice changes.
- **SC-003**: 100% of tested invoices above 500,000 remain pending after one valid approval and become Approved after a different Director provides the second approval.
- **SC-004**: 100% of tested duplicate-user and wrong-sequence approval attempts are refused with a clear reason while preserving status, decided-by value, and recorded approvals; Admin and legacy Approver calls to Approve are denied by endpoint authorization without changing invoice state.

## Assumptions

- The existing sign-in process remains in use, and its trusted user information can identify Manager and Director roles without mapping the legacy Approver or Admin roles to them.
- Approval endpoint authorization and Reject endpoint authorization are separate: only Manager and Director may use Approve, while Manager, Director, Approver, and Admin may use Reject regardless of invoice amount.
- All amounts use the invoice's existing currency and scale; the limits are inclusive and apply to the numeric invoice amount as stated.
- Refused approval attempts are evaluated against the invoice state immediately before each attempt; a valid first approval on a high-value invoice is preserved when a later attempt is refused.
