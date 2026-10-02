using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-STORY-AMC-001-W4 — Story Result&lt;T&gt; / ApiResponseFactory pipeline.</summary>
public sealed class StoryModuleAmcW4ResultGuardTests
{
    [Fact]
    public void Story_handlers_return_Result_and_endpoints_use_ApiResponseFactory_From()
    {
        var root = Repo();
        var operation = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Story/Tooba.Story.Application/Stories/Composition/StoryOperation.cs"));
        Assert.Contains("SemanticException", operation, StringComparison.Ordinal);
        Assert.Contains("Result.Failure", operation, StringComparison.Ordinal);

        var adminCommands = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Story/Tooba.Story.Application/Stories/Commands/Admin/AdminStoryCommands.cs"));
        Assert.Contains("IRequest<Result<AdminStorySnapshot>>", adminCommands, StringComparison.Ordinal);
        Assert.Contains("StoryOperation.ExecuteAsync", adminCommands, StringComparison.Ordinal);

        foreach (var relative in new[]
                 {
                     "src/backend/Modules/Story/Tooba.Story.Endpoints/Admin/StoryAdminEndpoints.cs",
                     "src/backend/Modules/Story/Tooba.Story.Endpoints/Seller/StorySellerEndpoints.cs",
                     "src/backend/Modules/Story/Tooba.Story.Endpoints/Storefront/StoryStorefrontEndpoints.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            Assert.Contains("api.From(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json(await sender.Send", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (Exception ex) when (ex is SemanticException", text, StringComparison.Ordinal);
        }

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"storyModuleAmc001W4\"", sot, StringComparison.Ordinal);
        Assert.Contains("STORY_RESULT_PIPELINE_APPLIED", sot, StringComparison.Ordinal);
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
