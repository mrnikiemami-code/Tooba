using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-LOCALIZATION-AMC-001 / R1 — CLOSED_HOST_ZERO + failure-semantics repair.
/// </summary>
public sealed class HostLocalizationAmcGuardTests
{
    [Fact]
    public void Host_localization_folder_is_absent()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Localization");
        Assert.False(Directory.Exists(folder), "Host/Localization must be HOST_ZERO / ABSENT");
    }

    [Fact]
    public void Host_has_no_localization_namespace()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        foreach (var path in Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("namespace Tooba.Host.Localization", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Host.Localization", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Localization_endpoints_own_admin_languages_and_content_owns_reference_guard()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Endpoints/Admin/LocaleAdminEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Endpoints/LocalizationEndpointModule.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Content/Tooba.Content.Infrastructure/Adapters/ContentLanguageReferenceGuard.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Contracts/Ports/ILanguageReferenceGuard.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Contracts/Errors/LanguageErrorCodes.cs")));

        var endpoints = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Endpoints/Admin/LocaleAdminEndpoints.cs"));
        Assert.Contains("ApiResponseFactory", endpoints, StringComparison.Ordinal);
        Assert.Contains("ILocalizationAdminAuthorizer", endpoints, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/languages", File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Endpoints/LocalizationEndpointModule.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain("errorCode = ex.Message", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("TryMapLanguageFault", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ioe.Message", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", endpoints, StringComparison.Ordinal);

        var guard = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Content/Tooba.Content.Infrastructure/Adapters/ContentLanguageReferenceGuard.cs"));
        Assert.Contains("Tooba.Localization.Contracts", guard, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Localization.Application", guard, StringComparison.Ordinal);

        var contentModule = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Content/Tooba.Content.Infrastructure/ContentModule.cs"));
        Assert.Contains("ContentLanguageReferenceGuard", contentModule, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapLocalizationModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddLocalizationEndpointPresentation", program, StringComparison.Ordinal);
        Assert.Contains("HostLocalizationAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapLocaleAdminEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ContentLanguageReferenceGuard", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Directory_throws_SemanticException_with_LanguageErrorCodes_not_InvalidOperation()
    {
        var directory = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Localization/Tooba.Localization.Infrastructure/Languages/LanguageDirectory.cs"));
        Assert.Contains("SemanticException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("ContractOperationException", directory, StringComparison.Ordinal);
    }

    [Fact]
    public void Sot_records_localization_host_zero_r1()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"hostLocalizationAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostLocalizationAmcR1\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-LOCALIZATION-AMC-001-R1", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_LOCALIZATION_AMC_001_R1_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
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
