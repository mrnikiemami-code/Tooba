using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Tooba.AddressBook.Endpoints;

/// <summary>
/// FOUNDATION SHELL ONLY — AddressBook HTTP ownership composition.
/// This task creates the module boundary only. It intentionally maps ZERO routes:
/// all six current AddressBook routes are still Host-owned in
/// <c>Tooba.Host/AddressBook/AddressBookEndpoints.cs</c> and must not be switched yet.
/// The <see cref="MapAddressBookModuleEndpoints"/> extension is a temporary
/// FOUNDATION_ONLY_NO_ROUTES placeholder so a later bounded slice can move route ownership
/// without changing its public shape.
/// </summary>
public static class AddressBookEndpointModule
{
    /// <summary>
    /// FOUNDATION_ONLY_NO_ROUTES — registers no AddressBook routes in this task.
    /// Program.cs must not call this yet; Host keeps all six route mappings.
    /// </summary>
    public static IEndpointRouteBuilder MapAddressBookModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }

    /// <summary>
    /// FOUNDATION_ONLY_NO_ROUTES — module-owned presentation registration hook.
    /// Intentionally registers nothing yet; error catalog / admin grid normalizer / authorizer seams
    /// are added by the later bounded AddressBook CQRS+endpoints slice.
    /// </summary>
    public static IServiceCollection AddAddressBookEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
