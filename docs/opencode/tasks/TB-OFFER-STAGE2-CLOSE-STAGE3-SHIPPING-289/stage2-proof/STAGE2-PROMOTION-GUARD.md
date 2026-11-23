# STAGE 2 PROMOTION RUNTIME GUARD — TB-289 (architect decision)

Per `ARCHITECT-PROMOTION-RUNTIME-CLOSURE.md`: historical/cross-owned Promotion memberships are NOT deleted; the
authoritative invariant is **no Archived/not-found Offer may receive a campaign quote from mere membership**.

## Change (Promotion-owned, minimal)
`Tooba.Promotion.Infrastructure/Adapters/CampaignCartPriceAuthority.cs`:
- Injected existing `IOfferLookupGateway` (Offer.Contracts; already registered by OfferModule).
- After store/campaign-runtime/membership checks and BEFORE resolving any campaign price, resolve the Offer and
  fail closed: `if (offer is null || offer.Status != OfferStatus.Active) return null;`.
- No change to campaign schema, price math, Catalog all-Draft, Inventory, Offer 287 fingerprint, AGENTS/Cursor.
- Storefront projection (`MerchandisingCampaignQuery`, ~line 290) already rejects non-Active offers; this closes the
  cart-price edge that previously resolved by membership only. No cached stale read grants a discount; no fallback.

Evidence: source lines (FindOfferAsync / OfferStatus.Active / TB-289 comment). Host build 0 errors,
candidate `Tooba.Host.dll` SHA-256 `8E6196268F25DBC1EE040D07494CBD1FAC5472D88C653F057415D0D17D8831D4`
(`promotion-guard-host-build-summary.txt`).

## Status (truthful)
- C01 runtime guard: **IMPLEMENTED + BUILD-VERIFIED** (source-level fail-closed). Connected cart-price proof
  (Archived Offer + valid membership + active campaign ⇒ no discount; Active Offer ⇒ discount; foreign/invalid/time
  fail closed) was **NOT_RUN** in this session — not executed; not claimed PASS.
- C04 (287 T01–T14), C05 (real VERIFY realOk=true), C07 (Playwright): **NOT_RUN**.
- No fabricated PASS. A final connected run on the backed-up dev env (reset → boot → storefront GET + cart campaign
  quote for archived vs active offer; 287 matrix; VERIFY; Playwright) is still required before STAGE2_READY.
