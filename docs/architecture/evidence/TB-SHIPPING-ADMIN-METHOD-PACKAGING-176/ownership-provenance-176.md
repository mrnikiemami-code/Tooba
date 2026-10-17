# TB-SHIPPING-ADMIN-METHOD-PACKAGING-176 — ownership / provenance matrix

Task: finish the flat-method detail tab «بسته‌بندی و کرایه» for one canonical method, repair the
TB-175 gap that View mode had no `readOnly` wiring, and state the true customer effect.

Base: `cf0438651ddba756847ba21c04cc3bd788927a65` (HEAD == origin/main at claim).

## 1. Owner read/write provenance (traced in source)

| Surface | Owner | Actual path | Identity | Persistence |
|---|---|---|---|---|
| Method binding | Fulfillment, via Shipping admin API | `GET /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/binding` | server-resolved `methodCode` | Fulfillment catalog |
| Packaging read | Shipping | `GET /v1/admin/shipping-services/{serviceCode}/packaging-policies` | path `serviceCode`; rows carry `optionCode` | `shipping.packaging_policies` |
| Packaging create | Shipping | `POST .../packaging-policies` | route `serviceCode` + body `optionCode` | new row |
| Packaging update | Shipping | `PUT .../packaging-policies/{packagingPolicyId}` | route `serviceCode`, route id, body `optionCode` | row updated, id stable |
| Packaging activation | Shipping | `PATCH .../packaging-policies/{packagingPolicyId}/active` | route `serviceCode` must match the stored row | `is_active` |
| Fee resolution | Shipping domain | `ShippingPackagingFeeResolver.Resolve` | `(serviceCode, optionCode, destination scope)` | pure, no I/O |
| Fee consumer | Shipping | `ShippingOperationalQuoteEvaluator` | method + destination | Admin operational quote only |
| Customer checkout amount | Order | `StorefrontShippingCalculator` / checkout preview | method + seller | **does not read packaging policies** |
| Operational shadow | Order | `StorefrontShippingOperationalShadowObserver` | method | telemetry only; `Enabled`/`PrerequisitesConfirmed` default **false** |

Both mutating handlers validate the `(serviceCode, optionCode)` pair against the real Fulfillment
catalog before writing (`shipping.service_option.unknown`), and the directory refuses a cross-service
id. So method isolation is owner-correct; no new seam was needed.

## 2. Identity semantics

| Row | Meaning | Admin tab treatment |
|---|---|---|
| `optionCode = "standard"` | policy owned by `tipax:standard` | shown and editable |
| `optionCode = "express"` | policy owned by sibling `tipax:express` | never shown, never mutated |
| `optionCode = null` | legacy service-level policy | never shown as “this method's policy”, never mutated; the evaluator uses it as the documented fallback |

Scope precedence in the resolver: option-specific policy wins; service-level is the explicit
fallback; deepest destination scope wins inside a group; two equally specific policies are reported
`Ambiguous` with zero fee (never “first row wins”).

## 3. Customer effect (the decisive finding)

`ShippingPackagingFeeResolver` is consumed only by `ShippingOperationalQuoteEvaluator`. No Order /
checkout path reads `shipping.packaging_policies`; the only Order-side bridge is the operational
shadow observer, which is default-OFF and observation-only. Therefore saving a packaging policy does
**not** change the customer's payable amount, freight charge or any settlement share today. The tab
now says this explicitly (`shipping-one-level-packaging-customer-effect`) instead of implying a
charge change.

## 4. What this task changed

1. `flat-method-detail-dialog.tsx`: pass `readOnly` into the Packaging tab (TB-175 gap).
2. `shipping-one-level-packaging-tab.tsx`: `readOnly` prop, single write gate
   (`packagingWritesAllowed`), early guards in `submitCreate` / `submitEdit` / `toggle` / `startEdit`,
   every disabling control bound to the same gate, explicit read-only notice.
3. `shipping-one-level-packaging-model.ts`: pure `packagingWritesAllowed` plus stable Persian
   disclosures for read-only, customer effect, identity isolation and the legacy service-level row.
4. Tests: 2 new tab tests (mutation-route guard + provenance disclosures); 11/11 in the file.

No backend contract, schema, migration, gate, CSS, storefront or shared surface was changed.
