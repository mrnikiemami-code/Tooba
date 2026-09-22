# cart-error-code-map

Exact machine-code → public SemanticError mapping (`CartExceptionMapper.TryMapExact`).

| Exact message | Public code |
|---|---|
| cart.missing | cart.missing |
| cart.guest.invalid | cart.guest.invalid |
| cart.guest_secret.invalid | cart.guest.invalid |
| cart.access.denied | cart.access.denied |
| cart.version.conflict | cart.version.conflict |
| cart.version.stale | cart.version.conflict |
| cart.expired | cart.expired |
| cart.quantity.invalid | cart.quantity.invalid |
| cart.line.quantity_positive | cart.quantity.invalid |
| cart.line.quantity_ceiling | cart.quantity.invalid |
| cart.quantity_policy.missing | cart.quantity.invalid |
| offer.min_quantity.not_met | cart.quantity.invalid |
| offer.max_quantity.exceeded | cart.quantity.invalid |
| cart.line.missing | cart.line.missing |
| cart.offer.unavailable | cart.offer.unavailable |
| cart.offer.missing | cart.offer.unavailable |
| cart.offer.inactive | cart.offer.unavailable |
| cart.offer.channel_mismatch | cart.offer.unavailable |
| cart.inventory.insufficient | cart.inventory.insufficient |
| cart.inventory.missing | cart.inventory.insufficient |
| cart.inventory.stale | cart.inventory.stale |
| cart.rejected | cart.rejected |
| cart.line.requires_active | cart.rejected |
| cart.converted.not_expirable | cart.rejected |
| checkout.authentication_required | checkout.authentication_required |

Default: no map → exception propagates (not cart.rejected).

HTTP catalog status mapping unchanged via `CartErrorCatalogContributor`.
