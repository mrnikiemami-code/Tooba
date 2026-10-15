# TB-SHIPPING-ONE-LEVEL-TABS-095 — evidence summary

Task: `TB-SHIPPING-ONE-LEVEL-TABS-095` · Parent: `TB-SHIPPING-ONE-LEVEL-TABS-094`
Mode: `BOUNDED_SAFE_INDEPENDENT_METHOD_ACTIVATION`
Status: **BLOCKED** — decision `BLOCKED_ACTIVATION_AUTHORITY` (activation stays read-only)
Stop-State: `STOP_FOR_ARCHITECT` · automaticNextImplementationTask: `NONE`
Plan: `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-095.md`

| Item | Value |
| --- | --- |
| Expected-Base-SHA | `1bbf2607cb21792282aa3b20c7d55feb66c85a5c` |
| Base-SHA | `ed81ca84a5b900dcf35d04ee18871fc14926497d` |
| Base is ancestor | yes (`git merge-base --is-ancestor` → 0) |
| `src/**` drift `1bbf2607c..HEAD` | none |
| Bridge task row | `c0bcd41f-bb4b-4243-ac82-79b9f22dec5c` |

## Outcome

Mission A was mandatory and decisive: a safe, ownership-preserving independent method activation is
**not** available inside this task's authority, and a method-row-only toggle is explicitly forbidden as
a misleading apparent activation. Mission B was therefore not started, and no source file was changed.

## The finding in three sentences

1. The storefront's customer-visible shipping methods come **only** from the legacy two-level catalog
   (`shipping_services.is_active` + `shipping_service_options.is_active`) plus
   `Tooba:ShippingMethods:EnabledCodes` and `Tooba:ShippingMethods:Rates`; the one-level
   `shipping_methods.is_active` flag has **no** storefront/Checkout/Order consumer.
2. Activating `post:express` therefore requires flipping legacy option
   `shipping_service_options(638c583e…, post/express).is_active`, whose **only** existing writer is the
   destructive full-replace `ShippingServiceDirectory.UpdateAsync` (delete + re-insert of options and
   option translations) — expressly forbidden, and there is no option-level write endpoint.
3. Making it correct would create a **dual writer** on that legacy column (the legacy full-replace PUT
   can silently revert or delete what the one-level admin just did), and the three optionless methods
   would require a *different*, service-level semantics that the task forbids for inactive parents. Both
   are ownership decisions that must be authorized first.

## Live read-only parity (shared Development stack)

Verified target first: `dev-singlestore / store-alpha / tenant-alpha`. Authorized admin session,
`/api/auth/me` = 200. Two authorized read-only GETs, no writes.

| method_code | method row | legacy effective | agree |
| --- | --- | --- | --- |
| `post:express` | inactive | INACTIVE (`post` active, option `express` inactive) | yes |
| `post:standard` | active | ACTIVE | yes |
| `tipax:express` | active | ACTIVE | yes |
| `tipax:standard` | active | ACTIVE | yes |
| `snapp_courier` | inactive | INACTIVE (`snapp_courier` service inactive) | yes |
| `store_courier` | active | ACTIVE (optionless) | yes |
| `in_person` | active | ACTIVE (optionless) | yes |

The list is honest **today** only because the one-level rows were backfilled from the legacy catalog;
nothing keeps the two in sync afterwards. Also noted: the customer label is built from **legacy**
translations («پست — پیشتاز»), so the 094 admin display name would not reach the storefront.

## Files in this directory

| File | Content |
| --- | --- |
| `summary.md` | this file |
| `log-activation-authority-map.txt` | file:line authority map (storefront eligibility, one-level consumers, legacy writers, destructive `UpdateAsync`) |
| `log-activation-state-parity.txt` | live read-only legacy-vs-one-level parity + what activation would require |
| `log-activation-decision.txt` | the `BLOCKED_ACTIVATION_AUTHORITY` decision, the safe design, the four authorization questions |
| `RESULT.bridge.txt` | the exact Worker Result contract posted to Bridge |
| `post-result.js` | the one-shot poster used after commit/push (no probe payloads) |
| `task-receipt.json` | the persisted downloaded `.task.md` receipt |

## Authorization needed before a 096-style task can implement this

- Q1 Which side owns the activation fact: the one-level `shipping_methods.is_active`, or the legacy
  service/option rows the storefront actually reads?
- Q2 Is a narrow, identity-preserving single-column `ShippingServiceOption.SetActive` writer
  authorized, and is the legacy destructive full-service `UpdateAsync` to be repaired/guarded, or
  treated as an authoritative override with a visible warning?
- Q3 Authorized semantics for the three optionless methods (`snapp_courier`, `store_courier`,
  `in_person`) — refuse, or a separately authorized legacy service-level activation?
- Q4 After activation, should the storefront show the legacy label or the 094 admin display name?

## Safety / rollback

No DB write, no migration/seed/backfill, no Host restart (no executable change), no gate change
(`ShippingRateAuthority` / `ShippingOperationalShadow` untouched), no staging/production contact, no auth
bypass. **Rollback: none required** — nothing was written; activation controls remain read-only exactly
as accepted in 094.
