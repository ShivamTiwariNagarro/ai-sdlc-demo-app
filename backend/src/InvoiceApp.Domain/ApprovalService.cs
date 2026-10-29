namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// </summary>
public class ApprovalService
{
    private const decimal ManagerApprovalLimit = 50_000m;
    private const decimal DirectorApprovalLimit = 500_000m;

    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        return UpdateAtomically(invoiceId, invoice =>
        {
            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException(
                    $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
            }

            invoice.Status = InvoiceStatus.PendingApproval;
        });
    }

    public Invoice Approve(string invoiceId, string approver, string role)
    {
        return UpdateAtomically(invoiceId, invoice =>
        {
            EnsurePending(invoice);
            EnsureEligibleRole(role);
            EnsurePositiveAmount(invoice);

            if (invoice.Amount <= DirectorApprovalLimit)
            {
                ApproveWithinSingleApprovalLimit(invoice, approver, role);
                return;
            }

            ApproveHighValueInvoice(invoice, approver, role);
        });
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        return UpdateAtomically(invoiceId, invoice =>
        {
            EnsurePending(invoice);

            invoice.Status = InvoiceStatus.Rejected;
            invoice.DecidedBy = approver;
            invoice.DecidedAt = DateTime.UtcNow;
            invoice.DecisionNote = reason;
        });
    }

    private Invoice UpdateAtomically(string invoiceId, Action<Invoice> transition) =>
        _repository.UpdateAtomically(invoiceId, transition)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsureEligibleRole(string role)
    {
        if (role is not ("Manager" or "Director"))
        {
            throw new InvalidOperationException(
                $"Role {role} is not permitted to approve invoices.");
        }
    }

    private static void EnsurePositiveAmount(Invoice invoice)
    {
        if (invoice.Amount <= 0)
        {
            throw new InvalidOperationException("Invoice amount must be greater than zero.");
        }
    }

    private static void ApproveWithinSingleApprovalLimit(Invoice invoice, string approver, string role)
    {
        if (invoice.Approvals.Count != 0)
        {
            throw new InvalidOperationException(
                "An invoice within the single-approval limit cannot have prior approval records.");
        }

        var limit = role == "Manager" ? ManagerApprovalLimit : DirectorApprovalLimit;
        if (invoice.Amount > limit)
        {
            throw new InvalidOperationException(
                $"{role} approval limit is {FormatAmount(limit)}; invoice amount is {FormatAmount(invoice.Amount)}.");
        }

        RecordApproval(invoice, approver, role, 1);
        CompleteApproval(invoice, approver);
    }

    private static void ApproveHighValueInvoice(Invoice invoice, string approver, string role)
    {
        if (invoice.DecidedBy is not null || invoice.DecidedAt is not null)
        {
            throw new InvalidOperationException(
                "A high-value invoice cannot have final decision details before both approvals are recorded.");
        }

        if (invoice.Approvals.Count == 0)
        {
            RecordApproval(invoice, approver, role, 1);
            return;
        }

        if (invoice.Approvals.Count != 1)
        {
            throw new InvalidOperationException(
                "A high-value invoice can have only one prior approval while awaiting its second approver.");
        }

        var firstApproval = invoice.Approvals[0];
        if (firstApproval.Sequence != 1 || firstApproval.Role is not ("Manager" or "Director"))
        {
            throw new InvalidOperationException(
                "The existing first approval is invalid; it must be from a Manager or Director.");
        }

        if (string.Equals(firstApproval.Username, approver, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The same user cannot provide both approvals for an invoice.");
        }

        if (role != "Director")
        {
            throw new InvalidOperationException(
                "The second approval for an invoice above 500000 must be provided by a Director.");
        }

        RecordApproval(invoice, approver, role, 2);
        CompleteApproval(invoice, approver);
    }

    private static void RecordApproval(Invoice invoice, string approver, string role, int sequence)
    {
        invoice.Approvals.Add(new InvoiceApproval(approver, role, sequence, DateTime.UtcNow));
    }

    private static void CompleteApproval(Invoice invoice, string approver)
    {
        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
    }

    private static string FormatAmount(decimal amount) =>
        amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }
}
