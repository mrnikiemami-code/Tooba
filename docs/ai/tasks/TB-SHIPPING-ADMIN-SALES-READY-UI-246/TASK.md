PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SALES-READY-UI-246
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R6
Channel: tooba-main
Program: Tooba Shipping Admin — sales-ready authoring UI polish
Mode: POLISH_EXISTING_AUTHORING_SCREEN_WITHOUT_CHANGING_BUSINESS_RULES
Expected-Base-SHA: e1714cb64c468bba3b18ad2dae4cf02aa30f0a74
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# User-visible objective

Turn the existing Shipping method authoring screen into a coherent, professional, sale-ready admin experience on the **canonical application**. This is a visual/information-architecture repair of the existing screen, not a new screen, design system, backend contract or parallel version.

The two user receipts are the acceptance reference. The current page is too technical, text-heavy and visually weak:

- internal publication explanations dominate the header;
- actions are scattered across separate boxes and «اعمال» is vague;
- «یک پنجره کامل روز (بدون نوبت ساعتی)» is not understandable;
- the weekly schedule is a long flat form with weak day boundaries and AM/PM inputs;
- destinations are tiny text fragments, while the delivery range/rate is the dominant card;
- hierarchy, spacing, grouping and responsive behavior do not look ready for customer delivery.

# Required skill and design constraints

- Read and follow `.cursor/skills/tooba-frontend-feature/SKILL.md` before editing.
- Reuse the existing Tooba design tokens, controls, icons and components. No new UI framework, global CSS reset or isolated mock page.
- Keep RTL/FA first and preserve every DB-provided language tab; never hardcode the language registry to FA/EN.
- Use plain merchant language. Remove implementation/history/activation commentary from the visible UI.
- Preserve all business behavior, IDs, payloads, validation, permissions, publication rules and API ownership.

# A — professional page header and actions

- Replace the current title/action/publish blocks with one compact page header:
  - breadcrumb/back action;
  - title `ویرایش روش ارسال` and method name;
  - one concise status badge such as `منتشرشده` / `پیش‌نویس` from the real state;
  - primary `ذخیره تغییرات`, secondary `انصراف`.
- Remove these visible paragraphs entirely:
  - `نسخهٔ منتشرشده انتخاب شده است.`
  - `انتشار فقط از حالت مشاهده اعمال می‌شود.`
  - `این انتخاب فقط نسخهٔ منتشرشده را مشخص می‌کند؛ اتصال آن به سفارش مشتری هنوز فعال نشده است.`
- Do not replace them with another paragraph. Publication actions that are not valid in edit mode stay absent/disabled according to the existing rule; status is communicated by the badge only.
- Make the save action sticky/consistently reachable on long forms without duplicating actions.
- Rename vague `اعمال` to `ذخیره تغییرات` in FA and the correct localized equivalent elsewhere.

# B — clear sections and density

- Keep the existing two main tabs but rename/clarify them as merchant concepts:
  - `اطلاعات روش`
  - `مناطق و تعرفه‌ها`
- Use a consistent max-width, 24px section rhythm, clear section headings, short optional helper text and fewer nested borders.
- Remove technical/redundant helpers that restate labels or backend behavior. Keep only guidance needed to make a merchant decision.
- Preserve the DB-driven language tabs. Replace the technical translation-count sentence with compact coverage/status indicators only if useful; do not invent missing translations.
- Payment timing and calculation mode become compact selectable cards/segmented controls with clear labels and one-line consequences, not tiny radio text spread across the width.

# C — understandable receiving schedule

Replace the unclear duration selector and flat weekly rows with one cohesive `زمان‌بندی تحویل` card.

## Slot behavior selector

- Label: `نحوه نمایش ساعت تحویل به مشتری`.
- Present the existing lawful choices as understandable visual options:
  - `بازه کاری کامل` — example text `مثلاً ۹:۰۰ تا ۱۷:۰۰`;
  - `بازه‌های ساعتی` — show the existing configured duration choices such as ۶۰/۹۰/۱۲۰ minutes from the current options.
- Do not invent duration values or change slot arithmetic. The selected value must remain the exact existing model value (`null`/duration).
- Explain the result with a live, compact example only: `مشتری بازه‌های ۹–۱۰، ۱۰–۱۱… را می‌بیند`.

## Weekly schedule

- Render seven clearly separated day rows/cards.
- Each row must show:
  - day name;
  - clear `فعال` / `تعطیل` toggle;
  - existing time window(s) as readable 24-hour FA ranges (`۰۹:۰۰ تا ۱۷:۰۰`);
  - existing add/remove/edit window controls where supported.
- Closed days must collapse time inputs and visibly say `تعطیل`.
- Do not display browser AM/PM text in the FA interface.
- Preserve independent per-day and multiple-window behavior already accepted in the admin domain. Do not normalize all days to one schedule.
- `بازگرداندن مقدار ذخیره‌شده` becomes a quiet secondary action inside this card, not a competing primary button.

# D — destinations and rates must be primary, readable objects

Redesign the existing `مناطق و تعرفه‌ها` tab without changing its data model.

- Replace tiny destination fragments such as `کشور: ایران ×` with prominent destination chips/cards, minimum 40px control height, clear geography icon and hierarchy:
  - country / province / city path as supplied by the existing Geography data;
  - accessible remove action;
  - wrapping layout for multiple destinations.
- `افزودن منطقه` becomes a clear primary action inside the destination section.
- Preserve the single geographic restriction and hierarchical exclusivity rules; no duplicate geography picker.
- Each shipping range becomes a clean card with:
  - editable localized title;
  - prominent destinations;
  - delivery-day range;
  - rate rows presented as a readable table/card list with condition, freight, packaging, delivery days and edit/delete actions;
  - move/delete range actions visually secondary and destructive action clearly separated.
- Replace technical sentence `محصول‌هایی که هزینه یکسانی دارند...` with a short merchant heading/description or remove it if the section already explains itself.
- Empty states must explain the next action in one sentence and show the relevant button.

# E — responsive, accessible and visually consistent

- Desktop target: 1440/1366 width with no excessive empty columns or stretched fields.
- Tablet/mobile target: 1024/390 with one-column cards, no clipped actions and no horizontal overflow.
- Use 44px minimum interactive height, visible focus rings, associated labels, keyboard-operable tabs/toggles and proper `aria` state.
- Do not rely on color alone for published/active/closed state.
- Keep typography readable: section title, field label, value, helper in a consistent four-level hierarchy.
- Avoid decorative gradients, oversized hero areas and extra marketing copy; this is a serious admin tool.

# F — focused proof

Add focused source/DOM/component facts for:

1. the three unwanted publication paragraphs are absent;
2. one header owns status, save and cancel; no duplicated publish/action panel;
3. `ذخیره تغییرات` uses the existing save path and payload unchanged;
4. DB-driven language tabs remain dynamic;
5. slot selector labels map exactly to existing null/duration values;
6. FA time ranges render in 24-hour form without AM/PM;
7. seven independent day rows preserve active/closed and multiple-window values;
8. destination chips/cards expose hierarchy, accessible removal and retain exclusivity validation;
9. rate range/rate row edit/delete/reorder events retain existing handlers and IDs;
10. 1440/1024/390 layout has no horizontal overflow and actions stay reachable.

Run focused frontend tests, `npm run typecheck`, and one production frontend build. Do not run backend or broad solution suites.

# G — canonical live preview and handoff

Use only the canonical application:

- FE `http://127.0.0.1:3000` → Host `http://127.0.0.1:5088`;
- SingleStore / `store-alpha` / `tooba_alpha`;
- existing route and draft:
  `http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`.

Deliver the **same route** ready for the user to inspect. No alternate port, duplicate page, static prototype or task buyer.

Practical acceptance without business mutation:

- load the real draft and verify its existing values render in the polished controls;
- switch tabs/languages locally and exercise schedule/destination/rate edit controls without saving;
- verify cancel/back behavior;
- leave the draft/database unchanged and do not publish/unpublish/save;
- provide DOM/layout receipts and the usable URL. No screenshots or image artifacts.

# Preservation and process

- Frontend only. No backend/contract/domain/migration/schema/seed/data edits.
- Preserve owner permissions, publication state, Geography exclusivity, DB language registry, rate rules, weekly windows and slot-duration semantics.
- Checked-in gates remain OFF; TB185 stays paused/default-OFF/PendingAllocation.
- Hard focus box: 35 minutes (UI 25, focused proof/live 10); one correction round maximum; stop honestly at cutoff.
- Update evidence and all three recovery TOPs. One implementation commit plus evidence pin; no amend/force/reset/rebase.
- Worker PASS is not Architect/Visual ACCEPT. End `STOP_FOR_ARCHITECT`; automatic next task `NONE`.

# Worker result delivery contract

Return exactly:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SALES-READY-UI-246
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R6
Channel: tooba-main
Worker-Id: <worker>
Agent-Type: cursor
Program: Tooba Shipping Admin — sales-ready authoring UI polish
Mode: POLISH_EXISTING_AUTHORING_SCREEN_WITHOUT_CHANGING_BUSINESS_RULES
Bridge-Task-Row: <row-id>
Status: <PASS|PARTIAL|BLOCKED>
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: e1714cb64c468bba3b18ad2dae4cf02aa30f0a74
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
