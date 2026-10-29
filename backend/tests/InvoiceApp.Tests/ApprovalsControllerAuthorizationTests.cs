using System.Reflection;
using InvoiceApp.Api.Auth;
using InvoiceApp.Api.Controllers;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalsControllerAuthorizationTests
{
    [Theory]
    [InlineData("Manager", true)]
    [InlineData("Director", true)]
    [InlineData("Approver", false)]
    [InlineData("Admin", false)]
    public void Approve_AuthorizesOnlyManagersAndDirectors(string role, bool expectedAuthorization)
    {
        var (context, repository) = ExecuteFilters(nameof(ApprovalsController.Approve), role, 100m);

        Assert.Equal(expectedAuthorization, context.Result is null);
        AssertInvoiceUnchanged(repository.GetById("INV-1")!);
        if (!expectedAuthorization)
        {
            Assert.IsType<StatusCodeResult>(context.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, ((StatusCodeResult)context.Result!).StatusCode);
        }
    }

    [Theory]
    [InlineData("Manager", 100, true)]
    [InlineData("Manager", 500_001, true)]
    [InlineData("Director", 100, true)]
    [InlineData("Director", 500_001, true)]
    [InlineData("Approver", 100, true)]
    [InlineData("Approver", 500_001, true)]
    [InlineData("Admin", 100, true)]
    [InlineData("Admin", 500_001, true)]
    public void Reject_AuthorizesAllSpecifiedRolesRegardlessOfAmount(
        string role,
        decimal amount,
        bool expectedAuthorization)
    {
        var (context, repository) = ExecuteFilters(nameof(ApprovalsController.Reject), role, amount);

        Assert.Equal(expectedAuthorization, context.Result is null);
        AssertInvoiceUnchanged(repository.GetById("INV-1")!);
    }

    private static (ActionExecutingContext Context, FakeInvoiceRepository Repository) ExecuteFilters(
        string actionName,
        string role,
        decimal amount)
    {
        var invoice = new Invoice
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = amount,
            Status = InvoiceStatus.PendingApproval
        };
        var repository = new FakeInvoiceRepository(invoice);
        var controller = new ApprovalsController(new ApprovalService(repository));
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession("demo.user", role);
        var method = typeof(ApprovalsController).GetMethod(actionName)
            ?? throw new InvalidOperationException($"Action {actionName} was not found.");
        var descriptor = new ControllerActionDescriptor
        {
            ActionName = actionName,
            ControllerTypeInfo = typeof(ApprovalsController).GetTypeInfo(),
            MethodInfo = method
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor);
        controller.ControllerContext = new ControllerContext(actionContext);

        var filters = typeof(ApprovalsController)
            .GetCustomAttributes<RequireRoleAttribute>(inherit: true)
            .Concat(method.GetCustomAttributes<RequireRoleAttribute>(inherit: true))
            .Cast<IFilterMetadata>()
            .ToList();
        var context = new ActionExecutingContext(
            actionContext,
            filters,
            new Dictionary<string, object?>(),
            controller);

        foreach (var filter in filters.OfType<RequireRoleAttribute>())
        {
            filter.OnActionExecuting(context);
            if (context.Result is not null)
            {
                break;
            }
        }

        return (context, repository);
    }

    private static void AssertInvoiceUnchanged(Invoice invoice)
    {
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
    }
}
