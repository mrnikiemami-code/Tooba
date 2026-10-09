using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-STORY-AMC-001-W3 — Story capability foldering / VS solution grouping.</summary>
public sealed class StoryModuleAmcW3StructureGuardTests
{
    [Fact]
    public void Story_projects_are_capability_foldered_with_matching_namespaces_and_slnx_group()
    {
        var root = Repo();

        Assert.False(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Domain/StoryEntities.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Application/StoryContracts.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Infrastructure/StoryDirectory.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Infrastructure/StoryDevelopmentSeed.cs")));

        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Domain/Aggregates/Story.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Domain/Aggregates/StoryItem.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Domain/Enums/StoryStatus.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Domain/Rules/StoryRules.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Domain/Tenant/StoryTenantIds.cs")));

        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Application/Stories/Models/StoryModels.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Application/Stories/Ports/IStoryDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Application/Stories/Commands/Admin/AdminStoryCommands.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Application/Stories/Queries/Storefront/GetPublicStoriesQuery.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Application/Composition/StoryOperation.cs")));
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Application/Stories/Composition")));

        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Infrastructure/Directories/StoryDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Infrastructure/Development/StoryDevelopmentSeed.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Infrastructure/DependencyInjection/StoryModule.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Infrastructure/Messaging/StoryOutboxRegistration.cs")));
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Infrastructure/Directory")));

        AssertNs(root, "src/backend/Modules/Story/Tooba.Story.Domain/Aggregates/Story.cs", "namespace Tooba.Story.Domain.Aggregates;");
        AssertNs(root, "src/backend/Modules/Story/Tooba.Story.Application/Stories/Models/StoryModels.cs", "namespace Tooba.Story.Application.Stories.Models;");
        AssertNs(root, "src/backend/Modules/Story/Tooba.Story.Application/Composition/StoryOperation.cs", "namespace Tooba.Story.Application.Composition;");
        AssertNs(root, "src/backend/Modules/Story/Tooba.Story.Infrastructure/Directories/StoryDirectory.cs", "namespace Tooba.Story.Infrastructure.Directories;");
        AssertNs(root, "src/backend/Modules/Story/Tooba.Story.Infrastructure/DependencyInjection/StoryModule.cs", "namespace Tooba.Story.Infrastructure.DependencyInjection;");
        AssertNs(root, "src/backend/Modules/Story/Tooba.Story.Infrastructure/Messaging/StoryOutboxRegistration.cs", "namespace Tooba.Story.Infrastructure.Messaging;");

        var appRootCs = Directory.GetFiles(
                Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Application"),
                "*.cs",
                SearchOption.TopDirectoryOnly);
        Assert.Empty(appRootCs);

        var domainRootCs = Directory.GetFiles(
                Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Domain"),
                "*.cs",
                SearchOption.TopDirectoryOnly);
        Assert.Empty(domainRootCs);

        var infraRootCs = Directory.GetFiles(
                Path.Combine(root, "src/backend/Modules/Story/Tooba.Story.Infrastructure"),
                "*.cs",
                SearchOption.TopDirectoryOnly);
        Assert.Empty(infraRootCs);

        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/Story/\">", slnx, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(slnx, "Modules/Story/Tooba.Story.Application/Tooba.Story.Application.csproj"));

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"storyModuleAmc001W3\"", sot, StringComparison.Ordinal);
        Assert.Contains("STORY_CAPABILITY_FOLDERING_APPLIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ROOT_FINAL_CERTIFIED", sot, StringComparison.Ordinal);
    }

    private static void AssertNs(string root, string relative, string expected)
    {
        var text = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        Assert.Contains(expected, text, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var i = 0; (i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0; i += needle.Length)
            count++;
        return count;
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
