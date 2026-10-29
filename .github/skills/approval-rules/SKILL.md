---
name: approval-rules
description: 'Invoice approval limits and lifecycle rules. Use when implementing, testing, or reviewing ApprovalService and role-based invoice approvals.'
---

# Approval Rules

Use this skill when changing or reviewing invoice approval behavior.

## Rules

- The assignment's approval business roles are Manager and Director. Do not treat the API's existing `Approver` role as a synonym for either; Admin must not count as an approver. If the API only exposes `Approver` and `Admin`, update identity/role flow and authorization as part of implementation rather than inventing a mapping.
- A Manager may single-approve invoices up to and including 50,000.
- A Director may single-approve invoices up to and including 500,000, including invoices below 50,000.
- An invoice above 500,000 requires two distinct approvers. The first approver may be a Manager or Director; the second approver must be a Director.
- The same user must never count as both approvers for one invoice.
- An Admin does not count as an approver for these limits.
- Validate the approver's role, invoice amount, and existing approval before changing invoice state.
- A rejected attempt must return a clear reason and leave the invoice status and recorded approvals unchanged.

## Implementation and review checks

- Keep approval business rules in `InvoiceApp.Domain`; controllers pass the signed-in user's identity and role to the service.
- Use inclusive comparisons at 50,000 and 500,000.
- Compare user identities, not roles, when checking whether approvers are distinct.
- Ensure it is specifically the second approver who must be a Director.
- Avoid partial updates: validate all conditions before recording an approval or changing invoice state.
- Verify rejected attempts preserve all relevant invoice state, not just that an exception or error was returned.