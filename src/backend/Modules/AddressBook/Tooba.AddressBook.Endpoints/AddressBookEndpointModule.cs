using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.AddressBook.Endpoints.Customer;

namespace Tooba.AddressBook.Endpoints;

/// <summary>
/// AddressBook HTTP ownership composition.
/// Read routes (List + Get) are module-owned as of `TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001`;
/// the four write routes (create/update/delete/set-default) are still Host-owned by
/// <c>Tooba.Host/AddressBook/AddressBookEndpoints.cs</c> and will move in a later bounded slice.
/// </summary>
public static class AddressBookEndpointModule
{
    /// <summary>Maps the currently module-owned AddressBook routes (List + Get only).</summary>
    public static IEndpointRouteBuilder MapAddressBookModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/customer/addresses");
        AddressBookCustomerReadEndpoints.MapReads(group);
        return app;
    }

    /// <summary>
    /// Module-owned presentation registration hook. Intentionally registers nothing yet; error catalog /
    /// admin grid normalizer / authorizer seams are added by later bounded AddressBook slices.
    /// </summary>
    public static IServiceCollection AddAddressBookEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
