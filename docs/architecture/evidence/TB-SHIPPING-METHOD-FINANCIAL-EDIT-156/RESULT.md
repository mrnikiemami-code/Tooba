# TB-SHIPPING-METHOD-FINANCIAL-EDIT-156 — result

Status: **PARTIAL** — STOP_FOR_ARCHITECT
Base SHA: `c752e77bb8471b28ea4d3951b2a77d276bd8412d`
Implementation SHA: `5832aa683f57c1b4c52716ac982216f2b99d0f15` (evidence folder in the follow-up evidence commit)
Channel: `tooba-main` · Worker: `tooba-worker-01` · Mode: BOUNDED_REAL_METHOD_SCOPED_FIXED_IRR_EDITOR_AND_ISOLATED_READ_WRITE_PROOF

## Why PARTIAL and not PASS

The task sets the bar explicitly: *"PASS only when real method-owned fixed IRR can be saved, read back and
shown in a private authorized runtime, with sibling isolation and safe failure proven; otherwise
PARTIAL/BLOCKED, not a cosmetic PASS."*

The **private authorized runtime (isolated Host + scratch DB + private frontend + authorized Admin
browser)** was **not** built in this session, so the HTTP-framing and visuals of the new/changed surfaces
are not proven live. Everything that could be proven with the real owner code and the real write seam
**was** proven, and is listed below. No live HTTP status, JSON sample or screenshot is claimed.

### Precise blocker for Mission 2 (honest, no shortcut taken)

* TB-154's isolated runtime was fully torn down after that task (`docs/architecture/evidence/TB-SHIPPING-GROUP-UX-ACCEPTANCE-154/cleanup-154.txt`):
  `.tmp-154-run/`, `.tmp-154-host-out/` and the scratch container no longer exist (verified: `Test-Path`
  returns nothing, and only `rabbitmq` + `postgres-db` run in Docker). Nothing was reusable, so the whole
  recipe would have to be rebuilt from zero inside a 45-minute hard box.
* The rebuild is not a small step: publish an isolated Host, start a scratch PostgreSQL, seed
  tenant/edition, seed the one-level shipping catalog, seed an **admin actor whose `tenant#view` and
  `fulfillment.view` authorization decisions resolve to Allow**, then a private Next frontend and a
  browser session. Admin authorization in the real Host only honours the Development dev-actor header
  (`AdminPanelAccess.DevActorHeader`) and still requires real authorization tuples, so the scratch DB
  needs an authorization seed that no current script provides.
* The shared `:3000` / `:5088` stack and `tooba_alpha` / `store-alpha` were **left untouched**: they were
  not restarted, not written to, and they run a build that predates TB-155, so they could not have
  produced honest evidence for this route anyway.

Accordingly Mission 2 is reported **PARTIAL** with the verified slice below; a live acceptance run is the
narrow next step.

## Mission 1 — method-scoped fixed-amount editor (COMPLETE, with reuse, no overlapping endpoint)

**Reuse decision (as required).** Inspection showed the existing Shipping-owned write API **already**
stores a method+option-specific fixed rule: `POST/PUT /v1/admin/shipping-services/{serviceCode}/rates`
with `optionCode` taken from the authoritative server binding, and the one-level Rates & Coverage tab
already passes `resolvedBinding.optionCode` (never a client-side split of `methodCode`). No overlapping
endpoint, command or contract was added, and no schema/migration change was needed.

What was actually missing was **safe write semantics**: the tab would happily let an operator edit or add
a rule on top of an ambiguous or legacy-shared money state. That is what this task implements.

New: `src/frontend/app/admin/shipping-services/shipping-method-financial-edit-gate.ts` — a pure,
React-free gate over the verified TB-155 status:

| Verified status | Gate | Create | In-place edit |
| --- | --- | --- | --- |
| `NeedsConfiguration` | `create-allowed` | yes (this is the fix) | n/a |
| `FinanciallyConfigured` (single currency, no duplicate scope) | `edit-allowed` | yes | yes |
| `LegacySharedUnresolved` | `legacy-shared` | yes, as a **new owned rule** | **no** (shared rows are not this method's tariff and are never edited/inherited) |
| `Conflicting` (`ambiguous_owned_scope`) | `blocked-conflict` | **no** | **no** — actionable text: deactivate/remove one duplicate row first |
| `Conflicting` (`mixed_currency`) | `blocked-conflict` | **no** | **no** |
| unreadable / unknown status (`null`) | `blocked-unverified` | **no** | **no** |

Wired into `shipping-one-level-rates-tab.tsx`:

* the gate panel (`data-testid=shipping-one-level-financial-edit-gate`, `data-gate=<kind>`) states the
  reason in Persian and, when blocked, the required action;
* `افزودن نرخ` / `افزودن مجوز پوشش` are disabled and refuse in the handler when the gate does not allow
  create; `ذخیره` / `ویرایش` are disabled and refuse when it does not allow in-place edit;
* the activate/deactivate controls stay **enabled** — deactivating a duplicate is precisely the repair
  path out of `blocked-conflict`;
* the script re-reads the TB-155 status after every successful create/edit/activation so the displayed
  amount and financial state come from the server, not from local state;
* a legitimately stored `0` amount is labelled as a stored data amount
  (`ZERO_AMOUNT_NOTE_FA`), never as a free mode;
* an unverified status fails closed; the shared legacy amount is never pre-filled into the form;
* identity comes only from the server binding resolver — no client-side `methodCode` split, no
  provider/sibling leakage, no second configuration level.

`flat-general-tab.tsx` (the optional compact pointer): the verified financial block now points to
«تعرفه و پوشش» as the place where that method's fixed amount is edited, and states the tab performs no
write while ambiguous or unverified. The four tabs, three row actions, multilingual naming, RTL and the
design system are unchanged.

The legacy rate/coverage dialogs are intentionally left intact (the OFF-gate route) — nothing was deleted.

## Verification actually performed (real owner code, no fake badges)

| Check | Result |
| --- | --- |
| `ShippingMethodFinancialEditAuthorityTests` (new, 5 tests) — real `ShippingDbContext` + the real `ShippingRateCoverageDirectory` the endpoints use, read back through the TB-155 handler | 5/5 PASS |
| `ShippingMethodFinancialAuthorityTests` (TB-155, still green) + the above | 19/19 PASS |
| Full Shipping project | 308 PASS / 1 FAIL — the single failure is the pre-existing, unrelated `OperationalQuoteCompatibilityTests` failure recorded in TB-155 (`backend-shipping-suite-156.txt`) |
| Frontend focused tests (gate, financial model, rates tab, tabs, General-tab pinned guards, flat rows) | 60/60 PASS |
| `npx tsc --noEmit` | exit 0 |

The backend test file proves exactly the semantics Mission 2 asks for, at the owner-code level:

1. saving a fixed IRR rule for `tipax:standard` reads back as that method's own proven amount with its own
   currency, while `tipax:express` stays `NeedsConfiguration` with the sibling row counted only;
2. editing the owned amount changes only that method and the new amount is visible after re-read
   (the reload display path), and the other option is not invented;
3. creating an owned rule **preserves** the legacy `optionCode = null` row untouched (not edited, not
   deleted) while only the owned row becomes this method's money;
4. a competing identical scope is **denied fail-closed** and the stored amount is not overwritten
   (single rule, original amount);
5. an owned option rule is never attributed to the optionless/service-level identity.

## Not verified (stated plainly)

* No live isolated Host + scratch DB + private frontend acceptance run; therefore no real HTTP status/JSON
  sample for `GET …/financial-status` and no browser screenshot of the new gate or of a save→reload cycle.
  The route's authorization/mapping was previously pinned by TB-155 source guards; the write path is
  pinned here by real-seam tests, not by an HTTP round trip.
* Storefront quote dependency of an edited amount (does a saved owned rule change a real storefront quote
  for the same method) was not exercised in a runtime.
* Admin authorization seeding for a scratch runtime remains the concrete infrastructure blocker.

## Remaining modes still not supported (unchanged by this task)

`FREE_TO_CUSTOMER`, `COLLECT_ON_DELIVERY` and `PROVIDER_QUOTE` still have no field, rule or integration in
the owner, so they remain unrepresentable and unquotable; this task deliberately invented none of them and
introduced no settlement share, payout or allocation concept.

## Changed files

* `src/frontend/app/admin/shipping-services/shipping-method-financial-edit-gate.ts` (new)
* `src/frontend/app/admin/shipping-services/shipping-method-financial-edit-gate.test.ts` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-rates-tab.tsx` (gate wired, fail-closed writes)
* `src/frontend/app/admin/shipping-services/flat-general-tab.tsx` (compact pointer line)
* `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Admin/ShippingMethodFinancialEditAuthorityTests.cs` (new)
* `docs/ai/tasks/TB-SHIPPING-METHOD-FINANCIAL-EDIT-156.task.md`, this evidence folder

No production price calculation, storefront quote, ledger, settlement/payout, checkout group identity or
schedule, PSP, carrier calendar, EF logging, schema or migration was touched. Commit was fast-forward and
`HEAD == origin/main` was verified before reporting.

## Realistic next step (Architect decision)

1. Authorize a dedicated **isolated runtime harness** task (scratch container + isolated Host publish +
   private frontend + **admin authorization seed** script) — this is now a repeated blocker for every
   shipping admin acceptance task and is worth doing once, durably.
2. In that runtime, run the read→write→reload acceptance for one method (`tipax:standard`), confirm the
   sibling `tipax:express` is unchanged and the storefront quote dependency, then close this PARTIAL.
