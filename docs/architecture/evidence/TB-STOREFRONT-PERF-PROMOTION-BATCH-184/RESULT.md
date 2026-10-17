# TB-STOREFRONT-PERF-PROMOTION-BATCH-184 — Promotion owner batch read

Parent: TB-STOREFRONT-PERF-QUERY-SCALE-183 (which identified the remaining Home `+1` query per card)
Base-SHA: `47c818686bc0b668eaf8896cc894920e251a4fce`

## 1. Problem restated (from TB-183 evidence)

TB-183 attributed the Home scaling exactly:

| published items | DB commands / request | arithmetic |
| --- | --- | --- |
| 3 | 38 | `35 fixed + 3 × 1` |
| 12 | 47 | `35 fixed + 12 × 1` |

The `1` per card was `StorefrontComposer.ComposeProductAsync` → `ICheckoutPromotionPort.EvaluateForCheckoutAsync` →
`PromotionDirectory.EvaluateAsync` → `_db.Promotions.AsNoTracking().ToListAsync()`.
Every Home card therefore re-read the whole `promotion.promotions` table.

## 2. Ownership decision

* The `+1` was **inside the Promotion owner**, not in Storefront: the consumer already supplies typed
  `CheckoutPromotionEvaluationRequest` and receives a typed `CheckoutPromotionEvaluationResult` through Contracts.
* `ICheckoutPromotionPort` / `CheckoutPromotionEvaluationRequest` / `CheckoutPromotionEvaluationResult` are therefore
  **unchanged** — the Storefront and Order consumer slices and the whole production checkout path are untouched.
* Retrieval, eligibility, ordering, stacking and discount computation stay in Promotion. No Promotion entity and no
  Promotion `DbContext` crosses a module boundary; no cross-module join and no new Host logic.

## 3. What was changed (smallest owner slice)

| File | Change |
| --- | --- |
| `Promotion.Application/Promotions/Ports/IPromotionCandidateSource.cs` (new) | Owner-internal read port: `GetAsync(ct)` + `Invalidate()`. |
| `Promotion.Application/Promotions/Ports/PromotionCandidateSnapshot.cs` (new) | Scoped snapshot: first `GetAsync` performs the single real read, later calls reuse it, `Invalidate()` forces a fresh read. Exposes `LoadCount` for verification. |
| `Promotion.Application/Promotions/PromotionDecisionEngine.cs` (new) | The previous decision logic moved verbatim out of `PromotionDirectory.EvaluateAsync` into a pure function over an already-materialized candidate set. Same expiry/eligibility facts, same `Priority desc, PromotionId asc` ordering, same `Exclusive` vs `Stackable` selection, same `ComputeDiscount`, same `INPUT_INVALID` / `COUPON_NOT_APPLICABLE` / `REDEMPTION_BLOCKED` / `CURRENCY_MISMATCH` rejections, same rounding mode. |
| `Promotion.Infrastructure/Directories/ScopedPromotionCandidateSource.cs` (new) | Adapter binding the snapshot to `PromotionDbContext.Promotions.AsNoTracking()`. |
| `Promotion.Infrastructure/Directories/PromotionDirectory.cs` | `EvaluateAsync` now reads candidates through the source and delegates to the engine; every mutation (`SaveChanges`) in the same scope calls `Invalidate()` so the snapshot can never serve stale definitions. |
| `Promotion.Infrastructure/DependencyInjection/PromotionModule.cs` | `AddScoped<IPromotionCandidateSource, ScopedPromotionCandidateSource>()` — scoped, same lifetime as `DbContext` / `IPromotionDirectory`; no singleton and no process-wide cache. |

Semantics preserved / strengthened:

* The snapshot is **per DI scope (= one HTTP request)**, never global. A later request always re-reads the table.
* Within one request every card, and every checkout line, is evaluated against **one** materialized candidate set —
  strictly more consistent than TB-183 behaviour, where different cards in the same request could observe different
  promotion rows.
* Any create/activate/expire/update in the same scope invalidates the snapshot, so add-then-evaluate sequences in
  admin/promotion flows see their own write.
* No promotion is skipped, no nonzero discount is zeroed, no price is mutated: the decision engine receives exactly
  the same rows the previous code read (read once instead of N times).

## 4. Focused tests (`Tooba.Promotion.Tests`)

Command: `dotnet test src/backend/Modules/Promotion/Tooba.Promotion.Tests/Tooba.Promotion.Tests.csproj`
Result: **44 passed / 0 failed** (31 pre-existing + 13 new in `Behavior/PromotionBatchEvaluationTests.cs`).

New behaviour locks:

* empty promotion set → `0` discount, base amount untouched;
* applicable percentage → single exact discount;
* non-applicable promotion scoped to another seller → no discount;
* expired and not-yet-effective definitions → no discount (time window honoured);
* window boundary: inclusive `EffectiveFrom`, exclusive `EffectiveTo` (existing domain rule re-pinned);
* competing promotions: highest `Priority` exclusive wins; stackables apply in priority order on the remaining amount;
* fixed-amount promotion in a foreign currency → `CURRENCY_MISMATCH`, never a zeroed/incorrect discount;
* blocked redemption → `REDEMPTION_BLOCKED`, no discount;
* invalid input (`quantity <= 0`, negative base, negative quantity) → `INPUT_INVALID` fail-closed;
* **one snapshot load serves 12 consecutive card evaluations** (`LoadCount == 1`);
* **mutation + `Invalidate()` in the same scope re-reads** (`LoadCount == 2`) instead of serving stale rows.

## 5. Runtime A/B — real request-scoped command accounting

Isolation: task-owned disposable PostgreSQL 16 container (`tooba-tb184-pg`, loopback `55504`) + task-owned Host
(`127.0.0.1:5455`), `tooba_184_scratch`, EF command logging enabled, requests correlated by W3C `traceparent` →
request `Activity.TraceId` in the JSON console log (`Scopes[].TraceId`).
No shared `:3000` / `:5088` / `:5432`, no `tooba_alpha`, no shared Host, Redis or RabbitMQ touched.

Fixture: `products=175 published=12 draft=162 active_offers=46 active_prices=47 stock=47 promotions=0`, built through
genuine owner CQRS routes only (`POST /v1/seller/offers`, `PUT /v1/seller/offers/{id}/price`,
`PUT /v1/seller/offers/{id}/inventory`, `POST /v1/admin/products/{id}/publish`). 40 publish attempts, all answered
`200`; publish readiness validators were not bypassed and no SQL was used to publish a product.

### Home `GET /v1/storefront/home`

| tag | published | requests | DB commands per request | payload | sha prefix |
| --- | --- | --- | --- | --- | --- |
| size A | 3 | 3 | **36 / 36 / 36** | 33,962 B | `6633D88419BA` (identical on all 3) |
| size B | 12 | 3 | **36 / 36 / 36** | 51,704 B | `8FDE10CBB4E9` (identical on all 3) |

Attribution: `attributedEfCommands = 288` (= 8 correlated requests × 36), `unattributedEfCommands = 50,842`
(startup migrations/seed and background work, explicitly excluded by trace id). Wall time: size A mean 212.3 ms
(108.6 / 154.5 / 373.8), size B mean 141.3 ms (99.9 / 148.0 / 176.1). `dbCommandMs` is identical within noise
(54.0 vs 55.3) because the promotion read is now amortized, not the dominant cost.

### Before → after

| published items | TB-183 commands | TB-184 commands | delta |
| --- | --- | --- | --- |
| 3 | 38 | 36 | −2 |
| 12 | 47 | 36 | −11 |

The command count is now **flat in the number of published products** (`+9` published items → `0` extra commands),
which is the direct proof that the promotion-driven `+1` per Home card is gone. The 36 baseline contains exactly one
`promotion.promotions` read per Home request instead of one per card.

## 6. Payload parity / no regression

`payload-parity-184.js` → `payload-parity-184.json`:

* size A: 3 distinct products, 10 card occurrences, **0** missing price / seller / currency / stock / slug, `IRR` only;
* size B: 12 distinct products, 37 card occurrences, **0** missing price / seller / currency / stock / slug, `IRR` only;
* non-product sections identical in shape between A and B (`brands 20`, `categories 119`, `bestSellerColumns 4`,
  `homeCategories 16`, `featuredReviews 3`, `latestArticles 4`); product sections grow only through the published
  population (`featuredProducts 3 → 12`, `newArrivals 3 → 8`, `productRail 0 → 4`, `mostViewedProducts 3 → 12`),
  never through a lost field or a changed price;
* `promotionalAmountExclusiveOfTax` still present on `4` cards in both sizes, so promotion-driven pricing fields are
  unchanged and no discount was zeroed;
* each size returned a **byte-identical body** across its three measured requests (sha in §5).

## 7. Remaining release gates

`PERF-CRIT-001` remains **OPEN** in `docs/planning/MUST-FIX-BEFORE-RELEASE.md`. TB-184 closes the promotion N+1, but
closing the gate still requires the architect's acceptance and the user's browser-level responsiveness check on the
Home surface. TB-184 does not close it.

* Wall time on the synthetic fixture is not a substitute for browser acceptance; size A still shows a single 373.8 ms
  outlier that must be read as noise in this 3-sample fixture, not as a certified p95.
* The 36-command baseline itself is out of TB-184 scope (no further decomposition attempted).

## 8. Commands actually run

```text
git rev-parse HEAD                      -> 47c818686bc0b668eaf8896cc894920e251a4fce (== origin/main)
dotnet build Promotion.Infrastructure   -> build succeeded, 0 errors
dotnet build Host.Tests                 -> build succeeded, 0 errors
dotnet test  Promotion.Tests            -> 44 passed / 0 failed
node attribute-commands-184.js          -> per-request 36/36/36 (A) and 36/36/36 (B)
node payload-parity-184.js              -> payload-parity-184.json (0 missing fields both sizes)
```

## 9. Isolation and cleanup

* Removed: container `tooba-tb184-pg`, databases `tooba_184_scratch` / `tooba_184_messaging`, Host process,
  `.tmp-184-run`, `.tmp-184-pw.txt`, `.tmp-184-stage1*.txt`.
* Ports `55504` and `5455` verified free afterwards; no `tooba-tb184-pg` container remains.
* No secret is committed: the scratch password was generated per run, passed by environment variable only, and is
  absent from every file in this directory.
* Untouched: shared `:3000`, `:5088`, `:5432`, `tooba_alpha`, `store-alpha`, Redis, RabbitMQ, `.next`, shared Host,
  unrelated dirty files, and no stash/rebase/reset/force-push was used.
