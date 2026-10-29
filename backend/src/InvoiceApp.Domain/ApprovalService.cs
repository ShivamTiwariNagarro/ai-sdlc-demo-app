namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// One invoice at a time, decided as a whole by a single approver.
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
            EnsureWithinSingleApprovalLimit(invoice, role);

            invoice.Status = InvoiceStatus.Approved;
            invoice.DecidedBy = approver;
            invoice.DecidedAt = DateTime.UtcNow;
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

    private static void EnsureWithinSingleApprovalLimit(Invoice invoice, string role)
    {
        var limit = role == "Manager" ? ManagerApprovalLimit : DirectorApprovalLimit;
        if (invoice.Amount > limit)
        {
            throw new InvalidOperationException(
                $"{role} approval limit is {FormatAmount(limit)}; invoice amount is {FormatAmount(invoice.Amount)}.");
        }
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
