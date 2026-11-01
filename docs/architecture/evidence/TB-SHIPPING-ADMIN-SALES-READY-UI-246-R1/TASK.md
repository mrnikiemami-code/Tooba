PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R1
Parent-Task: TB-SHIPPING-ADMIN-SALES-READY-UI-246
Channel: tooba-main
Program: Tooba Shipping Admin — complete the sales-ready authoring UI
Mode: CLOSE_REVIEWED_VISUAL_DENSITY_ACCESSIBILITY_AND_LIVE_FORM_GAPS
Expected-Base-SHA: d31f6c2385d2a2a95ebb3cf770da7245671582a6
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# Architect review of 246

246 is **PARTIAL, not accepted**. Keep its valid frontend work, but do not treat its worker PASS as delivery.

Measured review findings:

1. The live browser never loaded the authorized draft; it stopped at `loadDenied`. Therefore the real schedule, destinations, rates, language tabs, sticky actions, overflow and editing controls were not exercised on the exact route required by section G.
2. The issued minimum interactive height was 44px, but the delivered groups editor still contains many `min-h-7`, `min-h-8` and `min-h-9` edit/move/delete/title/rate controls.
3. The new focused test mostly checks copy and a duplicated duration constant. It does not prove the requested real 1440/1024/390 layout, seven rendered independent day rows, destination hierarchy/removal, rate handlers/IDs, or the unchanged save path and payload.
4. Destination presentation improved to a breadcrumb chip, but the surrounding range card still uses dense 11px controls and weak hierarchy; this is not yet a consistent presentation-ready admin surface.
5. The task explicitly asked to exercise the existing real draft without saving. That acceptance remains open and cannot be replaced by a shell-only denial receipt.

# User-visible objective

Finish the **same** existing page so it is visibly coherent and presentation-ready with the real authorized draft loaded. Do not create a new page, port, prototype or alternate runtime.

Canonical route:

`http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`

Canonical runtime only: FE3000 → Host5088, SingleStore/store-alpha/tooba_alpha.

# Required skill

Read and follow `.cursor/skills/tooba-frontend-feature/SKILL.md`. Reuse the existing design system and icon vocabulary.

# A — retain the correct 246 work

Keep:

- the compact header, method name and real status badge;
- `ذخیره تغییرات` and `انصراف`;
- removal of the three internal publication paragraphs;
- absence of publish/withdraw actions in edit mode;
- DB-driven language tabs;
- the existing null/duration model and lawful slot-duration values;
- 24-hour FA time presentation;
- existing payloads, handlers, validation, permission and publication behavior.

Do not restore any removed technical paragraph or add replacement marketing/explanatory prose.

# B — complete control sizing and hierarchy

On edit mode, every user-operated button, tab, toggle, input, select and icon action must have a minimum 44px hit target. This includes:

- move up/down, edit, remove and confirm/cancel actions;
- group title and range inputs;
- add destination/add rate controls;
- destination remove action;
- day active/closed toggles and time inputs;
- language tabs and main tabs;
- sticky save/cancel actions.

Text can remain compact, but hit targets cannot be `min-h-7/8/9`. Preserve keyboard focus rings and disabled states.

Use a consistent content width and spacing. Remove remaining 11px primary field/action labels where they reduce readability. Merchant labels and values should be at least the existing normal small/body scale.

# C — finish destinations and rates

For each delivery area card:

- make the localized area title and destination block the first visual hierarchy;
- render each real destination as a clear geography object with icon, verified breadcrumb path and accessible 44px remove target;
- keep multiple destinations wrapping cleanly;
- place `افزودن مقصد` beside/under the destination block as the clear next action;
- render delivery-day range as a compact named subsection;
- render rate rows with readable columns/cards: condition, freight, packaging, delivery days, edit and delete;
- retain exact handler ownership, row IDs, group IDs, reorder, exclusivity and validation;
- visually separate destructive delete-area from reorder and ordinary edit actions.

Remove the remaining top technical intro box if the card structure already explains the section. No backend/process prose.

# D — schedule presentation

With the real stored schedule loaded:

- show one coherent `زمان‌بندی تحویل` card;
- show the current exact choice as either `بازه کاری کامل` or `بازه‌های ساعتی`, mapped to the existing null/duration value;
- show seven independent day cards/rows;
- active day: day name, `فعال`, readable 24-hour from/to range and supported window actions;
- closed day: `تعطیل`, with time controls collapsed;
- do not show AM/PM in FA;
- do not flatten or rewrite independent days/windows;
- keep `بازگرداندن مقدار ذخیره‌شده` secondary.

Do not duplicate lawful duration values inside a test. Import/use the production authority so tests cannot drift from the UI.

# E — responsive presentation

Verify the loaded form at 1440, 1024 and 390 CSS widths without screenshots:

- no horizontal overflow;
- header/sticky actions remain reachable;
- main tabs and language tabs wrap/scroll accessibly;
- day cards become one-column when needed;
- destination chips and rate actions remain readable and do not clip;
- no fixed wide child forces the page wider than the viewport.

Use DOM/layout measurements: innerWidth, clientWidth, scrollWidth and bounding rectangles for key sections.

# F — focused behavioral proof

Add component/DOM facts that render the actual components rather than only checking message strings:

1. exactly one merchant header owns status/save/cancel;
2. banned publication prose and edit-mode publish actions are absent;
3. save button reaches the existing apply callback; no payload/transport path changes;
4. language tabs are produced from an arbitrary DB language list, including a third language;
5. production slot options map exactly to null/duration values;
6. seven distinct stored day states render independently, active/closed correctly;
7. FA times contain no AM/PM;
8. destination breadcrumb and accessible remove call the existing area handler with the original identity;
9. rate edit/delete/reorder call existing handlers with original group/row IDs;
10. all interactive controls in the tested screen have at least a 44px hit target;
11. responsive containers produce no overflow at 1440/1024/390.

Run focused authoring tests, `npm run typecheck`, and one production frontend build only.

# G — real canonical live acceptance

This is mandatory for PASS.

- Use an authorized normal admin session for the exact canonical route. A `loadDenied` shell is PARTIAL.
- Load the real draft and record the real values shown: method name, content languages, stored slot duration, seven day states, destination breadcrumbs, delivery range and rate row.
- Switch both main tabs and every server-provided language tab.
- Exercise locally without saving: change and restore one schedule field, open and close destination picker, open and close one rate edit, verify cancel/back handling without losing the confirmed baseline.
- Do not click Save, publish or withdraw; no PUT/business mutation.
- Leave the exact URL open for user review.
- No screenshots or image artifacts. Provide DOM/layout receipts and the usable URL.

If no authorized admin session can be obtained, stop PARTIAL with the exact blocker; do not call the task PASS.

# Scope and preservation

- Frontend only. No backend, contract, schema, migration, seed or business-data edits.
- No alternate port or duplicate app.
- Keep checked-in gates OFF; TB185 remains paused/default-OFF/PendingAllocation.
- Do not touch checkout/cart/payment/shipping buyer behavior.
- One implementation commit plus evidence/recovery commits; no amend/force/reset/rebase.
- Focus box: 30 minutes (UI 20, proof/live 10), one correction round maximum.
- Update RESULT/RECEIPTS and all three recovery TOPs.
- Worker PASS is not Architect/Visual ACCEPT.
- End STOP_FOR_ARCHITECT; automatic next NONE.

# Worker result envelope

Return exactly:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R1
Parent-Task: TB-SHIPPING-ADMIN-SALES-READY-UI-246
Channel: tooba-main
Worker-Id: <worker>
Agent-Type: cursor
Program: Tooba Shipping Admin — complete the sales-ready authoring UI
Mode: CLOSE_REVIEWED_VISUAL_DENSITY_ACCESSIBILITY_AND_LIVE_FORM_GAPS
Bridge-Task-Row: <row-id>
Status: <PASS|PARTIAL|BLOCKED>
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: d31f6c2385d2a2a95ebb3cf770da7245671582a6
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
