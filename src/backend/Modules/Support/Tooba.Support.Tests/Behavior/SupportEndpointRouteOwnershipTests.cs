using Xunit;

namespace Tooba.Support.Tests.Behavior;

/// <summary>Source-level route and capability ownership checks for Support Endpoints.</summary>
public sealed class SupportEndpointRouteOwnershipTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string EndpointsRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Support", "Tooba.Support.Endpoints");

    [Fact]
    public void Customer_seller_admin_routes_and_caps_are_preserved()
    {
        var customer = File.ReadAllText(Path.Combine(EndpointsRoot(), "Customer", "SupportCustomerEndpoints.cs"));
        var seller = File.ReadAllText(Path.Combine(EndpointsRoot(), "Seller", "SupportSellerEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(EndpointsRoot(), "Admin", "SupportAdminEndpoints.cs"));
        var module = File.ReadAllText(Path.Combine(EndpointsRoot(), "SupportEndpointModule.cs"));

        Assert.Contains("MapGroup(\"/v1/customer/support\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/seller/support\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/admin/support\")", module, StringComparison.Ordinal);

        Assert.Contains("MapGet(\"/tickets\"", customer, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/tickets\"", customer, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/tickets/{ticketId:guid}\"", customer, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/tickets/{ticketId:guid}/replies\"", customer, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/tickets/{ticketId:guid}/close\"", customer, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/tickets/{ticketId:guid}/reopen\"", customer, StringComparison.Ordinal);
        Assert.Contains("Idempotency-Key", customer, StringComparison.Ordinal);

        Assert.Contains("\"support.view\"", seller, StringComparison.Ordinal);
        Assert.Contains("\"support.create\"", seller, StringComparison.Ordinal);
        Assert.Contains("\"support.reply\"", seller, StringComparison.Ordinal);
        Assert.Contains("Idempotency-Key", seller, StringComparison.Ordinal);

        Assert.Contains("\"support.view\"", admin, StringComparison.Ordinal);
        Assert.Contains("\"support.manage\"", admin, StringComparison.Ordinal);
        Assert.Contains("MapPatch(\"/tickets/{ticketId:guid}\"", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/demo-preview\"", admin, StringComparison.Ordinal);
        Assert.Contains("IsDevelopment()", admin, StringComparison.Ordinal);
        Assert.Contains("Results.NotFound()", admin, StringComparison.Ordinal);
        Assert.Contains("Idempotency-Key", admin, StringComparison.Ordinal);
        Assert.Contains("IsInternalNote", admin, StringComparison.Ordinal);
    }
}
