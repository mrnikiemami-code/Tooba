# foundation-errors — TB-TMAR-HOST-ERRORS-AMC-001-W1

## Codes added (Foundation-owned)

| Constant | Code | HTTP | Classification |
|---|---|---|---|
| `PlatformEditionUnconfigured` | `platform.edition.unconfigured` | 503 | Platform |
| `PlatformConnectionUnconfigured` | `platform.connection.unconfigured` | 503 | Platform |
| `PlatformResolutionFailed` | `platform.resolution.failed` | 404 | NotFound |

Registered exactly once in `FoundationErrorCatalogContributor`.

## Resources

- `FoundationErrors.resx` (EN) — non-empty for all three codes
- `FoundationErrors.fa.resx` (FA) — non-empty and distinct from EN

## Host-boot catalog necessity (Order de-dupe)

Injecting `IExceptionPresentationService` into middleware surfaces full catalog build on Host startup. Latent duplicate descriptors for Catalog-owned `reservation.policy.initial|retry|max.invalid` were removed from `OrderErrorCatalogContributor` (Catalog remains sole descriptor owner; Order keeps stable codes + FA resources). Deterministic Host-boot necessity for W1; not an Order redesign.
