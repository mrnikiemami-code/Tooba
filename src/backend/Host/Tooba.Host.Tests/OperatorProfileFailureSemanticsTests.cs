using Tooba.BuildingBlocks;
using Tooba.OperatorProfile.Contracts.Errors;
using Xunit;
using DomainProfile = Tooba.OperatorProfile.Domain.OperatorProfile;

namespace Tooba.Host.Tests;

/// <summary>TB-TMAR-HOST-OPERATORPROFILE-AMC-001-R1 — failure-semantics focused proofs.</summary>
public sealed class OperatorProfileFailureSemanticsTests
{
    [Fact]
    public void Domain_rejects_short_display_name_with_stable_profile_rejected_code()
    {
        var ex = Assert.Throws<SemanticException>(() =>
            DomainProfile.Create(Guid.NewGuid(), "ab", null, null, null, DateTimeOffset.UtcNow));
        Assert.Equal(OperatorProfileErrorCodes.ProfileRejected, ex.Error.Code);
    }

    [Fact]
    public void Domain_rejects_empty_actor_with_stable_profile_rejected_code()
    {
        var ex = Assert.Throws<SemanticException>(() =>
            DomainProfile.Create(Guid.Empty, "مدیر تست", null, null, null, DateTimeOffset.UtcNow));
        Assert.Equal(OperatorProfileErrorCodes.ProfileRejected, ex.Error.Code);
    }

    [Fact]
    public void Directory_upsert_does_not_broadly_catch_invalid_operation()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(
            root,
            "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Infrastructure/OperatorProfileDirectory.cs");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("catch (InvalidOperationException)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("message.Contains", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_invalid_operation_is_not_remapped_by_directory_source()
    {
        // Without a broad catch, any unexpected InvalidOperationException from EF/runtime
        // propagates unchanged and cannot become operator.profile.rejected at this boundary.
        var root = FindRepoRoot();
        var text = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Infrastructure/OperatorProfileDirectory.cs"));
        Assert.DoesNotContain("catch (InvalidOperationException)", text, StringComparison.Ordinal);
        Assert.Contains("SemanticException", text, StringComparison.Ordinal);
        Assert.Contains("OperatorProfileErrorCodes.ProfileRejected", text, StringComparison.Ordinal);
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
