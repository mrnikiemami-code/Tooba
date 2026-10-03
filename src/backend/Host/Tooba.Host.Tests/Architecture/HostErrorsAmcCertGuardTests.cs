using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Order.Endpoints.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT — durable certification of Host/Errors as
/// HOST_ERRORS_AMC_CERTIFIED / HOST_ERRORS_CANONICAL_GLOBAL_EXCEPTION_BOUNDARY_CERTIFIED.
/// </summary>
public sealed class HostErrorsAmcCertGuardTests
{
    private static readonly string[] PlatformCodes =
    [
        FoundationErrorCodes.PlatformEditionUnconfigured,
        FoundationErrorCodes.PlatformConnectionUnconfigured,
        FoundationErrorCodes.PlatformResolutionFailed,
    ];

    private static readonly string[] ReservationPolicyCodes =
    [
        "reservation.policy.initial.invalid",
        "reservation.policy.retry.invalid",
        "reservation.policy.max.invalid",
    ];

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Persistence)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Errors_certified_one_file_thin_handler_exact_namespace()
    {
        var errors = Dir("src/backend/Host/Tooba.Host/Errors");
        Assert.True(Directory.Exists(errors));
        var files = Directory.GetFiles(errors, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "ToobaExceptionHandler.cs" }, files);
        Assert.False(File.Exists(Path.Combine(errors, "PlatformExceptionMapper.cs")));

        var handler = File.ReadAllText(Path.Combine(errors, "ToobaExceptionHandler.cs"));
        Assert.Contains("namespace Tooba.Host.Errors", handler, StringComparison.Ordinal);
        Assert.Contains(": IExceptionHandler", handler, StringComparison.Ordinal);
        Assert.Contains("IExceptionPresentationService", handler, StringComparison.Ordinal);
        Assert.Contains("_presentation.WriteAsync", handler, StringComparison.Ordinal);
        Assert.Contains("return true", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("ISafeErrorMapper", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("ApiResponseFactory", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("new ProblemDetails", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("IProblemDetailsService", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("ILogger", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("ActivitySource", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("Meter", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformExceptionMapper", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("MappedPlatformError", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("TypeForwardedTo", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Bad Request\"", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Internal Server Error\"", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Not Found\"", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Service Unavailable\"", handler, StringComparison.Ordinal);
        foreach (var line in File.ReadLines(Path.Combine(errors, "ToobaExceptionHandler.cs")))
        {
            if (ForeignModuleLayer.IsMatch(line.Trim()))
                Assert.Fail("foreign module layer: " + line.Trim());
        }
    }

    [Fact]
    public void Host_production_mapper_residue_and_program_registration_zero_parallel()
    {
        var hostRoot = Dir("src/backend/Host/Tooba.Host");
        foreach (var path in Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("PlatformExceptionMapper", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MappedPlatformError", text, StringComparison.Ordinal);
        }

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Errors;", program, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(program, @"AddExceptionHandler<\s*ToobaExceptionHandler\s*>\s*\(\s*\)").Count);
        Assert.Contains("UseExceptionHandler()", program, StringComparison.Ordinal);

        var multi = Read("src/backend/Host/Tooba.Host/MultiTenancy/TenantResolutionMiddleware.cs");
        Assert.Contains("IExceptionPresentationService", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("IProblemDetailsService", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("new ProblemDetails", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Not Found\"", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Service Unavailable\"", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", multi, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", multi, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.PlatformEditionUnconfigured", multi, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.PlatformConnectionUnconfigured", multi, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.PlatformResolutionFailed", multi, StringComparison.Ordinal);
        Assert.Contains("record.Status != TenantStatus.Active", multi, StringComparison.Ordinal);
        Assert.Contains("FailClosed()", multi, StringComparison.Ordinal);
    }

    [Fact]
    public void Foundation_platform_matrix_and_reservation_policy_catalog_integrity()
    {
        var foundation = new FoundationErrorCatalogContributor().Contribute()
            .GroupBy(d => d.Code, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        Assert.Single(foundation[FoundationErrorCodes.PlatformEditionUnconfigured]);
        Assert.Single(foundation[FoundationErrorCodes.PlatformConnectionUnconfigured]);
        Assert.Single(foundation[FoundationErrorCodes.PlatformResolutionFailed]);
        Assert.Equal(503, foundation[FoundationErrorCodes.PlatformEditionUnconfigured].Single().HttpStatus);
        Assert.Equal(503, foundation[FoundationErrorCodes.PlatformConnectionUnconfigured].Single().HttpStatus);
        Assert.Equal(404, foundation[FoundationErrorCodes.PlatformResolutionFailed].Single().HttpStatus);

        var resources = new FoundationErrorResourceSet();
        var en = CultureInfo.GetCultureInfo("en");
        var fa = CultureInfo.GetCultureInfo("fa");
        foreach (var code in PlatformCodes)
        {
            Assert.True(resources.Owns(code));
            Assert.False(string.IsNullOrWhiteSpace(resources.GetString(code, en)));
            Assert.False(string.IsNullOrWhiteSpace(resources.GetString(code, fa)));
            Assert.NotEqual(resources.GetString(code, en), resources.GetString(code, fa));
        }

        AssertResxHas(Repo("src/backend/BuildingBlocks/Tooba.BuildingBlocks/Localization/Resources/FoundationErrors.resx"), PlatformCodes);
        AssertResxHas(Repo("src/backend/BuildingBlocks/Tooba.BuildingBlocks/Localization/Resources/FoundationErrors.fa.resx"), PlatformCodes);

        var catalogSrc = Read("src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCatalogContributor.cs");
        var catalogCodes = Read("src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs");
        var orderSrc = Read("src/backend/Modules/Order/Tooba.Order.Endpoints/Errors/OrderErrorCatalogContributor.cs");
        foreach (var code in ReservationPolicyCodes)
            Assert.Contains($"\"{code}\"", catalogCodes, StringComparison.Ordinal);

        Assert.Contains("HoldPolicyReservationInitialInvalid", catalogSrc, StringComparison.Ordinal);
        Assert.Contains("HoldPolicyReservationRetryInvalid", catalogSrc, StringComparison.Ordinal);
        Assert.Contains("HoldPolicyReservationMaxInvalid", catalogSrc, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status400BadRequest", catalogSrc, StringComparison.Ordinal);
        Assert.DoesNotContain("D(ReservationPolicyErrors.InitialInvalid", orderSrc, StringComparison.Ordinal);
        Assert.DoesNotContain("D(ReservationPolicyErrors.RetryInvalid", orderSrc, StringComparison.Ordinal);
        Assert.DoesNotContain("D(ReservationPolicyErrors.MaxInvalid", orderSrc, StringComparison.Ordinal);

        // Order keeps stable machine codes for validators/resources.
        var orderCodes = Read("src/backend/Modules/Order/Tooba.Order.Application/Admin/Settings/ReservationPolicy/ReservationPolicyErrors.cs");
        foreach (var code in ReservationPolicyCodes)
            Assert.Contains($"\"{code}\"", orderCodes, StringComparison.Ordinal);

        // Order contributor must not re-register the three Catalog-owned descriptors.
        var orderDescriptors = new OrderErrorCatalogContributor().Contribute()
            .Select(d => d.Code)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var code in ReservationPolicyCodes)
            Assert.DoesNotContain(code, orderDescriptors);
    }

    [Fact]
    public void Sot_certifies_errors_and_preserves_security_admin_multitenancy_not_opened()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_CANONICAL_GLOBAL_EXCEPTION_BOUNDARY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostErrorsAmc001W2Cert\"", sot, StringComparison.Ordinal);
        // Errors W2-CERT historical block still records Multitenancy as NOT_OPENED at certify time.
        Assert.Contains("\"multiTenancyCertification\": \"NOT_OPENED\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"implementationCommit\": \"e190e213c491fd530d86c7e5680cb0607b5e98d3\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostMultiTenancyAmc001W1\"", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
    }

    [Fact]
    public void Sot_security_and_errors_cert_blocks_have_exactly_one_docsStamp_each()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        AssertBlockDocsStamp(sot, "hostSecurityAmc001W3Cert", "73a80ee28ed9dc054be5adae0f7115e72c115ded");
        AssertBlockDocsStamp(sot, "hostErrorsAmc001W2Cert", "8d5e6a2dce7b34e2ceeb4166e5d324467bc3a8f3");
    }

    private static void AssertBlockDocsStamp(string json, string blockName, string expectedStamp)
    {
        var key = $"\"{blockName}\":";
        var start = json.IndexOf(key, StringComparison.Ordinal);
        Assert.True(start >= 0, blockName + " missing");
        var brace = json.IndexOf('{', start);
        Assert.True(brace >= 0);
        var depth = 0;
        var end = -1;
        for (var i = brace; i < json.Length; i++)
        {
            var c = json[i];
            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    end = i;
                    break;
                }
            }
        }

        Assert.True(end > brace);
        var block = json.Substring(brace, end - brace + 1);
        var matches = Regex.Matches(block, @"""docsStamp""\s*:");
        Assert.True(matches.Count == 1, $"{blockName} docsStamp count={matches.Count}");
        Assert.Contains($"\"docsStamp\": \"{expectedStamp}\"", block, StringComparison.Ordinal);

        // Also detect any duplicated property names inside the block via raw key scan.
        var keys = Regex.Matches(block, @"""(?<k>[^""]+)""\s*:")
            .Select(m => m.Groups["k"].Value)
            .ToList();
        var dup = keys.GroupBy(k => k, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();
        Assert.True(dup.Length == 0, $"{blockName} duplicate properties: " + string.Join(",", dup));
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
