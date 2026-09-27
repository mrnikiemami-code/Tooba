# Content Disposition Map — TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001

| Old Host type / member | Destination |
| --- | --- |
| `CustomerPanelEndpoints.MapCustomerPanelEndpoints` | `CustomerProfileEndpointModule.MapCustomerProfileModuleEndpoints` |
| `GET /dev-context` | `CustomerAccountDashboardEndpoints.GetDevContext` |
| `GET /dashboard` | `CustomerAccountDashboardEndpoints` → `GetCustomerAccountDashboardQuery` |
| `GET /profile` | `CustomerProfileEndpoints` → `GetCustomerProfilePageQuery` |
| `PUT /profile` | `CustomerProfileEndpoints` → `UpsertCustomerProfileCommand` |
| `CustomerProfileWriteRequest` | `CustomerProfile.Endpoints.Customer.CustomerProfileWriteRequest` |
| `ResolveActor` | `CustomerAccountActorResolver` (ICurrentAuthenticatedUser + StorefrontGuestActor) |
| `CustomerPanelComposer.ComposeDashboardAsync` | `GetCustomerAccountDashboardQueryHandler` |
| `CustomerPanelComposer.ComposeProfileAsync` | `GetCustomerProfilePageQueryHandler` |
| `CustomerPanelComposer.UpsertProfileAsync` | `UpsertCustomerProfileCommandHandler` → `ICustomerProfileDirectory` |
| `CustomerDashboardPage` | `CustomerProfile.Application.Models.CustomerDashboardPage` (RecentOrders = `CustomerOrderListItemDto`) |
| `CustomerProfilePage` | `CustomerProfile.Application.Models.CustomerProfilePage` |
| Order Application query usage | `ICustomerOrderDashboardSummaryPort` / Order.Infrastructure adapter |
| `IWishlistDirectory.CountAsync` | `IWishlistCountPort` |
| `IAddressBookDirectory.CountAsync` | `IAddressBookCountPort` |
| Hard-coded `مشتری توبا` / dev label | `CustomerAccountPresentation.resx` |
| Host DI `CustomerPanelComposer` | Removed; `AddCustomerProfileEndpointPresentation` |
| Host files | **DELETED** |
