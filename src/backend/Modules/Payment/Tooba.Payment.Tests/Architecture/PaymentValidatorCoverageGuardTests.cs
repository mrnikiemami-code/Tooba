using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Commands.CompleteSandboxPayment;
using Tooba.Payment.Application.Commands.ConfirmAdminDeposit;
using Tooba.Payment.Application.Commands.InitiateStorefrontPayment;
using Tooba.Payment.Application.Commands.ProcessPaymentWebhook;
using Tooba.Payment.Application.Commands.ReconcileAdminPayment;
using Tooba.Payment.Application.Commands.ReconcileStalePayments;
using Tooba.Payment.Application.Commands.RejectAdminDeposit;
using Tooba.Payment.Application.Commands.RetryManualPayment;
using Tooba.Payment.Application.Commands.RetryUnpaidPayment;
using Tooba.Payment.Application.Commands.SubmitManualPaymentEvidence;
using Tooba.Payment.Application.Commands.UploadManualPaymentProof;
using Tooba.Payment.Application.Queries.GetAdminPayment;
using Tooba.Payment.Application.Queries.GetStorefrontPayment;
using Tooba.Payment.Application.Queries.GetStorefrontPaymentSandboxContext;
using Tooba.Payment.Application.Queries.GetStorefrontWalletQuote;
using Tooba.Payment.Application.Queries.ListStorefrontPaymentMethods;
using Tooba.Payment.Application.Queries.QueryAdminPaymentsGrid;
using Tooba.Payment.Application.Validators.Admin;
using Tooba.Payment.Application.Validators.Storefront;
using Tooba.Payment.Application.Validators.Webhooks;
using Xunit;

namespace Tooba.Payment.Tests.Architecture;

/// <summary>
/// TB-TMAR-PAYMENT-PRECERT-VALIDATION-002 — complete Payment endpoint-reachable validator coverage.
/// Exactly 16 endpoint-reachable MediatR requests: 15 carry a concrete transport validator and 1
/// carries no input. The worker-only ReconcileStalePaymentsCommand is explicitly excluded.
/// Payment structure certification remains PENDING.
/// </summary>
public sealed class PaymentValidatorCoverageGuardTests
{
    private const string ValidatorRequired = "VALIDATOR_REQUIRED_PRESENT";

    private const string NoValidatorRequired = "NO_VALIDATOR_REQUIRED_NO_INPUT";

    private static readonly (string RequestTypeName, string Surface, string Classification)[] Manifest =
    [
        // Storefront (10): 9 required + 1 no-input
        ("InitiateStorefrontPaymentCommand", "Storefront", ValidatorRequired),
        ("GetStorefrontWalletQuoteQuery", "Storefront", ValidatorRequired),
        ("GetStorefrontPaymentQuery", "Storefront", ValidatorRequired),
        ("GetStorefrontPaymentSandboxContextQuery", "Storefront", ValidatorRequired),
        ("CompleteSandboxPaymentCommand", "Storefront", ValidatorRequired),
        ("SubmitManualPaymentEvidenceCommand", "Storefront", ValidatorRequired),
        ("RetryManualPaymentCommand", "Storefront", ValidatorRequired),
        ("RetryUnpaidPaymentCommand", "Storefront", ValidatorRequired),
        ("UploadManualPaymentProofCommand", "Storefront", ValidatorRequired),
        ("ListStorefrontPaymentMethodsQuery", "Storefront", NoValidatorRequired),
        // Admin (5): all required
        ("GetAdminPaymentQuery", "Admin", ValidatorRequired),
        ("ReconcileAdminPaymentCommand", "Admin", ValidatorRequired),
        ("ConfirmAdminDepositCommand", "Admin", ValidatorRequired),
        ("RejectAdminDepositCommand", "Admin", ValidatorRequired),
        ("QueryAdminPaymentsGridQuery", "Admin", ValidatorRequired),
        // Webhook (1): required
        ("ProcessPaymentWebhookCommand", "Webhook", ValidatorRequired),
    ];

    private static readonly (string Request, Type Validator, string Surface)[] RequiredValidators =
    [
        ("InitiateStorefrontPaymentCommand", typeof(InitiateStorefrontPaymentCommandValidator), "Storefront"),
        ("GetStorefrontWalletQuoteQuery", typeof(GetStorefrontWalletQuoteQueryValidator), "Storefront"),
        ("GetStorefrontPaymentQuery", typeof(GetStorefrontPaymentQueryValidator), "Storefront"),
        ("GetStorefrontPaymentSandboxContextQuery", typeof(GetStorefrontPaymentSandboxContextQueryValidator), "Storefront"),
        ("CompleteSandboxPaymentCommand", typeof(CompleteSandboxPaymentCommandValidator), "Storefront"),
        ("SubmitManualPaymentEvidenceCommand", typeof(SubmitManualPaymentEvidenceCommandValidator), "Storefront"),
        ("RetryManualPaymentCommand", typeof(RetryManualPaymentCommandValidator), "Storefront"),
        ("RetryUnpaidPaymentCommand", typeof(RetryUnpaidPaymentCommandValidator), "Storefront"),
        ("UploadManualPaymentProofCommand", typeof(UploadManualPaymentProofCommandValidator), "Storefront"),
        ("GetAdminPaymentQuery", typeof(GetAdminPaymentQueryValidator), "Admin"),
        ("ReconcileAdminPaymentCommand", typeof(ReconcileAdminPaymentCommandValidator), "Admin"),
        ("ConfirmAdminDepositCommand", typeof(ConfirmAdminDepositCommandValidator), "Admin"),
        ("RejectAdminDepositCommand", typeof(RejectAdminDepositCommandValidator), "Admin"),
        ("QueryAdminPaymentsGridQuery", typeof(QueryAdminPaymentsGridQueryValidator), "Admin"),
        ("ProcessPaymentWebhookCommand", typeof(ProcessPaymentWebhookCommandValidator), "Webhook"),
    ];

    [Fact]
    public void Complete_endpoint_inventory_is_exactly_16_with_15_required_and_1_no_input()
    {
        Assert.Equal(16, Manifest.Length);
        Assert.Equal(15, Manifest.Count(x => x.Classification == ValidatorRequired));
        Assert.Equal(1, Manifest.Count(x => x.Classification == NoValidatorRequired));
        Assert.Equal(10, Manifest.Count(x => x.Surface == "Storefront"));
        Assert.Equal(5, Manifest.Count(x => x.Surface == "Admin"));
        Assert.Equal(1, Manifest.Count(x => x.Surface == "Webhook"));

        var names = Manifest.Select(x => x.RequestTypeName).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Storefront_inventory_matches_endpoint_constructions_exactly()
    {
        var constructed = ConstructedRequests(Path.Combine(
            PaymentRoot(), "Tooba.Payment.Endpoints", "Storefront", "PaymentStorefrontEndpoints.cs"));
        var manifested = Manifest
            .Where(x => x.Surface == "Storefront")
            .Select(x => x.RequestTypeName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(manifested, constructed);
    }

    [Fact]
    public void Admin_inventory_matches_endpoint_constructions_exactly()
    {
        var constructed = ConstructedRequests(Path.Combine(
            PaymentRoot(), "Tooba.Payment.Endpoints", "Admin", "PaymentAdminEndpoints.cs"));
        var manifested = Manifest
            .Where(x => x.Surface == "Admin")
            .Select(x => x.RequestTypeName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(manifested, constructed);
    }

    [Fact]
    public void Webhook_inventory_matches_endpoint_constructions_exactly()
    {
        var constructed = ConstructedRequests(Path.Combine(
            PaymentRoot(), "Tooba.Payment.Endpoints", "Webhooks", "PaymentWebhookEndpoints.cs"));
        Assert.Equal(new[] { "ProcessPaymentWebhookCommand" }, constructed);
    }

    [Fact]
    public void Fifteen_required_validators_resolve_via_foundation_DI_and_no_input_query_has_none()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(InitiateStorefrontPaymentCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        var appAsm = typeof(InitiateStorefrontPaymentCommand).Assembly;

        foreach (var (request, validatorType, _) in RequiredValidators)
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == request);
            var registered = sp.GetService(typeof(IValidator<>).MakeGenericType(requestType));
            Assert.NotNull(registered);
            Assert.Equal(validatorType, registered!.GetType());

            Assert.Contains(
                validatorType.GetInterfaces(),
                i => i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(IValidator<>)
                    && i.GetGenericArguments()[0] == requestType);
        }

        var listType = appAsm.GetTypes().Single(t => t.Name == nameof(ListStorefrontPaymentMethodsQuery));
        Assert.True(
            sp.GetService(typeof(IValidator<>).MakeGenericType(listType)) is null,
            "ListStorefrontPaymentMethodsQuery is NO_VALIDATOR_REQUIRED_NO_INPUT but a validator is registered");
    }

    [Fact]
    public void Worker_only_reconcile_command_is_excluded_and_has_no_validator()
    {
        var appAsm = typeof(InitiateStorefrontPaymentCommand).Assembly;
        var workerRequest = appAsm.GetTypes().Single(t => t.Name == nameof(ReconcileStalePaymentsCommand));
        Assert.DoesNotContain(workerRequest.Name, Manifest.Select(x => x.RequestTypeName));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(appAsm);
        using var sp = services.BuildServiceProvider();
        Assert.True(
            sp.GetService(typeof(IValidator<>).MakeGenericType(workerRequest)) is null,
            "ReconcileStalePaymentsCommand must remain NO_VALIDATOR_REQUIRED_INTERNAL_WORKER");

        var worker = File.ReadAllText(Path.Combine(
            PaymentRoot(), "Tooba.Payment.Infrastructure", "Workers", "PaymentReconciliationWorker.cs"));
        Assert.Contains("ISender", worker, StringComparison.Ordinal);
    }

    [Fact]
    public void All_sixteen_requests_are_real_mediatr_requests_and_endpoints_use_ISender()
    {
        var appAsm = typeof(InitiateStorefrontPaymentCommand).Assembly;
        foreach (var (name, _, _) in Manifest)
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            Assert.True(requestType.IsAssignableTo(typeof(MediatR.IBaseRequest)));
        }

        foreach (var file in EndpointFiles())
        {
            var text = File.ReadAllText(file);
            Assert.Contains("ISender sender", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void No_endpoint_or_relevant_handler_directly_invokes_validators()
    {
        foreach (var file in EndpointFiles())
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("IValidator", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ValidateAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Validator", text, StringComparison.Ordinal);
        }

        foreach (var (request, _, _) in RequiredValidators)
        {
            var handlerText = HandlerSourceFor(request);
            Assert.DoesNotContain("IValidator", handlerText, StringComparison.Ordinal);
            Assert.DoesNotContain("ValidateAsync", handlerText, StringComparison.Ordinal);
            Assert.DoesNotContain("Validator", handlerText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validator_folder_layout_is_exactly_storefront_admin_webhooks()
    {
        var validatorsRoot = Path.Combine(PaymentRoot(), "Tooba.Payment.Application", "Validators");
        var folders = Directory.GetDirectories(validatorsRoot)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "Admin", "Storefront", "Webhooks" }, folders);

        Assert.Equal(5, Directory.GetFiles(Path.Combine(validatorsRoot, "Admin"), "*Validator.cs").Length);
        Assert.Equal(9, Directory.GetFiles(Path.Combine(validatorsRoot, "Storefront"), "*Validator.cs").Length);
        Assert.Equal(1, Directory.GetFiles(Path.Combine(validatorsRoot, "Webhooks"), "*Validator.cs").Length);
    }

    [Fact]
    public void MediatR_package_version_remains_12_5_0()
    {
        var csproj = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "Tooba.BuildingBlocks.csproj"));
        Assert.Contains("Include=\"MediatR\" Version=\"12.5.0\"", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Payment_remains_not_structure_certified()
    {
        var state = File.ReadAllText(Path.Combine(
            RepoRoot(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001", state, StringComparison.Ordinal);

        var manifests = File.ReadAllText(Path.Combine(
            RepoRoot(), "docs", "architecture", "tmar-module-structure-manifests.json"));
        var at = manifests.IndexOf("\"uncertifiedHttpOwningModules\"", StringComparison.Ordinal);
        Assert.True(at >= 0, "uncertifiedHttpOwningModules missing");
        var window = manifests[at..Math.Min(manifests.Length, at + 2000)];
        Assert.Contains("Payment", window, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> EndpointFiles() =>
    [
        Path.Combine(PaymentRoot(), "Tooba.Payment.Endpoints", "Storefront", "PaymentStorefrontEndpoints.cs"),
        Path.Combine(PaymentRoot(), "Tooba.Payment.Endpoints", "Admin", "PaymentAdminEndpoints.cs"),
        Path.Combine(PaymentRoot(), "Tooba.Payment.Endpoints", "Webhooks", "PaymentWebhookEndpoints.cs"),
    ];

    private static string[] ConstructedRequests(string endpointFile) =>
        Regex.Matches(File.ReadAllText(endpointFile), @"\bnew\s+([A-Za-z0-9_]+(?:Command|Query))\b")
            .Select(m => m.Groups[1].Value)
            .Where(n => n is not ("Command" or "Query"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    private static string HandlerSourceFor(string requestTypeName)
    {
        var applicationRoot = Path.Combine(PaymentRoot(), "Tooba.Payment.Application");
        var file = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.Ordinal)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == requestTypeName);
        Assert.True(file is not null, $"no handler file found for {requestTypeName}");
        return File.ReadAllText(file!);
    }

    private static string PaymentRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Payment");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
