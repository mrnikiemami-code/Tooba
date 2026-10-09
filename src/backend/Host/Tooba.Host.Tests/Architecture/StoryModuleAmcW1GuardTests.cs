using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-STORY-AMC-001-W1 — Story semantic/localization failure channel.</summary>
public sealed class StoryModuleAmcW1GuardTests
{
    [Fact]
    public void Story_business_failures_use_semantic_codes_not_message_text()
    {
        var root = Repo();
        var mapper = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Story/Tooba.Story.Application/Stories/StoryFailureMapper.cs"));
        Assert.DoesNotContain("ToSemantic", mapper, StringComparison.Ordinal);
        Assert.DoesNotContain("message.Contains", mapper, StringComparison.Ordinal);
        Assert.DoesNotContain("یافت نشد", mapper, StringComparison.Ordinal);
        Assert.DoesNotContain("ناامن", mapper, StringComparison.Ordinal);

        var composer = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Story/Tooba.Story.Application/Stories/Presentation/StoryPresentationComposer.cs"));
        Assert.DoesNotContain("StoryFailureMapper.ToSemantic", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tenant resolve نشده", composer, StringComparison.Ordinal);
        Assert.Contains("StoryErrorCodes.TenantMissing", composer, StringComparison.Ordinal);

        foreach (var relative in new[]
                 {
                     "src/backend/Modules/Story/Tooba.Story.Domain/Aggregates/Story.cs",
                     "src/backend/Modules/Story/Tooba.Story.Domain/Aggregates/StoryItem.cs",
                     "src/backend/Modules/Story/Tooba.Story.Domain/Rules/StoryRules.cs",
                     "src/backend/Modules/Story/Tooba.Story.Infrastructure/Directories/StoryDirectory.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            Assert.Contains("SemanticException", text, StringComparison.Ordinal);
            Assert.Contains("StoryErrorCodes", text, StringComparison.Ordinal);
            Assert.DoesNotContain("یافت نشد", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ناامن", text, StringComparison.Ordinal);
            Assert.Empty(Regex.Matches(text, @"throw new InvalidOperationException\(""[^""]*[\u0600-\u06FF]"));
        }

        var domainCsproj = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Story/Tooba.Story.Domain/Tooba.Story.Domain.csproj"));
        Assert.Contains("Tooba.Story.Contracts", domainCsproj, StringComparison.Ordinal);

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"storyModuleAmc001W1\"", sot, StringComparison.Ordinal);
        Assert.Contains("SEMANTIC_LOCALIZATION_FAILURE_CHANNEL", sot, StringComparison.Ordinal);
        Assert.Contains("\"storyModuleAmc001W2Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("STORY_SEMANTIC_FAILURE_CHANNEL_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ROOT_FINAL_CERTIFIED", sot, StringComparison.Ordinal);
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
