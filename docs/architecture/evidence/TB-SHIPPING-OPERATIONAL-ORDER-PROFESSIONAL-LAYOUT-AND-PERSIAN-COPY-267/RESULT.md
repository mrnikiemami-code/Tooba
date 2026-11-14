# TB-267 — RESULT (worker)

Status **PASS within the issued presentation-only scope**
Decision **PROFESSIONAL_OPERATIONAL_ORDER_LAYOUT_WITH_HUMANIZED_PERSIAN_OPERATOR_COPY_FINANCE_FREE_SHIPPING_ROUTE_AND_ZERO_FUNCTIONAL_CHANGE**

NOT Architect ACCEPT. NOT user VISUAL ACCEPT. NOT an all-shipping / carrier / financial claim.

- Task: `docs/ai/tasks/TB-SHIPPING-OPERATIONAL-ORDER-PROFESSIONAL-LAYOUT-AND-PERSIAN-COPY-267/TASK.md` (issued verbatim, copied to this folder).
- Issued `Expected-Base-SHA`: `bf9aa8fccad50d984c54f7bacd8cbcea3bcfc1a6`.
- Actual work base: `500c6583c` (the two documentation commits that issue this task sit on that base; verified ancestor of the final HEAD — see `result-contract.txt`).
- Mode honoured: presentation and operator copy only. No backend, endpoint, contract, capability, mutation, schema, migration, or data change. No order of business events changed.
- The user's exercised Order B `01a1db2f-e36e-7000-b7d3-afb2fe86e81a` was **read-only** the whole time (GET-only). Its stored facts (Returned source + Delivered resend + damaged decision) were read as-is and are unchanged.

## What was actually wrong (measured, not guessed)

| Symptom the user reported | Source cause found |
| --- | --- |
| "جا نماییها اصلاً حرفهای نیست" — giant mostly empty panels | the work target and the recipient were two independent full-width cards; the target card carried a 2-column `dl` inside a card that was mostly empty space, and there was no shared two-column frame |
| telephone stranded at the far edge | the recipient value used a flex row whose `dd` pushed the number to the opposite edge of a full-width line |
| minuscule dense dates/deadlines | the shared items panel and the delivery section still emitted `text-[10px]`/`text-[11px]` microtext — **7 + 27 occurrences** before, **0** now |
| raw `Asia/Tehran` amid RTL dates | the carrier zone was printed as the raw IANA id: now **0** raw ids and **0** ISO `YYYY-MM-DD` strings and **0** GUIDs in the visible copy of the route; the human label «به وقت تهران» is derived from the existing zone data |
| duplicate group info in the top target and in the cards | the deadline triple was rendered in the overview **and** in the group card — the overview copy was removed, the authoritative block stays with the group that owns the plan |
| two narrow dense parcel cards with long TRK prose | parcels were rendered in a squeezed list; the source/child relationship was a repeated raw code inside an amber paragraph |
| finance-heavy bottom grid on an operations-only route | the shared items/shipping panel had no presentation option and always emitted price/line-total/fee columns |

## Landed

1. **One new pure formatter module** `src/frontend/lib/operational-format.ts` (`operationalDayLabel`, `operationalInstantLabel`, `operationalTimeZoneLabel`, `operationalTimeWindowLabel`). It only reformats already-projected strings: Jalali day, human instant, `Asia/Tehran → «به وقت تهران»`, ASCII→Persian digits in time ranges. It never computes or shifts an instant, so no accepted plan, deadline or promise is altered.
2. **Compact page header** (`order-operations-view.tsx`): title, breadcrumb, order reference as secondary **LTR** text (`dir="ltr"`, truncated), and the existing two actions grouped at `min-h-11` (44px). Reference falls back to `checkoutId` only when the reference is absent.
3. **One overview frame** for the work target + recipient: `grid gap-4 lg:grid-cols-[minmax(0,1.4fr)_minmax(260px,0.8fr)]` (a class already present in the compiled Tailwind output), stacked below `lg`. Recipient name / mobile / address are three labelled `dt`/`dd` rows of the same card; the mobile is a `bdi dir="ltr"` beside its own label; long addresses wrap (`break-words`).
4. **Operational group card** now reads as labelled rows: «تحویل مشتری» (customer promise, untouched instants) and «موعد ارسال حامل» (carrier handoff, never presented as the customer promise) in a 2-column `dl`; the deadline triple «آمادهسازی از / خروج به سوی حامل / آخرین مهلت تحویل به حامل» is a dedicated 3-column block whose header carries the single human zone label «مهلتهای عملیاتی · به وقت تهران». A legacy policy-v1 plan still renders **no** deadline block and invents none.
5. **Contents before parcels, full-width parcel cards** (`admin-order-operational-delivery-section.tsx`): the one-line product/quantity summary is a compact block above the parcel list; each parcel is a full-width card (measured 1052px inside a 1086px group at 1440) with its own title «مرسولهٔ n», status chip, `carrier · n قلم`, then tracking.
6. **Human lineage instead of code prose**: source↔resend/replacement is a chip «ارسال مجدد در مرسولهٔ ۲ · تحویلشده» / «ارسال مجدد از مرسولهٔ ۱»; the remaining capacity sentence names the linked human parcel; the child's technical identity is available through an explicit `<details>` «پیگیری ارسال مجدد: مرسولهٔ ۲» whose summary is a 44px control. The dangling «مرسولهٔ ارسال مجدد ·» with no fact after it is gone — the exception line now states only facts that exist, joined properly.
7. **Tracking** has its own line with a selectable LTR `bdi`, and a 44px copy button that acknowledges locally («کپی» → «کپی شد» for 2s). It copies an already-projected value: no endpoint, no mutation.
8. **Finance-free on the shipping-only route**: `financeFree?: boolean` is threaded from the operations route through `AdminOrderItemsShippingPanel` and `AdminOrderOperationalDeliverySection`. On that route the price/line-total columns, group fee headers and fee breakdown are omitted; product, quantity, packing/status, allocation, return window and every physical action remain. The full order detail route does not pass the prop, so its finance display is unchanged.

## Proven once

- Typecheck `npx tsc --noEmit`: **0**.
- Mounted suites (once, after the final change): `admin-order-operational-delivery.mount.test.ts` + `admin-order-operations-view.mount.test.ts` **68/68**, `admin-order-operations-route.mount.test.ts` **5/5**.
- `npm run build`: **0** errors, `BUILD_ID EwxE4g90_VArF5iXhm6oO`, `_buildManifest.js` served **200**.
- Live served route (real Order B, GET only), measured — see `layout-before-after-metrics.md`:
  - 1440: overview two columns target **671px** / recipient **384px**, both **175px** tall; group **1086px**; parcel cards **1052px**; document overflow **0**; clipped elements **0**; controls under 44px inside `main` **0**; sub-12px text inside `main` **0**; h1 `20px/28px`, values `14px/20px`.
  - 1024: two columns **379px / 260px**, overflow **0**.
  - 390: stacked full-width **332px** each, overflow **0**, off-screen controls **0**, controls under 44px **0**, clipped **0**.
  - Copy audit on the served text: raw `Asia/Tehran` **0**, ISO dates **0**, GUIDs **0**, «به وقت تهران» appears twice (once in the overview carrier row, once in the group deadline block — two self-contained surfaces).
  - Money audit on the shipping-only route: `مبلغ / جمع کل / مالیات / قیمت / کارمزد / تومان / ریال` **0** matches; table headers `# / محصول / تعداد / انتخاب تعداد / وضعیت / تخصیص ارسال / مهلت مرجوعی / عملیات`. The same order's full page still shows «مبلغ کل سفارش / مبلغ قابل پرداخت / مبلغ ناخالص …».

## Known limits

- The resend relationship is asserted from the parcel snapshot the server projects (`resendOfShipmentId`, linked child, receipt condition). The route was **not** functionally re-exercised: no create/dispatch/deliver/return/refund/cancel/save/publish was performed, by design.
- The two-column `lg:` grid uses a utility that already exists in the emitted CSS; `tailwind.config.ts` still does not scan `features/**`, so new feature-level responsive utilities must be verified in the DOM (reported, not fixed globally — no global style sweep was authorised).
- The user's own fixture names («TB264R2 تیپاکس اکسپرس آزمایشی», «TB266R1 گیرنده») are stored business data and were deliberately **not** sanitised.
- The empty-card / microtext numbers in `layout-before-after-metrics.md` for the "before" column are static source counts and the architect's written description of the rejected screenshot; they are not a re-render of the previous bundle.
- No user VISUAL ACCEPT is claimed. `STOP_FOR_ARCHITECT`, `automaticNextImplementationTask: NONE`.
