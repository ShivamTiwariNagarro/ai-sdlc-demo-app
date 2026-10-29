---
name: write-unit-tests
description: 'Write and review xUnit unit tests for ApprovalService and invoice approval rules in this repository.'
---

# Writing Unit Tests

Use this skill when adding or reviewing unit tests for `ApprovalService`.

## Conventions

- Create one test class per class under test, named `<ClassUnderTest>Tests`.
- Use `FakeInvoiceRepository`; do not use real storage.
- Name tests to describe the action and expected result, for example `Approve_ManagerAboveLimit_Throws`.
- Follow the existing test project patterns and keep business-rule tests focused.

## Coverage and assertions

- Test the exact 50,000 and 500,000 boundaries and an amount one cent above each relevant limit.
- Cover Manager and Director approvals, including a Director approving an invoice below 50,000.
- Cover invoices above 500,000 requiring two distinct approvers, with the second approver a Director.
- Include negative tests proving the same user cannot count twice and an Admin cannot count as an approver for these limits.
- For successful and rejected attempts, assert observable outcomes: invoice status, `DecidedBy` where applicable, recorded approvals, and clear error-message text.
- After every rejected attempt, assert that the invoice status and recorded approvals are unchanged.
- Do not treat “no exception was thrown” or “an exception was thrown” as sufficient evidence by itself.