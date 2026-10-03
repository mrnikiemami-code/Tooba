using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-MEDIA-AMC-001-W2 — Domain/Application/Infra layer structure without root dumps.</summary>
public sealed class MediaModuleAmcW2StructureGuardTests
{
    [Fact]
    public void Media_layers_are_capability_cohesive_without_root_dumps()
    {
        var root = Repo();
        var domain = Path.Combine(root, "src/backend/Modules/Media/Tooba.Media.Domain");
        var app = Path.Combine(root, "src/backend/Modules/Media/Tooba.Media.Application");
        var infra = Path.Combine(root, "src/backend/Modules/Media/Tooba.Media.Infrastructure");
        var contracts = Path.Combine(root, "src/backend/Modules/Media/Tooba.Media.Contracts");

        Assert.False(File.Exists(Path.Combine(domain, "MediaAsset.cs")));
        Assert.False(File.Exists(Path.Combine(app, "MediaContracts.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "MediaAsset.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Enums", "MediaAssetStatus.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IMediaDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IMediaObjectStore.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Models", "MediaAssetInfo.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Assets", "MediaDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Storage", "LocalFileMediaStore.cs")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "MediaErrorCodes.cs")));

        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "MediaModule.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
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
