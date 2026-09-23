using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Completeness.Commands.AddAdminOrderNote;
using Tooba.Order.Application.Admin.Completeness.Models;
using Tooba.Order.Application.Customer.Models;
using Tooba.Order.Application.Customer.Queries.GetCustomerOrderDetail;
using Tooba.Order.Application.Validation;
using Xunit;
using System.Text.RegularExpressions;

namespace Tooba.Order.Tests.Validation;

/// <summary>TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001 — FluentValidation pipeline + Order validators.</summary>
public sealed class OrderValidationPipelineTests
{
    [Fact]
    public void MediatR_package_remains_12_5_0()
    {
        var csproj = Path.Combine(
            RepoRoot(), "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "Tooba.BuildingBlocks.csproj");
        var xml = File.ReadAllText(csproj);
        Assert.Contains("Include=\"MediatR\" Version=\"12.5.0\"", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("Version=\"13.", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void Exactly_one_ValidationBehavior_registration_in_foundation()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "TmarFoundation.cs"));
        Assert.Single(
            Regex.Matches(
                source,
                @"AddTransient\(typeof\(IPipelineBehavior<,>\),\s*typeof\(ValidationBehavior<,>\)\)"));
        Assert.Contains("AddValidatorsFromAssembly(assembly)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_validators_have_no_dbcontext_or_foreign_module_deps()
    {
        var app = Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Application");
        foreach (var file in Directory.GetFiles(app, "*Validator.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Catalog.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Payment.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Fulfillment.Application", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Invalid_primitive_request_is_rejected_before_handler()
    {
        var probe = new HandlerProbe();
        await using var provider = BuildSender(probe);
        var sender = provider.GetRequiredService<ISender>();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new GetCustomerOrderDetailQuery(Guid.Empty, Guid.Empty)));

        Assert.Contains(ex.Errors, e => e.ErrorCode == OrderValidationCodes.ActorUserIdRequired);
        Assert.Contains(ex.Errors, e => e.ErrorCode == OrderValidationCodes.CheckoutIdRequired);
        Assert.Equal(0, probe.Calls);
    }

    [Fact]
    public async Task Valid_request_reaches_handler()
    {
        var probe = new HandlerProbe();
        await using var provider = BuildSender(probe);
        var sender = provider.GetRequiredService<ISender>();

        var actor = Guid.Parse("11111111-1111-7111-8111-111111111111");
        var checkout = Guid.Parse("22222222-2222-7222-8222-222222222222");
        var result = await sender.Send(new GetCustomerOrderDetailQuery(actor, checkout));

        Assert.True(result.IsFailure);
        Assert.Equal("probe.reached", result.FirstError.Code);
        Assert.Equal(1, probe.Calls);
    }

    [Fact]
    public async Task Multiple_validation_errors_are_deterministic()
    {
        var probe = new HandlerProbe();
        await using var provider = BuildSender(probe);
        var sender = provider.GetRequiredService<ISender>();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new AddAdminOrderNoteCommand(Guid.Empty, new AdminOrderActor(Guid.Empty), "")));

        var codes = ex.Errors.Select(e => e.ErrorCode).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Contains(OrderValidationCodes.CheckoutIdRequired, codes);
        Assert.Contains(OrderValidationCodes.ActorUserIdRequired, codes);
        Assert.Contains(OrderValidationCodes.NoteBodyRequired, codes);
        Assert.Equal(codes, codes.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(0, probe.Calls);
    }

    [Fact]
    public void ValidationException_maps_to_stable_validation_failed_semantics()
    {
        var catalog = new ErrorDefinitionCatalog([new FoundationErrorCatalogContributor()]);
        var mapper = new SafeErrorMapper(catalog);
        var ex = new ValidationException([
            new FluentValidation.Results.ValidationFailure("CheckoutId", "x")
            {
                ErrorCode = OrderValidationCodes.CheckoutIdRequired
            },
            new FluentValidation.Results.ValidationFailure("Body", "y")
            {
                ErrorCode = OrderValidationCodes.NoteBodyRequired
            },
        ]);

        var mapped = mapper.Map(ex);
        Assert.Equal("validation.failed", mapped.ErrorCode);
        Assert.Equal(ErrorClassification.Validation, mapped.Classification);
        Assert.Contains(OrderValidationCodes.CheckoutIdRequired, mapped.ValidationErrors!["CheckoutId"]);
        Assert.Contains(OrderValidationCodes.NoteBodyRequired, mapped.ValidationErrors!["Body"]);
    }

    [Fact]
    public void Transport_facing_requests_with_input_have_validators_or_are_documented()
    {
        var app = Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Application");
        var validatorTypes = Directory.GetFiles(app, "*Validator.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        string[] required =
        [
            "AddAdminOrderNoteCommandValidator",
            "GetAdminOrderOperationalHistoryQueryValidator",
            "GetAdminOrderDetailQueryValidator",
            "CancelOrderCommandValidator",
            "QueryAdminOrdersGridQueryValidator",
            "RecoverOrderInventoryReservationCommandValidator",
            "EnsureOrderSupplyCommandValidator",
            "SubmitStorefrontCheckoutCommandValidator",
            "CancelPendingCheckoutCommandValidator",
            "GetCustomerOrderDetailQueryValidator",
            "RetryCustomerUnpaidOrderCommandValidator",
            "GetSellerOrderDetailQueryValidator",
        ];

        foreach (var name in required)
        {
            Assert.Contains(name, validatorTypes);
        }
    }

    private static ServiceProvider BuildSender(HandlerProbe probe)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(probe);
        services.AddValidatorsFromAssembly(typeof(GetCustomerOrderDetailQueryValidator).Assembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(FoundationPingCommand).Assembly);
        });
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient<IRequestHandler<GetCustomerOrderDetailQuery, Result<CustomerOrderDetailPage>>, ProbeCustomerDetailHandler>();
        services.AddTransient<IRequestHandler<AddAdminOrderNoteCommand, Result<AdminOrderNoteView>>, ProbeAddNoteHandler>();
        return services.BuildServiceProvider();
    }

    private sealed class HandlerProbe
    {
        public int Calls;
    }

    private sealed class ProbeCustomerDetailHandler(HandlerProbe probe)
        : IRequestHandler<GetCustomerOrderDetailQuery, Result<CustomerOrderDetailPage>>
    {
        public Task<Result<CustomerOrderDetailPage>> Handle(
            GetCustomerOrderDetailQuery request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref probe.Calls);
            return Task.FromResult(Result.Failure<CustomerOrderDetailPage>(new SemanticError("probe.reached")));
        }
    }

    private sealed class ProbeAddNoteHandler(HandlerProbe probe)
        : IRequestHandler<AddAdminOrderNoteCommand, Result<AdminOrderNoteView>>
    {
        public Task<Result<AdminOrderNoteView>> Handle(
            AddAdminOrderNoteCommand request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref probe.Calls);
            return Task.FromResult(Result.Failure<AdminOrderNoteView>(new SemanticError("probe.reached")));
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
