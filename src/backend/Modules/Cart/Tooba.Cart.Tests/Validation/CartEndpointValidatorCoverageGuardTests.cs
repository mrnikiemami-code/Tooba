using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Cart.Application.Commands.AddCartLine;
using Tooba.Cart.Application.Commands.ChangeCartLineQuantity;
using Tooba.Cart.Application.Commands.RemoveCartLine;
using Tooba.Cart.Application.Queries.GetCart;
using Tooba.Cart.Application.Validation;
using Xunit;

namespace Tooba.Cart.Tests.Validation;

/// <summary>TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001 — endpoint-reachable Cart validator coverage.</summary>
public sealed class CartEndpointValidatorCoverageGuardTests
{
    /// <summary>
    /// Explicit classification for every Cart.Endpoints-reachable IRequest.
    /// New input-bearing requests must be added here with a concrete validator.
    /// </summary>
    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        ("CreateGuestCartCommand", "NO_VALIDATOR_REQUIRED"),
        ("GetCurrentAuthenticatedCartQuery", "NO_VALIDATOR_REQUIRED"),
        ("MergeCartAfterLoginCommand", "NO_VALIDATOR_REQUIRED"),
        ("GetCartQuery", "VALIDATOR_REQUIRED"),
        ("AddCartLineCommand", "VALIDATOR_REQUIRED"),
        ("ChangeCartLineQuantityCommand", "VALIDATOR_REQUIRED"),
        ("RemoveCartLineCommand", "VALIDATOR_REQUIRED"),
    ];

    [Fact]
    public void Manifest_covers_every_endpoint_reachable_request_exactly_once()
    {
        var names = Manifest.Select(x => x.RequestTypeName).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());

        var endpointText = string.Join(
            "\n",
            Directory.GetFiles(
                    Path.Combine(RepoRoot(), "src", "backend", "Modules", "Cart", "Tooba.Cart.Endpoints"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));

        foreach (var (name, _) in Manifest)
        {
            Assert.Contains(name, endpointText, StringComparison.Ordinal);
        }

        var constructed = System.Text.RegularExpressions.Regex.Matches(
                endpointText,
                @"\bnew\s+([A-Za-z0-9_]+(?:Command|Query))\b")
            .Select(m => m.Groups[1].Value)
            .Where(n => n is not ("Command" or "Query"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var required = Manifest.Select(x => x.RequestTypeName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(required, constructed);
    }

    [Fact]
    public void Every_VALIDATOR_REQUIRED_request_resolves_concrete_IValidator_via_foundation_DI()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(GetCartQuery).Assembly);
        using var sp = services.BuildServiceProvider();

        var appAsm = typeof(GetCartQuery).Assembly;
        foreach (var (name, classification) in Manifest.Where(x => x.Classification == "VALIDATOR_REQUIRED"))
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            var validatorType = typeof(IValidator<>).MakeGenericType(requestType);
            Assert.True(sp.GetService(validatorType) is not null, $"missing IValidator<{name}>");
        }
    }

    [Fact]
    public void NO_VALIDATOR_REQUIRED_requests_have_no_validator_registered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(GetCartQuery).Assembly);
        using var sp = services.BuildServiceProvider();

        var appAsm = typeof(GetCartQuery).Assembly;
        foreach (var (name, classification) in Manifest.Where(x => x.Classification == "NO_VALIDATOR_REQUIRED"))
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            Assert.True(
                sp.GetService(typeof(IValidator<>).MakeGenericType(requestType)) is null,
                $"{name} is classified NO_VALIDATOR_REQUIRED but a validator is registered");
        }
    }

    [Fact]
    public void Validators_flag_only_primitive_shape()
    {
        var getCart = new GetCartQueryValidator().Validate(new GetCartQuery(Guid.Empty, "guest"));
        Assert.Contains(getCart.Errors, e => e.ErrorCode == CartValidationCodes.CartIdRequired);

        var add = new AddCartLineCommandValidator().Validate(new AddCartLineCommand(Guid.Empty, null, -1, Guid.Empty, 1));
        Assert.Contains(add.Errors, e => e.ErrorCode == CartValidationCodes.CartIdRequired);
        Assert.Contains(add.Errors, e => e.ErrorCode == CartValidationCodes.OfferIdRequired);
        Assert.Contains(add.Errors, e => e.ErrorCode == CartValidationCodes.ExpectedVersionMin);

        var change = new ChangeCartLineQuantityCommandValidator()
            .Validate(new ChangeCartLineQuantityCommand(Guid.Empty, null, -1, Guid.Empty, 0));
        Assert.Contains(change.Errors, e => e.ErrorCode == CartValidationCodes.CartIdRequired);
        Assert.Contains(change.Errors, e => e.ErrorCode == CartValidationCodes.LineIdRequired);
        Assert.Contains(change.Errors, e => e.ErrorCode == CartValidationCodes.ExpectedVersionMin);

        var remove = new RemoveCartLineCommandValidator().Validate(new RemoveCartLineCommand(Guid.Empty, null, -1, Guid.Empty));
        Assert.Contains(remove.Errors, e => e.ErrorCode == CartValidationCodes.CartIdRequired);
        Assert.Contains(remove.Errors, e => e.ErrorCode == CartValidationCodes.LineIdRequired);
        Assert.Contains(remove.Errors, e => e.ErrorCode == CartValidationCodes.ExpectedVersionMin);
    }

    [Fact]
    public void Validators_accept_structurally_valid_transport_input()
    {
        // Only primitive shape is validated; identifier validity/ownership stays business validation.
        Assert.True(new GetCartQueryValidator().Validate(new GetCartQuery(Guid.NewGuid(), null)).IsValid);
        Assert.True(new AddCartLineCommandValidator()
            .Validate(new AddCartLineCommand(Guid.NewGuid(), null, 0, Guid.NewGuid(), 0)).IsValid);
        Assert.True(new RemoveCartLineCommandValidator()
            .Validate(new RemoveCartLineCommand(Guid.NewGuid(), null, 0, Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Zero_quantity_change_is_not_rejected_by_transport_validation()
    {
        var result = new ChangeCartLineQuantityCommandValidator()
            .Validate(new ChangeCartLineQuantityCommand(Guid.NewGuid(), null, 0, Guid.NewGuid(), 0));

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorCode)));
    }

    [Fact]
    public void ValidationException_maps_to_stable_validation_failed_semantics()
    {
        var catalog = new ErrorDefinitionCatalog([new FoundationErrorCatalogContributor()]);
        var mapper = new SafeErrorMapper(catalog);
        var ex = new ValidationException([
            new FluentValidation.Results.ValidationFailure("CartId", "x")
            {
                ErrorCode = CartValidationCodes.CartIdRequired,
            },
        ]);

        var mapped = mapper.Map(ex);
        Assert.Equal("validation.failed", mapped.ErrorCode);
        Assert.Equal(ErrorClassification.Validation, mapped.Classification);
        Assert.Contains(CartValidationCodes.CartIdRequired, mapped.ValidationErrors!["CartId"]);
    }

    [Fact]
    public void Validators_do_not_encode_business_state_decisions()
    {
        var appRoot = Path.Combine(RepoRoot(), "src", "backend", "Modules", "Cart", "Tooba.Cart.Application");
        var validatorFiles = Directory.EnumerateFiles(appRoot, "*Validator.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(4, validatorFiles.Length);

        foreach (var file in validatorFiles)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("ICartDirectory", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IsAuthenticated", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Async(", text, StringComparison.Ordinal);
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
