# TB-SHIPPING-CARRIER-ADAPTER-FOUNDATION-247 — RESULT

Status: **PASS for adapter foundation; runtime activation remains intentionally OFF**.

Implemented a provider-neutral rate request/result, provider plug-in port and case-insensitive registry.
Added the Tipax V4 Pricing adapter with the official `OmPriceDto -> ETipaxPriceMinListDto[]` shape,
OAuth password grant, refresh-token route, cached token, one bounded 401 retry, timeout/cancellation and
typed neutral outcomes. Tipax credentials exist only as private configuration values. The adapter emits
no request when disabled or incompletely configured.

No database schema, migration, public endpoint, storefront change, checkout activation or business-data
write was added. Existing Manual/Free quotes remain unchanged. `CarrierApi` continues to require the
owner configuration, provider mapping and captured quote slices listed in the plan before it can become
an operational checkout mode.

Verification:

- `TipaxV4RateProviderTests`: 4/4 passed (disabled zero-I/O, exact wire/normalization, bounded 401 retry,
  registry extension/duplicate guard).
- Carrier + module structure/composition + captured revision focused set: 27/27 passed.
- Shipping projects compiled with zero new production errors. Existing unrelated warnings remain.

Plan: `docs/planning/shipping/CARRIER-ADAPTER-AND-TIPAX-V4-PLAN-2026-11-02.md`.
