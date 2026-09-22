using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P10-T004-R1 — payment actor must follow authenticated session (not hard-coded guest).
/// </summary>
public sealed class StorefrontPaymentActorOwnershipTests
{
    [Fact]
    public void Payment_composer_resolves_actor_from_authenticated_session()
    {
        var root = FindRepoRoot();
        var orchestrator = File.ReadAllText(Path.Combine(
            root,
            "src", "backend", "Modules", "Payment", "Tooba.Payment.Application", "Orchestration", "StorefrontPaymentOrchestrator.cs"));
        var authorizer = File.ReadAllText(Path.Combine(
            root,
            "src", "backend", "Host", "Tooba.Host", "Storefront", "HostPaymentStorefrontAuthorizer.cs"));
        Assert.Contains("ResolvePaymentActor", orchestrator, StringComparison.Ordinal);
        Assert.Contains("authenticatedUserId", orchestrator, StringComparison.Ordinal);
        Assert.Contains("CurrentAuthenticatedSession", authorizer, StringComparison.Ordinal);
        Assert.Contains("session.IsAuthenticated", authorizer, StringComparison.Ordinal);
        Assert.Contains("session.UserId", authorizer, StringComparison.Ordinal);
        Assert.DoesNotContain("aaaaaaaa-aaaa-4aaa-8aaa-000000000009", orchestrator, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
