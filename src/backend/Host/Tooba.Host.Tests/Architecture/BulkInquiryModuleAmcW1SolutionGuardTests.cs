using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-BULKINQUIRY-AMC-001-W1 — Solution Explorer /Modules/BulkInquiry/ grouping.</summary>
public sealed class BulkInquiryModuleAmcW1SolutionGuardTests
{
    [Fact]
    public void BulkInquiry_projects_are_grouped_under_modules_bulkinquiry()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/BulkInquiry/\">", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/BulkInquiry/Tooba.BulkInquiry.Domain/Tooba.BulkInquiry.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/BulkInquiry/Tooba.BulkInquiry.Contracts/Tooba.BulkInquiry.Contracts.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/BulkInquiry/Tooba.BulkInquiry.Application/Tooba.BulkInquiry.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/BulkInquiry/Tooba.BulkInquiry.Infrastructure/Tooba.BulkInquiry.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/BulkInquiry/Tooba.BulkInquiry.Endpoints/Tooba.BulkInquiry.Endpoints.csproj", slnx, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(slnx, "<Folder Name=\"/Modules/BulkInquiry/\">"));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
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
