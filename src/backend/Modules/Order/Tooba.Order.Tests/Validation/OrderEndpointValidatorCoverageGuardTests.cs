using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Commands.AssignTracking;
using Tooba.Order.Application.Admin.Operations.Commands.CancelOrder;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Customer.Queries.GetCustomerOrderDetail;
using Tooba.Order.Application.Storefront.Shipping.Commands.CommitStorefrontShipping;
using Tooba.Order.Application.Storefront.Shipping.Queries.ProjectStorefrontShipping;
using Tooba.Order.Application.Validation;
using Xunit;

namespace Tooba.Order.Tests.Validation;

/// <summary>TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001-R1 — exhaustive endpoint-reachable validator coverage.</summary>
public sealed class OrderEndpointValidatorCoverageGuardTests
{
    /// <summary>
    /// Explicit classification for every Order.Endpoints-reachable IRequest.
    /// New input-bearing requests must be added here with a concrete validator.
    /// </summary>
    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        // Admin completeness
        ("AddAdminOrderNoteCommand", "VALIDATOR_REQUIRED"),
        ("DeleteAdminOrderNoteCommand", "VALIDATOR_REQUIRED"),
        ("ListAdminOrderNotesQuery", "VALIDATOR_REQUIRED"),
        ("GetAdminOrderOperationalHistoryQuery", "VALIDATOR_REQUIRED"),
        ("GetAdminOrderInvoiceQuery", "VALIDATOR_REQUIRED"),
        ("GetAdminOrderReceiptQuery", "VALIDATOR_REQUIRED"),
        // Admin detail / grid / customers / legacy
        ("GetAdminOrderDetailQuery", "VALIDATOR_REQUIRED"),
        ("QueryAdminOrdersGridQuery", "VALIDATOR_REQUIRED"),
        ("QueryAdminCustomersGridQuery", "VALIDATOR_REQUIRED"),
        ("ListAdminOrdersQuery", "NO_VALIDATOR_REQUIRED"),
        ("ListAdminCustomersQuery", "NO_VALIDATOR_REQUIRED"),
        // Admin operations
        ("GetAdminOrderOperationsQuery", "VALIDATOR_REQUIRED"),
        ("ListAdminOrderReturnEligibilityQuery", "VALIDATOR_REQUIRED"),
        ("CancelOrderCommand", "VALIDATOR_REQUIRED"),
        ("ApproveReturnCommand", "VALIDATOR_REQUIRED"),
        ("AssignConsolidatedPackageTrackingCommand", "VALIDATOR_REQUIRED"),
        ("AssignTrackingCommand", "VALIDATOR_REQUIRED"),
        ("CancelConsolidatedPackageCommand", "VALIDATOR_REQUIRED"),
        ("CancelShipmentCommand", "VALIDATOR_REQUIRED"),
        ("ConfirmDepositCommand", "VALIDATOR_REQUIRED"),
        ("CorrectTrackingCommand", "VALIDATOR_REQUIRED"),
        ("CreateConsolidatedPackageCommand", "VALIDATOR_REQUIRED"),
        ("CreateShipmentCommand", "VALIDATOR_REQUIRED"),
        ("DeliverConsolidatedPackageCommand", "VALIDATOR_REQUIRED"),
        ("DeliverShipmentCommand", "VALIDATOR_REQUIRED"),
        ("DispatchConsolidatedPackageCommand", "VALIDATOR_REQUIRED"),
        ("DispatchShipmentCommand", "VALIDATOR_REQUIRED"),
        ("MarkFulfillmentPackedCommand", "VALIDATOR_REQUIRED"),
        ("MarkFulfillmentProcessingCommand", "VALIDATOR_REQUIRED"),
        ("PackFulfillmentSelectedCommand", "VALIDATOR_REQUIRED"),
        ("RecoverInventoryReservationCommand", "VALIDATOR_REQUIRED"),
        ("RejectDepositCommand", "VALIDATOR_REQUIRED"),
        ("RejectReturnCommand", "VALIDATOR_REQUIRED"),
        ("RequestReturnCommand", "VALIDATOR_REQUIRED"),
        ("RestoreCancelledOrderCommand", "VALIDATOR_REQUIRED"),
        ("RestoreDepositCommand", "VALIDATOR_REQUIRED"),
        ("RetryRefundCommand", "VALIDATOR_REQUIRED"),
        ("UnconfirmDepositCommand", "VALIDATOR_REQUIRED"),
        ("UnpackFulfillmentCommand", "VALIDATOR_REQUIRED"),
        ("UnprocessFulfillmentCommand", "VALIDATOR_REQUIRED"),
        // Recovery / supply
        ("AssessOrderInventoryRecoveryQuery", "VALIDATOR_REQUIRED"),
        ("AuditOrderInventoryRecoveryQuery", "VALIDATOR_REQUIRED"),
        ("GetOrderSupplyStatusQuery", "VALIDATOR_REQUIRED"),
        // Storefront
        ("PreviewStorefrontCheckoutQuery", "VALIDATOR_REQUIRED"),
        ("SubmitStorefrontCheckoutCommand", "VALIDATOR_REQUIRED"),
        ("GetStorefrontCheckoutQuery", "VALIDATOR_REQUIRED"),
        ("ListStorefrontPendingPaymentsQuery", "NO_VALIDATOR_REQUIRED"),
        ("CancelPendingCheckoutCommand", "VALIDATOR_REQUIRED"),
        ("HidePendingPaymentCardCommand", "VALIDATOR_REQUIRED"),
        ("ProjectStorefrontShippingQuery", "VALIDATOR_REQUIRED"),
        ("SaveStorefrontShippingSelectionCommand", "VALIDATOR_REQUIRED"),
        ("CommitStorefrontShippingCommand", "VALIDATOR_REQUIRED"),
        // Customer / seller
        ("ListCustomerOrdersQuery", "VALIDATOR_REQUIRED"),
        ("GetCustomerOrderDetailQuery", "VALIDATOR_REQUIRED"),
        ("RetryCustomerUnpaidOrderCommand", "VALIDATOR_REQUIRED"),
        ("ListSellerOrdersQuery", "VALIDATOR_REQUIRED"),
        ("GetSellerOrderDetailQuery", "VALIDATOR_REQUIRED"),
    ];

    [Fact]
    public void Manifest_covers_every_endpoint_reachable_request_exactly_once()
    {
        var names = Manifest.Select(x => x.RequestTypeName).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());

        var endpointText = string.Join(
            "\n",
            Directory.GetFiles(
                    Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));

        foreach (var (name, _) in Manifest)
        {
            Assert.Contains(name, endpointText, StringComparison.Ordinal);
        }

        // No unexplained *Command/*Query construction in endpoints outside the manifest.
        var constructed = System.Text.RegularExpressions.Regex.Matches(
                endpointText,
                @"\bnew\s+([A-Za-z0-9_]+(?:Command|Query))\b")
            .Select(m => m.Groups[1].Value)
            .Where(n => n is not ("Command" or "Query"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var required = Manifest.Select(x => x.RequestTypeName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(required, constructed.OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Every_VALIDATOR_REQUIRED_request_resolves_concrete_IValidator_via_foundation_DI()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(GetCustomerOrderDetailQuery).Assembly);
        using var sp = services.BuildServiceProvider();

        var appAsm = typeof(GetCustomerOrderDetailQuery).Assembly;
        foreach (var (name, classification) in Manifest.Where(x => x.Classification == "VALIDATOR_REQUIRED"))
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            var validatorType = typeof(IValidator<>).MakeGenericType(requestType);
            var validator = sp.GetService(validatorType);
            Assert.True(validator is not null, $"missing IValidator<{name}>");
        }
    }

    [Fact]
    public async Task Shared_admin_operation_rules_apply_to_multiple_concrete_commands()
    {
        var probe = new Counter();
        await using var sp = BuildOpsProbe(probe);
        var sender = sp.GetRequiredService<ISender>();
        var empty = new AdminOrderOperationRequest(
            "", null, null, null, null, null, null, null, null, null, null, null, null, null, null);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new CancelOrderCommand(Guid.Empty, Guid.Empty, empty)));
        await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new AssignTrackingCommand(Guid.Empty, Guid.Empty, empty)));
        Assert.Equal(0, probe.Value);
    }

    [Fact]
    public async Task Shipping_invalid_primitive_input_short_circuits()
    {
        var probe = new Counter();
        await using var sp = BuildShippingProbe(probe);
        var sender = sp.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new CommitStorefrontShippingCommand(Guid.Empty, null, -1, "", null)));
        await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new ProjectStorefrontShippingQuery(Guid.Empty, null, null, null, null)));
        Assert.Equal(0, probe.Value);
    }

    private static ServiceProvider BuildOpsProbe(Counter probe)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidatorsFromAssembly(typeof(CancelOrderCommand).Assembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(FoundationPingCommand).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton(probe);
        services.AddTransient<IRequestHandler<CancelOrderCommand, Result<object>>, OpsProbeHandler<CancelOrderCommand>>();
        services.AddTransient<IRequestHandler<AssignTrackingCommand, Result<object>>, OpsProbeHandler<AssignTrackingCommand>>();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildShippingProbe(Counter probe)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidatorsFromAssembly(typeof(CommitStorefrontShippingCommand).Assembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(FoundationPingCommand).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton(probe);
        services.AddTransient<IRequestHandler<CommitStorefrontShippingCommand, Result<Tooba.Order.Application.Storefront.Models.StorefrontCheckoutPage>>, ShippingCommitProbeHandler>();
        services.AddTransient<IRequestHandler<ProjectStorefrontShippingQuery, Result<Tooba.Order.Application.Storefront.Models.StorefrontShippingProjection>>, ShippingProjectProbeHandler>();
        return services.BuildServiceProvider();
    }

    private sealed class Counter
    {
        public int Value;
    }

    private sealed class OpsProbeHandler<TRequest>(Counter probe)
        : IRequestHandler<TRequest, Result<object>>
        where TRequest : IRequest<Result<object>>
    {
        public Task<Result<object>> Handle(TRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref probe.Value);
            return Task.FromResult(Result.Failure<object>(new SemanticError("probe.reached")));
        }
    }

    private sealed class ShippingCommitProbeHandler(Counter probe)
        : IRequestHandler<CommitStorefrontShippingCommand, Result<Tooba.Order.Application.Storefront.Models.StorefrontCheckoutPage>>
    {
        public Task<Result<Tooba.Order.Application.Storefront.Models.StorefrontCheckoutPage>> Handle(
            CommitStorefrontShippingCommand request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref probe.Value);
            return Task.FromResult(Result.Failure<Tooba.Order.Application.Storefront.Models.StorefrontCheckoutPage>(
                new SemanticError("probe.reached")));
        }
    }

    private sealed class ShippingProjectProbeHandler(Counter probe)
        : IRequestHandler<ProjectStorefrontShippingQuery, Result<Tooba.Order.Application.Storefront.Models.StorefrontShippingProjection>>
    {
        public Task<Result<Tooba.Order.Application.Storefront.Models.StorefrontShippingProjection>> Handle(
            ProjectStorefrontShippingQuery request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref probe.Value);
            return Task.FromResult(Result.Failure<Tooba.Order.Application.Storefront.Models.StorefrontShippingProjection>(
                new SemanticError("probe.reached")));
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
