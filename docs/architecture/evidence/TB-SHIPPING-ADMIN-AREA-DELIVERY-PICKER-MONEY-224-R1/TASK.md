PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224-R1
Parent-Task: TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: BOUNDED_INVALID_RANGE_AND_MONEY_INTEGRITY_REPAIR
Reviewed-Source: d7e3f4e3d72acb588c2d9d0de7f24495d8b90053
HARD25 minutes / stop implementation15 / final10 focused checks+mounted trial+recovery+commit.
ONE correction round. No full suite replay, new feature, seller access, schema or Storefront work.
User requests Architect acceptance BEFORE being called to test. TB224 NOT ACCEPTED yet; never label ready
just because worker tests pass. Preserve the four224 UX changes, compact publication and all user data.

READ FIRST
Fresh TOP AGENTS/canonical recovery/new-chat guide, applicable UI and Analyze->Migrate->Certify skills.
Fetch safe fast-forward; inspect local diff preserve unrelated work/backend line-ending drift.
Persist task verbatim. No reset/stash/rebase/force-push/git add-A/Bridge/historical scripts.

DEFECT A — TYPED RANGE IS NOT THE COMMITTED RANGE
Source confirmed: use-area-range-edits.ts stores partial/invalid pairs in a ref then returns without
writeRates; AreaDeliveryRange owns separate text. The previous valid row values survive while different
invalid text is visible. Dirty/discard/Apply must account for the actual operator's text, not stale rows.
Repair ONE cohesive form-state ownership inside existing authoring drafts/intent:
- keep raw shared min/max per stable area key as controlled state integrated with localDirty and issues;
- every meaningful raw change is local work, including blank/partial/invalid and row-less area text;
- partial/invalid range blocks Apply with field-local issue + owning area focus, never silently saves old
duration, defaults, truncates, normalizes or loses the typed edit;
- valid pair updates exactly that area's row values through ONE composed commit/Apply path;
- first new row inherits only that area's explicit valid choice; legacy unequal row ranges preserved
until explicit valid operator choice, no first/min/max guess;
- confirmed cancel resets shared raw ranges AND rows from SAME confirmed baseline; reject-cancel retains
both exactly. Saving/reconcile/fresh baseline follows existing session latch rules. View/busy guards remain.
- a row-less range cannot persist in the unchanged DTO: localDirty must be honest, prevent silent success
that loses it; show a short localized add-row-or-clear message if Apply would omit it.
- out-of-range/removed group keys never leak to another area, stable key survives reorder.
No second persistent grouping store, uncontrolled local text/ref authority or per-row duration UI return.
Keep DTO/capture format unchanged; shared valid pair remains repeated per row for compatibility.

DEFECT B — INVALID MONEY MUST NEVER TURN INTO ANOTHER VALID AMOUNT
Source confirmed sanitizeMoneyText drops every unsupported character and all but first decimal separator.
Examples "-100" -> "100", "1e3" -> "13", "12.3.4" -> "12.34". Not authorized financial normalization.
Allow ONLY explicit harmless display normalization: Persian/Arabic digits -> ASCII, recognized correctly
formed grouping separators, recognized decimal separator, allowed boundary whitespace under clear policy.
Unsupported signs/letters/exponent/multiple decimal separators/malformed grouping must remain visibly
invalid with validation blocking Apply, or the entire invalid edit must be refused with explicit feedback.
Never remove invalid parts and accept the remaining digits as money; do not accept "12,34" as1234.
Prefer preserving typed invalid text in existing raw draft+issue path; canonical wire only after validation.
No Number/parseFloat arithmetic/rounding; no scale forcing, inferred currency policy or hardcoded amount.
Blank remains blank. 0.50 and1850000.00 keep exact fraction tokens; grouping alone never dirty.
Caret: decimal separator and localized digits need explicit position handling; digit-count-only anchoring
cannot distinguish before/after dot and counts Persian digits as0. Verify normal typing "12" -> "." -> "5"
really yields12.5, not125.; backspace at comma removes intended digit rather than repeatedly reinserting
comma without action. Do not claim helper assertions alone prove browser typing.
Use existing component/design primitives; no package/framework/globalCSS.

RED FIRST, NARROW PROOF
Write focused regressions on SAME helpers/state paths the component uses, demonstrate source defects first:
A initial1..3 -> type blank/partial/reversed/outofrange => dirty, Apply blocked, raw visible, old1..3 never
silently submitted; valid4..9 => all/only own rows updated; reject+confirm discard; empty-area staged range
cannot be silently omitted; legacy conflict unchanged by unrelated edits.
B "-100","1e3","abc100","12.3.4","12,34" never become valid altered amounts; no request on invalid Apply.
Valid120,000.00 roundtrip120000.00; Persian/Arabic grouped paste exact;0.50;blank; decimal typing/deletion/caret.
One typecheck, exact affected test classes and relevant narrow architecture guards only.
No155/144/127/194/Shipping/solution batch; no unrelated size baseline correction.

MOUNTED COMPLETION GATE — REAL DOM, NO USER WRITE
Reuse FE3000/Host5088 after verifying actual identities, installed isolated browser; no /v1 interception.
Initial actual draft read for baseline, no assumption about revision/token. Do not reload user's tab.
- type partial range while another valid local edit exists -> actual Apply disabled + visible issue;
- restore valid range, cancel reject then confirm -> same baseline after reenter, no residue;
- money type character-by-character "12.5", paste invalid negative/letter/multi-dot/grouping -> actual
visible validation/refusal without silently changed accepted money; final display grouping/caret checked;
- publication/destination navigation/closed-day controls quick retained inspection only, no broad matrix.
Zero Apply/PUT/publication/catalog/migration/seed; normal discard local trial work.
Preserve draft, selected capture, existing screenshots and immutable history. No restart for frontend-only.
If runtime impossible report exact blocker and mark mounted proof pending, don't claim READY.

FINISH
docs/architecture/evidence/TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224-R1/{TASK.md,RESULT.md}
and append-only correction to224 RESULT. Record source defect, actual red/green counts, mounted facts,
remaining limits, exact elapsed. Scoped commit/push/fetch parity authorized.
Update ALL3 TOP recovery files: AGENTS.md; docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md;
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md.
Mark224 superseded pending repair review; user must NOT be called to test before Architect acceptance.
Keep compact-order-evidence decision pointer; Storefront still not connected; no seller-admin work.
STOP_FOR_ARCHITECT / Automatic-Next-Task NONE. No next implementation envelope.
