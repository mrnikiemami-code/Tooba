# TB-SHIPPING-MVP-SHADOW-PARITY-068 — honest shadow parity + policy readiness

Mode: `BOUNDED_EXISTING_DEV_SHIPPING_POLICY_PARITY_PROOF`
Base: `2bca9e0e602c7a025108215edf0986a1a31b7346` (== `origin/main` at claim time)
Status: **PARTIAL** — the shadow gate was genuinely enabled and produced real comparable evidence, the
legacy projection stayed byte-identical, the override was restored and the host is healthy; but one
observed `AmountMismatch` remains (service-vs-option collapse) and one destination-resolution gap could not
be isolated inside the timebox. Neither is called parity.

## 1. Target provenance (no PID assumption)

| Check | Evidence |
| --- | --- |
| process listening on `:5088` | `Tooba.Host.exe` pid 35696 (restored process), `listen=127.0.0.1:5088` |
| environment is Development | `GET /v1/admin/dev-context` → `200 {actorLabel: admin-actor@tooba.local, tenantId: store-alpha}`; that route is mapped only inside `if (app.Environment.IsDevelopment())` |
| host health | `GET /health` → `200 {"status":"ok"}` |
| tenant/edition | `Tooba:Edition=SingleStore`, `Tooba:DeploymentId=dev-singlestore`, single active tenant `store-alpha` |
| database | `Tooba:PostgreSQL:ConnectionReferences:tenant-alpha` → `Database=tooba_alpha` (127.0.0.1:5432); independently confirmed with a read-only `select current_database()` against the container that owns `:5432` (`postgres-db`, postgres:17) → `tooba_alpha` |

The outdated 067 PID was not trusted: the old process (11540) was stopped during the controlled relaunches
and the current process identity was re-proven afterwards. Full log: `log-target-proof.txt`.

## 2. Mission A — real method → service mapping and legacy semantics

Legacy projection method identity is built in `StorefrontShippingService` as
`methodCode = $"{service.Code}:{option.Code}"` and `serviceCode = service.Code`, so **methodCode is not
serviceCode** and must never be equated.

Live DB `fulfillment` shipping-service tree (`GET /v1/admin/shipping-methods`, authorized):

| serviceCode | label | options | projection methodCodes produced |
| --- | --- | --- | --- |
| `post` | پست | `standard` | `post:standard` |
| `tipax` | تیپاکس | `express`, `standard` | `tipax:express`, `tipax:standard` |
| `store_courier` | پیک فروشگاه | — | `store_courier` |
| `in_person` | تحویل حضوری | — | `in_person` |
| (`snapp_courier` is listed in `Tooba:ShippingMethods:EnabledCodes` but has **no row in the DB shipping-service tree**, so it is never offered) | | | |

Legacy quote semantics (`StorefrontShippingCalculator` + `ShippingMethodsOptions`):

- price = `BasePrice`, or `0` when `FreeAboveSubtotal` is set and `cartSubtotal >= threshold`;
- `leadDays` = `rate.LeadDays`; the promise shown to the customer is
  `minimumDeliveryDate = today + max(sellerPreparationDays) + methodLeadDays`;
- `providerKind` is display-only and never reaches the price;
- development host has no `Tooba:ShippingMethods:Rates` section, so the **built-in default** rate table is
  used; `DefaultSellerPreparationDays = 1` with a per-seller override map that sets the observed seller
  (`01a030d1-40cb-7000-8abe-6d31739956c5`) to `3`.

Measured legacy result for the representative cart (subtotal `8702542` IRR, destination البرز/کرج):

| methodCode | serviceCode | amount | leadDays |
| --- | --- | --- | --- |
| `post:standard` | `post` | 120 000 | 4 |
| `tipax:express` | `tipax` | 260 000 | 1 |
| `tipax:standard` | `tipax` | 180 000 | 3 |
| `store_courier` | `store_courier` | 75 000 | 1 |
| `in_person` | `in_person` | 0 | 0 |

Shipping-owned resolution precedence (`ShippingRatePolicyEngine`, read-only via
`ShippingRateQuoteLookup` + `ShippingRatePolicyQuoteLookup` seam):

1. policies filtered by `ServiceCode` **and** `CountryId`, then `IsActive`, then `Covers(destination)`;
2. of the covering policies the **deepest scope level wins**;
3. two equally specific covering policies are `ScopeConflict`, never resolved by row order;
4. currency must match the request;
5. an active coverage grant for the same service + destination is **required**, otherwise `NotCovered`
   (fail-closed: an empty grant set is *not* "no restriction");
6. `FreeAboveThreshold` is evaluated by Shipping itself, not by the legacy base price.

Consequences for comparability (specific, not guessed):

- `post`, `store_courier`, `in_person` are **exactly comparable** (one legacy amount per service code).
- `tipax` is **not per-option comparable**: the Shipping policy key is `ServiceCode` only and a duplicate
  scope for the same service is rejected, so one policy can carry only one amount while legacy `tipax`
  exposes two differently priced options. Recorded as a structural mismatch, not forced to parity.
- `snapp_courier` cannot be compared at all: it is configured as enabled but absent from the DB service
  tree, so the storefront never emits it.
- per-seller preparation days cannot be represented by a single platform-wide rate policy.

## 3. Mission B — SYNTHETIC DEVELOPMENT-ONLY fixture ledger

Scope used for every fixture: country `IR` `e3451206-e503-51a8-91ad-afa9ab70e330` / administrative area
`alborz` `1f8050a8-c325-59fa-959f-82d76b5b2bc5` (area level — deliberately **not** the existing 065
city-level rows). All rows were created through the authorized Shipping admin HTTP routes with the
development admin actor; **no SQL write was issued and no business rule was bypassed**.

| service | ratePolicyId | charge | prep | lead | grantId |
| --- | --- | --- | --- | --- | --- |
| `post` | `01a13d65-cebe-7000-8b67-50096b8b8a4d` | 120 000 | 1 | 4 | `01a13d65-cf50-7000-9436-100faa454cc0` |
| `store_courier` | `01a13d65-cf13-7000-b9bb-d73f95c9b65f` | 75 000 | 1 | 1 | `01a13d65-cf6d-7000-89ee-01e58d680d09` |
| `in_person` | `01a13d65-cf25-7000-8064-f76ca9186f76` | 0 | 1 | 0 | `01a13d65-cf7d-7000-a9e5-efcf060f0a90` |
| `tipax` | `01a13d65-cf37-7000-ad21-ac8e9b2c89d7` | 180 000 | 1 | 3 | `01a13d65-cf8e-7000-a8e5-2205f6167d63` |

The amounts are **test fixtures chosen to reproduce the measured legacy amounts** so a comparison is
possible. They are NOT user-approved commercial tariffs and must never be presented as such.

Counts before → after (authorized admin read, plus independent read-only SQL):

| service | rate policies before/after (active) | grants before/after (active) |
| --- | --- | --- |
| `post` | 3/4 (0/1) — the other three are the six identified 065 fixtures, all still inactive | 3/4 (0/1) |
| `tipax` | 0/1 (0/1) | 0/1 (0/1) |
| `store_courier` | 0/1 (0/1) | 0/1 (0/1) |
| `in_person` | 0/1 (0/1) | 0/1 (0/1) |
| `snapp_courier` | 0/0 | 0/0 |

No pre-existing row was modified. Reversibility (the admin surface exposes no delete route, so
deactivation is the strongest available state and an inactive row neither covers nor quotes):

```
PATCH /v1/admin/shipping-services/<service>/rates/<ratePolicyId>/active            {"isActive":false}
PATCH /v1/admin/shipping-services/<service>/coverage-grants/<grantId>/active       {"isActive":false}
```

## 4. Mission C — gate proof, live matrix, mismatch classification

Gate: `StorefrontShippingRateAuthorityOptions` is a startup `IOptions<T>` snapshot, so the temporary
Development-scoped override had to be applied as **process environment variables** on one controlled
relaunch — `Tooba__ShippingRateAuthority__ShadowEnabled=true` and
`Tooba__ShippingRateAuthority__PrerequisitesConfirmed=true`. **No configuration file was created or
modified**, and the restore relaunch carries no override. `IOptions<T>` being a snapshot is itself a
finding: the gate cannot be toggled without a restart.

The exact cart/session used: one disposable Development customer created through canonical
`/v1/auth/register` + `/v1/auth/login`, one canonical cart with one real catalogue line
(`01a13d66-3b5a-7000-b748-bfb2f3680793`), no order, no payment. The same session was reused for the OFF,
ON and post-restore runs.

Whether the shadow actually ran (not inferred from an unchanged projection — the observer logs a stable
code, so a real run is directly observable):

| # | request shape | observer code | candidates | outcome set |
| --- | --- | --- | --- | --- |
| 1 | province only (`البرز`) | `DestinationNotComparable` | 5 | none |
| 2 | province + city text (`البرز` + `کرج`) | `DestinationNotComparable` | 5 | none |
| 3 | validated Geography ids (IR/alborz/karaj) | **`Observed`** | 5 | `AmountMismatch,Match` |
| 4 | malformed city id inside a valid hierarchy | `DestinationNotComparable` | 5 | none |
| 5 | incoherent hierarchy (area ≠ city's parent) | `DestinationNotComparable` | 5 | none |
| 6 | tipax-isolation variant (tipax rate+grant deactivated) | **`Observed`** | 5 | `LegacyAllowedShippingDenied,Match` |

Legacy projection identity — the returned payload is **byte-identical with the gate OFF and ON**, all five
shapes, same length 2322, and identical again after the restore:

| shape | identical(OFF,ON) |
| --- | --- |
| province only | true |
| city text | true |
| validated ids | true |
| malformed city id | true |
| incoherent hierarchy | true |

Mismatch matrix (deduced from the observed outcome sets plus the measured legacy amounts, which is the only
assignment consistent with both):

| service | legacy amount | fixture charge | result | cause |
| --- | --- | --- | --- | --- |
| `post` | 120 000 | 120 000 | Match | — |
| `tipax:standard` | 180 000 | 180 000 | Match | — |
| `tipax:express` | 260 000 | 180 000 | **AmountMismatch** | one policy per `ServiceCode`; the second priced option of the same service cannot be represented |
| `store_courier` | 75 000 | 75 000 | Match | — |
| `in_person` | 0 | 0 | Match | — |

Request 6 is what proves the `tipax` result is a real evaluation and not a skipped one: with tipax coverage
removed, both tipax candidates fail closed (`LegacyAllowedShippingDenied`) while the four single-amount
services still Match.

Eligibility semantics: legacy considered all five methods allowed at this destination, and Shipping agreed
in six of the seven `Observed` evaluations when coverage was seeded — coverage is the only lever, and it is
explicitly fail-closed (an empty grant set means NotCovered, never "unrestricted").
Delivery semantics: not comparable at all — the shadow compares amount + eligibility only. Shipping keeps
`PreparationDays` and `LeadDays` separate while legacy collapses seller preparation into one platform
default plus a per-seller map, so no promise comparison is possible from this seam.

## 5. Mission D — no source change

No source file was changed. The observed `AmountMismatch` is not a defect that can be fixed inside the
allowed paths without changing the Shipping rate-policy **data shape** (adding an option/rate-key
dimension) or silently rewriting pricing — both explicitly out of scope. The
`DestinationNotComparable` result for legacy Persian text is a real observation but its precise internal
cause was **not** isolated inside the timebox, so it is recorded as an open gap rather than guessed at.

Specific blockers / decisions required from the Architect:

1. **Option-level pricing.** Decide whether Shipping rate policy must gain an option dimension, or whether a
   service-level collapse (one published amount per service code) is acceptable. Until then, `tipax`-style
   multi-option services cannot reach amount parity by construction.
2. **Coverage seeding.** Approve explicit coverage grants per service + real destination. No broad country
   grant was inserted; only the single representative area was seeded.
3. **Legacy text resolution gap.** `ResolveLegacyAsync("IR", "البرز", null, null)` returned a non-`Resolved`
   outcome even though the read-only SQL proof shows the country code `IR` is active and the alborz area
   carries the translation label `البرز` (identical UTF-8 bytes `d8a7d984d8a8d8b1d8b2`). The resolver
   genuinely returned a safe non-comparison; the exact step (country match vs area-translation query on the
   request path) was not isolated. The live storefront is unaffected because 067 already makes the frontend
   send validated ids — request 3 is the path that actually compares.
4. **`snapp_courier`.** Config says enabled, the DB service tree has no row; decide which side is wrong.
5. **Per-seller preparation days.** A single platform-wide policy cannot express the per-seller override.

## 6. Mission E — focused regression, restore, elapsed

- Focused regression: 1–3 representative live projections per gate state (5 shapes OFF, 5 shapes ON, 1
  tipax variant, 1 post-restore) — no broad suite, no second scratch environment, no test loops.
- Restore: the override process was stopped and the host relaunched with **no** override env; `GET /health`
  → `200`; the restored process logged **0** observer lines for the same projection (the observer returns
  `Disabled` before touching the logger), which is positive evidence the flags are OFF again. All six
  fixtures were returned to and left in the state recorded in §3; the tipax variant was explicitly
  re-activated and the final admin read was persisted.
- No build was performed (no source change), so the host never held a DLL lock during this task.

Elapsed: ≈ 47 minutes — **over the 45-minute HARD timebox**, reported as such. The overrun came from (a) one
discarded override relaunch (the category-specific log-level override could not be set with the PowerShell
`$env:` syntax because the category name contains dots, which suppressed the very diagnostic that proves the
shadow ran) and (b) the attempted isolation of the legacy-text destination gap.

## 7. Evidence index

```
log-target-proof.txt    target/environment/database proof + read-only SQL confirmation
log-baseline.txt        process, dev-context, /health, per-service rate/grant baseline
log-fixtures.txt        fixture creation, exact IDs, after counts, reversibility recipe
log-override-on.txt     controlled override relaunch (and why a second one was needed)
log-matrix-OFF.txt      projection matrix, gate OFF
log-matrix-ON.txt       projection matrix, gate ON
log-variant-v2.txt      tipax-isolation variant and exact restore
log-parity-matrix.txt   OFF/ON identity table + observer diagnostic excerpt
log-override-restore.txt  restore relaunch, no override
log-restore-verify.txt  post-restore identity + zero observer lines
payloads-OFF.json / payloads-ON.json   raw projection payloads per shape
probe-baseline.ps1 / probe-fixtures.ps1 / probe-matrix.ps1 / probe-variant.ps1
```

Nothing here asserts Shipping or Checkout production readiness. This is synthetic, development-scoped
parity evidence only.
