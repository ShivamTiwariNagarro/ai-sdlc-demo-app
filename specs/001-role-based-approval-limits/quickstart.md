# Quickstart: Role-Based Invoice Approval Limits

## Prerequisites

- .NET 8 SDK.
- Node.js and the existing dependencies in `e2e/package-lock.json`.
- Playwright Chromium installed for the E2E project.
- Stryker.NET 4.13.0 installed as specified in the assignment.

## Validate the Domain rules

From the repository root:

```powershell
dotnet test backend/InvoiceApp.sln
```

Expected: all xUnit tests pass, including the 50,000 and 500,000 boundaries, over-limit rejection and unchanged state, role eligibility, two distinct approvers, Director-only second approval, and same-invoice concurrent attempts.

## Validate the UI flow

From `e2e`:

```powershell
npm test
```

Expected: the Playwright test signs in as `demo.manager`, submits an invoice above 50,000, attempts approval, and observes an accessible alert containing the rejection reason; the invoice remains pending.

The application can also be started from the repository root with:

```powershell
dotnet run --project backend/src/InvoiceApp.Api
```

Open `http://localhost:5000` and sign in with the demo Manager or Director account configured in `backend/src/InvoiceApp.Api/appsettings.json`.

## Measure mutation testing

If needed, install the assignment-pinned version:

```powershell
dotnet tool install --global dotnet-stryker --version 4.13.0
```

From `backend/tests/InvoiceApp.Tests`, run before implementation and again after the feature and tests are complete:

```powershell
dotnet stryker
```

The test project references only `InvoiceApp.Domain`, which Stryker selects in test-project mode. Record each mutation score and command/report summary in `ASSIGNMENT.md`; retain no generated mutation reports unless required by the submission.

## Design references

- Approval states and data: [data-model.md](data-model.md)
- HTTP behavior: [contracts/approvals-api.md](contracts/approvals-api.md)
