using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status, decimal amount = 100m) =>
        new() { Id = id, CustomerName = "Test School", Amount = amount, Status = status };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    private static (ApprovalService Service, FakeInvoiceRepository Repository) ServiceAndRepositoryFor(
        params Invoice[] invoices)
    {
        var repository = new FakeInvoiceRepository(invoices);
        return (new ApprovalService(repository), repository);
    }

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_ManagerWithinLimit_SetsStatusDecisionMakerAndTimestamp()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_ManagerAtLimit_Succeeds()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_000m));

        var result = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_ManagerAboveLimit_RefusesWithoutChangingInvoice()
    {
        AssertApprovalRefusedWithoutMutation(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_000.01m),
            "Manager",
            "demo.manager",
            "Manager approval limit is 50000; invoice amount is 50000.01.");
    }

    [Fact]
    public void Approve_DirectorAtLimit_Succeeds()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_000m));

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Collection(
            result.Approvals,
            approval =>
            {
                Assert.Equal("demo.director", approval.Username);
                Assert.Equal("Director", approval.Role);
                Assert.Equal(1, approval.Sequence);
            });
    }

    [Theory]
    [InlineData(500_000.01)]
    [InlineData(500_001)]
    public void Approve_DirectorAboveSingleApprovalLimit_RecordsFirstApprovalOnly(decimal amount)
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, amount));

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        Assert.Collection(
            result.Approvals,
            approval =>
            {
                Assert.Equal("demo.director", approval.Username);
                Assert.Equal("Director", approval.Role);
                Assert.Equal(1, approval.Sequence);
            });
    }

    [Fact]
    public void Approve_DirectorBelowManagerLimit_Succeeds()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 12_345m));

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Theory]
    [InlineData("Admin", "demo.admin")]
    [InlineData("Approver", "demo.approver")]
    public void Approve_IneligibleRole_RefusesWithoutChangingInvoice(string role, string username)
    {
        AssertApprovalRefusedWithoutMutation(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 100m),
            role,
            username,
            $"Role {role} is not permitted to approve invoices.");
    }

    [Fact]
    public void Approve_NonPositiveAmount_RefusesWithoutChangingInvoice()
    {
        AssertApprovalRefusedWithoutMutation(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 0m),
            "Manager",
            "demo.manager",
            "Invoice amount must be greater than zero.");
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.manager", "Manager"));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", "demo.manager", "Manager"));
    }

    [Fact]
    public void Approve_AlreadyDecidedInvoice_RefusesWithoutChangingInvoice()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Approved);
        invoice.DecidedBy = "previous.approver";
        invoice.DecidedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        invoice.DecisionNote = "Previously approved";
        invoice.Approvals.Add(new InvoiceApproval(
            "previous.approver",
            "Director",
            1,
            new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)));

        AssertApprovalRefusedWithoutMutation(
            invoice,
            "Manager",
            "demo.manager",
            "Invoice INV-1 is Approved; only pending invoices can be decided.");
    }

    [Fact]
    public void Approve_ManagerThenDirectorForHighValueInvoice_ApprovesAfterBothApprovals()
    {
        var (service, repository) = ServiceAndRepositoryFor(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_000.01m));

        var firstResult = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.PendingApproval, firstResult.Status);
        Assert.Null(firstResult.DecidedBy);
        Assert.Null(firstResult.DecidedAt);
        Assert.Collection(
            firstResult.Approvals,
            approval =>
            {
                Assert.Equal("demo.manager", approval.Username);
                Assert.Equal("Manager", approval.Role);
                Assert.Equal(1, approval.Sequence);
                Assert.NotEqual(default, approval.ApprovedAt);
            });

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Collection(
            result.Approvals,
            firstApproval =>
            {
                Assert.Equal("demo.manager", firstApproval.Username);
                Assert.Equal("Manager", firstApproval.Role);
                Assert.Equal(1, firstApproval.Sequence);
            },
            secondApproval =>
            {
                Assert.Equal("demo.director", secondApproval.Username);
                Assert.Equal("Director", secondApproval.Role);
                Assert.Equal(2, secondApproval.Sequence);
            });
        Assert.Equal(InvoiceStatus.Approved, repository.GetById("INV-1")!.Status);
    }

    [Fact]
    public void Approve_DirectorThenDirectorForHighValueInvoice_ApprovesAfterBothApprovals()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m));

        var firstResult = service.Approve("INV-1", "director.one", "Director");

        Assert.Equal(InvoiceStatus.PendingApproval, firstResult.Status);
        Assert.Null(firstResult.DecidedBy);
        Assert.Null(firstResult.DecidedAt);
        Assert.Single(firstResult.Approvals);

        var result = service.Approve("INV-1", "director.two", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("director.two", result.DecidedBy);
        Assert.Equal(2, result.Approvals.Count);
        Assert.Equal(new[] { "Director", "Director" }, result.Approvals.Select(approval => approval.Role));
        Assert.Equal(new[] { 1, 2 }, result.Approvals.Select(approval => approval.Sequence));
    }

    [Fact]
    public void Approve_DirectorThenManagerForHighValueInvoice_RefusesWithoutChangingInvoice()
    {
        var (service, repository) = ServiceAndRepositoryFor(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m));
        service.Approve("INV-1", "demo.director", "Director");

        AssertApprovalRefusedWithoutMutation(
            service,
            repository,
            "INV-1",
            "demo.manager",
            "Manager",
            "The second approval for an invoice above 500000 must be provided by a Director.");
    }

    [Fact]
    public void Approve_ManagerThenManagerForHighValueInvoice_RefusesWithoutChangingInvoice()
    {
        var (service, repository) = ServiceAndRepositoryFor(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m));
        service.Approve("INV-1", "manager.one", "Manager");

        AssertApprovalRefusedWithoutMutation(
            service,
            repository,
            "INV-1",
            "manager.two",
            "Manager",
            "The second approval for an invoice above 500000 must be provided by a Director.");
    }

    [Fact]
    public void Approve_SameUserTwiceForHighValueInvoice_RefusesWithoutChangingInvoice()
    {
        var (service, repository) = ServiceAndRepositoryFor(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m));
        service.Approve("INV-1", "same.user", "Director");

        AssertApprovalRefusedWithoutMutation(
            service,
            repository,
            "INV-1",
            "same.user",
            "Director",
            "The same user cannot provide both approvals for an invoice.");
    }

    [Fact]
    public async Task Approve_ConcurrentDirectorsForHighValueInvoice_UsesLatestInvoiceState()
    {
        var (service, repository) = ServiceAndRepositoryFor(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m));

        var results = await Task.WhenAll(
            Task.Run(() => service.Approve("INV-1", "director.one", "Director")),
            Task.Run(() => service.Approve("INV-1", "director.two", "Director")));

        Assert.Contains(results, result => result.Status == InvoiceStatus.PendingApproval);
        Assert.Contains(results, result => result.Status == InvoiceStatus.Approved);

        var invoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
        Assert.Equal(2, invoice.Approvals.Count);
        Assert.Equal(2, invoice.Approvals.Select(approval => approval.Username)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(new[] { 1, 2 }, invoice.Approvals.Select(approval => approval.Sequence));
        Assert.Equal("Director", invoice.Approvals[1].Role);
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.approver", "  "));
    }

    private static void AssertApprovalRefusedWithoutMutation(
        Invoice invoice,
        string role,
        string username,
        string expectedReason)
    {
        invoice.DecidedBy = "existing.decision";
        invoice.DecidedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        invoice.DecisionNote = "Existing decision details";
        var originalState = Copy(invoice);
        var (service, repository) = ServiceAndRepositoryFor(invoice);

        AssertApprovalRefusedWithoutMutation(
            service,
            repository,
            invoice.Id,
            username,
            role,
            expectedReason,
            originalState);
    }

    private static void AssertApprovalRefusedWithoutMutation(
        ApprovalService service,
        FakeInvoiceRepository repository,
        string invoiceId,
        string username,
        string role,
        string expectedReason)
    {
        var currentInvoice = repository.GetById(invoiceId)!;
        AssertApprovalRefusedWithoutMutation(
            service,
            repository,
            invoiceId,
            username,
            role,
            expectedReason,
            Copy(currentInvoice));
    }

    private static void AssertApprovalRefusedWithoutMutation(
        ApprovalService service,
        FakeInvoiceRepository repository,
        string invoiceId,
        string username,
        string role,
        string expectedReason,
        Invoice originalState)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoiceId, username, role));

        Assert.Equal(expectedReason, exception.Message);
        AssertInvoiceStateEqual(originalState, repository.GetById(invoiceId)!);
    }

    private static void AssertInvoiceStateEqual(Invoice expected, Invoice actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.CustomerName, actual.CustomerName);
        Assert.Equal(expected.Amount, actual.Amount);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.DecidedBy, actual.DecidedBy);
        Assert.Equal(expected.DecidedAt, actual.DecidedAt);
        Assert.Equal(expected.DecisionNote, actual.DecisionNote);
        Assert.Equal(expected.Approvals, actual.Approvals);
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
