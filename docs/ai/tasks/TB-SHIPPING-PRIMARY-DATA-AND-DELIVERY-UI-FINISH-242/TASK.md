PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-PRIMARY-DATA-AND-DELIVERY-UI-FINISH-242
Parent-Task: TB-SHIPPING-RECEIVING-SLOT-BOUNDARIES-AND-COMMIT-241-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — original data and polished configured delivery choice
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: RESTORE_ORIGINAL_SINGLESTORE_PRIMARY_AND_POLISH_EXISTING_DELIVERY_PICKER
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

0 — USER REQUIREMENT / WHAT "FINISH" MEANS
Latest user: «زمانش خیلی زشت است قبلا Ui قشنگ تری داشت ... دیتابیس اصلی من نیست ... زمان هم بر اساس تنظیمات
سرویس ارسال لود شود نه همینطوری» then «اره این قضیه رو باید تمامش کنیم خیلی با کیفیت کار ui انجام بده از مهارت ui استفاده کن».
This task is explicitly authorized AFTER 241-R1; no automatic continuation of a previous envelope.
Deliver ONE primary3000/5088 on the user's ORIGINAL SingleStore installation tooba_alpha/store-alpha,
with a polished date/time chooser derived from THAT service's real published configuration, preserving data.
No screenshots/images/image generation/visual capture/viewport campaign. No second version, new app port,
new scheduling authority, test fixture relabelling, or silent Marketplace DB at3000.

Reviewed base c616e38b11c500ef2341d286ff6c6dc504ab34b2 (source03b24cc35, 241-R1 scope accepted).
Use ONE cohesive implementation + handoff; operational focus45min, checkpoint10min original configuration/authority
verified, checkpoint30min UI+real response ready, final15min rebuild/cutover/focused proof/recovery.
These are work checkpoints, NOT a replacement delivery promise. Original delivery deadline MISSED.
Do not spend the box re-running previous owner/payment/history campaigns. Report a material blocker promptly.

1 — REQUIRED SKILL / OWNERSHIP (READ BEFORE CODE)
Apply the actual repository UI skill:
.cursor/skills/tooba-frontend-feature/SKILL.md
Read docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md and
docs/visual-baselines/CRITICAL-STOREFRONT-VISUAL-REVIEW.md, plus root AGENTS.md.
The root read and applied this skill for issuance. Worker must report the exact skill used and relevant reused UI.
User's explicit NO SCREENSHOTS overrides capture instructions. No permission pause is required for authorized UI work.
Read-only Shopeiva reference rule:
.cursor/rules/immutable-shopeiva-reference.mdc
D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva must NEVER be written.
Read it only if needed to recover existing checkout visual language; do not launch a separate template project.
Preserve Home/PDP/header/global CSS and established design system; shipping picker visuals are explicitly in scope.

Existing owner map (no parallel framework):
- app/storefront/storefront-shipping.tsx: orchestrates choices/state/API calls.
- storefront-shipping-group-cards.tsx: group/method summary + existing GroupSchedulePicker and CapturedMethodRow.
- storefront-shipping-date-chips.tsx: DateChipStrip and CapturedSlotPicker.
- storefront-shipping-continue-state.ts: ONE owning validity/blocker authority.
- storefront-shipping-captured-selection.ts: server echo and exact slot intent.
- storefront-shipping-api.ts: typed response/request adapter.
- existing design-system, storefront tokens, actual locale formatting helpers.
Current CapturedSlotPicker uses tiny min-h-8/text11/radio size3 pills, hardcoded FA and bg-white;
legacy GroupSchedulePicker has larger rounded-2xl time choices with Clock and clear selected styling.
Reuse that accepted design vocabulary and accepted server-authoritative captured behavior. Do not revive fixed legacy
9–12/12–15 presets or import legacy business policy as the source of captured slots.
Avoid further growth of the already oversized shipping.tsx. Extract only cohesive presentational responsibility.

2 — RESTORE THE ORIGINAL DATABASE / EDITION, NOT JUST THE PORT
Current primary at receipt: Host5088 PID41232 Marketplace/TenantNULL/tooba_marketplace_tb239,
FE3000 PID18832 (old241 production bundle), user task Chrome CDP9350 PID47688.
Latest user explicitly REVOKES using this trial DB as the main installation.
Restore real SingleStore/store-alpha context backed by ORIGINAL tooba_alpha using preserved pre241 private runtime
configuration/connection reference. Verify actual DB identity through the normal connection/context, edition,
tenant, deployment, market/currency and official migration history BEFORE writes.
Do not merely replace Edition enum while retaining Marketplace connection/context.
Do not merge/copy/import any task Marketplace accounts/carts/publications/captures into original DB.
Keep the entire Marketplace DB and profiles/worktrees intact for optional future explicit use; no second app server.
No drop/reset/reseed/stock replenishment/synthetic renaming/new database/role changes/business SQL/cross-DB copy.

Before changing config, privately back up both runtime configurations and any relevant original form/session state.
Do not publish secrets/connection strings/tokens. Do not submit any user's unsaved admin draft to "save" local input.
Do not enumerate/automate/reload/close arbitrary user tabs; task profile9350 is now user review work, also frozen.
Primary config switch may mean that profile's Marketplace session no longer exists on SingleStore; do not pretend
same session/account/cart survived across databases. Preserve its data and explain the edition switch in the result.

If original DB lacks already-authored forward migrations needed by current code:
produce official MigrationRunner PLAN, verify exact tooba_alpha target, take task-private recoverable DB backup,
then apply ONLY necessary existing pending migrations through the normal runner. Report exact history delta.
User authorizes additive compatibility migrations for restoring this local primary; no history rewrite, no changing
applied231/232/236/241 migrations, no new migration unless an actual source/schema mismatch proves it necessary.
No EF snapshot regeneration, manual ALTER/DDL, adoption of contradictory schema, or migration reset.

Preserve original user draft01a16eeb-70b3-7000-87c2-564c82835496 and its rules/week/rates/translations/pointer,
real stock/accounts/addresses/orders and user local work. Existing nonproduction DEV230 draft
01a17d0d-e033-7000-af43-49a1daa28822 may be READ but not silently used as if it were the user's chosen service.
Do not change a service's duration/range/day/weight/type/pointer to make a screenshot or a green price.

3 — PROVE SETTINGS ARE THE AUTHORITY, ON THIS ORIGINAL INSTALLATION
Trace the actual original service/method binding -> original owner -> selected publication -> immutable weekly
schedule -> duration/weekday hours/timezone ->213 canonical calendar ->nested date/slots response ->picker.
Record a sanitized authority ledger: actual binding, draft/snapshot/revision/format, actual weekday open/close,
slot duration (including null), timezone, response date/slot tick bounds. Product UI must not display these ids.

Configuration lives in the selected shipping method's authoring «برنامهٔ کاری هفتگی» and «مدت هر نوبت تحویل».
It is method-level, NOT one global receiving grid and NOT a geography-row setting.
The old legacy schedule can also expose timeWindows; inspect any existing original service configuration before
claiming it is absent. Preserve its supported behavior. Do not silently combine its mutable windows with a
different selected capture. If authoring changes are unpublished, explain briefly through the existing admin state;
do not substitute draft hours or publish the user's draft without instruction.
Do not add another settings screen, another duration default, second calendar/quote, or client slot generation.

- Config60 inside09–17 =>only server09–10…16–17.
- Config120 =>only09–11…15–17.
- NULL/format1 meansONE real whole authored window, not an inferred60 and not fake hourly choices.
- Closed day none; different weekday/group retains own hours; timezone is server-owned.
- Keep241-R1 bounded integer ticks/no wrap/no short tail/pre-mutation unfit checks.
- Server unavailable/unknown measurement stays honest; do not zero weight/amount conditions or invent measurements.
If original user method cannot quote because its real conditions are unmet, state the exact reason and preserve it;
do not claim finished priced handoff or switch back to Marketplace/test data to conceal it.
For optional controlled configuration tests use existing focused unit fixtures, not edits to the user's service.

4 — UI QUALITY CONTRACT (IMPLEMENT, NOT ONLY A PLAN)
Goal: quiet, coherent storefront checkout design. Recover the established visual vocabulary instead of adding
a new design style or piling small radio pills into the card.

A. One clean delivery section per real group:
seller identity + method name + price remain legible. Then clear ordered date selection followed by time selection.
Use concise labels «روز تحویل» and «ساعت تحویل». Remove repeated whole-day/recommendation/selected-interval
text that competes with the actual chosen subinterval; keep one concise chosen date+time summary.
Recommendation must never look selected. No render/effect auto-selection of a multi-slot day.

B. Date tiles:
reuse existing DateChipStrip styling, consistent spacing/height, aligned weekday/day/month hierarchy,
selected state visible by border/background/check and accessible state; do not change dates coming from server.
Keep date strip independently scrollable on narrow cards without body overflow and without hidden clipping masks.

C. Time choices:
replace tiny mixed radio+text pills with well-spaced selectable rounded cards matching the existing legacy
time chooser/design tokens. Entire card clickable, at least44px touch height, readable13–14px time text,
subtle Clock icon and explicit selected check/state; no extra large decorative artwork.
Use a responsive GRID, two sensible columns on narrow card and more only when actual width permits; min-width0.
No horizontal body overflow, microscopic radio dot, browser-default control dominating the visual, or narrow
unreadable wrapping of a time range. Avoid redundant Clock/check on every tiny element.
Native accessible radios may be visually hidden with peer/focus-visible styling; keep labels/group names unique
per server group+method+date, checked state and keyboard interaction working. Do not use cosmetic clickable divs.
Selected border/background use primary and existing surface/border/text tokens, not unrelated palette/globalCSS.
Use dir=ltr/bdi for actual range; localize visible digits/labels using actual locale, not hardcoded "fa".
No fixed hour strings except explanatory admin copy; use only server slots and safe display formatting.
No count/capacity/limited-remaining/paid delivery promise invented by the UI.

D. States and behaviour:
Before a date: short prompt to choose date, no fabricated slots.
After a date: only its own server slots. A group/date/method change clears only stale affected choice.
Loading/failed/unavailable context never presents old money/date/time as current or enables Continue.
Valid choices in other groups survive an unrelated group choice.
Keep one short owning Continue blocker from the existing helper; selection reason must name the missing group.
Continue enabled only with actual required address+current confirmed response+every required group choice.
ONE selection request and then normal payment navigation. Same chosen interval on payment summary and Order.
Single-window legacy behaviour must remain compatible; do not reopen accepted CAS/goods/coupon algorithms.

5 — FOCUSED VERIFICATION AND PRIMARY HANDOFF (NO SCREENSHOTS)
Run typecheck and exact adjacent picker/intent/continue-state tests, plus test:critical-storefront ONCE.
Known unrelated baseline reds (landing bg-white, source size, UtcNow/packaging) are not a reason for loops/rebaseline.
Use DOM/computed style/accessible states and actual HTTP receipts for this task, no image/capture tools.
Do not claim user Visual ACCEPT from tests or DOM.

Normal original-installation flow:
- Confirm original store identity/real catalog and actual shipping config binding, not task Marketplace fixtures.
- Normal auth/BFF to original account if legitimate retained credentials/session are available; otherwise use normal
  existing product auth and state the account handoff honestly. No OTP bypass/cookie injection/guessing phone number.
  A minimal separately labelled local DEV test buyer/cart via public APIs is allowed if needed; no broad seed wave,
  fake seller, stock, price or published shipping rule.
- Real chosen date -> server-generated time cards; choose one; Continue enabled with no hidden blocker.
- Selection200 carries own selected ticks and exact revision TEXT; payment preview200 shows same chosen interval.
- Do NOT final-submit the USER's review cart. Existing earlier manual Order receipts remain; no PSP campaign required.
- A negative verifies configuration/response change invalidates old choice rather than quietly selecting another hour.

Build BOTH frontend production bundle AND Host from final committed main source; R1 FE stale-bundle condition
must end here. Prepare immutable outputs before bounded swap. Stop verified dev watcher before editing its source.
Replace ONLY verified primary3000/5088 processes, no globalDocker restart/kill-by-name/new user app port.
Verify FE BFF targets5088 and actual originalSingleStore/store-alpha/tooba_alpha after swap; rebuild source parity.
Leave one visible authenticated user-ready primary window in a separately identified task-owned profile if needed;
do not close existing user windows or destroy old trial profiles. No discarded data or duplicate running app versions.
Final URL only http://127.0.0.1:3000/fa/shipping. If blocked, give exact stage rather than an unusable login URL.

6 — RECOVERY / RESULT / GIT
Update AGENTS.md + docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md +
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md with actual edition/DB/source/bundle/config authority,
original-settings provenance, new UI boundaries/skill used, visible session and exact usable stage.
Preserve241/241-R1 history; latest user reversal of Marketplace promotion supersedes that runtime decision only,
not the requirement to keep Marketplace/Seller support in code.
No automatic243, no broad architectural wave. Checked-in gatesOFF; task-local runtime config may enable existing
coherent captured flags as needed, never fake quote/capture fallback. TB185 paused/defaultOFF/PendingAllocation.
Exact-path normal commit/push, no amend/force/reset/stash/rebase/mass add. No secrets in git/docs/results.

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-PRIMARY-DATA-AND-DELIVERY-UI-FINISH-242
Parent-Task: TB-SHIPPING-RECEIVING-SLOT-BOUNDARIES-AND-COMMIT-241-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — original data and polished configured delivery choice
Mode: RESTORE_ORIGINAL_SINGLESTORE_PRIMARY_AND_POLISH_EXISTING_DELIVERY_PICKER
Status: PASS|PARTIAL|BLOCKED|RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: <full issue SHA>
Base-SHA: <actual full source SHA>
Code-SHA: <full SHA or NONE>
Evidence-SHA: <full SHA>
automaticNextImplementationTask: NONE
<only primary URL; actual source/build/edition/tenant/DB; exact UI skill and reused component; real service weekly
configuration and selected publication ledger ->response ->chosen time ->selection/payment preview;
before/after DOM facts and focused checks; preservation/migrations/private backup; user session usable stage;
elapsed and unmet facts. No screenshots, no invented PASS or user Visual ACCEPT>
END_TOOBA_WORKER_RESULT
Append the task-end marker on a separate final line when transmitting the result.

END_TOOBA_TASK
