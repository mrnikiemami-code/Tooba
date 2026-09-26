using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.AddressBook.Endpoints.Customer;
using Tooba.AddressBook.Endpoints.Errors;
using Tooba.AddressBook.Endpoints.Resources;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.AddressBook.Endpoints;

/// <summary>
/// AddressBook HTTP ownership composition.
/// List + Get became module-owned in `TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001`, Create + Update in
/// `TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-001`, and Delete + SetDefault in
/// `TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002`. All six AddressBook routes are module-owned and Host
/// AddressBook HTTP ownership is ZERO; the Host legacy map call is retired.
/// </summary>
public static class AddressBookEndpointModule
{
    /// <summary>Maps the module-owned AddressBook routes (List, Get, Create, Update, Delete, SetDefault).</summary>
    public static IEndpointRouteBuilder MapAddressBookModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/customer/addresses");
        AddressBookCustomerReadEndpoints.MapReads(group);
        AddressBookCustomerWriteEndpoints.MapWrites(group);
        return app;
    }

    /// <summary>
    /// Registers module-owned AddressBook presentation seams. The customer actor resolver is the neutral
    /// actor-authority seam used by module endpoints; it consumes the already-registered shared
    /// <c>ICurrentAuthenticatedUser</c> and does not introduce a second auth/session system.
    /// The AddressBook error catalog contributor and resource set feed the canonical
    /// <c>ApiResponseFactory</c>/<c>IErrorDefinitionCatalog</c> presentation stack — no parallel problem pipeline.
    /// </summary>
    public static IServiceCollection AddAddressBookEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IAddressBookCustomerActorResolver, AddressBookCustomerActorResolver>();
        services.AddSingleton<IErrorCatalogContributor, AddressBookErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, AddressBookErrorResourceSet>();
        return services;
    }
}
