# Data Model: Role-Based Invoice Approval Limits

## Invoice

The existing invoice remains the lifecycle aggregate and gains an ordered collection of approvals.

| Field | Type | Meaning and constraints |
|---|---|---|
| `Id` | string | Existing invoice identifier. |
| `Amount` | decimal | Existing positive amount, evaluated against inclusive 50,000 and 500,000 thresholds. |
| `Status` | `InvoiceStatus` | Existing lifecycle status. Approval attempts are valid only while `PendingApproval`. |
| `DecidedBy` | nullable string | Remains unset after the first high-value approval; for a final two-person decision it is the second Director's username. |
| `DecidedAt` | nullable UTC date/time | Remains unset until the invoice is finally approved or rejected. |
| `Approvals` | ordered collection of `Approval` | Zero or one approval while awaiting a second decision; one completed approval for an eligible invoice at or below 500,000; exactly two distinct approvals for an invoice above 500,000 once approved. |

Existing invoice fields unrelated to approval remain unchanged.

## Approval

One immutable record of a user's approval of an invoice.

| Field | Type | Meaning and constraints |
|---|---|---|
| `Username` | string | Trusted signed-in identity. Must be distinct from every prior approver on this invoice. |
| `Role` | string | Trusted role at the time of approval; eligible values are `Manager` or `Director`. |
| `Sequence` | integer | Approval order, starting at 1; high-value approval sequence 2 must be a Director. |
| `ApprovedAt` | UTC date/time | Time this approval was accepted. |

Approval entries are append-only for an attempt. A failed attempt changes no invoice fields or approval entries.

## Approval State Transitions

| Current state | Amount and actor | Result |
|---|---|---|
| `PendingApproval`, no approvals | Positive amount `<= 50,000`, Manager | Append approval; set `Approved`, `DecidedBy`, and `DecidedAt`. |
| `PendingApproval`, no approvals | Positive amount `<= 500,000`, Director | Append approval; set `Approved`, `DecidedBy`, and `DecidedAt`. |
| `PendingApproval`, no approvals | Amount `> 500,000`, Manager or Director | Append first approval; remain pending; leave final decision fields unset. |
| `PendingApproval`, one approval, amount `> 500,000` | Distinct Director | Append second approval; set `Approved`, `DecidedBy` to this Director, and `DecidedAt`. |
| Any state | Ineligible role, excessive single approval, duplicate identity, invalid lifecycle state, or invalid second role | Reject with a clear reason and preserve invoice state. |

All approval transitions for an invoice are evaluated and committed atomically against its latest state. An approval call that arrives concurrently is processed after the current transition and must revalidate all rules.

## Roles

- `Manager`: may approve alone through 50,000; may be the first approver above 500,000.
- `Director`: may approve alone through 500,000 and may be the first or second approver above 500,000.
- `Admin`: cannot approve; existing rejection capability is preserved.
- `Approver`: legacy role; cannot approve or contribute an approval.

The API supplies the signed-in username and role; neither is accepted from an approval request body.
