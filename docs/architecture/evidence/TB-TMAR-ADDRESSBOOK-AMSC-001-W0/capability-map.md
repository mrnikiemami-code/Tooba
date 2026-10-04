# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Capability Map

## Business capabilities owned by AddressBook

| Capability | Responsibility | Owner layer | Artifacts |
| --- | --- | --- | --- |
| **Customer address book** (single capability) | Private per-customer delivery/recipient addresses: create, read, list, update, delete, set-default; owner-scoped uniqueness of the default address | `Domain` (`CustomerAddress` aggregate) + `Infrastructure` (`AddressBookDirectory`) | 6 CQRS requests, 5 validators, 1 aggregate, 1 DbContext/schema, 2 migrations |

The module has **one** business capability. There is no Admin/Seller audience, no second aggregate, no
second persistence concern.

## Audience map (Endpoints)

| Audience folder | Routes | Requests |
| --- | --- | --- |
| `Endpoints/Customer/` | `GET /v1/customer/addresses` | `ListCustomerAddressesQuery` |
| `Endpoints/Customer/` | `GET /v1/customer/addresses/{addressId:guid}` | `GetCustomerAddressQuery` |
| `Endpoints/Customer/` | `POST /v1/customer/addresses` | `CreateCustomerAddressCommand` |
| `Endpoints/Customer/` | `PUT /v1/customer/addresses/{addressId:guid}` | `UpdateCustomerAddressCommand` |
| `Endpoints/Customer/` | `DELETE /v1/customer/addresses/{addressId:guid}` | `DeleteCustomerAddressCommand` |
| `Endpoints/Customer/` | `POST /v1/customer/addresses/{addressId:guid}/default` | `SetDefaultCustomerAddressCommand` |

Route count: **6**. Host-owned AddressBook routes: **0**.

## Contracts map (module boundary)

| Contract | Consumer | Direction |
| --- | --- | --- |
| `Contracts/Dtos/CustomerAddressRecord` | Order (checkout imaging), CustomerProfile (dashboard count context) | outbound |
| `Contracts/Ports/IAddressBookCheckoutLookup` | `Order.Application/Storefront/Services/StorefrontCheckoutService`, `StorefrontShippingService` | outbound |
| `Contracts/Ports/IAddressBookCountPort` | `CustomerProfile.Application/Queries/GetCustomerAccountDashboard` | outbound |
| `Contracts/Errors/AddressBookErrorCodes` | AddressBook Endpoints/Application only (module-owned vocabulary) | internal boundary |

## Application-internal (not boundary) map

| Type | Consumer | Why Application-internal |
| --- | --- | --- |
| `Application/Ports/IAddressBookDirectory` | AddressBook handlers + Infrastructure adapter | write/list ownership stays inside the module; it *extends* `IAddressBookCheckoutLookup` so the read port is exposed without exposing writes |
| `Application/Models/CustomerAddressWrite` | AddressBook handlers + Infrastructure adapter | module-internal input; deliberately carries **no** owner id |

## Dependency direction map

```text
Endpoints ──▶ Application ──▶ Domain
    │              │
    │              └──▶ Contracts   (module-owned codes/DTO/ports)
    └──▶ BuildingBlocks.Presentation (ApiResponseFactory, error catalog, localization)
                    │
Infrastructure ─────┴──▶ Application.Ports + Domain + Persistence + ModuleContracts
    └──▶ Order.Contracts.Fulfillment  (StorefrontGuestActor constant only)
```

Foreign edges (both directions) are `*.Contracts` only. Verified in `coupling-and-boundaries.md`.

## Capability → folder target (W2)

Because the module owns exactly one capability, the canonical capability folder is the plural
capability name used by the sibling single-capability modules (`UserPreference.LocalePreferences`,
`Wishlist.Customer`, `BulkInquiry.Storefront`):

```text
Application/Addresses/
  Commands/     (4 requests)
  Queries/      (2 requests)
  Validators/   (5 validators)
```

`Models/`, `Ports/` and the shared `Validators/AddressBookFluentRules.cs` stay as cross-capability
shared folders; W2 will decide with recorded evidence whether `Models`/`Ports` move under
`Addresses/` or remain shared (both are accepted shapes — see `folder-granularity.md`).
