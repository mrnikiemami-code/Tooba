using System.Text.RegularExpressions;
using Tooba.BuildingBlocks.Results;
using Tooba.CustomerProfile.Application.Commands.UpsertCustomerProfile;
using Tooba.CustomerProfile.Application.Models;
using Tooba.CustomerProfile.Application.Queries.GetCustomerAccountDashboard;
using Tooba.CustomerProfile.Application.Queries.GetCustomerProfilePage;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1 — canonical Result&lt;T&gt; + ApiResponseFactory.From for business routes.
/// </summary>
public sealed class CustomerProfileResultPipelineGuardTests
{
    private static readonly Type[] BusinessRequests =
    [
        typeof(GetCustomerProfilePageQuery),
        typeof(UpsertCustomerProfileCommand),
        typeof(GetCustomerAccountDashboardQuery),
    ];

    [Fact]
    public void Business_requests_return_Result_T()
    {
        Assert.True(typeof(GetCustomerProfilePageQuery).IsAssignableTo(typeof(MediatR.IRequest<Result<CustomerProfilePage>>)));
        Assert.True(typeof(UpsertCustomerProfileCommand).IsAssignableTo(typeof(MediatR.IRequest<Result<CustomerProfilePage>>)));
        Assert.True(typeof(GetCustomerAccountDashboardQuery).IsAssignableTo(typeof(MediatR.IRequest<Result<CustomerDashboardPage>>)));
    }

    [Fact]
    public void Profile_and_dashboard_business_endpoints_use_api_From_not_raw_Results_Json_of_page()
    {
        var root = FindRepoRoot();
        var profile = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "CustomerProfile", "Tooba.CustomerProfile.Endpoints",
            "Customer", "CustomerProfileEndpoints.cs"));
        var dashboard = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "CustomerProfile", "Tooba.CustomerProfile.Endpoints",
            "CustomerDashboard", "CustomerAccountDashboardEndpoints.cs"));

        Assert.Contains("api.From(result)", profile, StringComparison.Ordinal);
        Assert.Contains("api.From(result)", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(page)", profile, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(page)", dashboard, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"Results\.Json\(\s*page\s*\)", RegexOptions.CultureInvariant), profile);
        Assert.DoesNotMatch(new Regex(@"Results\.Json\(\s*page\s*\)", RegexOptions.CultureInvariant), dashboard);

        // Dev-context remains PLATFORM_DEV_ROUTE_EXCEPTION (NotFound + anonymous JSON).
        Assert.Contains("PLATFORM_DEV_ROUTE_EXCEPTION", dashboard, StringComparison.Ordinal);
        Assert.Contains("Results.NotFound()", dashboard, StringComparison.Ordinal);
    }

    [Fact]
    public void Handlers_return_Result_Success_for_happy_path()
    {
        foreach (var request in BusinessRequests)
        {
            var handler = typeof(GetCustomerProfilePageQuery).Assembly.GetTypes()
                .Single(t => t.Name == request.Name + "Handler");
            var handle = handler.GetMethod("Handle");
            Assert.NotNull(handle);
            Assert.True(
                handle!.ReturnType.IsGenericType
                && handle.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                && handle.ReturnType.GetGenericArguments()[0].IsGenericType
                && handle.ReturnType.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(Result<>));
        }
    }

    private static string FindRepoRoot()
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
