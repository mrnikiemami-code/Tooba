using Tooba.Localization.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-LOCALIZATION-AMSC-001-W1 — migrate wave durable guard.
/// Locks the canonical fault typing, transport-validation codes, resource-backed error
/// localization and the zero-foreign-coupling boundary established by the migrate wave.
/// </summary>
public sealed class LocalizationModuleAmsc001W1MigrateGuardTests
{
    private static readonly string[] ProductionProjects =
    [
        "Tooba.Localization.Contracts",
        "Tooba.Localization.Domain",
        "Tooba.Localization.Application",
        "Tooba.Localization.Infrastructure",
        "Tooba.Localization.Endpoints",
    ];

    private static readonly string[] AllowedExternalNamespaces =
    [
        "Tooba.Localization",
        "Tooba.BuildingBlocks",
        "Tooba.ModuleContracts",
        "Tooba.Persistence",
    ];

    [Fact]
    public void Operation_seam_maps_typed_codes_and_never_parses_message_text()
    {
        var text = Read("src/backend/Modules/Localization/Tooba.Localization.Application/Composition/LocalizationOperation.cs");

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("SemanticException", text, StringComparison.Ordinal);
        Assert.Contains("LanguageErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Contains(ex.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Declared_code_catalog_is_the_single_known_code_source()
    {
        Assert.True(LanguageErrorCodes.IsKnown(LanguageErrorCodes.NotFound));
        Assert.True(LanguageErrorCodes.IsKnown(LanguageErrorCodes.Inactive));
        Assert.False(LanguageErrorCodes.IsKnown("content.publish.check.body"));
        Assert.False(LanguageErrorCodes.IsKnown(null));
        Assert.False(LanguageErrorCodes.IsKnown(" "));
    }

    [Fact]
    public void Validators_emit_transport_codes_and_never_business_codes()
    {
        var validators = new[]
        {
            "CreateLanguageCommandValidator.cs",
            "UpdateLanguageCommandValidator.cs",
            "PatchLanguageCommandValidator.cs",
        };

        foreach (var file in validators)
        {
            var text = Read($"src/backend/Modules/Localization/Tooba.Localization.Application/Languages/Validators/{file}");
            Assert.Contains("LocalizationValidationCodes", text, StringComparison.Ordinal);
            Assert.DoesNotContain("LanguageErrorCodes", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validation_codes_are_not_registered_as_error_catalog_descriptors()
    {
        var contributor = Read("src/backend/Modules/Localization/Tooba.Localization.Contracts/Errors/LocalizationErrorCatalogContributor.cs");
        Assert.DoesNotContain("LocalizationValidationCodes", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("localization.validation.", contributor, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_code_has_zero_raw_localization_error_code_literals()
    {
        var declared = typeof(LanguageErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

        Assert.Equal(19, declared.Length);

        foreach (var project in ProductionProjects)
        {
            if (project == "Tooba.Localization.Contracts")
            {
                continue;
            }

            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                foreach (var code in declared)
                {
                    Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void Production_code_has_zero_foreign_module_edges()
    {
        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                foreach (var line in File.ReadAllLines(file))
                {
                    var trimmed = line.Trim();
                    if (!trimmed.StartsWith("using Tooba.", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var allowed = AllowedExternalNamespaces.Any(ns =>
                        trimmed.StartsWith($"using {ns}", StringComparison.Ordinal));
                    Assert.True(allowed, $"foreign module edge in {Relative(file)}: {trimmed}");
                }
            }
        }
    }

    [Fact]
    public void Domain_and_application_raise_typed_codes_only()
    {
        foreach (var project in new[] { "Tooba.Localization.Domain", "Tooba.Localization.Application" })
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("InvalidOperationException(", text, StringComparison.Ordinal);
                Assert.DoesNotContain("new Exception(", text, StringComparison.Ordinal);
            }
        }
    }

    private static IEnumerable<string> ProductionSources(string project)
    {
        var root = Path.Combine(Repo(), "src/backend/Modules/Localization", project);
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal))
            .Where(path => !path.EndsWith("DbContextModelSnapshot.cs", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(Repo(), relativePath));

    private static string Relative(string absolute) =>
        absolute.Replace(Repo() + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/');

    private static string Repo()
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
