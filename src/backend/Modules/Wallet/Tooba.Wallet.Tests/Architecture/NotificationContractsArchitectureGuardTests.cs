using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Wallet.Tests.Architecture;

/// <summary>
/// Guard for Tooba.Notification.Contracts introduced by BATCH-002-R1.
/// </summary>
public sealed class NotificationContractsArchitectureGuardTests
{
    private static readonly string[] AllowedFolders = ["Commands", "Copy", "Dtos", "Ports", "Routes"];

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
                || Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string ContractsRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Notification", "Tooba.Notification.Contracts");

    [Fact]
    public void Contracts_has_no_implementation_project_references()
    {
        var csproj = Path.Combine(ContractsRoot(), "Tooba.Notification.Contracts.csproj");
        Assert.True(File.Exists(csproj));
        var doc = XDocument.Load(csproj);
        var refs = doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
        Assert.Empty(refs);
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Persistence", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Contracts_public_types_use_Contracts_namespace_and_physical_layout()
    {
        var rootCs = Directory.EnumerateFiles(ContractsRoot(), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToArray();
        Assert.True(rootCs.Length == 0, "Notification.Contracts root dumping-ground: " + string.Join(", ", rootCs));

        foreach (var dir in Directory.EnumerateDirectories(ContractsRoot()))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj") continue;
            Assert.Contains(name, AllowedFolders);
        }

        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(ContractsRoot(), "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
                continue;
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("TypeForwardedTo", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IServiceProvider", text, StringComparison.Ordinal);

            var ns = Regex.Match(text, @"^namespace\s+([\w.]+)", RegexOptions.Multiline).Groups[1].Value;
            if (string.IsNullOrEmpty(ns) || !ns.StartsWith("Tooba.Notification.Contracts", StringComparison.Ordinal))
                violations.Add($"{file}: ns={ns}");
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
        Assert.True(File.Exists(Path.Combine(ContractsRoot(), "Ports", "INotificationCreationPort.cs")));
        Assert.True(File.Exists(Path.Combine(ContractsRoot(), "Commands", "CreateNotificationCommand.cs")));
    }
}
