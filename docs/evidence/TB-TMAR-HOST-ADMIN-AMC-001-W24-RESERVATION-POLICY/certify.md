# Certify — ReservationPolicy Admin/Seller (W24)

Surface: `/v1/admin/settings/reservation-policy/*` + `/v1/seller/settings/reservation-policy/*`  
Owner: Order (HTTP/CQRS) + Catalog Contracts (persistence)  
Host Admin shells: REMOVED  
Admin count after: 32

## Checks

| Check | Result |
|---|---|
| Module endpoint ownership | PASS — OrderEndpointModule |
| CQRS MediatR | PASS |
| ApiResponseFactory + Order authorizers | PASS |
| Cross-module persistence | PASS — CatalogDbContext only behind Catalog Contracts port |
| Offer access | PASS — Offer.Contracts only |
| Host Admin files removed | PASS |
| HoldPolicy Host still present | EXPECTED BLOCK (out of scope) |
| Durable guard | PASS — HostAdminAmcReservationPolicyGuardTests |
| SoT | PASS — hostAdminAmcReservationPolicy |
| Focused Host tests | PASS (per migrate agent 18/18) |

## Certify verdict

**SURFACE_CERTIFIED_FOR_HOST_EVACUATION** (Order settings capability; not full Order STRUCTURE_CERTIFIED reopen).
