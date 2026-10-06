using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-OPERATORPROFILE-AMSC-001-W3 — ARCH-COMPLETE-002 certification lock.
/// Pins the certified verdict, the canonical seam set (declared-code guard + dual-mechanism typed-fault
/// seam + bilingual resource pair), the exhaustive validator matrix, contracts-only boundaries with the
/// foreign-consumer seam, own-schema persistence, module endpoint ownership, Host closure and the
/// certified manifest/SoT state.
/// </summary>
public sealed class OperatorProfileModuleAmsc001W3CertGuardTests
{
    [Fact]
    public void OperatorProfile_is_amsc001_certified_in_manifest_and_sot()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-module-structure-manifests.json"))
                .Replace("\uFEFF", string.Empty));
        var moduleEntry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "OperatorProfile");
        Assert.True(moduleEntry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", moduleEntry.GetProperty("lockVersion").GetString());
        Assert.Contains("TB-TMAR-OPERATORPROFILE-AMSC-001", moduleEntry.GetProperty("certificationNote").GetString(), StringComparison.Ordinal);

        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));
        var block = sot.RootElement.GetProperty("operatorProfileAmsc001W3");
        Assert.Equal("OPERATORPROFILE_AMSC_001_CERTIFIED", block.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", block.GetProperty("lockVersion").GetString());
        Assert.True(block.GetProperty("microserviceExtractable").GetBoolean());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "OperatorProfile"));
    }

    [Fact]
    public void Canonical_seams_are_present_and_single_owned()
    {
        var root = Repo();

        var codes = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Contracts/Errors/OperatorProfileErrorCodes.cs"));
        Assert.Contains("public static bool IsKnown(string? code)", codes, StringComparison.Ordinal);
        Assert.Equal(6, Regex.Matches(codes, "public const string ").Count);

        var operation = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Application/Composition/OperatorProfileOperation.cs"));
        Assert.Contains("catch (ContractOperationException ex) when (OperatorProfileErrorCodes.IsKnown(ex.Code))", operation, StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", operation, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", operation, StringComparison.Ordinal);

        // Exactly one catalog contributor and one resource set, registered by the module composition.
        Assert.Equal(
            new[] { "OperatorProfileErrorCatalogContributor.cs" },
            Directory.EnumerateFiles(Src(root, "Tooba.OperatorProfile.Contracts/Errors"), "*Contributor.cs").Select(Path.GetFileName).ToArray());
        var module = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Infrastructure/OperatorProfileModule.cs"));
        Assert.Contains("IErrorCatalogContributor, OperatorProfileErrorCatalogContributor>", module, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, OperatorProfileErrorResourceSet>", module, StringComparison.Ordinal);

        // Bilingual resource pair exists for the operator.profile. keyspace.
        Assert.True(File.Exists(Src(root, "Tooba.OperatorProfile.Contracts/Resources/OperatorProfileErrors.resx")));
        Assert.True(File.Exists(Src(root, "Tooba.OperatorProfile.Contracts/Resources/OperatorProfileErrors.fa.resx")));
        var en = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Contracts/Resources/OperatorProfileErrors.resx"));
        var fa = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Contracts/Resources/OperatorProfileErrors.fa.resx"));
        foreach (var key in new[] { "operator.profile.rejected", "operator.profile.validation.actor_required", "operator.profile.validation.display_name", "operator.profile.validation.first_name", "operator.profile.validation.last_name", "operator.profile.validation.bio" })
        {
            Assert.Contains($"\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"\"{key}\"", fa, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validator_matrix_is_exhaustive_for_both_endpoint_reachable_requests()
    {
        var root = Repo();
        var command = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Application/Admin/Commands/UpsertOperatorProfileCommand.cs"));
        var query = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Application/Admin/Queries/GetOperatorProfileQuery.cs"));
        Assert.Contains(": IRequest<Result<OperatorProfileAdminResponse>>", command, StringComparison.Ordinal);
        Assert.Contains(": IRequest<Result<OperatorProfileAdminResponse>>", query, StringComparison.Ordinal);
        Assert.Contains("IRequestHandler<UpsertOperatorProfileCommand, Result<OperatorProfileAdminResponse>>", command, StringComparison.Ordinal);
        Assert.Contains("IRequestHandler<GetOperatorProfileQuery, Result<OperatorProfileAdminResponse>>", query, StringComparison.Ordinal);

        var commandValidator = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Application/Admin/Validators/UpsertOperatorProfileCommandValidator.cs"));
        var queryValidator = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Application/Admin/Validators/GetOperatorProfileQueryValidator.cs"));
        Assert.Contains("AbstractValidator<UpsertOperatorProfileCommand>", commandValidator, StringComparison.Ordinal);
        Assert.Contains("AbstractValidator<GetOperatorProfileQuery>", queryValidator, StringComparison.Ordinal);
        Assert.DoesNotContain("OperatorProfileErrorCodes.ProfileRejected", commandValidator, StringComparison.Ordinal);

        // Endpoint dispatch stays ISender + ApiResponseFactory only.
        var endpoints = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Endpoints/Admin/OperatorProfileAdminEndpoints.cs"));
        Assert.Contains("sender.Send(", endpoints, StringComparison.Ordinal);
        Assert.Contains("api.From(", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/\", GetAsync)", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/\", PutAsync)", endpoints, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(endpoints, "group\\.Map").Count);
    }

    [Fact]
    public void Boundaries_stay_contracts_only_and_persistence_is_module_owned()
    {
        var root = Repo();
        var foreignPattern = "Tooba\\.(Order|Cart|Catalog|Identity|AccessControl|Content|CustomerProfile|Notification|Media|Party|Fulfillment|Inventory|BulkInquiry|Localization|Payment|Settlement|Story|Wishlist|UserPreference|PageComposition|ProductQnA)\\.(Application|Infrastructure|Domain|Endpoints)";
        foreach (var project in new[] { "Contracts", "Domain", "Application", "Infrastructure", "Endpoints" })
        {
            var projectDir = Src(root, $"Tooba.OperatorProfile.{project}");
            foreach (var file in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.False(
                    System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(file), foreignPattern),
                    $"foreign module reference in {file}");
            }
        }

        // Own schema + own DbContext; the only DbSet pair is module entity + own outbox.
        var db = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Infrastructure/Persistence/OperatorProfileDbContext.cs"));
        Assert.Contains("\"operator_profile\"", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<OperatorProfileEntity> Profiles", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<OutboxMessage> OutboxMessages", db, StringComparison.Ordinal);
        foreach (var foreignNs in new[] { "Tooba.Order.", "Tooba.Cart.", "Tooba.Catalog.", "Tooba.Identity.", "Tooba.AccessControl.", "Tooba.Content.", "Tooba.CustomerProfile.", "Tooba.Notification.", "Tooba.Media.", "Tooba.Party.", "Tooba.Fulfillment.", "Tooba.Inventory.", "Tooba.Localization.", "Tooba.Payment.", "Tooba.Settlement." })
        {
            Assert.DoesNotContain(foreignNs, db, StringComparison.Ordinal);
        }

        // Foreign consumers reach only the Contracts port.
        var contractsPorts = File.ReadAllText(Src(root, "Tooba.OperatorProfile.Contracts/Ports/ActorDisplayContracts.cs"));
        Assert.Contains("interface IActorDisplayLookup", contractsPorts, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_closure_and_solution_grouping_are_preserved()
    {
        var root = Repo();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/OperatorProfile")));
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/OperatorProfile/\">", slnx, StringComparison.Ordinal);
    }

    private static string Src(string root, string relative) => Path.Combine(
        root, "src", "backend", "Modules", "OperatorProfile", relative.Replace('/', Path.DirectorySeparatorChar));

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
