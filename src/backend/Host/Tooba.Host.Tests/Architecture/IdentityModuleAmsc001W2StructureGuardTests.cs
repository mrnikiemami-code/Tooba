using System.Xml.Linq;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Application.Auth.Validators;
using Tooba.Identity.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-IDENTITY-AMSC-001-W2 — durable structure gate for the Identity module:
/// canonical <c>Contracts/Errors</c> capability folder, capability-first Application validators,
/// path↔namespace exactness, no stale transport residue, single typed-fault classification seam,
/// and the canonical <c>/Modules/Identity/</c> Solution Explorer grouping.
/// </summary>
public sealed class IdentityModuleAmsc001W2StructureGuardTests
{
    private static string IdentityRoot(string root) =>
        Path.Combine(root, "src", "backend", "Modules", "Identity");

    [Fact]
    public void Contracts_uses_canonical_errors_folder_and_problems_folder_is_gone()
    {
        var contracts = Path.Combine(IdentityRoot(Repo()), "Tooba.Identity.Contracts");

        Assert.False(Directory.Exists(Path.Combine(contracts, "Problems")));
        Assert.True(Directory.Exists(Path.Combine(contracts, "Errors")));

        foreach (var file in new[]
                 {
                     "IdentityErrorCodes.cs",
                     "IdentityErrorCatalogContributor.cs",
                     "IdentityErrorResourceSet.cs",
                     "IdentityDuplicateIdentifierFault.cs",
                 })
        {
            var path = Path.Combine(contracts, "Errors", file);
            Assert.True(File.Exists(path), $"missing {file}");
            Assert.Contains("namespace Tooba.Identity.Contracts.Errors", File.ReadAllText(path), StringComparison.Ordinal);
        }

        Assert.True(File.Exists(Path.Combine(contracts, "Resources", "IdentityErrors.resx")));
        Assert.True(File.Exists(Path.Combine(contracts, "Resources", "IdentityErrors.fa.resx")));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void Application_is_capability_first_with_no_root_validators_folder()
    {
        var app = Path.Combine(IdentityRoot(Repo()), "Tooba.Identity.Application");

        Assert.False(Directory.Exists(Path.Combine(app, "Validators")));
        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));

        var codes = Path.Combine(app, "Auth", "Validators", "IdentityValidationCodes.cs");
        Assert.True(File.Exists(codes));
        Assert.Contains(
            "namespace Tooba.Identity.Application.Auth.Validators",
            File.ReadAllText(codes),
            StringComparison.Ordinal);

        Assert.True(Directory.Exists(Path.Combine(app, "Auth", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "Auth", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Auth", "Validators")));
        Assert.True(Directory.Exists(Path.Combine(app, "Composition")));
    }

    [Fact]
    public void No_identity_source_references_the_retired_namespaces()
    {
        var identity = IdentityRoot(Repo());
        foreach (var file in Directory.GetFiles(identity, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Identity.Contracts.Problems", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Identity.Application.Validators;", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Transport_models_keep_only_request_shapes()
    {
        var models = File.ReadAllText(Path.Combine(
            IdentityRoot(Repo()),
            "Tooba.Identity.Endpoints",
            "Auth",
            "IdentityAuthHttpModels.cs"));

        Assert.Contains("RegisterRequest", models, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterResponse", models, StringComparison.Ordinal);
        Assert.DoesNotContain("SessionResponse", models, StringComparison.Ordinal);
        Assert.DoesNotContain("AcceptedResponse", models, StringComparison.Ordinal);
        Assert.DoesNotContain("MeResponse", models, StringComparison.Ordinal);
    }

    [Fact]
    public void Shape_only_validators_are_registered_and_do_not_validate_identifier_kind_on_login()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(RegisterAuthUserCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<IValidator<LoginWithPasswordCommand>>());
        Assert.IsType<LoginWithPasswordCommandValidator>(sp.GetService<IValidator<LoginWithPasswordCommand>>());
        Assert.IsType<RefreshAuthSessionCommandValidator>(sp.GetService<IValidator<RefreshAuthSessionCommand>>());
        Assert.IsType<CompleteOtpLoginCommandValidator>(sp.GetService<IValidator<CompleteOtpLoginCommand>>());

        var source = File.ReadAllText(Path.Combine(
            IdentityRoot(Repo()),
            "Tooba.Identity.Application",
            "Auth",
            "Validators",
            "LoginWithPasswordCommandValidator.cs"));
        Assert.DoesNotContain("x.IdentifierKind", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Classification_is_single_typed_fault_seam_and_otp_codes_are_constants()
    {
        var identity = IdentityRoot(Repo());
        var seam = File.ReadAllText(Path.Combine(identity, "Tooba.Identity.Application", "Composition", "IdentityOperation.cs"));
        Assert.Contains("catch (ContractOperationException ex)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (ArgumentException)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (InvalidOperationException)", seam, StringComparison.Ordinal);

        var sender = File.ReadAllText(Path.Combine(identity, "Tooba.Identity.Infrastructure", "Otp", "OtpDeliveryProviderSender.cs"));
        Assert.Contains("throw new ContractOperationException(", sender, StringComparison.Ordinal);
        Assert.Contains("IdentityErrorCodes.OtpDeliveryRateLimited", sender, StringComparison.Ordinal);
        Assert.Contains("IdentityErrorCodes.OtpDeliveryInvalidDestination", sender, StringComparison.Ordinal);
        Assert.Contains("IdentityErrorCodes.OtpDeliveryUnconfigured", sender, StringComparison.Ordinal);
        Assert.DoesNotContain("\"identity.otp.delivery.", sender, StringComparison.Ordinal);

        var codes = File.ReadAllText(Path.Combine(identity, "Tooba.Identity.Contracts", "Errors", "IdentityErrorCodes.cs"));
        Assert.Contains("\"identity.otp.delivery.rate_limited\"", codes, StringComparison.Ordinal);
        Assert.Contains("\"identity.otp.delivery.invalid_destination\"", codes, StringComparison.Ordinal);
        Assert.Contains("\"identity.otp.delivery.unconfigured\"", codes, StringComparison.Ordinal);
    }

    [Fact]
    public void No_hardcoded_persian_fault_prose_in_domain_rules()
    {
        var normalizer = File.ReadAllText(Path.Combine(
            IdentityRoot(Repo()),
            "Tooba.Identity.Domain",
            "Rules",
            "LoginIdentifierNormalizer.cs"));

        Assert.Contains("ContractOperationException", normalizer, StringComparison.Ordinal);
        Assert.Contains("IdentityErrorCodes.ValidationFailed", normalizer, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new ArgumentException(\"", normalizer, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new ArgumentOutOfRangeException(", normalizer, StringComparison.Ordinal);
    }

    [Fact]
    public void Identity_projects_stay_grouped_under_modules_identity_in_visual_studio()
    {
        var doc = XDocument.Load(Path.Combine(Repo(), "src", "backend", "Tooba.slnx"));
        var identityFolder = doc.Root!.Elements("Folder").Single(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/Identity/", StringComparison.Ordinal));

        Assert.Equal(
            new[]
            {
                "Modules/Identity/Tooba.Identity.Domain/Tooba.Identity.Domain.csproj",
                "Modules/Identity/Tooba.Identity.Contracts/Tooba.Identity.Contracts.csproj",
                "Modules/Identity/Tooba.Identity.Application/Tooba.Identity.Application.csproj",
                "Modules/Identity/Tooba.Identity.Infrastructure/Tooba.Identity.Infrastructure.csproj",
                "Modules/Identity/Tooba.Identity.Endpoints/Tooba.Identity.Endpoints.csproj",
            },
            identityFolder.Elements("Project").Select(p => (string?)p.Attribute("Path") ?? string.Empty).ToArray());
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
}
