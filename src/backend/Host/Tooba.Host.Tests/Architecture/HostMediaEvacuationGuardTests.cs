using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-MEDIA-EVACUATE-001 — the Media HTTP boundary (admin upload/query/get + public
/// binary serving with SVG fallback) is owned by <c>Tooba.Media.Endpoints</c>. Host must no longer
/// own a Media endpoint implementation, must not carry a <c>Tooba.Host.Media</c> namespace, and the
/// Media Endpoints project must have zero Host dependency. Admin certification is untouched.
/// </summary>
public sealed class HostMediaEvacuationGuardTests
{
    private const string MediaEndpointsProject = "src/backend/Modules/Media/Tooba.Media.Endpoints";

    [Fact]
    public void Host_media_endpoint_implementation_and_namespace_are_gone()
    {
        var host = HostRoot();
        Assert.False(File.Exists(Path.Combine(host, "Media", "MediaEndpoints.cs")));
        Assert.False(Directory.Exists(Path.Combine(host, "Media")));

        var stale = Directory.GetFiles(host, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => Regex.IsMatch(File.ReadAllText(p), @"(?m)^namespace\s+Tooba\.Host\.Media;"))
            .Select(Path.GetFileName)
            .ToArray();
        Assert.Empty(stale);

        var program = File.ReadAllText(Path.Combine(host, "Program.cs"));
        Assert.DoesNotContain("using Tooba.Host.Media;", program, StringComparison.Ordinal);
        Assert.DoesNotContain("app.MapMediaEndpoints();", program, StringComparison.Ordinal);
        Assert.Contains("app.MapMediaModuleEndpoints();", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Media_endpoints_owns_all_four_routes_with_parity()
    {
        var admin = ReadMedia("Admin/MediaAdminEndpoints.cs");
        Assert.Contains("MapGroup(\"/v1/admin/media\")", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/upload\", UploadAsync).DisableAntiforgery()", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/\", QueryAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{id:guid}\", GetAsync)", admin, StringComparison.Ordinal);

        var module = ReadMedia("MediaEndpointModule.cs");
        Assert.Contains("MapGet(\"/v1/media/{id:guid}\", MediaAssetServing.ServeAsync)", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Media_endpoints_has_zero_host_dependency()
    {
        foreach (var file in Directory.GetFiles(MediaRoot(), "*.cs", SearchOption.AllDirectories)
                     .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                 && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Host", text);
            Assert.DoesNotContain("Host.Admin.Access", text);
        }

        var csproj = File.ReadAllText(Path.Combine(MediaRoot(), "Tooba.Media.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Host", csproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Media.Application", csproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.BuildingBlocks", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Media_admin_authorization_uses_the_neutral_platform_seam()
    {
        var admin = ReadMedia("Admin/MediaAdminEndpoints.cs");
        Assert.Equal(3, CountOccurrences(admin, "adminAccess.RequireAuthorizedAsync"));
        Assert.Contains("IAdminPanelAccess", admin, StringComparison.Ordinal);
        Assert.Contains("using Tooba.BuildingBlocks.Security;", admin, StringComparison.Ordinal);
    }

    [Fact]
    public void Media_stable_error_codes_and_placeholder_fallback_are_preserved()
    {
        var admin = ReadMedia("Admin/MediaAdminEndpoints.cs");
        Assert.Contains("media.upload.failed", admin, StringComparison.Ordinal);
        Assert.Contains("media.missing", admin, StringComparison.Ordinal);

        var serving = ReadMedia("Admin/MediaAssetServing.cs");
        Assert.Contains("enableRangeProcessing: true", serving, StringComparison.Ordinal);
        Assert.Contains("image/svg+xml", serving, StringComparison.Ordinal);
        Assert.Contains("PlaceholderSvg", serving, StringComparison.Ordinal);
    }

    [Fact]
    public void Media_presentation_hygiene_has_no_message_parsing_or_new_codes()
    {
        var admin = ReadMedia("Admin/MediaAdminEndpoints.cs");
        Assert.DoesNotContain("ex.Message.Contains", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ErrorCatalog", admin, StringComparison.Ordinal);
    }

    [Fact]
    public void Storefront_consumes_the_media_module_serving_helper()
    {
        Assert.False(Directory.Exists(Path.Combine(HostRoot(), "Storefront")));

        var mediaModule = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Media", "Tooba.Media.Endpoints", "MediaEndpointModule.cs"));
        Assert.Contains("MapMediaStorefrontEndpoints()", mediaModule, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_certification_remains_untouched()
    {
        var admin = Path.Combine(HostRoot(), "Admin");
        Assert.Equal(17, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
        Assert.Empty(Directory.GetFiles(admin, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host.Tests", "Architecture",
            "HostAdminCanonicalCertificationGuardTests.cs")));
    }

    [Fact]
    public void Media_endpoints_project_is_grouped_in_the_solution()
    {
        var slnx = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Tooba.slnx"));
        Assert.Contains("Modules/Media/Tooba.Media.Endpoints/Tooba.Media.Endpoints.csproj", slnx, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = text.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }

    private static string ReadMedia(string relative) =>
        File.ReadAllText(Path.Combine(MediaRoot(), relative.Replace('/', Path.DirectorySeparatorChar)));

    private static string MediaRoot() => Path.Combine(FindRepoRoot(), MediaEndpointsProject);

    private static string HostRoot() =>
        Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Tooba.sln"))
                || File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
