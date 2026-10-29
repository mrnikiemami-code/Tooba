# TB-SHIPPING-USER-ENVIRONMENT-DATA-INTEGRITY-AND-DELIVERY-244-R1 — WORKER RESULT

Status: **BLOCKED** (compensation complete; lawful delivery stopped on missing merchant decisions).  
Stop-State: STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.  
Worker result is not Architect acceptance.

## Canonical runtime

| Item | Actual |
| --- | --- |
| Base / issue | `9484c59a75c9152ab75cf103057db1d6ee890fff` |
| Code-SHA | **NONE** (no product source change) |
| Host / FE | PID 30328 publish-primary `:5088` / PID 28396 `src/frontend` `:3000` |
| Edition / DB | SingleStore / store-alpha / `tooba_alpha` |
| Cart | `01a17b2d-e219-7000-b3b2-5bcdbe6810c5` Active v48 qty18 — intact, unsubmitted |

## A — Compensation status (every 244 mutation)

| Mutation | Pre-check (=244 write?) | Action via owning API | Post state |
| --- | --- | --- | --- |
| 4× offer fulfillment Platform+type `01a16f43…` | yes | PUT `responsibility: null` | null / null |
| `demo_attr_mobile_weight` unit kg | yes (was kg) | PATCH unit `g` | `g` |
| Category bindings daf5 + dba9 | yes | DELETE bindings | 0 remaining |
| Product weights 1.5 on 3 products | yes | PUT attributes `clear:true` | 0 remaining |
| Rate rows [0,1000) + [2000,1e9) | yes | PUT rate-rows keep only `01a16f4e-1c45…` [1000,2000) | 1 row |
| Publication | — | POST publication | snapshot `01a185cb-6909…` selVer **10** |
| Cart selection from snapshot `01a185c1-458a…` | accepted groups still on disk | No clear-without-reselect API; **invalidated for presentation** | payment-draft **400** `storefront.captured_selection.unavailable`; projection **unassigned** (not payment-ready) |

No independent post-244 user edits detected → no compensation conflicts. No business SQL.

## B — Authoritative per-line ledger (after restore)

| Line | Offer | Product label | Qty | Fulfillment | Shipping weight | Group total | Rate coverage | Outcome |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `01a17b2d-e315…` | `0b589982…` | هواوی گوشی هوشمند مجموعه 4 | 11 | **missing** | **missing** | n/a | [1000,2000)g only on draft `01a16eeb…` | `storefront.captured_preview.unassigned` |
| `01a17b2d-ea0b…` | `3bf70c9e…` | سامسونگ گوشی ساده سری 1 | 3 | **missing** | **missing** | n/a | same | unassigned |
| `01a17b2e-34a6…` | `0c4ae7f9…` | شیائومی گوشی هوشمند نسخه 3 | 3 | **missing** | **missing** | n/a | same | unassigned |
| `01a183ef-bb94…` | `6fce25fb…` | شیائومی گوشی هوشمند نسخه 3 | 1 | **missing** | **missing** | n/a | same | unassigned |

Definition `demo_attr_mobile_weight` exists (unit `g`) but is **not** bound/populated on these products; sibling values are not authority. No per-offer fulfillment assignment remains. Rate band not evaluated without measurement + assignment.

## C — Merchant decision table (required — do not invent)

| Offer / product | Missing responsibility / type | Missing per-unit shipping/package weight + unit | Resulting group total | Existing rate coverage | Exact merchant decision required |
| --- | --- | --- | --- | --- | --- |
| `0b589982…` / هواوی گوشی هوشمند مجموعه 4 | responsibility + shippingTypeId | packaged unit weight + unit (g or kg) | 11 × unit | [1000,2000) g on post/NULL draft | Platform vs Seller vs Store + type; weight of **one packaged unit** |
| `3bf70c9e…` / سامسونگ گوشی ساده سری 1 | same | same | 3 × unit | same | same for this offer/product |
| `0c4ae7f9…` / شیائومی گوشی هوشمند نسخه 3 (seller `01a0ffff…`) | same | same | 3 × unit | same | same for this offer |
| `6fce25fb…` / شیائومی گوشی هوشمند نسخه 3 (seller `01a030d1…`) | same | same | 1 × unit | same | same for this offer (may share product weight with prior Xiaomi row once set on product) |

If assigned weights make any group total outside [1000,2000) g, also decide rate tier(s) / amounts (do not clone/widen blindly).

## Stage URL

`http://127.0.0.1:3000/fa/shipping` — honest unassigned / Continue blocked; cart Active **unsubmitted**; 0 checkout/payment.  
Not delivery-complete.

## SHAs

Expected-Base-SHA / Base-SHA: `9484c59a75c9152ab75cf103057db1d6ee890fff`  
Code-SHA: NONE  
Evidence-SHA: *(this evidence commit)*  
local==origin/main after push.
