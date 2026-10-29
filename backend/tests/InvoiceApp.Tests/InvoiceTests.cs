using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class InvoiceTests
{
    [Fact]
    public void NewInvoice_HasExpectedDefaultValues()
    {
        var repository = new FakeInvoiceRepository(new Invoice());

        var invoice = Assert.IsType<Invoice>(repository.GetById(string.Empty));

        Assert.Equal(string.Empty, invoice.Id);
        Assert.Equal(string.Empty, invoice.CustomerName);
        Assert.Equal(0m, invoice.Amount);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.NotEqual(default, invoice.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, invoice.CreatedAt.Kind);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
        Assert.Empty(invoice.Approvals);
    }
}
