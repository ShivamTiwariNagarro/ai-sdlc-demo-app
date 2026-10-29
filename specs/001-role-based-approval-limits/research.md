# Research: Role-Based Invoice Approval Limits

## Atomic approval updates

**Decision**: Add a synchronous atomic-update operation to `IInvoiceRepository`. The operation loads the latest invoice state, runs the Domain transition, and commits it as one indivisible operation. The in-memory implementation can protect this operation with its repository-wide lock. The test fake implements the same contract.

**Rationale**: `GetById` currently returns a mutable invoice after the repository's per-call lock has been released. A service-level lock on a fetched object is not a robust repository contract: it relies on all callers receiving and locking the same object instance. Keyed process-local locks require all service instances to share the same registry, and their lifetime/cleanup adds complexity. An atomic repository operation provides an explicit serialization boundary while leaving all approval rules in `ApprovalService` and lifecycle data in `Invoice`.

**Boundary**: The current in-memory singleton repository provides atomicity within one app process. A future database or multi-instance implementation must provide equivalent transaction or concurrency-control semantics. This plan does not claim distributed locking.

**Testing**: Test success and rejection for concurrent same-invoice attempts released together, verifying that each transition validates against the state committed by the preceding transition. Include unit tests for the repository contract using `FakeInvoiceRepository`.

**Alternatives considered**:

- Locking only the fetched invoice: rejected because it depends on object identity and every caller cooperating with that lock.
- A static/per-service keyed lock registry: rejected as the primary consistency boundary because separate registries or processes do not coordinate.
- Relying on each repository `GetById` and `Update` lock independently: rejected because it does not make the read-check-write sequence atomic.

## Role and identity source

**Decision**: The controller reads the username and role from `HttpContext.GetSession()` and passes them to `ApprovalService`; no approver identity or role is accepted from a request body. Configure demo Manager and Director accounts in `appsettings.json`.

**Rationale**: The existing sign-in flow stores trusted username and role in the session. Domain rules can be tested independently with explicit identities and roles, while the controller does not become a policy layer.

**Alternatives considered**: Mapping `Approver` or `Admin` into a business approver role is explicitly excluded by the feature requirements.

## API and UI errors

**Decision**: Keep the existing `Handle(...)` path for service errors. Invalid approval transitions use the repository's existing `InvalidOperationException` to `409 Conflict` mapping and `{ "error": "..." }` payload. Render the API error in the existing UI `role="alert"` region.

**Rationale**: This preserves the established controller/service error convention and gives the user a visible reason without adding an API error format.

**Alternatives considered**: A new response envelope or controller-specific rule checks are unnecessary and risk inconsistent behavior.

## Mutation testing

**Decision**: Use the pinned global tool version `dotnet-stryker` 4.13.0. Install with `dotnet tool install --global dotnet-stryker --version 4.13.0`. Run `dotnet stryker` from `backend/tests/InvoiceApp.Tests`; the test project has a single project reference to `InvoiceApp.Domain`, and Stryker's test-project mode selects the referenced source project.

**Rationale**: The assignment pins this version for .NET 8 compatibility. Pinning avoids accidentally selecting a newer tool runtime. Capture the baseline score before feature implementation and the final score after all feature tests, then include both in `ASSIGNMENT.md`.

**Alternatives considered**: Using an unpinned newest release was rejected because the assignment warns it targets a later runtime.

**References**: [Stryker.NET operating modes](https://stryker-mutator.io/docs/stryker-net/operating-modes/); [Stryker.NET getting started](https://stryker-mutator.io/docs/stryker-net/getting-started/); [dotnet-stryker 4.13.0 on NuGet](https://www.nuget.org/packages/dotnet-stryker/4.13.0).
