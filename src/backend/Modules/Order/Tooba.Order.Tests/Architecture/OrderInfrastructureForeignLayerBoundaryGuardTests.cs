using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R2 — durable guard proving the touched
/// Tooba.Order.Infrastructure destination project is genuinely Contracts-only: its project graph and
/// every source file must have ZERO foreign Application/Infrastructure/Domain dependency.
/// No whitelist of current violations.
/// </summary>
public sealed class OrderInfrastructureForeignLayerBoundaryGuardTests
{
    private const string OrderInfrastructure = "src/backend/Modules/Order/Tooba.Order.Infrastructure";

    private static readonly Regex ForeignLayerReference = new(
        @"Tooba\.(?<module>[A-Za-z0-9]+)\.(?<layer>Application|Infrastructure|Domain)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Project_graph_has_zero_foreign_application_infrastructure_or_domain_references()
    {
        var violations = new List<string>();
        foreach (var line in File.ReadLines(RepoFile($"{OrderInfrastructure}/Tooba.Order.Infrastructure.csproj")))
        {
            var include = Regex.Match(line, @"ProjectReference\s+Include=""(?<path>[^""]+)""");
            if (!include.Success)
            {
                continue;
            }

            var reference = include.Groups["path"].Value.Replace('\\', '/');
            var module = Regex.Match(reference, @"Tooba\.(?<module>[A-Za-z0-9]+)\.(?<layer>Application|Infrastructure|Domain)\.csproj");
            if (module.Success && !string.Equals(module.Groups["module"].Value, "Order", StringComparison.Ordinal))
            {
                violations.Add(reference);
            }
        }

        Assert.True(
            violations.Count == 0,
            "foreign Application/Infrastructure/Domain project references: " + string.Join("; ", violations));
    }

    [Fact]
    public void Source_files_have_zero_foreign_application_infrastructure_or_domain_imports_or_fqns()
    {
        var violations = new List<string>();
        foreach (var file in ProductionSources())
        {
            var text = File.ReadAllText(file);
            foreach (Match match in ForeignLayerReference.Matches(text))
            {
                if (string.Equals(match.Groups["module"].Value, "Order", StringComparison.Ordinal))
                {
                    continue;
                }

                violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}:{match.Value}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "foreign Application/Infrastructure/Domain leakage: " + string.Join("; ", violations));
    }

    [Fact]
    public void Payment_contracts_reference_is_explicit_and_direct()
    {
        var csproj = File.ReadAllText(RepoFile($"{OrderInfrastructure}/Tooba.Order.Infrastructure.csproj"));

        Assert.Contains("Tooba.Payment.Contracts.csproj", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Application.csproj", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Contracts_only_boundaries_are_present_for_every_foreign_module_used()
    {
        var csproj = File.ReadAllText(RepoFile($"{OrderInfrastructure}/Tooba.Order.Infrastructure.csproj"));

        foreach (var contract in new[]
                 {
                     "Tooba.Cart.Contracts.csproj",
                     "Tooba.Catalog.Contracts.csproj",
                     "Tooba.Payment.Contracts.csproj",
                     "Tooba.Fulfillment.Contracts.csproj",
                     "Tooba.AccessControl.Contracts.csproj",
                 })
        {
            Assert.Contains(contract, csproj, StringComparison.Ordinal);
        }
    }

    private static IEnumerable<string> ProductionSources() =>
        Directory.EnumerateFiles(RepoFile(OrderInfrastructure), "*.cs", SearchOption.AllDirectories)
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !x.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string RepoFile(string relative) =>
        Path.Combine(RepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
