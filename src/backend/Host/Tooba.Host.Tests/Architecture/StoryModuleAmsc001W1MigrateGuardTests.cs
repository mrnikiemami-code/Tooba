using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.Story.Application.Stories.Validators;
using Tooba.Story.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-STORY-AMSC-001-W1 — migrate wave durable guard.
/// Locks the Persian code-documentation standard compliance on the touched Story surface, the
/// bilingual localization of every declared Story validation machine code, the provenance-driven
/// transport validator completion for GetPublicStoriesQuery (locale/market), the canonical typed
/// fault seam and the zero-foreign-coupling boundary preserved by the migrate wave.
/// </summary>
public sealed class StoryModuleAmsc001W1MigrateGuardTests
{
    private const string StoryRootRelative = "src/backend/Modules/Story";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Story.Contracts",
        "Tooba.Story.Domain",
        "Tooba.Story.Application",
        "Tooba.Story.Infrastructure",
        "Tooba.Story.Endpoints",
    ];

    private static readonly string[] AllowedExternalNamespaces =
    [
        "Tooba.Story",
        "Tooba.BuildingBlocks",
        "Tooba.ModuleContracts",
        "Tooba.Persistence",
    ];

    [Fact]
    public void Story_production_xml_documentation_is_persian_per_standard_32()
    {
        // Standard 32 requires strong professional Persian XML on Tooba-owned public members.
        // After stripping XML tags (summary/param/returns openers and closers carry no prose), a
        // production doc line that still has Latin words but no Persian letters is a regression.
        var persian = new Regex(@"[\u0600-\u06FF]", RegexOptions.Compiled);
        var latinWords = new Regex(@"[A-Za-z]{3,}", RegexOptions.Compiled);
        var xmlTags = new Regex(@"<[^>]*>", RegexOptions.Compiled);

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (!line.TrimStart().StartsWith("///", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var prose = xmlTags.Replace(line.Replace("///", string.Empty, StringComparison.Ordinal), string.Empty);
                    if (!latinWords.IsMatch(prose))
                    {
                        continue;
                    }

                    Assert.True(
                        persian.IsMatch(prose),
                        $"English-only XML doc in {Relative(file)}:{i + 1}: {line.Trim()}");
                }
            }
        }
    }

    [Fact]
    public void Every_declared_validation_code_is_localized_bilingually()
    {
        var declared = typeof(StoryValidationCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

        Assert.Equal(10, declared.Length);
        Assert.All(declared, code => Assert.StartsWith("story.", code, StringComparison.Ordinal));

        var en = Read($"{StoryRootRelative}/Tooba.Story.Endpoints/Resources/StoryErrors.resx");
        var fa = Read($"{StoryRootRelative}/Tooba.Story.Endpoints/Resources/StoryErrors.fa.resx");

        foreach (var code in declared)
        {
            Assert.Contains($"name=\"{code}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{code}\"", fa, StringComparison.Ordinal);
        }

        // Every declared stable error code is localized too, and the module resource set owns the prefix.
        var errorCodes = typeof(StoryErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();
        foreach (var code in errorCodes)
        {
            Assert.Contains($"name=\"{code}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{code}\"", fa, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Public_stories_query_validator_closes_the_locale_market_provenance_gap()
    {
        var validators = Read($"{StoryRootRelative}/Tooba.Story.Application/Stories/Validators/StoryValidators.cs");
        Assert.Contains("GetPublicStoriesQueryValidator", validators, StringComparison.Ordinal);
        Assert.Contains("StoryValidationCodes.LocaleInvalid", validators, StringComparison.Ordinal);
        Assert.Contains("StoryValidationCodes.MarketInvalid", validators, StringComparison.Ordinal);

        // Transport validators emit stable machine codes, never user-facing prose.
        Assert.DoesNotContain("WithMessage(", validators, StringComparison.Ordinal);

        // Exactly sixteen transport validators — one per VALIDATOR_REQUIRED endpoint-reachable request.
        Assert.Equal(16, Regex.Matches(validators, @"class \w+Validator : AbstractValidator<").Count);
    }

    [Fact]
    public void Typed_fault_seam_never_classifies_by_message_text()
    {
        var seam = Read($"{StoryRootRelative}/Tooba.Story.Application/Stories/Composition/StoryOperation.cs");
        Assert.Contains("SemanticException", seam, StringComparison.Ordinal);
        Assert.Contains("Task<Result<T>> ExecuteAsync<T>", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", seam, StringComparison.Ordinal);

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message.StartsWith(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message ==", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Story_production_has_zero_foreign_module_edges()
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

        foreach (var project in ProductionProjects)
        {
            var csproj = XDocument.Load(Path.Combine(Repo(), StoryRootRelative, project, $"{project}.csproj"));
            foreach (var reference in csproj.Descendants("ProjectReference")
                         .Select(x => (string?)x.Attribute("Include") ?? string.Empty))
            {
                Assert.True(
                    reference.Contains("Tooba.Story.", StringComparison.Ordinal)
                    || reference.Contains("Tooba.BuildingBlocks", StringComparison.Ordinal)
                    || reference.Contains("Tooba.ModuleContracts", StringComparison.Ordinal)
                    || reference.Contains("Tooba.Persistence", StringComparison.Ordinal),
                    $"foreign project reference in {project}: {reference}");
            }
        }
    }

    [Fact]
    public void SoT_records_the_AMSC_001_W1_migrate_wave()
    {
        var sot = File.ReadAllText(Path.Combine(Repo(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"storyAmsc001W1\"", sot, StringComparison.Ordinal);
        Assert.Contains("MIGRATE_COMPLETE", sot, StringComparison.Ordinal);
        Assert.Contains("STORY_VALIDATION_CODES_LOCALIZED", sot, StringComparison.Ordinal);
    }

    private static IEnumerable<string> ProductionSources(string project)
    {
        var root = Path.Combine(Repo(), StoryRootRelative, project);
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                        && !p.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Repo(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

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
