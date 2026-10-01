# Analyze — Host/Reviews AMC-001

## Target

`src/backend/Host/Tooba.Host/Reviews/` (2 files: ReviewEndpoints.cs, ReviewPanelComposer.cs)

## True ownership

| Responsibility | Owner |
|---|---|
| Review aggregate + schema | Reviews.Domain / Infrastructure |
| Use cases (storefront/customer/seller/admin) | Reviews.Application CQRS |
| HTTP routes | Reviews.Endpoints |
| Seller product scope via Offers | Offer.Contracts `IOfferSellerProductIdLookup` |
| Product titles | Catalog.Contracts `ICatalogAdminProductTitleIdLookup` |
| Admin auth | `IReviewsAdminAuthorizer` → `IAdminPanelAccess` |
| Seller auth | Host `HostReviewsSellerAuthorizer` → `ISellerPanelAccess` |
| Host | composition + thin seller authorizer only |

## Coupling / blockers

1. Host endpoints used `AdminPanelAccess` + `CurrentAuthenticatedSession` + `ICatalogLookupGateway` (Application) + `ListSellerOffersQuery` (Offer.Application).
2. Message-parsing residue on submit duplicate (`قبلاً`).
3. Module missing Endpoints; Application flat contracts (FOUNDATION_PARTIAL).

## Final disposition

`READY_TO_MIGRATE` → evacuate Host folder to module Endpoints/CQRS (HOST_ZERO).
