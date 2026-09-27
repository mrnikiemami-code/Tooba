using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-CONTENT-AMC-001-R1 — Content COMPLETE_REFERENCE_PATTERN guards.</summary>
public sealed class HostContentAmcR1GuardTests
{
    [Fact]
    public void Content_Endpoints_has_no_Infrastructure_project_reference()
    {
        var csproj = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Content",
            "Tooba.Content.Endpoints", "Tooba.Content.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Content.Infrastructure", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_Infrastructure_does_not_reference_Media_Application()
    {
        var csproj = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Content",
            "Tooba.Content.Infrastructure", "Tooba.Content.Infrastructure.csproj"));
        Assert.DoesNotContain("Tooba.Media.Application", csproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Media.Contracts", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Localization.Application", csproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Localization.Contracts", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_composers_removed_and_endpoints_use_ISender()
    {
        var endpointsRoot = Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Content", "Tooba.Content.Endpoints");
        Assert.False(File.Exists(Path.Combine(endpointsRoot, "ContentPanelComposer.cs")));
        Assert.False(File.Exists(Path.Combine(endpointsRoot, "ContentAuthorPanelComposer.cs")));
        Assert.False(File.Exists(Path.Combine(endpointsRoot, "ContentArticleMediaPanelComposer.cs")));

        var texts = Directory.GetFiles(endpointsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText);
        var joined = string.Join("\n", texts);
        Assert.Contains("ISender", joined, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(new { title", joined, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message.Contains(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("ContentDbContext", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_Contracts_and_error_catalog_exist()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Content",
            "Tooba.Content.Contracts", "Errors", "ContentErrorCodes.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Content",
            "Tooba.Content.Endpoints", "Errors", "ContentErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Content",
            "Tooba.Content.Endpoints", "Resources", "ContentErrors.resx")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Content",
            "Tooba.Content.Endpoints", "Resources", "ContentErrors.fa.resx")));
    }

    [Fact]
    public void Content_endpoint_reachable_requests_are_classified()
    {
        var endpointText = string.Join("\n",
            Directory.GetFiles(
                    Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Content", "Tooba.Content.Endpoints"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));

        var constructed = Regex.Matches(endpointText, @"\bnew\s+([A-Za-z0-9_]+(?:Command|Query))\b")
            .Select(m => m.Groups[1].Value)
            .Where(n => n is not ("Command" or "Query"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Manifest.Length, constructed.Length);
        Assert.Equal(Manifest.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray(), constructed);

        foreach (var (name, _) in Manifest)
            Assert.Contains(name, endpointText, StringComparison.Ordinal);
    }

    private static readonly (string Name, string Classification)[] Manifest =
    [
        ("ListPublishedArticlesQuery", "NO_VALIDATOR_REQUIRED"),
        ("GetPublishedArticleBySlugQuery", "NO_VALIDATOR_REQUIRED"),
        ("ListPublicCategoriesQuery", "NO_VALIDATOR_REQUIRED"),
        ("GetPublicCategoryBySlugQuery", "NO_VALIDATOR_REQUIRED"),
        ("ListPublicAuthorsQuery", "NO_VALIDATOR_REQUIRED"),
        ("GetPublicAuthorBySlugQuery", "NO_VALIDATOR_REQUIRED"),
        ("ListAdminArticlesQuery", "NO_VALIDATOR_REQUIRED"),
        ("QueryAdminArticlesGridQuery", "VALIDATOR_REQUIRED"),
        ("GetAdminArticleQuery", "NO_VALIDATOR_REQUIRED"),
        ("GetPublishReadinessQuery", "NO_VALIDATOR_REQUIRED"),
        ("GetArticlePreviewQuery", "NO_VALIDATOR_REQUIRED"),
        ("ListArticleHistoryQuery", "NO_VALIDATOR_REQUIRED"),
        ("CreateArticleCommand", "VALIDATOR_REQUIRED"),
        ("UpdateArticleCommand", "VALIDATOR_REQUIRED"),
        ("PublishArticleCommand", "NO_VALIDATOR_REQUIRED"),
        ("UnpublishArticleCommand", "NO_VALIDATOR_REQUIRED"),
        ("ArchiveArticleCommand", "NO_VALIDATOR_REQUIRED"),
        ("DeleteArticleCommand", "NO_VALIDATOR_REQUIRED"),
        ("GetCategoryTreeQuery", "NO_VALIDATOR_REQUIRED"),
        ("GetCategoryWorkspaceQuery", "NO_VALIDATOR_REQUIRED"),
        ("CreateCategoryCommand", "VALIDATOR_REQUIRED"),
        ("UpdateCategoryCommand", "VALIDATOR_REQUIRED"),
        ("UpdateCategorySeoCommand", "VALIDATOR_REQUIRED"),
        ("UpdateCategoryMediaCommand", "VALIDATOR_REQUIRED"),
        ("MoveCategoryCommand", "VALIDATOR_REQUIRED"),
        ("ReorderCategoriesCommand", "VALIDATOR_REQUIRED"),
        ("ArchiveCategoryCommand", "NO_VALIDATOR_REQUIRED"),
        ("QueryAdminAuthorsGridQuery", "VALIDATOR_REQUIRED"),
        ("GetAuthorPickerListQuery", "NO_VALIDATOR_REQUIRED"),
        ("GetAuthorWorkspaceQuery", "NO_VALIDATOR_REQUIRED"),
        ("CreateAuthorCommand", "VALIDATOR_REQUIRED"),
        ("UpdateAuthorCommand", "VALIDATOR_REQUIRED"),
        ("DeactivateAuthorCommand", "NO_VALIDATOR_REQUIRED"),
        ("SearchTagsQuery", "NO_VALIDATOR_REQUIRED"),
        ("CreateTagCommand", "VALIDATOR_REQUIRED"),
        ("ListArticleTagsQuery", "NO_VALIDATOR_REQUIRED"),
        ("AssignArticleTagCommand", "NO_VALIDATOR_REQUIRED"),
        ("RemoveArticleTagCommand", "NO_VALIDATOR_REQUIRED"),
        ("GetArticleMediaWorkspaceQuery", "NO_VALIDATOR_REQUIRED"),
        ("AssignFeaturedMediaCommand", "NO_VALIDATOR_REQUIRED"),
        ("AssignSeoImageCommand", "NO_VALIDATOR_REQUIRED"),
        ("AddGalleryMediaCommand", "VALIDATOR_REQUIRED"),
        ("RemoveGalleryMediaCommand", "NO_VALIDATOR_REQUIRED"),
        ("ReorderGalleryCommand", "VALIDATOR_REQUIRED"),
        ("PatchGalleryMediaCommand", "VALIDATOR_REQUIRED"),
        ("ListArticleCommentsQuery", "NO_VALIDATOR_REQUIRED"),
        ("CreateArticleCommentCommand", "VALIDATOR_REQUIRED"),
        ("ApproveArticleCommentCommand", "NO_VALIDATOR_REQUIRED"),
        ("RejectArticleCommentCommand", "NO_VALIDATOR_REQUIRED"),
        ("HideArticleCommentCommand", "NO_VALIDATOR_REQUIRED"),
        ("MarkArticleCommentPendingCommand", "NO_VALIDATOR_REQUIRED"),
    ];

    private static string FindRepoRoot()
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
