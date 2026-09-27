# Boundary Map — TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001

```text
CustomerProfile.Endpoints
  ├─ CustomerAccountActorResolver  → BuildingBlocks.Security + Order.Contracts.Fulfillment.StorefrontGuestActor
  ├─ CustomerProfileEndpoints      → ISender (Application CQRS)
  └─ CustomerAccountDashboardEndpoints → ISender (Application CQRS)

CustomerProfile.Application (presentation composition handlers)
  ├─ ICustomerProfileDirectory     → CustomerProfile.Contracts
  ├─ ICustomerOrderDashboardSummaryPort → Order.Contracts
  ├─ IWishlistCountPort            → Wishlist.Contracts
  ├─ IAddressBookCountPort         → AddressBook.Contracts
  ├─ IIdentityContactLookup        → Identity.Contracts
  └─ ICustomerAccountDisplayTexts  → Endpoints resources

Order.Infrastructure
  └─ CustomerOrderDashboardSummaryAdapter → Order Application store/composer (Order-owned)

Wishlist.Infrastructure / AddressBook.Infrastructure
  └─ Directory implements Contracts count port
```

## Proof: no foreign Application from Endpoints
Guards scan CustomerProfile.Endpoints + Application for foreign `.Application` / Infrastructure / Domain usings and persistence types.

## Dashboard Contracts-only diagram
```text
GET /dashboard
  → GetCustomerAccountDashboardQuery
      → Order.Contracts summary
      → Wishlist.Contracts count
      → AddressBook.Contracts count
      → CustomerProfile.Contracts snapshot
      → display texts (Endpoints resx)
```
