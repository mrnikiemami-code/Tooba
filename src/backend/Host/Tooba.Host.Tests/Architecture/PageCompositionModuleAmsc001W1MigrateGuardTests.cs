using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PAGECOMPOSITION-AMSC-001-W1 — canonical typed-fault seam lock.
/// Pins the declared-code guard over all 8 module codes, the dual-mechanism
/// <c>PageCompositionOperation</c> mapping (ContractOperationException under the IsKnown filter +
/// SemanticException), the value-less overload and the no-message-heuristic rule.
/// </summary>
public sealed class PageCompositionModuleAmsc001W1MigrateGuardTests
{
    [Fact]
    public void Error_codes_owner_declares_IsKnown_guard_over_all_eight_codes()
    {
        var text = File.ReadAllText(Path.Combine(
            Repo(), "src/backend/Modules/PageComposition/Tooba.PageComposition.Contracts/Errors/PageCompositionErrorCodes.cs"));

        Assert.Contains("public static bool IsKnown(string? code)", text, StringComparison.Ordinal);
        Assert.Contains("private static readonly HashSet<string> KnownCodes", text, StringComparison.Ordinal);

        foreach (var code in new[]
        {
            "TenantMissing", "SectionMissing", "SectionTypeRejected", "ConfigRejected",
            "MutationRejected", "SectionTypeRequired", "SectionIdsRequired", "SectionIdRequired"
        })
        {
            Assert.Contains(code + ',', text, StringComparison.Ordinal);
        }

        foreach (var literal in new[]
        {
            "\"page-composition.tenant.missing\"",
            "\"page-composition.section.missing\"",
            "\"page-composition.section-type.rejected\"",
            "\"page-composition.config.rejected\"",
            "\"page-composition.mutation.rejected\"",
            "\"page-composition.sectionType.required\"",
            "\"page-composition.sectionIds.required\"",
            "\"page-composition.sectionId.required\""
        })
        {
            Assert.Contains(literal, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Operation_seam_maps_both_typed_fault_mechanisms_by_declared_code_only()
    {
        var text = File.ReadAllText(Path.Combine(
            Repo(), "src/backend/Modules/PageComposition/Tooba.PageComposition.Application/Composition/PageCompositionOperation.cs"));

        Assert.Contains(
            "catch (ContractOperationException ex) when (PageCompositionErrorCodes.IsKnown(ex.Code))",
            text,
            StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", text, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync(Func<Task> action)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
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
