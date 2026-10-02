using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.AccessControl.Application.Access.Queries;
using Tooba.AccessControl.Application.Assignments.Commands;
using Tooba.AccessControl.Application.Assignments.Queries;
using Tooba.AccessControl.Application.Assignments.Validators;
using Tooba.AccessControl.Application.Bootstrap.Commands;
using Tooba.AccessControl.Application.Ceiling.Commands;
using Tooba.AccessControl.Application.Ceiling.Queries;
using Tooba.AccessControl.Application.Ceiling.Validators;
using Tooba.AccessControl.Application.Exceptions;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions.Commands;
using Tooba.AccessControl.Application.Permissions.Queries;
using Tooba.AccessControl.Application.Permissions.Validators;
using Tooba.AccessControl.Application.Ports;
using Tooba.AccessControl.Application.Roles.Commands;
using Tooba.AccessControl.Application.Roles.Queries;
using Tooba.AccessControl.Application.Roles.Validators;
using Tooba.AccessControl.Contracts.Enums;
using Tooba.BuildingBlocks;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-TMAR-ACCESSCONTROL-PRECERT-VALIDATORS-001 — direct, in-memory transport-shape tests for the six
/// AccessControl validators plus the 19-request endpoint-reachable inventory.
/// No web host, no database, no business-rule assertions. Business rules (role existence/mutability,
/// code conflict, ceiling, escalation, catalog membership, category existence, assignment uniqueness)
/// remain Application/Domain owned and are intentionally not asserted here.
/// </summary>
public sealed class AccessControlValidatorTests
{
    private const string ValidatorRequiredPresent = "VALIDATOR_REQUIRED_PRESENT";

    private const string NoValidatorRequired = "NO_VALIDATOR_REQUIRED";

    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        ("CreateRoleCommand", ValidatorRequiredPresent),
        ("UpdateRoleCommand", ValidatorRequiredPresent),
        ("CloneRoleCommand", ValidatorRequiredPresent),
        ("AssignRoleCommand", ValidatorRequiredPresent),
        ("SetRolePermissionsCommand", ValidatorRequiredPresent),
        ("SetSellerCeilingCommand", ValidatorRequiredPresent),
        ("ArchiveRoleCommand", NoValidatorRequired),
        ("RemoveAssignmentCommand", NoValidatorRequired),
        ("EnsureAccessControlBootstrapCommand", NoValidatorRequired),
        ("GetEffectiveAccessQuery", NoValidatorRequired),
        ("GetRoleQuery", NoValidatorRequired),
        ("GetRolePermissionsQuery", NoValidatorRequired),
        ("GetSellerCeilingQuery", NoValidatorRequired),
        ("ListAssignmentsQuery", NoValidatorRequired),
        ("ListRolesQuery", NoValidatorRequired),
        ("ListPermissionCatalogQuery", NoValidatorRequired),
        ("ListSellerPermissionCatalogQuery", NoValidatorRequired),
        ("SearchAccessUsersQuery", NoValidatorRequired),
        ("ListScopeResourcesQuery", NoValidatorRequired),
    ];

    private static readonly (string RequestTypeName, Type ValidatorType)[] RequiredValidators =
    [
        ("CreateRoleCommand", typeof(CreateRoleCommandValidator)),
        ("UpdateRoleCommand", typeof(UpdateRoleCommandValidator)),
        ("CloneRoleCommand", typeof(CloneRoleCommandValidator)),
        ("AssignRoleCommand", typeof(AssignRoleCommandValidator)),
        ("SetRolePermissionsCommand", typeof(SetRolePermissionsCommandValidator)),
        ("SetSellerCeilingCommand", typeof(SetSellerCeilingCommandValidator)),
    ];

    [Fact]
    public void CreateRole_transport_shape()
    {
        var validator = new CreateRoleCommandValidator();

        Assert.False(validator.Validate(Command("  ", "valid-code")).IsValid);
        Assert.False(validator.Validate(Command(new string('n', 129), "valid-code")).IsValid);
        Assert.False(validator.Validate(Command("Role", " ")).IsValid);
        Assert.False(validator.Validate(Command("Role", "x")).IsValid);
        Assert.False(validator.Validate(Command("Role", "bad code!")).IsValid);
        Assert.False(validator.Validate(Command("Role", "valid-code", new string('d', 513))).IsValid);

        Assert.True(validator.Validate(Command("Role", "valid-code")).IsValid);
        Assert.True(validator.Validate(Command("Role", "valid_code-9", new string('d', 512))).IsValid);
    }

    [Fact]
    public void UpdateRole_transport_shape()
    {
        var validator = new UpdateRoleCommandValidator();

        Assert.False(validator.Validate(Update(" ")).IsValid);
        Assert.False(validator.Validate(Update(new string('n', 129))).IsValid);

        Assert.True(validator.Validate(Update("Role")).IsValid);
    }

    [Fact]
    public void CloneRole_transport_shape()
    {
        var validator = new CloneRoleCommandValidator();

        Assert.False(validator.Validate(Clone(" ")).IsValid);
        Assert.False(validator.Validate(Clone("Role", " ")).IsValid);
        Assert.False(validator.Validate(Clone("Role", "x")).IsValid);
        Assert.False(validator.Validate(Clone("Role", "bad code!")).IsValid);

        Assert.True(validator.Validate(Clone("Role", "valid-code")).IsValid);
    }

    [Fact]
    public void AssignRole_transport_shape()
    {
        var validator = new AssignRoleCommandValidator();

        Assert.False(validator.Validate(Assign(Guid.Empty, Id)).IsValid);
        Assert.False(validator.Validate(Assign(Id, Guid.Empty)).IsValid);

        Assert.True(validator.Validate(Assign(Id, Id)).IsValid);
    }

    [Fact]
    public void SetRolePermissions_transport_shape()
    {
        var validator = new SetRolePermissionsCommandValidator();

        Assert.False(validator.Validate(SetPermissions(null!)).IsValid);
        Assert.False(validator.Validate(SetPermissions([Grant(" ")])).IsValid);
        Assert.False(validator.Validate(SetPermissions([Grant(new string('p', 129))])).IsValid);

        Assert.True(validator.Validate(SetPermissions([])).IsValid);
        Assert.True(validator.Validate(SetPermissions([Grant("order.handle")])).IsValid);
    }

    [Fact]
    public void SetSellerCeiling_transport_shape()
    {
        var validator = new SetSellerCeilingCommandValidator();

        Assert.False(validator.Validate(SetCeiling(null!)).IsValid);
        Assert.False(validator.Validate(SetCeiling([Entry(" ")])).IsValid);
        Assert.False(validator.Validate(SetCeiling([Entry(new string('p', 129))])).IsValid);

        Assert.True(validator.Validate(SetCeiling([])).IsValid);
        Assert.True(validator.Validate(SetCeiling([Entry("order.handle")])).IsValid);
    }

    [Fact]
    public void Validators_use_stable_machine_readable_codes()
    {
        var validator = new CreateRoleCommandValidator();
        var codes = validator.Validate(Command(" ", "x")).Errors
            .Select(e => e.ErrorCode)
            .ToArray();

        Assert.Contains("accesscontrol.validation.role_name_required", codes);
        Assert.Contains("accesscontrol.validation.role_code_shape", codes);
        Assert.DoesNotContain(codes, c => string.IsNullOrWhiteSpace(c) || c == "validation.failed");
    }

    [Fact]
    public void Endpoint_inventory_is_exactly_19_with_6_required_validators()
    {
        Assert.Equal(19, Manifest.Length);
        Assert.Equal(6, Manifest.Count(x => x.Classification == ValidatorRequiredPresent));
        Assert.Equal(13, Manifest.Count(x => x.Classification == NoValidatorRequired));

        var names = Manifest.Select(x => x.RequestTypeName).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Six_required_validators_resolve_via_foundation_DI_and_13_requests_have_none()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(CreateRoleCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        var appAsm = typeof(CreateRoleCommand).Assembly;

        foreach (var (request, validatorType) in RequiredValidators)
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == request);
            var registered = sp.GetService(typeof(IValidator<>).MakeGenericType(requestType));
            Assert.NotNull(registered);
            Assert.Equal(validatorType, registered!.GetType());
        }

        foreach (var (name, _) in Manifest.Where(x => x.Classification == NoValidatorRequired))
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            Assert.True(
                sp.GetService(typeof(IValidator<>).MakeGenericType(requestType)) is null,
                $"{name} is NO_VALIDATOR_REQUIRED but a validator is registered");
        }
    }

    [Fact]
    public void Validator_files_sit_under_capability_Validators_folders_with_matching_namespaces()
    {
        var applicationRoot = Path.Combine(
            RepoRoot(), "src", "backend", "Modules", "AccessControl", "Tooba.AccessControl.Application");
        Assert.Empty(Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.TopDirectoryOnly));

        foreach (var (_, validatorType) in RequiredValidators)
        {
            var file = Directory
                .EnumerateFiles(applicationRoot, validatorType.Name + ".cs", SearchOption.AllDirectories)
                .Single(path => path.Contains($"{Path.DirectorySeparatorChar}Validators{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
            var relative = Path.GetRelativePath(applicationRoot, file).Replace('\\', '/');
            var dir = Path.GetDirectoryName(relative)!.Replace('\\', '/');
            var expectedNamespace = "Tooba.AccessControl.Application." + dir.Replace('/', '.');
            Assert.Equal(expectedNamespace, validatorType.Namespace);
            Assert.DoesNotContain("/Commands/", "/" + relative + "/", StringComparison.Ordinal);
            Assert.DoesNotContain("/Queries/", "/" + relative + "/", StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validators_only_declare_transport_shape_rules()
    {
        var applicationRoot = Path.Combine(
            RepoRoot(), "src", "backend", "Modules", "AccessControl",
            "Tooba.AccessControl.Application");

        var forbidden = new[]
        {
            "PermissionCatalog", "GetCategoryNamesAsync", "CategoryExistsAsync", "IsDelegable",
            "IAccessControlDirectory", "Escalat",
        };

        foreach (var file in Directory.EnumerateFiles(applicationRoot, "*Validator.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("ValidateAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AccessControlException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ISender", text, StringComparison.Ordinal);
            foreach (var needle in forbidden)
            {
                Assert.DoesNotContain(needle, text, StringComparison.Ordinal);
            }
        }
    }

    private static CreateRoleCommand Command(string name, string code, string description = "d") =>
        new(AccessOwnerScopeKind.Seller, Id, Id, "tenant", name, code, description, "t");

    private static UpdateRoleCommand Update(string name) =>
        new(Id, AccessOwnerScopeKind.Seller, Id, Id, "tenant", name, "d", "t");

    private static CloneRoleCommand Clone(string name, string code = "valid-code") =>
        new(Id, AccessOwnerScopeKind.Seller, Id, Id, "tenant", name, code, "d", "t");

    private static AssignRoleCommand Assign(Guid userId, Guid roleId) =>
        new(AccessOwnerScopeKind.Seller, Id, userId, roleId, Id, "tenant", "t");

    private static SetRolePermissionsCommand SetPermissions(IReadOnlyList<RolePermissionGrant> grants) =>
        new(Id, AccessOwnerScopeKind.Seller, Id, Id, "tenant", grants, "t");

    private static RolePermissionGrant Grant(string permissionId) =>
        new(permissionId, AccessScopeKind.GlobalWithinOwner, null, true);

    private static SetSellerCeilingCommand SetCeiling(IReadOnlyList<SellerCeilingEntryInput> entries) =>
        new(Id, entries, Id, "t");

    private static SellerCeilingEntryInput Entry(string permissionId) =>
        new(permissionId, true, AccessScopeKind.GlobalWithinOwner, null);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }
}
