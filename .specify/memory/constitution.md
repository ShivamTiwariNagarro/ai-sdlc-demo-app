<!--
Sync Impact Report
Version change: uninitialized template -> 1.0.0
Modified principles: none; this is the initial project constitution.
Added sections: five core principles, project constraints, development workflow,
and governance.
Removed sections: none.
Follow-up TODO: confirm the original ratification date.
-->

# Invoice Approval Demo App Constitution

## Core Principles

### I. Domain-Owned Business Rules
All invoice business rules MUST live in `InvoiceApp.Domain`. Controllers and
`Program.cs` MUST NOT implement business rules. This keeps policy independent of
the web and hosting layers and testable without infrastructure.

### II. Rule-Level Unit Tests
Every business rule MUST have positive and negative unit tests using
`FakeInvoiceRepository`; boundary conditions MUST also be tested. Approval
threshold tests MUST cover the exact 50,000 and 500,000 limits and amounts just
above them. Tests MUST verify the required outcome and clear rejection reason.

### III. Validate Before Mutating State
An action MUST validate all applicable rules before changing invoice state or
recording approvals. A rejected action MUST leave the invoice status and
recorded approvals unchanged.

### IV. Credential Safety
Real credentials, tokens, and customer data MUST NOT be committed. Demo
credentials are not real credentials, but logs and other artifacts MUST still
be reviewed for sensitive data before sharing or committing them.

### V. Repository Conventions
Contributors and automated agents MUST follow `AGENTS.md` for project layout,
implementation, testing, and review conventions. For approval behavior, the
assignment-specific approval rules in `AGENTS.md` take precedence over its
generic role examples.

## Project Constraints

- The project is an ASP.NET Core 8 Web API with a small static web UI.
- `InvoiceApp.Domain` owns lifecycle and approval rules and MUST NOT depend on
  web or storage code.
- Controllers MUST follow the repository's controller, request-record, role
  authorization, and error-handling conventions in `AGENTS.md`.
- Approval business roles and thresholds MUST follow the assignment-specific
  rules in `AGENTS.md`; roles MUST NOT be inferred by mapping unrelated roles.
- End-to-end tests MUST use page objects and accessible role- or label-based
  locators, not CSS or XPath selectors.

## Development Workflow

- Every new business rule or endpoint MUST include tests following the
  conventions in `AGENTS.md`.
- Tests MUST use `FakeInvoiceRepository` for invoice service rules and verify
  state preservation on rejected operations.
- The full suite, `dotnet test backend/InvoiceApp.sln`, MUST pass before a
  commit.
- Review findings in pull request descriptions MUST each have one disposition:
  fixed, dismissed with a reason, or deferred with a reason.
- Interaction logs MUST be reviewed for sensitive data before sharing or
  committing them.

## Governance

This constitution governs project-wide engineering principles. Amendments MUST
be made in this file, reviewed for consistency with `AGENTS.md`, and validated
against the project. Version numbers follow semantic versioning: MAJOR for
backward-incompatible changes to principles, MINOR for new or materially
expanded principles or sections, and PATCH for non-semantic clarifications.
Contributors and reviewers MUST check changes for compliance with this
constitution. If a generic convention conflicts with an approval-specific
assignment rule, the approval-specific rule governs.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): confirm original adoption date | **Last Amended**: 2026-10-29
