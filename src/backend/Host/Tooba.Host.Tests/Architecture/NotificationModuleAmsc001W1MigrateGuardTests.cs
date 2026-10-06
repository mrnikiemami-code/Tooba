using System.Text.RegularExpressions;
using Tooba.Notification.Contracts.Errors;
using Tooba.Notification.Application.Validators;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-NOTIFICATION-AMSC-001-W1 — migrate wave durable guard.
/// Locks the canonical typed-fault seam, the declared-code catalog, the transport-validation codes,
/// the Contracts error-code home, the bilingual resource pair and the zero-foreign-coupling
/// boundary established by the migrate wave.
/// </summary>
public sealed class NotificationModuleAmsc001W1MigrateGuardTests
{
    private static readonly string[] ProductionProjects =
    [
        "Tooba.Notification.Contracts",
        "Tooba.Notification.Domain",
        "Tooba.Notification.Application",
        "Tooba.Notification.Infrastructure",
        "Tooba.Notification.Endpoints",
    ];

    [Fact]
    public void Operation_seam_maps_typed_codes_and_never_parses_message_text()
    {
        var text = Read("src/backend/Modules/Notification/Tooba.Notification.Application/Composition/NotificationOperation.cs");

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("NotificationErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Declared_code_catalog_is_the_single_known_code_source()
    {
        Assert.True(NotificationErrorCodes.IsKnown(NotificationErrorCodes.Missing));
        Assert.True(NotificationErrorCodes.IsKnown(NotificationErrorCodes.TargetRouteUnsafe));
        Assert.False(NotificationErrorCodes.IsKnown("cart.line.currency_missing"));
        Assert.False(NotificationErrorCodes.IsKnown(null));
        Assert.False(NotificationErrorCodes.IsKnown(" "));
    }

    [Fact]
    public void Stable_codes_live_in_the_contracts_errors_home()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Notification/Tooba.Notification.Contracts/Errors/NotificationErrorCodes.cs")));
        Assert.False(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Notification/Tooba.Notification.Application/Errors/NotificationErrorCodes.cs")),
            "Application/Errors duplicate home must not resurrect");
    }

    [Fact]
    public void Contracts_raise_typed_faults_and_never_raw_literals()
    {
        var routes = Read("src/backend/Modules/Notification/Tooba.Notification.Contracts/Routes/NotificationTargetRoutes.cs");
        Assert.Contains("ContractOperationException(NotificationErrorCodes.TargetRouteEmpty)", routes, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException(NotificationErrorCodes.TargetRouteUnsafe)", routes, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException(NotificationErrorCodes.TargetRouteNotAllowed)", routes, StringComparison.Ordinal);
        Assert.DoesNotContain("\"notification.target_route", routes, StringComparison.Ordinal);

        var mapping = Read("src/backend/Modules/Notification/Tooba.Notification.Application/Models/NotificationRecipientKindMapping.cs");
        Assert.Contains("ContractOperationException(NotificationErrorCodes.RecipientKindInvalid)", mapping, StringComparison.Ordinal);
        Assert.DoesNotContain("\"notification.recipient_kind.invalid\"", mapping, StringComparison.Ordinal);
    }

    [Fact]
    public void Bilingual_resource_pair_covers_the_declared_module_keyspace()
    {
        var contractsRoot = Path.Combine(Repo(), "src/backend/Modules/Notification/Tooba.Notification.Contracts");
        Assert.True(File.Exists(Path.Combine(contractsRoot, "Errors", "NotificationErrorResources.cs")));

        var en = File.ReadAllText(Path.Combine(contractsRoot, "Resources", "NotificationErrors.resx"));
        var fa = File.ReadAllText(Path.Combine(contractsRoot, "Resources", "NotificationErrors.fa.resx"));
        foreach (var key in new[]
                 {
                     "notification.missing",
                     "notification.target_route.empty",
                     "notification.target_route.unsafe",
                     "notification.target_route.not_allowed",
                     "notification.recipient_kind.invalid",
                 })
        {
            Assert.Contains($"\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"\"{key}\"", fa, StringComparison.Ordinal);
        }

        Assert.Contains("IErrorResourceSet", Read("src/backend/Modules/Notification/Tooba.Notification.Infrastructure/DependencyInjection/NotificationModule.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void Validators_emit_transport_codes_and_are_exhaustively_present()
    {
        foreach (var file in new[]
                 {
                     "Customer/Commands/MarkCustomerNotificationReadCommandValidator.cs",
                     "Customer/Commands/DismissCustomerNotificationCommandValidator.cs",
                     "Customer/Queries/ListCustomerNotificationsQueryValidator.cs",
                     "Seller/Commands/MarkSellerNotificationReadCommandValidator.cs",
                     "Seller/Commands/DismissSellerNotificationCommandValidator.cs",
                     "Seller/Queries/ListSellerNotificationsQueryValidator.cs",
                 })
        {
            var text = Read($"src/backend/Modules/Notification/Tooba.Notification.Application/{file}");
            Assert.Contains("AbstractValidator<", text, StringComparison.Ordinal);
            Assert.Contains("NotificationValidationCodes", text, StringComparison.Ordinal);
            Assert.DoesNotContain("NotificationErrorCodes", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validation_codes_are_not_registered_as_error_catalog_descriptors()
    {
        var contributor = Read("src/backend/Modules/Notification/Tooba.Notification.Endpoints/Errors/NotificationErrorCatalogContributor.cs");
        Assert.DoesNotContain("NotificationValidationCodes", contributor, StringComparison.Ordinal);

        var codes = Read("src/backend/Modules/Notification/Tooba.Notification.Application/Validators/NotificationValidationCodes.cs");
        Assert.Contains("notification.validation.", codes, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_code_has_zero_raw_notification_error_code_literals()
    {
        var declared = typeof(NotificationErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

        Assert.Equal(5, declared.Length);

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                if (file.EndsWith("NotificationErrorCodes.cs", StringComparison.Ordinal))
                {
                    continue; // the declaration file itself is the single literal home
                }

                var text = File.ReadAllText(file);
                foreach (var code in declared)
                {
                    Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void Notification_error_codes_match_their_published_contract_values()
    {
        Assert.Equal("notification.missing", NotificationErrorCodes.Missing);
        Assert.Equal("notification.target_route.empty", NotificationErrorCodes.TargetRouteEmpty);
        Assert.Equal("notification.target_route.unsafe", NotificationErrorCodes.TargetRouteUnsafe);
        Assert.Equal("notification.target_route.not_allowed", NotificationErrorCodes.TargetRouteNotAllowed);
        Assert.Equal("notification.recipient_kind.invalid", NotificationErrorCodes.RecipientKindInvalid);

        Assert.Equal("notification.validation.notification_id_required", NotificationValidationCodes.NotificationIdRequired);
        Assert.Equal("notification.validation.customer_take_out_of_range", NotificationValidationCodes.CustomerTakeOutOfRange);
        Assert.Equal("notification.validation.customer_skip_negative", NotificationValidationCodes.CustomerSkipNegative);
        Assert.Equal("notification.validation.seller_take_out_of_range", NotificationValidationCodes.SellerTakeOutOfRange);
        Assert.Equal("notification.validation.seller_skip_negative", NotificationValidationCodes.SellerSkipNegative);
    }

    [Fact]
    public void Shared_session_code_stays_foundation_owned()
    {
        // The descriptor is registered exactly once, by the foundation; Notification only consumes.
        var contributor = Read("src/backend/Modules/Notification/Tooba.Notification.Endpoints/Errors/NotificationErrorCatalogContributor.cs");
        Assert.DoesNotContain("Code: \"customer.session.required\"", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("FoundationErrorCodes.CustomerSessionRequired,", contributor, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCatalogContributor", contributor, StringComparison.Ordinal);
        Assert.Equal("customer.session.required", NotificationSharedErrorCodes.CustomerSessionRequired);
    }

    [Fact]
    public void Contracts_boundary_stays_clean_and_routes_are_unchanged()
    {
        foreach (var project in ProductionProjects)
        {
            var refs = ProjectRefs(project);
            Assert.DoesNotContain(refs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
            if (project is not ("Tooba.Notification.Infrastructure" or "Tooba.Notification.Endpoints"))
            {
                Assert.DoesNotContain(refs, r => Regex.IsMatch(r, @"Tooba\.(Order|Payment|Fulfillment|Returns|Wallet|Support)\."));
            }
        }

        var module = Read("src/backend/Modules/Notification/Tooba.Notification.Endpoints/NotificationEndpointModule.cs");
        Assert.Contains("/v1/customer/notifications", module, StringComparison.Ordinal);
        Assert.Contains("/v1/seller/notifications", module, StringComparison.Ordinal);
        Assert.Contains("MapNotificationEndpoints(this IEndpointRouteBuilder", module, StringComparison.Ordinal);
    }

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Repo(), relativePath));

    private static IEnumerable<string> ProductionSources(string project)
    {
        var root = Path.Combine(Repo(), "src", "backend", "Modules", "Notification", project);
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> ProjectRefs(string project)
    {
        var doc = System.Xml.Linq.XDocument.Load(Path.Combine(
            Repo(), "src", "backend", "Modules", "Notification", project, $"{project}.csproj"));
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
