# worker-context-trust — TB-TMAR-HOST-OUTBOX-AMC-001

## FromOutbox (SingleStore)

- `message.TenantId` selects registry tenant; must exist + Active.
- ConnectionReference / edition / deployment from **registry**, never from message payload.
- Host headers irrelevant.
- Classification: **ANTI_SPOOF_REGISTRY_AUTHORITY**.

## FromOutbox (Marketplace)

- Tenant null; MarketplaceConnectionReference from registry or throw.

## FromPollTarget

- Uses target Edition/DeploymentId/ConnectionReference then re-looks up Active tenant in registry for SingleStore.
- Divergence window between enumeration and dispatch possible if tenant disabled mid-loop → throw fail-closed.
- Persian exception prose on fail: **HARDCODED_OPERATOR_RUNTIME_TEXT_DEBT** (internal, not user-facing HTTP).

## WorkerStoreCommerceContextFactory

- Marketplace → `DeploymentStoreCommerce`.
- SingleStore → Active tenant `StoreCommerce` record.
- No Market/Currency/SalesChannel defaults.
- Contracts: `Tooba.StoreContext.Contracts.Current` only.

## Poll targets vs MultiTenancy / Health

- Outbox SingleStore: **Active tenants only** (same family as MultiTenancy allowlist for serving).
- Health may probe broader configured set — intentional difference (dispatch only Active).
- Marketplace: one target when MarketplaceConnectionReference present.
