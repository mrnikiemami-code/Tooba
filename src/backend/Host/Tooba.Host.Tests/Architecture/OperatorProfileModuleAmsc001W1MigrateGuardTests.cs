using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-OPERATORPROFILE-AMSC-001-W1 — canonical typed-fault seam + declared-code guard locks.
/// Pins the certified Inventory/Media Operation shape so the module stays microservice-extractable:
/// result mapping stays inside the module, classification is by typed code only, and foreign codes
/// propagate untouched to the global exception boundary.
/// </summary>
public sealed class OperatorProfileModuleAmsc001W1MigrateGuardTests
{
    [Fact]
    public void Error_codes_owner_declares_IsKnown_guard_over_all_six_codes()
    {
        var path = Source("OperatorProfile", "Tooba.OperatorProfile.Contracts/Errors/OperatorProfileErrorCodes.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("public static bool IsKnown(string? code)", text, StringComparison.Ordinal);
        foreach (var code in new[]
                 {
                     "ProfileRejected", "ActorRequired", "InvalidDisplayName",
                     "InvalidFirstName", "InvalidLastName", "InvalidBio",
                 })
        {
            Assert.Contains(code + ',', text, StringComparison.Ordinal);
        }

        Assert.Contains("\"operator.profile.rejected\"", text, StringComparison.Ordinal);
        Assert.Contains("\"operator.profile.validation.actor_required\"", text, StringComparison.Ordinal);
        Assert.Contains("\"operator.profile.validation.display_name\"", text, StringComparison.Ordinal);
        Assert.Contains("\"operator.profile.validation.first_name\"", text, StringComparison.Ordinal);
        Assert.Contains("\"operator.profile.validation.last_name\"", text, StringComparison.Ordinal);
        Assert.Contains("\"operator.profile.validation.bio\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Operation_seam_maps_both_typed_fault_mechanisms_by_declared_code_only()
    {
        var text = File.ReadAllText(Source(
            "OperatorProfile",
            "Tooba.OperatorProfile.Application/Composition/OperatorProfileOperation.cs"));

        Assert.Contains(
            "catch (ContractOperationException ex) when (OperatorProfileErrorCodes.IsKnown(ex.Code))",
            text,
            StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", text, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)", text, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync(Func<Task> action)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", text, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (Exception", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Handler_failure_logs_carry_stable_code_through_structured_placeholder()
    {
        var command = File.ReadAllText(Source(
            "OperatorProfile",
            "Tooba.OperatorProfile.Application/Admin/Commands/UpsertOperatorProfileCommand.cs"));
        var query = File.ReadAllText(Source(
            "OperatorProfile",
            "Tooba.OperatorProfile.Application/Admin/Queries/GetOperatorProfileQuery.cs"));

        Assert.Contains(
            "logger.LogInformation(\"{OperatorProfileUpsertEvent}\", OperatorProfileErrorCodes.ProfileRejected)",
            command,
            StringComparison.Ordinal);
        Assert.Contains("logger.LogInformation(\"operator.profile.upsert.succeeded\")", command, StringComparison.Ordinal);
        Assert.Contains(
            "logger.LogInformation(\"{OperatorProfileGetEvent}\", OperatorProfileErrorCodes.ProfileRejected)",
            query,
            StringComparison.Ordinal);
        Assert.Contains("logger.LogInformation(\"operator.profile.get.succeeded\")", query, StringComparison.Ordinal);
    }

    private static string Source(string module, string relative) =>
        Path.Combine(
            RepoRoot(),
            "src", "backend", "Modules", module,
            relative.Replace('/', Path.DirectorySeparatorChar));

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
