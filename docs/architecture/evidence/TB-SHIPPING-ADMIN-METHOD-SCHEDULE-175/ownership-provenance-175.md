# TB-SHIPPING-ADMIN-METHOD-SCHEDULE-175 — ownership / provenance matrix

Task: make the existing Admin «برنامه کاری حمل» tab genuinely useful and method-isolated, with
real persisted writes through Shipping-owned paths, and trace the real customer effect.

Base: `f0159086ebd42fd125b51ca5f3cfe5261102ba14` (HEAD == origin/main at claim).

## 1. Read/write provenance (traced in source)

| Surface | Owner | Actual path | Identity | Persistence | Notes |
|---|---|---|---|---|---|
| Method binding (serviceCode/optionCode) | Fulfillment via Shipping admin API | `GET /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/binding` | server-resolved `methodCode` | Fulfillment catalog | Browser never splits `methodCode`; `flat-method-detail-dialog.tsx` passes the resolved pair as `bindingOverride`. |
| Admin Schedule read | Shipping | `GET /v1/admin/shipping-services/{serviceCode}/operating-schedules` | path `serviceCode`; rows carry `optionCode` | `shipping.operating_schedules` + `shipping.operating_windows` | Returns **all** identities of the service; the frontend scopes to the exact `(serviceCode, optionCode)` pair. |
| Admin Schedule write (create/replace) | Shipping | `PUT /v1/admin/shipping-services/{serviceCode}/operating-schedules` | route `serviceCode` + body `optionCode` | full replacement of that identity's window set | `UpsertShippingOperatingScheduleHandler` validates the pair against the real Fulfillment catalog (`shipping.service_option.unknown`) before writing. |
| Admin Schedule activation | Shipping | `PATCH /v1/admin/shipping-services/{serviceCode}/operating-schedules/{scheduleId}/active` | route `serviceCode` must match stored row | `is_active` | Cross-service id returns `shipping.service_scope.mismatch`; id is preserved on deactivate (auditable). |
| Schedule evaluation | Shipping domain | `ShippingScheduleAvailabilityEvaluator` | `(serviceCode, optionCode)` with option-first / service-level fallback | pure | Consumed by `ShippingOperationalQuoteLookup` (Admin operational quote). |
| Customer group horizons | Order | `StorefrontShippingService.BuildGroupHorizons` | seller × methodCode | cart draft | Uses `StorefrontShippingCalculator.ComputeMinimumDeliveryDate(today, sellerPreparationDays, method.LeadDays)` and the **static** `StorefrontShippingCalculator.DayTimeWindows`. It does not read the Shipping operating schedule. |
| Customer operational shadow | Order | `StorefrontShippingOperationalShadowObserver` | methodCode | none (telemetry only) | `Tooba:ShippingOperationalShadow` `Enabled`/`PrerequisitesConfirmed` both default **false**; documented to never change price, delivery promise, draft or order snapshot. |

## 2. Evaluator semantics mirrored by the Admin UI (no new business meaning)

`ShippingScheduleAvailabilityEvaluator`:

| Stored state for the exact identity | Evaluator | Admin tab (TB-175) |
|---|---|---|
| no row | `NotConfigured` — nothing restricted | «بدون محدودیت زمانی صریح (پیش‌فرض)» + explicit “not an availability guarantee” |
| only inactive row | `InactiveNotEnforced` — nothing restricted | «برنامهٔ ثبت‌شده غیرفعال است؛ محدودیتی اعمال نمی‌شود» |
| one active row with windows | `Available` inside / `OutsideWindows` outside | «محدود به بازه‌های ثبت‌شدهٔ همین روش» |
| one active row with zero windows | `OutsideWindows` everywhere | «محدود به بازه‌های ثبت‌شده — خالی» and explicitly **not** “unrestricted” |
| two active rows for the identity | `Ambiguous` | «ابهام داده» — no availability claim |

A method has **no extra Admin day/window restriction by default**; this does not assert real carrier
availability, calendar, capacity or time-slot booking.

## 3. Downstream truth (the decisive finding)

- The saved schedule is **not** consumed by `StorefrontShippingService.BuildGroupHorizons`.
  Customer minimum delivery date comes only from method `LeadDays` + seller preparation days, and
  eligible time windows come from the static `DayTimeWindows` list.
- The only bridge from Customer/Order to the operational schedule is the operational shadow
  observer, and it is **default-OFF and observation-only**.
- Therefore editing this Admin tab does **not** change a customer delivery promise today. The tab
  now states this explicitly (`shipping-one-level-schedule-customer-effect`) instead of implying a
  customer promise change.
- Carrier calendar, per-day capacity enforcement, time-slot booking and timezone-aware customer
  windows remain **OPEN** (no such owner contract exists in this slice).

## 4. What this task changed

1. `flat-method-detail-dialog.tsx`: pass `readOnly` to the Schedule tab (View mode was previously
   able to save/toggle a schedule — a real write from a read-only surface).
2. `shipping-one-level-schedule-tab.tsx`: accept `readOnly`, block every mutation path (form edits,
   row add/remove, save, activate/deactivate) and show an explicit read-only notice.
3. Explicit, honest coverage banner for the five owner states above, plus a customer-effect
   disclosure and an identity-isolation statement.
4. `shipping-one-level-schedule-model.ts`: pure `resolveScheduleCoverage`,
   `scheduleCoverageLabelFa`, `scheduleCoverageExplanationFa` and stable Persian disclosures.
5. Focused tests: 3 new tab tests (20 assertions total across model/wiring) + existing suite.

No backend contract, schema, migration, gate or shared surface was changed.
