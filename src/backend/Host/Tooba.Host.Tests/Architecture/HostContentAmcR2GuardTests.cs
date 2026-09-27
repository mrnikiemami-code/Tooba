using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-CONTENT-AMC-001-R2 — typed fault / zero message-classification guards.</summary>
public sealed class HostContentAmcR2GuardTests
{
    [Fact]
    public void Content_Application_Infrastructure_have_zero_message_classification()
    {
        foreach (var layer in new[] { "Tooba.Content.Application", "Tooba.Content.Infrastructure" })
        {
            var root = Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Content", layer);
            var joined = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));

            Assert.DoesNotContain("IsKnownCode(", joined, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticError(ex.Message", joined, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.Contains(", joined, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.StartsWith(", joined, StringComparison.Ordinal);
            Assert.DoesNotContain("PlatformHttpException", joined, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ContentOperation_maps_ContractOperationException_by_Code_only()
    {
        var path = Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Content",
            "Tooba.Content.Application", "Composition", "ContentOperation.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("catch (ContractOperationException ex)", text, StringComparison.Ordinal);
        Assert.Contains("new SemanticError(ex.Code)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IsKnownCode", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Media_readiness_bridge_throws_typed_ContractOperationException()
    {
        var path = Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Media",
            "Tooba.Media.Infrastructure", "Adapters", "MediaAssetReadinessBridge.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ContractOperationException(MediaAssetContractCodes.AssetMissing)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException(\"media.asset.missing\")", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_media_validator_translates_by_Code_not_message()
    {
        var path = Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Content",
            "Tooba.Content.Infrastructure", "Adapters", "ContentMediaAssetValidator.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ex.Code == MediaAssetContractCodes.AssetMissing", text, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException(ContentErrorCodes.MediaNotFound", text, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (InvalidOperationException)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_article_and_author_use_ApiResponseFactory_Created()
    {
        var articles = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Content",
            "Tooba.Content.Endpoints", "Admin", "ContentEndpoints.cs"));
        var authors = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Content",
            "Tooba.Content.Endpoints", "Admin", "ContentAuthorEndpoints.cs"));
        Assert.Contains("api.Created(", articles, StringComparison.Ordinal);
        Assert.Contains("api.Created(", authors, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(result.Value, statusCode: StatusCodes.Status201Created)", articles, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(result.Value, statusCode: StatusCodes.Status201Created)", authors, StringComparison.Ordinal);
    }

    [Fact]
    public void ContentErrorCodes_has_no_IsKnownCode_helper()
    {
        var text = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Content",
            "Tooba.Content.Contracts", "Errors", "ContentErrorCodes.cs"));
        Assert.DoesNotContain("IsKnownCode", text, StringComparison.Ordinal);
        Assert.DoesNotContain("KnownCodes", text, StringComparison.Ordinal);
    }

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
