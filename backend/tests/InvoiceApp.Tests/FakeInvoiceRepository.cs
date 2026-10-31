using InvoiceApp.Domain;

namespace InvoiceApp.Tests;

internal class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly object _lock = new();
    private readonly List<Invoice> _invoices = new();

    public FakeInvoiceRepository(params Invoice[] seed) => _invoices.AddRange(seed);

    public IReadOnlyList<Invoice> GetAll()
    {
        lock (_lock) return _invoices.ToList();
    }

    public Invoice? GetById(string id)
    {
        lock (_lock) return _invoices.FirstOrDefault(invoice => invoice.Id == id);
    }

    public IReadOnlyList<Invoice> GetByIds(IEnumerable<string> ids)
    {
        var wanted = ids.ToHashSet();
        lock (_lock) return _invoices.Where(invoice => wanted.Contains(invoice.Id)).ToList();
    }

    public Invoice Add(Invoice invoice)
    {
        lock (_lock)
        {
            _invoices.Add(invoice);
            return invoice;
        }
    }

    public void Update(Invoice invoice)
    {
        lock (_lock)
        {
            var index = _invoices.FindIndex(existing => existing.Id == invoice.Id);
            if (index >= 0) _invoices[index] = invoice;
        }
    }

    public Invoice? UpdateAtomically(string id, Action<Invoice> transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        lock (_lock)
        {
            var index = _invoices.FindIndex(invoice => invoice.Id == id);
            if (index < 0)
            {
                return null;
            }

            var updatedInvoice = Copy(_invoices[index]);
            transition(updatedInvoice);
            _invoices[index] = updatedInvoice;
            return updatedInvoice;
        }
    }

    public bool Remove(string id)
    {
        lock (_lock) return _invoices.RemoveAll(invoice => invoice.Id == id) > 0;
    }

    private static Invoice Copy(Invoice invoice) =>
        new()
        {
            Id = invoice.Id,
            CustomerName = invoice.CustomerName,
            Amount = invoice.Amount,
            Status = invoice.Status,
            CreatedAt = invoice.CreatedAt,
            DecidedBy = invoice.DecidedBy,
            DecidedAt = invoice.DecidedAt,
            DecisionNote = invoice.DecisionNote,
            Approvals = invoice.Approvals.ToList()
        };
}
