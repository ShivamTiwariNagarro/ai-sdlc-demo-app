# Approvals API Contract

## Authentication and authorization

All endpoints require the existing bearer-session authentication.

- The Approve endpoint is authorized only for `Manager` and `Director`.
- The Reject endpoint is authorized for `Manager`, `Director`, `Approver`, and `Admin`, with no invoice amount limit.
- `Admin` and the legacy `Approver` role cannot approve or count toward an approval.
- Username and role are read from the signed-in session, not request data.

## Approve an invoice

`POST /api/approvals/{invoiceId}/approve`

No request body is required. The server passes the session username and role to the Domain approval service.

**Success**: `200 OK`, with the updated invoice JSON. A successful high-value first approval returns a still-pending invoice with one recorded approval and no final decision maker. A final approval returns an approved invoice with all approvals recorded.

**Failure**:

| Status | Meaning | Body |
|---|---|---|
| `403 Forbidden` | Session role is not permitted to call the action. | Existing authorization response. |
| `404 Not Found` | Invoice does not exist. | `{ "error": "<clear message>" }` |
| `409 Conflict` | Invoice state, amount, approval sequence, or approver identity makes this approval request invalid. | `{ "error": "<clear reason>" }` |

Refused approval attempts (Approve requests that break an amount limit or approval-sequence rule) preserve status, `DecidedBy`, decision time, and recorded approvals. This refusal is distinct from calling the Reject endpoint, which performs an invoice rejection.

## Reject an invoice

`POST /api/approvals/{invoiceId}/reject`

Request body:

```json
{ "reason": "A concise explanation" }
```

The endpoint is authorized for `Manager`, `Director`, `Approver`, and `Admin`, regardless of invoice amount. It performs an invoice rejection; this is distinct from a refused approval attempt. Service errors use the controller's existing `Handle(...)` mapping.

## UI behavior

When an approval request is refused, the static UI displays the API error in the existing `role="alert"` decision-error region. It must not present a refused approval attempt as successful or refresh away the reason before it can be read.
