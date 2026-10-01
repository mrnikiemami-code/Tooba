using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ERRORS-AMC-001-W1 — migration guard for Host/Errors canonical presentation closure.
/// </summary>
public sealed class HostErrorsAmcW1GuardTests
{
    [Fact]
    public void Errors_exact_one_handler_namespace_mapper_absent()
    {
        var errors = Dir("src/backend/Host/Tooba.Host/Errors");
        Assert.True(Directory.Exists(errors));
        var files = Directory.GetFiles(errors, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "ToobaExceptionHandler.cs" }, files);

        var handler = File.ReadAllText(Path.Combine(errors, "ToobaExceptionHandler.cs"));
        Assert.Contains("namespace Tooba.Host.Errors", handler, StringComparison.Ordinal);
        Assert.Contains("IExceptionPresentationService", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("ISafeErrorMapper", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformExceptionMapper", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("MappedPlatformError", handler, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(errors, "PlatformExceptionMapper.cs")));
    }

    [Fact]
    public void MultiTenancy_and_program_use_canonical_presentation_no_mapper()
    {
        var multi = Read("src/backend/Host/Tooba.Host/MultiTenancy/TenantResolutionMiddleware.cs");
        Assert.DoesNotContain("PlatformExceptionMapper", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("MappedPlatformError", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("new ProblemDetails", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("IProblemDetailsService", multi, StringComparison.Ordinal);
        Assert.Contains("IExceptionPresentationService", multi, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.PlatformEditionUnconfigured", multi, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.PlatformConnectionUnconfigured", multi, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.PlatformResolutionFailed", multi, StringComparison.Ordinal);
        Assert.Contains("SemanticException", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Not Found\"", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Service Unavailable\"", multi, StringComparison.Ordinal);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Errors;", program, StringComparison.Ordinal);
        Assert.Contains("AddExceptionHandler<ToobaExceptionHandler>()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Foundation_platform_resolution_codes_unique_en_fa()
    {
        var contributor = new FoundationErrorCatalogContributor();
        var codes = new[]
        {
            FoundationErrorCodes.PlatformEditionUnconfigured,
            FoundationErrorCodes.PlatformConnectionUnconfigured,
            FoundationErrorCodes.PlatformResolutionFailed,
        };
        var owned = contributor.Contribute().GroupBy(d => d.Code, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        foreach (var code in codes)
        {
            Assert.True(owned.TryGetValue(code, out var list), code);
            Assert.Single(list!);
        }

        Assert.Equal(503, owned[FoundationErrorCodes.PlatformEditionUnconfigured].Single().HttpStatus);
        Assert.Equal(503, owned[FoundationErrorCodes.PlatformConnectionUnconfigured].Single().HttpStatus);
        Assert.Equal(404, owned[FoundationErrorCodes.PlatformResolutionFailed].Single().HttpStatus);

        var resources = new FoundationErrorResourceSet();
        var en = CultureInfo.GetCultureInfo("en");
        var fa = CultureInfo.GetCultureInfo("fa");
        foreach (var code in codes)
        {
            Assert.True(resources.Owns(code));
            var enMsg = resources.GetString(code, en);
            var faMsg = resources.GetString(code, fa);
            Assert.False(string.IsNullOrWhiteSpace(enMsg));
            Assert.False(string.IsNullOrWhiteSpace(faMsg));
            Assert.NotEqual(enMsg, faMsg);
        }

        AssertResxHas(Repo("src/backend/BuildingBlocks/Tooba.BuildingBlocks/Localization/Resources/FoundationErrors.resx"), codes);
        AssertResxHas(Repo("src/backend/BuildingBlocks/Tooba.BuildingBlocks/Localization/Resources/FoundationErrors.fa.resx"), codes);
    }

    [Fact]
    public void Sot_preserves_security_admin_cert_labels()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", sot, StringComparison.Ordinal);
    }

    private static void AssertResxHas(string path, IEnumerable<string> codes)
    {
        var doc = XDocument.Load(path);
        var names = doc.Root!
            .Elements("data")
            .Select(x => (string?)x.Attribute("name"))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var code in codes)
            Assert.Contains(code, names);
    }

    private static string Dir(string relative) => Path.Combine(Repo(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string Read(string relative) => File.ReadAllText(Repo(relative));

    private static string Repo(string? relative = null)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return relative is null
                    ? directory.FullName
                    : Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
