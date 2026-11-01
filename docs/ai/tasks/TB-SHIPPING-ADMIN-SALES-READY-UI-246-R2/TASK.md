PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R2
Parent-Task: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R1
Channel: tooba-main
Program: Tooba Shipping Admin — repair the rejected authoring layout
Mode: REBUILD_EXISTING_AUTHORING_LAYOUT_FROM_USER_VISUAL_REJECTION
Expected-Base-SHA: 6e4b15cb9788e2204da92c674a7b862a678279ef
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# User rejection is authoritative

The user visually reviewed the canonical page after 246-R1 and rejected it as: **«خیلی وحشتناک شده این چیه»**.

246-R1 Architect acceptance is revoked. Treat the attached user receipt as the visual source of truth. This is not a small accessibility adjustment; the delivered composition is visibly unsuitable for sale.

Measured problems in the user receipt:

1. The fixed white save bar cuts across the middle of the form and covers content.
2. Save/Cancel exist twice: detached in the header and again in the overlay.
3. The header has excessive empty space and actions are visually disconnected from the title.
4. Radio inputs were enlarged to 44×44 themselves, producing huge circles. The click target must be 44px, not the visual radio glyph.
5. Freight/calculation options are oversized, uneven cards with poor alignment.
6. The slot selector duplicates and contradicts itself: a full-window card is selected while a second “hourly slots” control also shows “full working window”.
7. Seven working days are rendered as tall repeated cards, making the page several screens long.
8. Active/closed state is confusing, especially Friday showing an action and a separate “closed” label.
9. Time inputs consume almost the full width despite containing only HH:mm values.
10. “Restore stored value” floats as a tiny isolated action.
11. Borders and nested white boxes dominate the information hierarchy; the screen looks like a wireframe, not a finished admin product.

# Required skill

Read and follow `.cursor/skills/tooba-frontend-feature/SKILL.md` before editing. Reuse the existing Tooba design system and icon set. Do not introduce a UI framework, global reset or parallel page.

# User-visible objective

Produce a compact, coherent, professional Shipping authoring form on the same canonical route:

`http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`

Canonical runtime only: FE3000 → Host5088, SingleStore/store-alpha/tooba_alpha.

The result must feel like one finished admin product: clear title, one action toolbar, compact form sections, understandable slot controls, and a dense but readable seven-day schedule.

# A — one header and one action toolbar

- Remove the viewport-wide fixed bottom overlay entirely.
- Render **one** action set only: `ذخیره تغییرات` and `انصراف`.
- Place it in a compact page toolbar aligned with the title/method/status.
- The toolbar may be `position: sticky` within the main content under the app header, but it must:
  - remain inside the content column;
  - never cover form fields;
  - have an opaque bounded card/background;
  - occupy normal document space before becoming sticky;
  - contain the only Save/Cancel controls.
- Reduce header whitespace. Back action, title, method name, status and actions must read as one compact unit.
- Keep the three internal publication paragraphs absent.
- Keep publish/withdraw absent in edit mode.

# B — correct 44px accessibility without giant controls

- Interactive **hit areas** remain at least 44px.
- Visible radio/check glyphs must be normal-sized (about 16–20px). Put them inside a 44px label/card target.
- Do not apply `min-h-11 min-w-11` to native radio/checkbox glyphs.
- Use normal switches/checkmarks from the existing design system where available.
- Buttons and inputs may be 40px visually only when their surrounding label/row provides a verified 44px hit area; otherwise use 44px.
- Add visible focus states without thick permanent blue outlines around selected cards.

# C — compact general settings

- Keep DB-driven language tabs and the method fields.
- Use a compact desktop grid:
  - shipping type as a normal field;
  - language/name/description in one clear content card;
  - freight payer and calculation mode as two aligned compact sections.
- Freight payer: one segmented control with `پیش‌کرایه` / `پس‌کرایه`.
- Calculation mode: one segmented control with `دستی` / `خودکار` / `رایگان`.
- Each option has one concise label; no giant radio circle and no excessive empty rows.
- On 390px stack these sections cleanly with no overflow.

# D — one unambiguous delivery-slot selector

Under `زمان‌بندی تحویل`:

- Time zone is one compact field.
- Label: `نحوه نمایش ساعت تحویل به مشتری`.
- Use a single segmented choice:
  - `بازه کاری کامل`;
  - `بازه‌های ساعتی`.
- If full window is selected, do not render a second duration select showing the same full-window value.
- If hourly is selected, then and only then reveal one compact duration selector using the existing production `SLOT_DURATION_OPTIONS`.
- Keep exact null/duration model semantics and payload unchanged.
- Show one short example under the selected mode, not multiple helper paragraphs.
- Move `بازگرداندن مقدار ذخیره‌شده` into the schedule header as a quiet secondary action.

# E — compact seven-day schedule

Desktop/tablet:

- Render one schedule table/list with one compact row per weekday, target height roughly 56–72px.
- Columns/areas: day name | status switch | start | end | supported window actions.
- Time fields must have sensible bounded width for `HH:mm` (not half-page each).
- Working row: one clear `فعال` state and the real 24-hour window(s).
- Closed row: one clear `تعطیل` state; time controls hidden/collapsed.
- Do not render both an “فعال” action and a separate “تعطیل” status for the same closed row.
- Preserve independent per-day values and all existing supported multiple-window behavior.
- No AM/PM in FA.

Mobile 390px:

- Each day may wrap to two compact lines, but it must not become a tall card with large unused space.
- All seven days remain readable without horizontal overflow.

# F — visual hierarchy

- Use fewer borders. Prefer one section card for general settings, one for schedule, and one per region/rate group.
- Avoid a separate bordered box around every small control.
- Use consistent 16/24px internal spacing and clear heading/body/helper hierarchy.
- Keep destinations prominent and rate hierarchy from R1, but apply the same compact visual language.
- Do not add technical explanations or marketing prose.
- Do not change sidebar/global admin shell.

# G — behavioral preservation

No change to:

- editor transport, save payload, IDs or revision handling;
- permissions/publication rules;
- DB language registry;
- Geography exclusivity;
- rate validation, group/rate handlers;
- slot arithmetic or null/duration semantics;
- backend/contracts/schema/migrations/data.

# H — focused proof

Add rendered component/DOM facts, not only source-regex checks:

1. exactly one Save and one Cancel exist in edit mode;
2. no fixed/inset viewport overlay exists;
3. sticky toolbar bounding box stays within the authoring content and does not overlap the next section;
4. native radio/checkbox visual glyph dimensions are no larger than 20px while label hit areas are at least 44px;
5. full-window selection hides the hourly duration selector;
6. hourly selection reveals exactly one duration selector and maps the existing exact duration;
7. seven weekdays render exactly once;
8. closed Friday has one coherent closed state and no time inputs;
9. desktop weekday row height is bounded and HH:mm inputs have bounded width;
10. 1440/1024/390 have no horizontal overflow;
11. save callback, day changes, slot changes and existing destination/rate handlers retain the original identity/payload path.

Run focused authoring tests, `npm run typecheck`, and one production frontend build only.

# I — canonical live acceptance

Mandatory for PASS:

- Use the authorized normal admin session on the exact canonical route.
- Load real draft revision 2 with fa-IR/en-US/ru-RU and ایران destination.
- Exercise tabs and full-window/hourly local switching, restore stored value, active/closed day behavior, destination/rate modal open-close.
- Do not Save, publish, withdraw or send PUT.
- Verify draft revision and bytes unchanged.
- Record DOM/layout measurements at 1440, 1024 and 390.
- Leave the exact route open for the user.
- No screenshots or image artifacts from the worker.

Report PARTIAL if the authorized real page cannot be loaded or if any visible acceptance item remains unverified.

# Preservation and process

- Frontend only; canonical FE3000/Host5088 only.
- No alternate port, duplicate runtime, prototype or mock.
- Checked-in gates stay OFF; TB185 remains paused/default-OFF/PendingAllocation.
- One implementation commit plus evidence/recovery commits; no amend/force/reset/rebase.
- Focus box: 30 minutes (UI 22, proof/live 8), one correction round maximum.
- Update RESULT/RECEIPTS and all three recovery TOPs.
- Worker PASS is not Architect or User Visual ACCEPT.
- End STOP_FOR_ARCHITECT; automatic next NONE.

# Worker result envelope

Return exactly:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R2
Parent-Task: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R1
Channel: tooba-main
Worker-Id: <worker>
Agent-Type: cursor
Program: Tooba Shipping Admin — repair the rejected authoring layout
Mode: REBUILD_EXISTING_AUTHORING_LAYOUT_FROM_USER_VISUAL_REJECTION
Bridge-Task-Row: <row-id>
Status: <PASS|PARTIAL|BLOCKED>
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 6e4b15cb9788e2204da92c674a7b862a678279ef
Base-SHA: <sha>
Code-SHA: <sha-or-NONE>
Evidence-SHA: <sha-or-NONE>
automaticNextImplementationTask: NONE
<full honest result>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
```

Never send placeholder/probe content.
END_TOOBA_TASK
