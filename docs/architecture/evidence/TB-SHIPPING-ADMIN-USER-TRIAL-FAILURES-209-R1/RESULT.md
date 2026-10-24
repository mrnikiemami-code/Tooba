# TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R1 — RESULT

Parent: TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209 (ARCHITECT-REVIEW: live save ACCEPTED, UI PARTIAL)
Mode: BOUNDED_READ_ONLY_MODE_PROFESSIONAL_FORM_AND_EXACT_ERROR_PLACEMENT_REPAIR
Base: `4d4929a297b0f3f717a649e41f46939d93828e06`
Verdict: **READY_FOR_ARCHITECT_REVIEW** — no user Visual ACCEPT claimed, no production acceptance claimed.

## 1. Actual elapsed (Claim → Finish)

- Claim: 2026-10-24T19:15:43Z (worker heartbeat `Working`, task row `c148ef34-7342-4987-aa18-f7230083d70c`).
- Implementation stop: ~19:34Z (≈18 min) — one bounded implementation correction, no second rewrite.
- Mounted inspection / evidence / commit: to ~19:52Z. Total ≈ 37 min, **over the 30-min hardbox by ≈7 min** (recorded, not relabelled). One correction round only; no test-repair loop.

## 2. Truthful VIEW state (was the review's defect #1)

- `authoring-rates-editor.tsx`: new exported `RateRowSummary` — the SAME stored row rendered as text
  (condition, freight, packaging, delivery range). No input, no button, no handler prop.
- `authoring-groups-editor.tsx`: the row list branches on `editable`
  (`editable ? RateRowEditor… : RateRowSummary…`), so view mode cannot construct an editable row at all.
- Group title in view mode is `<p data-testid="shipping-authoring-group-title-text">` (stored title, else
  the active-language draft title), never a `disabled` input. Add/remove/move/add-area/picker/add-row
  controls are all inside `editable ?` branches; the delete confirm gains a cancel test id.
- Handler-boundary guard (not styling): `authoring-screen.tsx` holds
  `mutationBlocked.current = mode !== "edit" || session.save === "saving" || session.reconcile === "pending"`
  and every mutation callback (`commit`, `pushEdits`, `changeRow`, `addRow`, `removeRow`, `addGroup`,
  `removeGroup`, `moveGroup`, `changeGroupTitle`, `addGroupArea`, `removeGroupArea`, `changeWeek`) returns
  early through `blockedNow()`. Programmatic activation in view/saving/reconciling cannot change rows.
- Panels receive `editable={!readOnly && !busy}` as a second, declarative layer.

Measured on the live mounted route (1440): Regions panel in view = **0** `input/select/textarea/button`
(`panelInputs: 0`), 1 read-only row; stored text `کرایهٔ حمل: 120000.00 · بسته‌بندی: 5000.00`,
`تحویل: 1 تا 3 روز`, `شرط: وزن (گرم): ۱٬۰۰۰–۲٬۰۰۰`, title `تست`, destination `ایران`.

## 3. Professional/compact General and Regions (defect #2)

- General: duplicate type card removed (`type-state` now renders only for Inactive/Missing/loading/error
  catalog states, so it can never contradict the selected type: measured `typeCards: 0`). Owner is one
  plain context line. Type select is a single 384px control; name/description are 862px wide inside an
  896px (`max-w-4xl`) column. Screen container is `max-w-6xl` (measured 1072px at 1440).
- Rates: primary row is now 4 readable fields — freight, packaging, delivery min, delivery max
  (measured `gridTemplateColumns: 247.25px ×4` at 1440) instead of ten narrow axes on one line.
  Optional weight/count/merchandise axes moved into a labelled `<details>` whose `<summary>` ALWAYS
  states the active condition (`شرایط اختیاری … · شرط: وزن (گرم): ۱۰۰۰–۲۰۰۰`), so nothing configured is
  invisible while collapsed. Expanding/collapsing has no value path (`onToggle` absent).
- Typography raised: no `text-[10px]` left in the feature UI (was labels/keys); rate labels 12px,
  rate inputs 14px/`min-h-9`, read-only row 12px, group title 14px.
- Area card: single intro line (duplicate edit-note removed), heading + currency + destinations + costs.
- Week: unchanged structure (one root week, existing valid-id `TimeZoneSelect`), container is now
  `tabIndex={-1}` so focus targeting works.
- 390px: `scrollWidth 390` = `innerWidth 390` (no overflow), row summary collapses to one 280px column
  in a 326px panel.

## 4. Exact, actionable error placement (defect #3, #4)

`ui/server-field-error.ts` was rewritten (path → section/row, exact code → reason):

- Path ownership is parsed per SEGMENT (`segmentOf` splits `name[index]`), so
  `regionGroups[0].rows[0].minDeliveryDays` → target `rates`, `rowIndex 0`,
  testId `shipping-authoring-rate-min-days` — it can no longer fall into the weekly schedule merely
  because the segment contains `Days`. `methodTranslations[…].description` → `description` (never `name`).
- Reason comes only from an **exact string match** against the registered code set
  (`REGISTERED` map built from `ShippingErrorCodes.cs`): timezone.required vs timezone.invalid vs
  `shipping.validation.schedule_timezone_id_invalid` are distinct; rate amount/duration/condition/row
  codes map to their own reasons. An unknown code (e.g. `vendor.experimental.name.rate.time.type.check`)
  yields `fieldRejectedServer` — no invented nameRequired/timeInvalid/typeUnavailable via substring.
- Placement is per owning field: name (`Field error`), description (own `Field error`), settings
  (under the freight fieldset), week time zone and week day list (inside the week section), rate row
  (`RateRowEditor.serverError`, keyed by submitted `rowIndex`). The top banner keeps a clickable summary
  and its entries focus the exact element: row repeats are resolved with
  `querySelectorAll(testId)[rowIndex]`.
- Raw codes moved out of prose into a collapsed `<details data-testid="shipping-authoring-save-diagnostics">`
  (`serverErrorCodes`). Unknown shapes still show the honest generic line.
- `time-zone-options.ts` comment corrected: a hand-kept SUGGESTION list, **not** market-derived, not a
  runtime-acceptance claim; stored ids outside the list are preserved (no silent default).
- Conflict/unknown-transport semantics, raw edited text, epoch, one-Apply and no-auto-retry are untouched.

## 5. Regression facts (focused)

`features/admin-shipping-authoring/user-trial-209-r1.test.ts` — **10/10 pass**:
description ownership; nested `minDeliveryDays` → rate row (not week); row index + input mapping
(`rows[2].freightAmount` → row 3 freight); required vs invalid timezone distinct; unknown code invents no
cause; known path + unknown code keeps path ownership; codes available for diagnostics only; view/summary
+ handler guard source facts; conditions disclosure has no mutation path; timezone comment honest.
`user-trial-209.test.ts` updated to the R1 contract (row-scoped label, generic unknown reason) — **6/6 pass**.
`npm run typecheck`: clean.

## 6. Live mounted inspection (no write)

Route `/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view`, FE `:3000`,
Host `:5088` untouched. Real reads only (`editor-document`, `authoring-types` — see §8).
Stored values before/after the session are identical (title `تست`, `عادی`, IRR, `ایران`, freight
`120000.00`, packaging `5000.00`, weights `1000–2000`, delivery `1–3`, week `Asia/Tehran`, Monday
`09:00–17:00`): edit opened → local freight changed to `999999` → «انصراف از ویرایش» → «خروج بدون ذخیره»
→ view restored with `120000.00` and 0 row inputs. No Apply, no PUT/PATCH/DELETE, no domain mutation.
Screenshots (`screenshots/`): `regions-view-1440.png`, `general-edit-1440.png`,
`regions-edit-conditions-open-1440.png`, `regions-view-390.png`. Synthetic/dev content only.

## 7. Honest limits

- No live save replay (as bounded): server-rejection RENDERING is proven by unit tests and source, not by
  a freshly rejected real PUT in this task.
- The screen still shows disabled inputs for name/description/type/settings in view mode (readable but
  disabled); the stricter "text-only General" wording was applied to Regions (the review's concrete
  complaint) and to the type summary; General's controls remain disabled fields.
- Browser interactions were DOM-driven (`Runtime.evaluate`, native value setter), not physical input;
  "no write" is worker observation (no Apply click, no mutating request initiated).
- `RateRowSummary` labels the condition axis with the existing `weightLabel` (now «وزن (گرم)»).
- Timebox exceeded by ≈7 min (§1). TB185 paused; default-OFF/PendingAllocation unchanged.
