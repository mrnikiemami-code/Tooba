# TB-SHIPPING-MVP-SHADOW-PARITY-068 — summary

**Status: PARTIAL** (not parity, not a production-readiness claim).

Full detail: `docs/planning/shipping/SHIPPING-MVP-SHADOW-PARITY-068.md`.

## What was proven

- Target positively re-proven: Development host on `:5088`, tenant `store-alpha`,
  `GET /v1/admin/dev-context` → 200, `/health` → 200, DB `tooba_alpha` confirmed by configuration **and** by
  an independent read-only `select current_database()`.
- Exact mapping documented: `methodCode = serviceCode:optionCode`, never equal to `serviceCode`. Live DB
  service tree = `post(standard)`, `tipax(express,standard)`, `store_courier`, `in_person`; `snapp_courier`
  is configured enabled but has no row in the service tree.
- The shadow gate was genuinely enabled (process-env override only, no config file touched) and **really
  ran**: the observer logged `Observed` with `candidates=5` for the validated-id request — not inferred from
  an unchanged projection.
- Legacy projection returned **byte-identical** payloads with the gate OFF and ON, for all five request
  shapes, and again after the restore.
- One observed `AmountMismatch` (`tipax:express`, 260 000 legacy vs 180 000 policy) whose cause is the
  service-vs-option collapse, isolated and proven with a tipax-removal variant that turned both tipax
  candidates into `LegacyAllowedShippingDenied` while the four single-amount services kept matching.
- Override restored to OFF, host healthy, all six synthetic fixtures left in the exact documented state.

## What is not proven

- No full parity: `tipax`-style multi-option services cannot reach amount parity while a rate policy is
  keyed by `ServiceCode` only. Needs an Architect decision on the Shipping data shape.
- Delivery/promise semantics are not comparable from this seam (Shipping separates preparation from lead
  days; legacy collapses them into one default plus a per-seller map).
- `ResolveLegacyAsync("IR","البرز",…)` returned non-`Resolved` although the read-only SQL proof shows the
  exact area translation label exists; the precise step was not isolated in the timebox and is recorded as
  an open gap, not a guess. The live storefront is unaffected (the 067 frontend sends validated ids).

## Fixtures (synthetic, DEVELOPMENT-ONLY, area `alborz` scope)

| service | ratePolicyId | charge | grantId |
| --- | --- | --- | --- |
| `post` | `01a13d65-cebe-7000-8b67-50096b8b8a4d` | 120 000 | `01a13d65-cf50-7000-9436-100faa454cc0` |
| `store_courier` | `01a13d65-cf13-7000-b9bb-d73f95c9b65f` | 75 000 | `01a13d65-cf6d-7000-89ee-01e58d680d09` |
| `in_person` | `01a13d65-cf25-7000-8064-f76ca9186f76` | 0 | `01a13d65-cf7d-7000-a9e5-efcf060f0a90` |
| `tipax` | `01a13d65-cf37-7000-ad21-ac8e9b2c89d7` | 180 000 | `01a13d65-cf8e-7000-a8e5-2205f6167d63` |

Deactivate via `PATCH /v1/admin/shipping-services/<service>/rates|coverage-grants/<id>/active`
with `{"isActive":false}`. Values are comparison fixtures reproducing *measured legacy* amounts — they are
**not** approved commercial tariffs.

## Cost

No source file changed; no build; 3 controlled stop/relaunch cycles (one discarded after a diagnostic
mistake); elapsed ≈ 47 min, over the 45-minute HARD timebox, reported honestly.
