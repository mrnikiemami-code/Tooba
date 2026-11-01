PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R3
Parent-Task: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R2
Channel: tooba-main
Program: Tooba Shipping Admin — prevent and explain overlapping destinations
Mode: ENFORCE_HIERARCHICAL_DESTINATION_EXCLUSIVITY_BEFORE_SAVE
Expected-Base-SHA: cadb45a42201b2e45b7414a7c6af59bc6607a4bc
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# User-reported defect

On the canonical authoring page the user's Save was rejected with:

`shipping.authoring.region_selection_overlap`

The UI kept the edits, but it allowed an invalid overlapping destination tree to be composed and only exposed a generic top-level server error plus the raw code.

Source-confirmed cause at R2:

- `AuthoringDestinationModal` calls `isDuplicateSelection`.
- `isDuplicateSelection` checks only exact `(kind,id)` equality.
- `withGroupAreaAdded` also blocks only exact duplicates.
- Neither path checks verified ancestor/descendant overlap across all region groups.
- The backend correctly rejects the final document under the established hierarchical exclusivity rule.

# User rule — must remain exact

A geographic restriction is defined once across the whole Shipping method:

- selecting a country covers every province/city/service area below it;
- selecting a province blocks its cities/service areas;
- selecting a city blocks its service areas;
- exact duplicates are forbidden;
- multiple sibling regions at the same allowed level remain lawful;
- non-overlapping branches remain lawful;
- the rule applies across all groups, not only inside one card.

Do not weaken the backend rule and do not auto-delete or guess which merchant destination should win.

# User-visible objective

Prevent overlap at the moment the operator presses `افزودن مقصد`, name the conflicting existing destination and region card, keep the staged choice intact, and make any already-composed overlap actionable before Save.

No raw server code in the primary merchant error.

Canonical route/runtime only:

`http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`

FE3000 → Host5088, SingleStore/store-alpha/tooba_alpha.

# Required skill

Read and follow `.cursor/skills/tooba-frontend-feature/SKILL.md`. Reuse the current R2 composition and design system. Do not reopen the visual redesign.

# A — one pure overlap authority

Add one pure frontend model authority that compares a proposed terminal with every selected terminal using only server-proven `ResolvedAreaAncestry` facts.

It must classify:

- exact duplicate;
- proposed ancestor of existing;
- proposed descendant of existing;
- no conflict.

Return a structured conflict containing:

- candidate identity;
- conflicting existing identity;
- conflicting group key/index;
- relationship kind.

Do not compare labels, translated names, array position alone or raw string prefixes. Fail closed when ancestry needed for a decision is unavailable.

Use the same authority for:

1. explicit destination Add;
2. current proposed-tree validation before Save;
3. explanation of a server `region_selection_overlap` rejection.

# B — block invalid Add before state mutation

On explicit `افزودن مقصد`:

- resolve the candidate's verified ancestry from the modal's current Geography read;
- resolve every existing selected area through the composed baseline + session-pick resolver;
- if overlap exists, do not call `onAdd` and do not mutate groups;
- keep country/province/city/service-area fields intact;
- show a localized message such as:
  `این مقصد با «ایران» در «محدوده ارسال ۱» هم‌پوشانی دارد. ابتدا مقصد قبلی را حذف کنید یا مقصد دیگری انتخاب کنید.`
- identify whether the new choice is broader, narrower or exactly repeated where useful;
- provide a close/back action only; never silently replace a destination.

Exact duplicate behavior must use this same result, not a second divergent check.

# C — validate the whole proposed tree locally

Before Save, scan every selected destination across every group:

- detect every exact/ancestor/descendant conflict once;
- surface the issue beside both affected region cards/destination chips;
- disable Save while an overlap remains;
- clicking the summary issue must open `مناطق و تعرفه‌ها` and focus/scroll to the first conflicting destination;
- removing either conflicting destination must clear the issue immediately without resetting unrelated edits.

Do not require a server round trip to discover an overlap that can be proven from the already trusted ancestry facts.

If ancestry is unavailable, retain the existing fail-closed unresolved-destination behavior; do not guess.

# D — server rejection mapping and copy

For `shipping.authoring.region_selection_overlap`:

- map it to the Regions section with a concise localized merchant message;
- derive and display the concrete local conflict pair when the current proposed tree proves it;
- if the pair cannot be derived, say:
  `دو مقصد انتخاب‌شده یک محدوده جغرافیایی مشترک دارند. یکی از آن‌ها را حذف یا محدودتر کنید.`
- keep unsaved edits intact;
- do not display the raw server code in the main alert;
- raw code may remain only inside the existing collapsed technical details intended for diagnostics.

Do not show the vague `سرور مقدار این فیلد را نپذیرفت` when this owned code is known.

# E — current user recovery

Do not modify, delete or auto-save the user's current merchant choices.

- The current browser rejection proves the backend preserved unsaved edits; retain that behavior.
- Do not refresh/close/automate arbitrary user tabs.
- Build/test in the repository first.
- Use the existing task-owned authorized admin profile for live proof.
- Leave the canonical route open with an actionable local overlap explanation.
- The user must choose which conflicting destination to remove; the system must not decide for them.

# F — focused proof

Add meaningful model/component facts:

1. exact duplicate across two groups is blocked;
2. country + child province is blocked in both insertion orders;
3. country + descendant city/service area is blocked;
4. province + child city/service area is blocked;
5. city + child service area is blocked;
6. sibling provinces are allowed;
7. sibling cities under one province are allowed;
8. non-overlapping branches are allowed;
9. conflict result carries the original candidate/existing identities and group ownership;
10. missing ancestry fails closed without inventing a relationship;
11. destination modal keeps staged path and never calls `onAdd` on conflict;
12. whole-tree validation disables Save and clears after either conflict is removed;
13. known server code maps to Regions with localized actionable copy and raw code absent from primary alert;
14. unrelated names, rates, schedule and translations remain untouched after conflict/refusal.

Run focused authoring tests, `npm run typecheck`, and one production frontend build only.

# G — canonical live acceptance without business mutation

Using the task-owned authorized admin session on the exact route:

- load real draft revision 2;
- locally stage country ایران in one group and a proven child destination in another, or the closest lawful fixture supported by real Geography;
- verify the second Add is refused before group mutation and the conflict names the existing group/destination;
- verify no Save/PUT occurs;
- verify removing/closing the staged conflict leaves the original draft and unrelated local fields intact;
- verify an exact duplicate across groups is also refused;
- verify one lawful sibling/non-overlapping choice can be staged and then discarded locally;
- confirm Host editor-document revision/bytes unchanged;
- leave the canonical route available for user review.

No screenshots or image artifacts.

# Scope and preservation

- Frontend-only unless the existing error-catalog mapping is already frontend-owned; no backend rule, contract, schema, migration, seed or data edit.
- Preserve R2 layout; no new visual redesign.
- No alternate port/runtime/page.
- Checked-in gates remain OFF; TB185 paused/default-OFF/PendingAllocation.
- One implementation commit plus evidence/recovery commits; no amend/force/reset/rebase.
- Focus box: 25 minutes (implementation 17, proof/live 8); one correction round maximum.
- Update RESULT/RECEIPTS and all three recovery TOPs.
- Worker PASS is not Architect/User Visual ACCEPT.
- End STOP_FOR_ARCHITECT; automatic next NONE.

# Worker result envelope

Return exactly:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R3
Parent-Task: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R2
Channel: tooba-main
Worker-Id: <worker>
Agent-Type: cursor
Program: Tooba Shipping Admin — prevent and explain overlapping destinations
Mode: ENFORCE_HIERARCHICAL_DESTINATION_EXCLUSIVITY_BEFORE_SAVE
Bridge-Task-Row: <row-id>
Status: <PASS|PARTIAL|BLOCKED>
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: cadb45a42201b2e45b7414a7c6af59bc6607a4bc
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
