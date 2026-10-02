using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-STORY-AMC-001-W6 — ARCH-COMPLETE-002 structure certification + microservice coupling ZERO.
/// </summary>
public sealed class StoryModuleAmcW6CertGuardTests
{
    private static readonly string[] ForeignModuleNeedles =
    [
        "Tooba.Catalog.",
        "Tooba.Party.",
        "Tooba.Identity.",
        "Tooba.Offer.",
        "Tooba.Order.",
        "Tooba.Media.",
        "Tooba.AccessControl.",
        "Tooba.Payment.",
        "Tooba.Content.",
        "Tooba.Reviews.",
        "Tooba.Cart.",
        "Tooba.Fulfillment.",
        "Tooba.Settlement.",
        "Tooba.Promotion.",
        "Tooba.Notification.",
        "Tooba.Support.",
        "Tooba.Wishlist.",
        "Tooba.AddressBook.",
    ];

    [Fact]
    public void Story_is_structure_certified_with_exact_one_manifest_entry()
    {
        var manifests = File.ReadAllText(Path.Combine(Repo(), "docs", "architecture", "tmar-module-structure-manifests.json"));
        using var doc = JsonDocument.Parse(manifests);
        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("module").GetString() == "Story")
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
    }

    [Fact]
    public void Story_root_allowlists_and_path_namespaces_match_manifest()
    {
        var root = Repo();
        var story = Path.Combine(root, "src", "backend", "Modules", "Story");

        AssertRootCs(Path.Combine(story, "Tooba.Story.Contracts"), Array.Empty<string>());
        AssertRootCs(Path.Combine(story, "Tooba.Story.Domain"), Array.Empty<string>());
        AssertRootCs(Path.Combine(story, "Tooba.Story.Application"), Array.Empty<string>());
        AssertRootCs(Path.Combine(story, "Tooba.Story.Endpoints"), ["StoryEndpointModule.cs"]);
        AssertRootCs(Path.Combine(story, "Tooba.Story.Infrastructure"), ["StoryModule.cs"]);

        Assert.False(Directory.Exists(Path.Combine(story, "Tooba.Story.Infrastructure", "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(story, "Tooba.Story.Infrastructure", "Persistence", "Migrations")));
        Assert.False(File.Exists(Path.Combine(story, "Tooba.Story.Endpoints", "StoryHttpErrors.cs")));
        Assert.True(File.Exists(Path.Combine(story, "Tooba.Story.Endpoints", "Errors", "StoryHttpErrors.cs")));

        AssertNs(root, "src/backend/Modules/Story/Tooba.Story.Endpoints/Errors/StoryHttpErrors.cs",
            "namespace Tooba.Story.Endpoints.Errors;");
        AssertNs(root, "src/backend/Modules/Story/Tooba.Story.Application/Stories/Composition/StoryOperation.cs",
            "namespace Tooba.Story.Application.Stories.Composition;");
    }

    [Fact]
    public void Story_has_zero_foreign_Application_Infrastructure_Domain_coupling()
    {
        var storyRoot = Path.Combine(Repo(), "src", "backend", "Modules", "Story");
        foreach (var file in Directory.EnumerateFiles(storyRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;

            var text = File.ReadAllText(file);
            foreach (var needle in ForeignModuleNeedles)
            {
                Assert.DoesNotContain(needle + "Application", text, StringComparison.Ordinal);
                Assert.DoesNotContain(needle + "Infrastructure", text, StringComparison.Ordinal);
                Assert.DoesNotContain(needle + "Domain", text, StringComparison.Ordinal);
            }
        }

        foreach (var csproj in Directory.EnumerateFiles(storyRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(csproj);
            Assert.DoesNotContain("Tooba.Catalog.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Party.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Identity.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Offer.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Media.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AccessControl.", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Story_endpoints_do_not_reference_Infrastructure_or_DbContext()
    {
        var endpoints = Path.Combine(Repo(), "src", "backend", "Modules", "Story", "Tooba.Story.Endpoints");
        var csproj = File.ReadAllText(Path.Combine(endpoints, "Tooba.Story.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Story.Infrastructure", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Story.Domain", csproj, StringComparison.Ordinal);

        foreach (var file in Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("StoryDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Story.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Story.Domain", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Host_Story_folder_remains_absent_and_slnx_groups_Story()
    {
        var root = Repo();
        Assert.False(Directory.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Story")));
        var slnx = File.ReadAllText(Path.Combine(root, "src", "backend", "Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/Story/\">", slnx, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_records_complete_reference_pattern_for_Story()
    {
        var sot = File.ReadAllText(Path.Combine(Repo(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"storyModuleAmc001W6Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", sot, StringComparison.Ordinal);
        Assert.Contains("STORY_STRUCTURE_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("LEGAL_CONTRACTS_ONLY", sot, StringComparison.Ordinal);
    }

    private static void AssertRootCs(string projectDir, string[] allowlist)
    {
        var actual = Directory.EnumerateFiles(projectDir, "*.cs")
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(allowlist.OrderBy(x => x, StringComparer.Ordinal).ToArray(), actual);
    }

    private static void AssertNs(string root, string relative, string expected)
    {
        var text = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        Assert.Contains(expected, text, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"\busing\s+\w+\s*=\s*", RegexOptions.Multiline), text);
    }

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
