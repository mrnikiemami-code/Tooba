# SHIPPING-ONE-LEVEL-UI-095 — activation authority: BLOCKED_ACTIVATION_AUTHORITY (no toggle shipped)

Task: `TB-SHIPPING-ONE-LEVEL-TABS-095` · Parent: `TB-SHIPPING-ONE-LEVEL-TABS-094`
Program: `TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE` · Mode: `BOUNDED_SAFE_INDEPENDENT_METHOD_ACTIVATION`
Stop-State: `STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`
Status: **BLOCKED** (Mission A decision: `BLOCKED_ACTIVATION_AUTHORITY`) — activation controls stay
**read-only**; **no** source change was made and **no** state was written.

---

## 1. Base / drift

| Item | Value |
| --- | --- |
| Expected-Base-SHA | `1bbf2607cb21792282aa3b20c7d55feb66c85a5c` |
| Base-SHA (pre-work `HEAD` = `origin/main`) | `ed81ca84a5b900dcf35d04ee18871fc14926497d` |
| `1bbf2607c…` ancestor of `HEAD` | yes |
| `src/**` drift `1bbf2607c..HEAD` | **none** (`git diff --stat 1bbf2607c..HEAD -- src/` empty) |

The two commits between are 094 result bookkeeping only (`docs/**` + the 094 evidence/result files).
No overlapping source drift ⇒ no `RECOVERY_CONFLICT`. Pre-existing dirty/untracked files were
preserved; no reset/stash/rebase/force push.

---

## 2. Mission A — activation authority (the finding)

### 2.1 Who actually decides what a customer can choose

`Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs` builds the
customer-visible shipping method list from the **legacy two-level catalog only**:

```text
:26   IShippingCatalogReader                         (legacy shipping_services / _options)
:379  ShippingMethodRegistry.Enabled(_shippingOptions)   -> Tooba:ShippingMethods:EnabledCodes
:382  services.Where(x => x.IsActive)                    -> shipping_services.is_active
:397  service.Options.Where(o => o.IsActive)             -> shipping_service_options.is_active
:412  label = $"{serviceName} — {optionName}"            -> LEGACY translations
:430  StorefrontShippingCalculator.ResolveRate(code, _shippingOptions)
```

`fulfillment.shipping_methods.is_active` (the flag the admin list shows) has **no** storefront,
Checkout, Order or Shipping consumer: the only non-test consumers of the one-level catalog are the
085/086 admin read bridge and the 094 admin name write, and the two one-level tables are declared only
in `FulfillmentDbContext`. Proven by exhaustive code search in
`evidence/…-095/log-activation-authority-map.txt`.

### 2.2 The live parity is honest *today*, but only by backfill

Read-only authorized reads on the shared Development stack (`dev-singlestore / store-alpha /
tenant-alpha`, verified before probing):

| method_code | method row | legacy service | legacy option | legacy effective | agree |
| --- | --- | --- | --- | --- | --- |
| `post:express` | inactive | `post` (active) | `express` **inactive** | INACTIVE | yes |
| `post:standard` | active | `post` (active) | `standard` active | ACTIVE | yes |
| `tipax:express` | active | `tipax` (active) | `express` active | ACTIVE | yes |
| `tipax:standard` | active | `tipax` (active) | `standard` active | ACTIVE | yes |
| `snapp_courier` | inactive | `snapp_courier` **inactive** | (optionless) | INACTIVE | yes |
| `store_courier` | active | `store_courier` active | (optionless) | ACTIVE | yes |
| `in_person` | active | `in_person` active | (optionless) | ACTIVE | yes |

So the five-tab list is not currently lying. It agrees because the one-level rows were backfilled from
the legacy catalog — **nothing keeps them in sync afterwards**.

### 2.3 Why a narrow activation is not available today

To make `post:express` genuinely customer-visible you must flip legacy option
`638c583e-4bce-4298-bdec-438b680af05e` (`post`/`express`) to active. The **only** existing writer of
that column is `ShippingServiceDirectory.UpdateAsync`, which is a destructive full replacement:

```text
:87-88  RemoveRange(existingTranslations) + AddTranslations(...)
:90-92  RemoveRange(existingOptionTranslations) ; RemoveRange(existingOptions)
:93     AddOptions(serviceId, model.Options, now)      // option ids re-minted unless echoed back
```

There is **no** option-level or method-level activation endpoint (no `options/` route; the only
deactivate is service-level and would kill `post:standard` as well). The task forbids touching that
destructive path, and a method-row-only toggle is explicitly forbidden as a misleading apparent
activation. Hence **nothing was implemented**.

### 2.4 The real ownership question (why this needs the Architect)

- **Dual writer.** Adding a narrow `shipping_service_options.is_active` writer would give that one
  column two independent admin writers with different semantics: the legacy full-replace PUT (rewrites
  option rows/ids and resets every active flag, knows nothing about `shipping_methods`) and the new
  narrow toggle (knows nothing about the legacy payload contract). The legacy save could silently
  revert — or delete — what the one-level admin just activated, so "transactionally consistent,
  ownership-preserving" cannot be guaranteed without either repairing the legacy writer or declaring a
  single owner of the activation fact.
- **Optionless methods.** `snapp_courier` / `store_courier` / `in_person` have `LegacyOptionCode = null`
  and map to the legacy **service** row. Toggling them means `ShippingService.SetActive`, i.e. the whole
  service — and activating `snapp_courier` would mean activating an inactive **parent** service, which
  the task forbids. Service-level and option-level activation need distinct, explicitly authorized
  semantics; one generic toggle button would be wrong for 3 of the 7 methods.
- **Label divergence.** The customer label is «{legacy service name} — {legacy option name}», so even a
  fully correct activation would display «پست — پیشتاز» while the merchant edited «پست پیشتاز» in the
  094 one-level editor. The 094 display name is admin-only.
- **Timebox.** 30 minutes HARD. Mission A consumed the budget, and a correct Mission B (seam + frontend
  toggle + confirmation + focused tests + proof) is a superset of 094, which was accepted at ~205
  minutes and explicitly must not be repeated. Implementing it now would be the "speculative expansion"
  the Architect forbade.

Full reasoning, the ready-to-implement safe design, and the four exact authorization questions are in
`evidence/…-095/log-activation-decision.txt`.

---

## 3. Mission B

**Not started** — Mission A produced a `BLOCKED_ACTIVATION_AUTHORITY` decision, which is the outcome
the task prescribes for exactly this situation ("document an explicit BLOCKED_ACTIVATION_AUTHORITY
decision, leave activation controls read-only, and STOP… This is preferable to a misleading green
button").

The five-tab shell, the 094 name editor and both read paths are unchanged. Activation remains
read-only in the UI, and no enable/disable control was added.

---

## 4. Proof actually produced

| Evidence | Content |
| --- | --- |
| `log-activation-authority-map.txt` | file:line map of storefront eligibility, the one-level consumers, the legacy writers and the destructive `UpdateAsync` |
| `log-activation-state-parity.txt` | live read-only legacy-vs-one-level parity table + what activating `post:express` would require |
| `log-activation-decision.txt` | the `BLOCKED_ACTIVATION_AUTHORITY` decision, D1–D8, the safe design, the four authorization questions, rollback |

No test suite was run: no source file changed, so there is nothing new to test. The pre-existing 094
suites still stand by reference (20/20 behavior, 8/8 coverage guard, 14/14 Host cert guard, 31/31
frontend focused, `tsc --noEmit` clean).

No DB write, no migration, no seed, no backfill, no restart of `:5088` (not needed — no executable
change), no `Tooba:ShippingRateAuthority` / `Tooba:ShippingOperationalShadow` change, no
staging/production contact.

---

## 5. Changed paths

Docs/evidence only:

- `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-095.md` (this file)
- `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-095/**`
- `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-095.task.md` (receipt, **never staged**)

No `src/**` file was modified.

---

## 6. Rollback

None required — no state was written and no code changed. Activation controls remain read-only exactly
as accepted in 094.

## 7. Elapsed

Claimed at `2026-10-15T21:53Z`-ish (the `GET /api/tasks/next` claim) and completed inside the 30-minute
budget; the exact figure is recorded in the posted result. Mission A (map + parity + decision) only.
