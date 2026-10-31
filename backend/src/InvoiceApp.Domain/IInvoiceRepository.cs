namespace InvoiceApp.Domain;

public interface IInvoiceRepository
{
    IReadOnlyList<Invoice> GetAll();
    Invoice? GetById(string id);
    IReadOnlyList<Invoice> GetByIds(IEnumerable<string> ids);
    Invoice Add(Invoice invoice);
    void Update(Invoice invoice);
    /// <summary>Persists a transition to a copy of the invoice only if the transition completes.</summary>
    /// <returns>The updated invoice, or null when no invoice with the given ID exists.</returns>
    Invoice? UpdateAtomically(string id, Action<Invoice> transition);
    bool Remove(string id);
}
