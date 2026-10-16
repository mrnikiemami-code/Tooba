# TB-SHIPPING-ADMIN-BROWSER-QUOTE-ACCEPTANCE-158 — RESULT

MODE: BOUNDED_PRIVATE_ADMIN_BROWSER_AND_STOREFRONT_QUOTE_ACCEPTANCE
VERDICT: PARTIAL — Deliverable A verified end-to-end in a real authorized private browser; Deliverable B established as non-propagation with code-level call chain; no customer-authenticated quote value could be produced inside the bounded timebox.

## SHAs
- Expected-Base-SHA: `16f8d26ad82ae6f0b54afdf779b94ee6a8f0a6d6` (HEAD == origin/main at claim time)
- Pre-flight: `main`, clean scoped tree, no reset/rebase/autostash/force-push
- Code SHA: unchanged (no product code change; see "Code changes")
- Evidence/Head SHA + origin parity: recorded in the Bridge result and in `RESULT.bridge.txt`

## Execution stack (task-owned, loopback only)
| Resource | Value |
| --- | --- |
| Postgres | disposable container `tooba-tb158-pg`, `127.0.0.1:55476`, db `tooba_158_scratch` / `tooba_158_messaging` |
| Host | private `Tooba.Host.exe` publish at `.tmp-158-host-out`, `http://127.0.0.1:5418` |
| Frontend | private `next dev` on `http://127.0.0.1:3112`, `TOOBA_HOST_ORIGIN=http://127.0.0.1:5418` |
| Migrations | real `Tooba.MigrationRunner apply --all-tenants` on scratch (shipping + geography schemas) |
| Destination fixture | `scratch-geography-fixture-158.sql` (country IR + area/city/service-area chain) |
| Authorization | real product AccessControl bootstrap for the resolved Development Admin actor (platform-admin, 55 grants) |

Isolation guard `isolation-guard.json`: `failClosed=true` — one Active tenant (`store-alpha` -> scratch reference), no `tooba_alpha`, no `tenant-alpha`, no `Port=5432`, no prior scratch ports, `ShippingRateAuthority`/`ShippingOperationalShadow` unset, Host port != 5088, web port != 3000.
Shared `:3000`, `:5088`, `:5432/tooba_alpha`, RabbitMQ/Redis, store-alpha data were never started, migrated, written or restarted. No credentials/tokens/cookies are stored in evidence (`CREDENTIALS_WRITTEN_TO_EVIDENCE=false`).

## Deliverable A — real authorized private browser Admin acceptance: VERIFIED
`browser-acceptance-158.json` holds the raw DOM assertions; screenshots are listed below.

1. Flat list, real Admin session: 7 method rows (`post:express`, `post:standard`, `tipax:express`, `tipax:standard`, `snapp_courier`, `store_courier`, `in_person`), footer «نمایش ۱ تا ۷ از ۷ ردیف», exactly **3 actions per row** and labels `[مشاهده, ویرایش, حذف]` — re-asserted after a full document reload.
2. `tipax:standard` dialog: `admin-shipping-method-detail-tipax:standard`, computed `direction: rtl`, **four non-nested tabs** (`عمومی`, `تعرفه و پوشش`, `برنامه کاری حمل`, `بسته‌بندی و کرایه`), `role=tablist` count 1, `role=tabpanel` count 1, nested tablists inside a panel count **0**.
3. Server binding, never split client-side: dialog context shows `روش یک‌سطحی tipax:standard → سرویس tipax · standard`, and the collapsed shared-metadata block resolves `serviceCode=tipax`, `optionCode=standard` from `GET .../one-level/tipax%3Astandard/binding`.
4. Initial financial state (authoritative GET): header badges `[tipax:standard, فعال, مالی: نیازمند تنظیم]`; the financial section states «هیچ قاعدهٔ نرخ فعالی متعلق به همین روش وجود ندارد؛ فعال‌بودن روش به‌معنای رایگان نبودن است و هیچ مبلغی ساخته نمی‌شود.» — no amount, no zero-as-FREE, activation not equated with quote readiness.
5. Create through the actual UI: `کشور=IR`, `مبلغ هزینه=185000`, `ارز=IRR`, `روزهای آماده‌سازی=1`, `روزهای حمل=3`, submit «افزودن نرخ». Notice: «نرخ «کشور · 11111111… · بدون شرط اندازه — برای همهٔ مرسوله‌ها» ایجاد شد.» Gate flips to «این روش یک قاعدهٔ نرخ اختصاصی و تأییدشده دارد؛ ویرایش همان قاعده امن است.»
6. Edit through the actual UI on the same row (`01a14672-d87d-7000-b9e3-21e8210e6313`): `مبلغ هزینه 185000 -> 205000`, save «ذخیره».
7. Full document reload, then re-open the same dialog: General badge `مالی: تنظیم‌شده`, reason «قاعدهٔ نرخ فعالِ همین روش با مبلغ و ارز اعلام‌شده وجود دارد.», money «مبلغ ثابت اعلام‌شده: ۲۰۵٬۰۰۰ IRR»; Rates & Coverage shows **exactly one** owned row with `۲۰۵٬۰۰۰ IRR` and the same `ratePolicyId` — persisted, authoritative, no stale value, no ambiguous duplicate.
8. Server read-back (same seam as TB-157): after create `FinanciallyConfigured / FixedAmount / IRR / 185000`, after edit `FinanciallyConfigured / FixedAmount / IRR / 205000`, `ruleCount=1`, `reasonCode=shipping.financial.owned_rules_present`.
9. Sibling and legacy isolation: `tipax:express` never inherits — `NeedsConfiguration / ruleCount=0` while the owned sibling rule exists, and `LegacySharedUnresolved / mode=None / ruleCount=0` once a provider-level row exists. A service-level `optionCode=null` row (99,000 IRR) written on the same service leaves `tipax:standard` at its own 185,000 with `activeServiceLevelRuleCount=1` — legacy money is never projected as this method's price.
10. Ambiguity guard: a second same-scope owned write returns `HTTP 400 shipping.rate_policy.scope_duplicate` and `ruleCount` stays 1.

## Deliverable B — genuine customer quote dependency: NON-PROPAGATION ESTABLISHED (no customer-authenticated quote value)
`storefront-quote-provenance-158.md` records the real attempt and the full chain.

- Real HTTP attempt with the Development actor header: `POST /v1/storefront/shipping/projection` -> `HTTP 401 checkout.authentication_required` (body saved in `storefront-shipping-projection-158.json`). The checkout identity gate (`HostOrderStorefrontActor` / `CheckoutIdentityGate`) needs a real customer session; the Development header seam is placement/list-only. No session, cookie or credential was fabricated.
- Code-level chain that prices the customer quote: `ProjectStorefrontShippingHandler` -> `StorefrontShippingService.ProjectAsync` -> `LoadEligibleMethodsAsync` (`IShippingCatalogReader.ListAsync`) -> `TryAddMethod` -> `StorefrontShippingCalculator.ResolveRate(methodCode, ShippingMethodsOptions)` -> `QuotePrice` -> `StorefrontShippingMethodView.PriceAmount` -> `SelectedShippingAmount`. The storefront's one-level integration is label-only (`ApplyOneLevelDisplayNamesAsync` changes `Label` only).
- `Shipping` owner `rate_policies` are not read on that path. Their only storefront consumer is the observe-only shadow: `Tooba:ShippingRateAuthority` (`StorefrontShippingRateAuthorityOptions`, defaults off) + `StorefrontShippingRateAuthorityPolicy` + `StorefrontShippingShadowObserver`/`Comparer`. The isolated overlay leaves the section unset and the isolation guard asserts it.
- Therefore the updated owner rate (205,000 IRR) does **not** reach the customer quote; the quote is priced from `Tooba:ShippingMethods`. Smallest necessary seam (DI binding + gated consumer swap, fail-closed mapping preserved) is documented and **not implemented**: it changes live customer pricing and is not self-authorized.

## Fail-closed visual behaviour observed
Financial-read-unavailable badge «وضعیت مالی: نامشخص» with the explicit unavailable message and no amount; activation and financial readiness shown as separate badges with the explicit «روش فعال بدون تعرفه، رایگان نیست» caveat; provider-level shared identity quarantined in a collapsed non-editable block; legacy service-level row surfaced as `LegacySharedUnresolved` with `ruleCount=0`.

## Code changes
None. Deliverable A reproduced the locked contract with the existing UI; Deliverable B's fix was out of the bounded scope. Only evidence and the task artifact were added.

## Tests / typecheck
Not required — no product code was touched. No broad suites or rebuild loops were run.

## Elapsed time
Within the HARD 45-minute timebox.

## Resources and cleanup evidence
`cleanup-158.txt`: Host PID 29452 stopped (`MATCHED_TASK_OWNED_MARKER=True`), frontend PID 38020 stopped, two stray private Next node trees on port 3112 stopped, disposable container `tooba-tb158-pg` removed, `SHARED_5432_3000_5088_TOUCHED=False`. `.tmp-158-*` build/run directories discarded.

## Screenshots
- `admin-flat-list-authorized-158.png` — 7 flat methods, 3 actions per row, authorized session
- `admin-tipax-standard-general-initial-158.png` — initial `NeedsConfiguration` financial state with no fabricated amount
- `admin-tipax-standard-rates-persisted-205000-158.png` — persisted single owned row at ۲۰۵٬۰۰۰ IRR after full reload

STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
