using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PARTY-AMSC-001-W1 — canonical typed-fault seam + inbound-boundary lock.
/// Pins the declared-code guard over all 11 module codes, the dual-mechanism
/// <c>PartyOperation</c> mapping (ContractOperationException under the IsKnown filter +
/// SemanticException), the value-less overload, the no-message-heuristic rule, and the
/// Contracts-only inbound dev-seed surface (Promotion consumes Party.Contracts, never Party.Application).
/// </summary>
public sealed class PartyModuleAmsc001W1MigrateGuardTests
{
    [Fact]
    public void Error_codes_owner_declares_IsKnown_guard_over_all_eleven_codes()
    {
        var text = File.ReadAllText(Path.Combine(
            Repo(), "src/backend/Modules/Party/Tooba.Party.Contracts/Errors/PartyErrorCodes.cs"));

        Assert.Contains("public static bool IsKnown(string? code)", text, StringComparison.Ordinal);
        Assert.Contains("private static readonly HashSet<string> KnownCodes", text, StringComparison.Ordinal);

        foreach (var code in new[]
        {
            "SellerSettingsMissing", "SellerSettingsRejected", "OperationRejected",
            "DisplayNameRequired", "DisplayNameLength", "LegalNameShape", "DescriptionShape",
            "SupportPhoneShape", "SupportEmailShape", "AddressLineShape", "AdminSellersGridRequestRequired"
        })
        {
            Assert.Contains(code + ',', text, StringComparison.Ordinal);
        }

        foreach (var literal in new[]
        {
            "\"seller.settings.missing\"",
            "\"seller.settings.rejected\"",
            "\"party.operation.rejected\"",
            "\"seller.settings.validation.display_name_required\"",
            "\"seller.settings.validation.display_name_length\"",
            "\"seller.settings.validation.legal_name_shape\"",
            "\"seller.settings.validation.description_shape\"",
            "\"seller.settings.validation.support_phone_shape\"",
            "\"seller.settings.validation.support_email_shape\"",
            "\"seller.settings.validation.address_line_shape\"",
            "\"party.admin.sellers.validation.grid_request_required\""
        })
        {
            Assert.Contains(literal, text, StringComparison.Ordinal);
        }

        Assert.Equal(11, Regex.Matches(text, "public const string ").Count);
    }

    [Fact]
    public void Operation_seam_maps_both_typed_fault_mechanisms_by_declared_code_only()
    {
        var text = File.ReadAllText(Path.Combine(
            Repo(), "src/backend/Modules/Party/Tooba.Party.Application/Composition/PartyOperation.cs"));

        Assert.Contains(
            "catch (ContractOperationException ex) when (PartyErrorCodes.IsKnown(ex.Code))",
            text,
            StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", text, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync(Func<Task> action)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Foreign_development_seeds_consume_party_contracts_only()
    {
        var root = Repo();
        var devDirectory = Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Contracts/Ports/IPartyDevelopmentDirectory.cs");
        Assert.True(File.Exists(devDirectory), "Party.Contracts development-seed surface missing");

        // Party.Infrastructure registers the Contracts adapter for the narrow dev-seed port.
        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Infrastructure/PartyModule.cs"));
        Assert.Contains("IPartyDevelopmentDirectory, PartyDevelopmentDirectoryAdapter>", module, StringComparison.Ordinal);

        // Promotion no longer references Party.Application in any production csproj or source file.
        var promotionInfraCsproj = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Tooba.Promotion.Infrastructure.csproj"));
        Assert.DoesNotContain("Tooba.Party.Application.csproj", promotionInfraCsproj, StringComparison.Ordinal);

        var promotionDir = Path.Combine(root, "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure");
        foreach (var file in Directory.EnumerateFiles(promotionDir, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.False(
                File.ReadAllText(file).Contains("Tooba.Party.Application", StringComparison.Ordinal),
                $"foreign Party.Application reference in {file}");
        }
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
