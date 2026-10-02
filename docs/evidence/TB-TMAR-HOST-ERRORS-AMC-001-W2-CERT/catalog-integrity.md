# catalog-integrity — TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT

W1 Host-boot necessity removed Order duplicate descriptors for Catalog-owned reservation.policy codes.

## Composed uniqueness (touched codes)

| Code | Owner | HTTP | Order descriptor |
|---|---|---|---|
| `reservation.policy.initial.invalid` | Catalog | 400 | ABSENT |
| `reservation.policy.retry.invalid` | Catalog | 400 | ABSENT |
| `reservation.policy.max.invalid` | Catalog | 400 | ABSENT |

## Preserved Order behavior

- Stable machine codes remain in `ReservationPolicyErrors`
- Validators still use those codes
- Order FA/resources ownership unchanged for those keys
- No Order/Catalog production edits in this CERT wave

## Regression check

`OrderErrorCatalogContributor.Contribute()` does not emit the three codes.  
Catalog contributor still registers them at 400.
