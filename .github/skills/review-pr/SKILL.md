---
name: review-pr
description: 'Review a pull request diff for invoice approval correctness, tests, and layering. Use when asked to review or judge PR changes.'
---

# Reviewing a Pull Request

Use this skill to review the requested pull request diff and judge each finding.

## Review checklist

- Verify the approval roles are Manager and Director, and that Admin cannot count. Do not assume the API's existing `Approver` role maps to either business role; check that identity, role flow, and authorization support the specified roles.
- Check each approval rule, including role-specific limits and inclusive boundaries.
- Check the two-approver flow above 500,000, distinct user identities, and that the second approver is a Director.
- Check that rejected attempts leave invoice status and recorded approvals unchanged.
- Check tests cover boundaries, positive and negative outcomes, and assert meaningful state and error messages.
- Check approval business logic stays in `InvoiceApp.Domain` and controllers pass the signed-in user's identity and role to the service.

## Findings and verdicts

- List every finding with exactly one verdict:
  - **fixed** — state what was changed.
  - **dismissed with a reason** — explain why it is not an issue.
  - **deferred with a reason** — explain why it is postponed.
- Do not omit findings; make the verdict and its rationale clear in the pull request description.