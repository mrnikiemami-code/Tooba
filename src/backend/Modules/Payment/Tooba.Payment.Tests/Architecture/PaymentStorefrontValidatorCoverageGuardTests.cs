using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Commands.CompleteSandboxPayment;
using Tooba.Payment.Application.Commands.InitiateStorefrontPayment;
using Tooba.Payment.Application.Commands.RetryManualPayment;
using Tooba.Payment.Application.Commands.RetryUnpaidPayment;
using Tooba.Payment.Application.Commands.SubmitManualPaymentEvidence;
using Tooba.Payment.Application.Commands.UploadManualPaymentProof;
using Tooba.Payment.Application.Queries.GetStorefrontPayment;
using Tooba.Payment.Application.Queries.GetStorefrontPaymentSandboxContext;
using Tooba.Payment.Application.Queries.GetStorefrontWalletQuote;
using Tooba.Payment.Application.Queries.ListStorefrontPaymentMethods;
using Tooba.Payment.Application.Validators.Storefront;
using Xunit;

namespace Tooba.Payment.Tests.Architecture;

/// <summary>
/// TB-TMAR-PAYMENT-PRECERT-VALIDATION-001 — storefront-only Payment validator coverage.
/// Exactly ten Payment.Endpoints storefront-reachable MediatR requests: nine carry a concrete
/// transport validator and one carries no input and is explicitly NO_VALIDATOR_REQUIRED_NO_INPUT.
/// Admin and Webhook validator coverage remains PENDING for TB-TMAR-PAYMENT-PRECERT-VALIDATION-002.
/// </summary>
public sealed class PaymentStorefrontValidatorCoverageGuardTests
{
    private const string ValidatorRequired = "VALIDATOR_REQUIRED_PRESENT";

    private const string NoValidatorRequired = "NO_VALIDATOR_REQUIRED_NO_INPUT";

    /// <summary>Explicit classification of every Payment.Endpoints storefront-reachable IRequest.</summary>
    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        ("InitiateStorefrontPaymentCommand", ValidatorRequired),
        ("GetStorefrontWalletQuoteQuery", ValidatorRequired),
        ("GetStorefrontPaymentQuery", ValidatorRequired),
        ("GetStorefrontPaymentSandboxContextQuery", ValidatorRequired),
        ("CompleteSandboxPaymentCommand", ValidatorRequired),
        ("SubmitManualPaymentEvidenceCommand", ValidatorRequired),
        ("RetryManualPaymentCommand", ValidatorRequired),
        ("RetryUnpaidPaymentCommand", ValidatorRequired),
        ("UploadManualPaymentProofCommand", ValidatorRequired),
        ("ListStorefrontPaymentMethodsQuery", NoValidatorRequired),
    ];

    private static readonly (string Request, Type Validator)[] RequiredValidators =
    [
        ("InitiateStorefrontPaymentCommand", typeof(InitiateStorefrontPaymentCommandValidator)),
        ("GetStorefrontWalletQuoteQuery", typeof(GetStorefrontWalletQuoteQueryValidator)),
        ("GetStorefrontPaymentQuery", typeof(GetStorefrontPaymentQueryValidator)),
        ("GetStorefrontPaymentSandboxContextQuery", typeof(GetStorefrontPaymentSandboxContextQueryValidator)),
        ("CompleteSandboxPaymentCommand", typeof(CompleteSandboxPaymentCommandValidator)),
        ("SubmitManualPaymentEvidenceCommand", typeof(SubmitManualPaymentEvidenceCommandValidator)),
        ("RetryManualPaymentCommand", typeof(RetryManualPaymentCommandValidator)),
        ("RetryUnpaidPaymentCommand", typeof(RetryUnpaidPaymentCommandValidator)),
        ("UploadManualPaymentProofCommand", typeof(UploadManualPaymentProofCommandValidator)),
    ];

    [Fact]
    public void Storefront_inventory_is_exhaustive_at_ten_with_nine_required_and_one_no_input()
    {
        Assert.Equal(10, Manifest.Length);
        Assert.Equal(9, Manifest.Count(x => x.Classification == ValidatorRequired));
        Assert.Equal(1, Manifest.Count(x => x.Classification == NoValidatorRequired));

        var names = Manifest.Select(x => x.RequestTypeName).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());

        var endpointText = StorefrontEndpointSource();
        foreach (var (name, _) in Manifest)
        {
            Assert.Contains(name, endpointText, StringComparison.Ordinal);
        }

        var constructed = Regex.Matches(endpointText, @"\bnew\s+([A-Za-z0-9_]+(?:Command|Query))\b")
            .Select(m => m.Groups[1].Value)
            .Where(n => n is not ("Command" or "Query"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var manifested = Manifest.Select(x => x.RequestTypeName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(manifested, constructed);
    }

    [Fact]
    public void Nine_required_validators_resolve_via_foundation_DI_and_list_query_has_none()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(InitiateStorefrontPaymentCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        var appAsm = typeof(InitiateStorefrontPaymentCommand).Assembly;

        foreach (var (request, validatorType) in RequiredValidators)
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
    public void All_ten_requests_are_real_mediatr_requests_and_storefront_endpoints_use_ISender()
    {
        var appAsm = typeof(InitiateStorefrontPaymentCommand).Assembly;
        foreach (var (name, _) in Manifest)
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            Assert.True(requestType.IsAssignableTo(typeof(MediatR.IBaseRequest)));
        }

        var endpointText = StorefrontEndpointSource();
        Assert.Contains("ISender sender", endpointText, StringComparison.Ordinal);
        Assert.Contains("api.From(", endpointText, StringComparison.Ordinal);
    }

    [Fact]
    public void Storefront_endpoints_and_handlers_never_invoke_validators_directly()
    {
        // No direct validator usage anywhere in the storefront endpoints.
        var endpointText = StorefrontEndpointSource();
        Assert.DoesNotContain("IValidator", endpointText, StringComparison.Ordinal);
        Assert.DoesNotContain("ValidateAsync", endpointText, StringComparison.Ordinal);
        Assert.DoesNotContain("Validator", endpointText, StringComparison.Ordinal);

        // No direct validator usage in the nine handler files either.
        foreach (var (request, _) in RequiredValidators)
        {
            var handlerText = HandlerSourceFor(request);
            Assert.DoesNotContain("IValidator", handlerText, StringComparison.Ordinal);
            Assert.DoesNotContain("ValidateAsync", handlerText, StringComparison.Ordinal);
            Assert.DoesNotContain("Validator", handlerText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MediatR_package_version_remains_12_5_0()
    {
        var csproj = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "Tooba.BuildingBlocks.csproj"));
        Assert.Contains("Include=\"MediatR\" Version=\"12.5.0\"", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_and_webhook_validation_remain_explicitly_pending_for_002()
    {
        // This slice closes storefront only; Admin + Webhook stay for VALIDATION-002.
        var adminValidators = Directory.Exists(Path.Combine(
            PaymentRoot(), "Tooba.Payment.Application", "Validators", "Admin"));
        var webhookValidators = Directory.Exists(Path.Combine(
            PaymentRoot(), "Tooba.Payment.Application", "Validators", "Webhooks"));
        Assert.False(adminValidators, "Admin validators belong to TB-TMAR-PAYMENT-PRECERT-VALIDATION-002");
        Assert.False(webhookValidators, "Webhook validators belong to TB-TMAR-PAYMENT-PRECERT-VALIDATION-002");

        var storefrontFolder = Path.Combine(PaymentRoot(), "Tooba.Payment.Application", "Validators", "Storefront");
        Assert.True(Directory.Exists(storefrontFolder));
        Assert.Equal(9, Directory.GetFiles(storefrontFolder, "*Validator.cs").Length);
    }

    private static string StorefrontEndpointSource() =>
        File.ReadAllText(Path.Combine(
            PaymentRoot(), "Tooba.Payment.Endpoints", "Storefront", "PaymentStorefrontEndpoints.cs"));

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
