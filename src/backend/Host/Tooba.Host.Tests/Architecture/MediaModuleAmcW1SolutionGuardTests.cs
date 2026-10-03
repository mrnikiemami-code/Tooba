using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-MEDIA-AMC-001-W1 — Media projects grouped under /Modules/Media/ in slnx.</summary>
public sealed class MediaModuleAmcW1SolutionGuardTests
{
    [Fact]
    public void Media_projects_live_under_modules_media_folder()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/Media/\">", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Media/Tooba.Media.Endpoints/Tooba.Media.Endpoints.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Media/Tooba.Media.Application/Tooba.Media.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Media/Tooba.Media.Infrastructure/Tooba.Media.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Media/Tooba.Media.Domain/Tooba.Media.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/Media/Tooba.Media.Contracts/Tooba.Media.Contracts.csproj", slnx, StringComparison.Ordinal);

        // Not left as loose children of the flat /Modules/ folder block only.
        var mediaFolderIndex = slnx.IndexOf("<Folder Name=\"/Modules/Media/\">", StringComparison.Ordinal);
        Assert.True(mediaFolderIndex > 0);
        var mediaBlock = slnx[mediaFolderIndex..];
        Assert.Contains("Tooba.Media.Endpoints", mediaBlock, StringComparison.Ordinal);
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
