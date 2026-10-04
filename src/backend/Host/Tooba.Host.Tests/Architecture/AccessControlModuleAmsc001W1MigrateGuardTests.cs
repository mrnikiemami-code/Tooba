using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-ACCESSCONTROL-AMSC-001-W1 (Migrate) — durable cohesion locks:
/// capability-first Models, single shared Validation folder, owned error-code constants,
/// and no command-shaped transport type sharing the CQRS vocabulary.
/// </summary>
public sealed class AccessControlModuleAmsc001W1MigrateGuardTests
{
    private const string ApplicationRelative = "src/backend/Modules/AccessControl/Tooba.AccessControl.Application";

    private static readonly string[] CapabilityModelFiles =
    [
        "Models/AccessOwnerScope.cs",
        "Roles/Models/AccessRoleDto.cs",
        "Roles/Models/CreateRoleRequest.cs",
        "Roles/Models/UpdateRoleRequest.cs",
        "Roles/Models/CloneRoleRequest.cs",
        "Permissions/Models/RolePermissionGrant.cs",
        "Assignments/Models/UserRoleAssignmentDto.cs",
        "Ceiling/Models/SellerCeilingEntryDto.cs",
        "Access/Models/EffectiveAccessDto.cs",
        "Access/Models/EffectivePermissionDto.cs",
        "Access/Models/AccessUserHitDto.cs",
    ];

    [Fact]
    public void AccessControl_models_are_capability_cohesive_and_no_mixed_dump_remains()
    {
        var app = Path.Combine(Repo(), ApplicationRelative.Replace('/', Path.DirectorySeparatorChar));

        Assert.False(
            File.Exists(Path.Combine(app, "Models", "AccessControlDtos.cs")),
            "the mixed Application/Models/AccessControlDtos.cs bundle must never return");

        foreach (var relative in CapabilityModelFiles)
        {
            var path = Path.Combine(app, relative.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), $"missing capability model {relative}");

            var expectedNamespace = "Tooba.AccessControl.Application." +
                                    Path.GetDirectoryName(relative)!.Replace('/', '.').Replace('\\', '.');
            Assert.Contains($"namespace {expectedNamespace};", File.ReadAllText(path), StringComparison.Ordinal);
        }

        // Every remaining file under the shared Application/Models root must be a cross-capability primitive,
        // never a capability-owned DTO or a transport body.
        var sharedModels = Directory.GetFiles(Path.Combine(app, "Models"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["AccessOwnerScope.cs"], sharedModels);
    }

    [Fact]
    public void AccessControl_validation_primitives_live_in_one_shared_Validation_folder()
    {
        var app = Path.Combine(Repo(), ApplicationRelative.Replace('/', Path.DirectorySeparatorChar));

        Assert.False(Directory.Exists(Path.Combine(app, "Exceptions")),
            "single-file Application/Exceptions/ must not return; it belongs in Validation/");
        Assert.False(Directory.Exists(Path.Combine(app, "Validators")),
            "single-file Application/Validators/ must not return; it belongs in Validation/");

        var validation = Path.Combine(app, "Validation");
        Assert.True(Directory.Exists(validation));
        Assert.Equal(
            ["AccessControlException.cs", "AccessControlFluentRules.cs", "AccessControlValidationCodes.cs"],
            Directory.GetFiles(validation, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // Capability validators stay next to their request; the shared folder holds primitives only.
        foreach (var file in Directory.GetFiles(validation, "*.cs", SearchOption.TopDirectoryOnly))
        {
            Assert.Contains(
                "namespace Tooba.AccessControl.Application.Validation;",
                File.ReadAllText(file),
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AccessControl_directory_emits_owned_error_constants_and_no_raw_code_literals()
    {
        var directory = File.ReadAllText(Path.Combine(
            Repo(),
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Directories/AccessControlDirectory.cs"));

        Assert.DoesNotMatch(new Regex(@"""access\.[a-z_]+"""), directory);
        Assert.Contains("AccessControlErrorCodes.", directory, StringComparison.Ordinal);
        Assert.Contains("using Tooba.AccessControl.Contracts.Errors;", directory, StringComparison.Ordinal);

        // Duplicate using directives are a stale-edit smell and must not return.
        var usings = Regex.Matches(directory, @"(?m)^using (?<v>[^;]+);")
            .Select(m => m.Groups["v"].Value.Trim())
            .ToArray();
        Assert.Equal(usings.Length, usings.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AccessControl_has_no_transport_type_shadowing_cqrs_command_vocabulary()
    {
        var app = Path.Combine(Repo(), ApplicationRelative.Replace('/', Path.DirectorySeparatorChar));

        foreach (var file in Directory.GetFiles(app, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("CreateAccessRoleCommand", text, StringComparison.Ordinal);
            Assert.DoesNotContain("UpdateAccessRoleCommand", text, StringComparison.Ordinal);
            Assert.DoesNotContain("CloneAccessRoleCommand", text, StringComparison.Ordinal);
        }

        // The authoritative MediatR requests keep the Command vocabulary exclusively.
        Assert.True(File.Exists(Path.Combine(app, "Roles", "Commands", "CreateRoleCommand.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Roles", "Commands", "UpdateRoleCommand.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Roles", "Commands", "CloneRoleCommand.cs")));
    }

    [Fact]
    public void AccessControl_solution_guard_parser_is_not_stale()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src", "backend", "Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/AccessControl/\">", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("<Folder Name=\"/Modules/\">", slnx, StringComparison.Ordinal);
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
