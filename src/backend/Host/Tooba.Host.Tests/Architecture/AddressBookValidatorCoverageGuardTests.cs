using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.AddressBook.Application.Customer.Create;
using Tooba.AddressBook.Application.Customer.Delete;
using Tooba.AddressBook.Application.Customer.Get;
using Tooba.AddressBook.Application.Customer.List;
using Tooba.AddressBook.Application.Customer.Ports;
using Tooba.AddressBook.Application.Customer.SetDefault;
using Tooba.AddressBook.Application.Customer.Update;
using Tooba.AddressBook.Application.Validators.Customer.Create;
using Tooba.AddressBook.Application.Validators.Customer.Delete;
using Tooba.AddressBook.Application.Validators.Customer.Get;
using Tooba.AddressBook.Application.Validators.Customer.SetDefault;
using Tooba.AddressBook.Application.Validators.Customer.Update;
using Tooba.BuildingBlocks;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001 — durable AddressBook endpoint-reachable
/// request/validator coverage guard. Exactly 6 endpoint-reachable MediatR requests exist:
/// 5 carry a concrete transport validator and 1 is explicitly NO_VALIDATOR_REQUIRED
/// (no transport input; actor authority comes from the trusted server-side seam).
/// AddressBook structure certification remains PENDING until the certification task.
/// </summary>
public sealed class AddressBookValidatorCoverageGuardTests
{
    private const string ValidatorRequired = "VALIDATOR_REQUIRED_PRESENT";
    private const string NoValidatorRequired = "NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT";

    private static readonly (string RequestTypeName, string Route, string Classification)[] Manifest =
    [
        ("ListCustomerAddressesQuery", "GET /v1/customer/addresses", NoValidatorRequired),
        ("GetCustomerAddressQuery", "GET /v1/customer/addresses/{addressId:guid}", ValidatorRequired),
        ("CreateCustomerAddressCommand", "POST /v1/customer/addresses", ValidatorRequired),
        ("UpdateCustomerAddressCommand", "PUT /v1/customer/addresses/{addressId:guid}", ValidatorRequired),
        ("DeleteCustomerAddressCommand", "DELETE /v1/customer/addresses/{addressId:guid}", ValidatorRequired),
        ("SetDefaultCustomerAddressCommand", "POST /v1/customer/addresses/{addressId:guid}/default", ValidatorRequired),
    ];

    private static readonly (string Request, Type Validator)[] RequiredValidators =
    [
        ("GetCustomerAddressQuery", typeof(GetCustomerAddressQueryValidator)),
        ("CreateCustomerAddressCommand", typeof(CreateCustomerAddressCommandValidator)),
        ("UpdateCustomerAddressCommand", typeof(UpdateCustomerAddressCommandValidator)),
        ("DeleteCustomerAddressCommand", typeof(DeleteCustomerAddressCommandValidator)),
        ("SetDefaultCustomerAddressCommand", typeof(SetDefaultCustomerAddressCommandValidator)),
    ];

    [Fact]
    public void Complete_endpoint_inventory_is_exactly_6_with_5_required_and_1_no_validator()
    {
        Assert.Equal(6, Manifest.Length);
        Assert.Equal(5, Manifest.Count(x => x.Classification == ValidatorRequired));
        Assert.Equal(1, Manifest.Count(x => x.Classification == NoValidatorRequired));

        var names = Manifest.Select(x => x.RequestTypeName).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            "ListCustomerAddressesQuery",
            Manifest.Single(x => x.Classification == NoValidatorRequired).RequestTypeName);
    }

    [Fact]
    public void Endpoint_constructions_match_the_manifests_exactly()
    {
        var constructed = new List<string>();
        foreach (var file in EndpointFiles())
        {
            constructed.AddRange(ConstructedRequests(file));
        }

        var manifested = Manifest
            .Select(x => x.RequestTypeName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(manifested, constructed.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Five_required_validators_resolve_via_foundation_DI_and_one_has_none()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(IAddressBookDirectory).Assembly);
        using var sp = services.BuildServiceProvider();

        var appAsm = typeof(IAddressBookDirectory).Assembly;

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

        var noValidatorRequest = appAsm.GetTypes().Single(t => t.Name == "ListCustomerAddressesQuery");
        Assert.True(
            sp.GetService(typeof(IValidator<>).MakeGenericType(noValidatorRequest)) is null,
            "ListCustomerAddressesQuery is NO_VALIDATOR_REQUIRED but a validator is registered");
    }

    [Fact]
    public void All_six_requests_are_real_mediatr_requests_and_endpoints_use_ISender()
    {
        var appAsm = typeof(IAddressBookDirectory).Assembly;
        foreach (var (name, _, _) in Manifest)
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            Assert.True(requestType.IsAssignableTo(typeof(MediatR.IBaseRequest)));
        }

        foreach (var file in EndpointFiles())
        {
            var text = StripDocComments(File.ReadAllText(file));
            Assert.Contains("ISender sender", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IAddressBookDirectory", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IValidator", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ValidateAsync", text, StringComparison.Ordinal);
        }
    }

    private static string StripDocComments(string text) =>
        Regex.Replace(text, @"///[^\r\n]*", string.Empty);

    [Fact]
    public void AddressBook_remains_uncertified_and_manifest_is_a_precert_entry()
    {
        var manifests = File.ReadAllText(Path.Combine(
            RepoRoot(), "docs", "architecture", "tmar-module-structure-manifests.json"));
        var at = manifests.IndexOf("\"module\": \"AddressBook\"", StringComparison.Ordinal);
        Assert.True(at >= 0, "AddressBook pre-cert manifest entry missing");

        var window = manifests[at..Math.Min(manifests.Length, at + 900)];
        Assert.Contains("\"structureCertified\": false", window, StringComparison.Ordinal);
        Assert.Contains("\"lockVersion\": \"ARCH-COMPLETE-002\"", window, StringComparison.Ordinal);

        using var doc = System.Text.Json.JsonDocument.Parse(manifests);
        var certified = new List<string>();
        foreach (var module in doc.RootElement.GetProperty("modules").EnumerateArray())
        {
            if (module.GetProperty("structureCertified").GetBoolean())
            {
                certified.Add(module.GetProperty("module").GetString()!);
            }
        }

        Assert.DoesNotContain("AddressBook", certified, StringComparer.Ordinal);
    }

    [Fact]
    public void AddressBook_root_capability_files_were_evacuated()
    {
        var moduleRoot = Path.Combine(RepoRoot(), "src", "backend", "Modules", "AddressBook");
        Assert.False(File.Exists(Path.Combine(moduleRoot, "Tooba.AddressBook.Application", "AddressBookContracts.cs")));
        Assert.False(File.Exists(Path.Combine(moduleRoot, "Tooba.AddressBook.Infrastructure", "AddressBookDirectory.cs")));
        Assert.False(File.Exists(Path.Combine(moduleRoot, "Tooba.AddressBook.Contracts", "CustomerAddressContracts.cs")));
        Assert.False(Directory.Exists(Path.Combine(moduleRoot, "Tooba.AddressBook.Infrastructure", "Migrations")));

        Assert.True(File.Exists(Path.Combine(moduleRoot, "Tooba.AddressBook.Application", "Customer", "Models", "CustomerAddressWrite.cs")));
        Assert.True(File.Exists(Path.Combine(moduleRoot, "Tooba.AddressBook.Application", "Customer", "Ports", "IAddressBookDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(moduleRoot, "Tooba.AddressBook.Infrastructure", "Directories", "AddressBookDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(moduleRoot, "Tooba.AddressBook.Contracts", "Customer", "CustomerAddressContracts.cs")));
        Assert.True(Directory.Exists(Path.Combine(moduleRoot, "Tooba.AddressBook.Infrastructure", "Persistence", "Migrations")));
    }

    private static IReadOnlyList<string> EndpointFiles() =>
    [
        Path.Combine(AddressBookRoot(), "Tooba.AddressBook.Endpoints", "Customer", "AddressBookCustomerReadEndpoints.cs"),
        Path.Combine(AddressBookRoot(), "Tooba.AddressBook.Endpoints", "Customer", "AddressBookCustomerWriteEndpoints.cs"),
    ];

    private static string[] ConstructedRequests(string endpointFile) =>
        Regex.Matches(File.ReadAllText(endpointFile), @"\bnew\s+([A-Za-z0-9_]+(?:Command|Query))\b")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static string AddressBookRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "AddressBook");

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
