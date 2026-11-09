# RESULT — TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2

Worker result only. **Worker PASS is not Architect/user ACCEPT.** Scope was exactly one repair: the
user-reported «ذخیره نمی کنه» on the two-calendar authoring form. UI appearance was already approved
and was not changed.

## 0. Summary

| Item | Value |
|---|---|
| Status | **PASS** within the issued save-repair scope |
| Decision | `CARRIER_ONLY_SCHEDULE_PATCH_OMITTED_FROM_SERIALIZABLE_EDIT_PREDICATE_REPAIRED_AND_REAL_BROWSER_SAVE_ROUNDTRIP_PROVEN` |
| Base | `9543b149e1d27210f823fafb94f80cd707b818be` (matches issued Expected-Base-SHA) |
| Root cause | `editor-session.isDirty()` did not consider `carrierSchedule` |
| Real failure stage | **client-side Apply gate** — Save was enabled, the click sent **ZERO PUT** (no request, no error, no notice); a reload showed the previous values |
| Fix | one capability added to that predicate (plus a comment and a focused regression suite) |
| Migration | none |
| Backend change | none (no publish needed) |
| Draft used for proof | task-owned `01a1beed-d7df-7000-bfc8-0185ea61afe3`, revisions 3 → 8 |
| User draft | `01a17d0d-e033-7000-af43-49a1daa28822` read only — still revision 2, `updatedAt` 2026-10-27, unchanged |

## 1. The reproduced defect (before any change)

Real browser, real route, real button, task-owned draft at revision 3, fresh headless profile:

```
GET  /v1/admin/shipping-services/authoring-drafts/01a1beed-…/editor-document   200  revision 3
UI   carrier tab → weekday 3 close 13:00 typed to 14:00
     Apply("ذخیره تغییرات").disabled = false      ← the screen said "you can save"
     unsaved hint present
CLICK Save
     PUT  count = 0                                ← nothing was ever sent
     no role=status notice, no role=alert, mode stayed edit, unsaved hint stayed
RELOAD
     carrier weekday 3 close = 13:00               ← the edit was lost
```

Raw facts: `docs/architecture/evidence/…-256-R2/dom/save-before-fix.json`
(`putCountAfter: 0`, `documentGets: 2`, `afterReload.close: "13:00"`).

The user's own draft has the same class of path: revision 2, **no** carrier schedule at all, one
resolved Country group with one rate row — i.e. the operator's real next step is to configure/copy the
carrier calendar and save it, which is exactly the branch that was blocked.

## 2. Root cause (exact)

`src/frontend/features/admin-shipping-authoring/session/editor-session.ts`

The session keeps the confirmed baseline apart from the edit intent and gates the ONE PUT twice:

1. `reduceEditorSession` on `saveStarted` refuses when `!isDirty(state.edits)`;
2. `selectApplyRequest` returns `{ status: "blocked", reason: "no-changes" }` under the same predicate.

`isDirty` listed `regionGroups`, `methodTranslations`, `weeklySchedule`, `methodSettings` and
`typeAssignment` — but **not** `carrierSchedule`, the capability TB-256 added. TB-256 taught
`buildEditorDocumentBody` to serialize it and `intentHasEdits` to count it, but the session predicate was
left behind. Consequence for a carrier-only patch:

- the screen's `canApply` was TRUE (it uses the composed intent plus `session.unsaved`, and
  `session.unsaved` was true because the raw local-dirty signal from `carrierWeekDirty` fired), so the
  operator saw an enabled «ذخیره تغییرات»;
- `controller.apply()` then hit the session gate, which classified the very same patch as
  `no-changes`, returned before any `fetch`, and the screen's `apply()` returns silently for any outcome
  that is not `sent`.

That is a capability-level divergence between two authorities for the same patch — not a disabled
button, not server validation, not a lost response, and not a rehydration problem. Customer-week-only,
general, rates or groups edits were unaffected because those capabilities were already listed.

## 3. The repair

`session/editor-session.ts` — `isDirty()` now counts the independent carrier calendar:

```ts
function isDirty(edits: EditorEdits): boolean {
  if (edits.regionGroups !== undefined) return true;
  if (edits.methodTranslations !== undefined) return true;
  if (edits.weeklySchedule !== undefined) return true;
  // TB-256-R2: the carrier-acceptance calendar is an INDEPENDENT serializable capability. …
  if (edits.carrierSchedule !== undefined) return true;
  if (edits.methodSettings !== undefined) return true;
  return edits.typeAssignment !== undefined;
}
```

Nothing else changed: no new endpoint, no new state machine, no revision/owner relaxation, no
suppressed validation, no parallel save path, no UI copy or layout change. The predicate now matches
`intentHasEdits` in `model/authoring-intent.ts` (all six serializable capabilities).

Touched source files:

- `src/frontend/features/admin-shipping-authoring/session/editor-session.ts` (the one-line-class repair)
- `src/frontend/features/admin-shipping-authoring/authoring-save-carrier-only-256-r2.test.ts` (new focused suite)
- `scripts/tb256r2-authoring-save-dom.mjs`, `scripts/tb256r2-authoring-save-cases.mjs` (task-owned proof scripts)

## 4. Real save proof — canonical browser, same database

Same source, same FE3000 → Host5088, same `tooba_alpha`, task-owned draft `01a1beed-…` (identity
confirmed first: `ownerKind: Store`, `serviceCode: tipax`, `optionCode: standard`, revision 3).

### 4.1 Carrier-only update — the previously failing case

```
UI    carrier tab → weekday 3 close: 13:00 → 14:00
      Apply.disabled = false, unsaved hint = true, no local issue
CLICK Save
PUT   /v1/admin/shipping-services/authoring-drafts/01a1beed-…/editor-document   200   (exactly ONE)
      body keys: ["expectedRevision","regionGroups","methodTranslations","carrierSchedule"]
      expectedRevision: 3
      carrierSchedule.timeZoneId: "Asia/Tehran"
      carrierSchedule.days[weekday 3]: 10:00:00 – 14:00:00
      weeklySchedule present in body: false            ← customer week PRESERVED, never echoed
UI    status: «تغییرات ذخیره شد (بازبینی 4).» · mode → view · unsaved hint gone · 0 alerts
GET   fresh document: revision 4, carrier weekday 3 = 10:00–14:00, customer week still 09:00–17:00
RELOAD same route: carrier weekday 3 close = 14:00, apply disabled, 7 carrier rows
```

Raw: `dom/save-after-fix.json` (`putCountAfter: 1`, `puts[0].status: 200`,
`hasWeeklySchedule: false`, `afterReload.close: "14:00"`).

### 4.2 Customer-only update retaining the carrier calendar

Customer weekday 3 start 09:00 → 10:00 → one PUT 200 with `weeklySchedule` and **no** `carrierSchedule`
(revision 4 → 5). Reload: customer 10:00–17:00 **and** carrier weekday 3 still 10:00–14:00.
Reverted to 09:00 with a second real save (revision 5 → 6); carrier still 10:00–14:00.
Raw: `dom/save-customer-only.json`, `dom/save-customer-restore.json`.

### 4.3 One-time copy, then an independent edit

Pressed «کپی از ساعات تحویل مشتری» (carrier became the customer week: weekdays 1–5 09:00–17:00, 6–7
closed), then changed ONLY carrier weekday 3 close to 16:00 → one PUT 200 carrying only
`carrierSchedule` (revision 6 → 7). Reload: carrier weekday 3 = 09:00–16:00, weekdays 6–7 closed, and
the **customer week still 09:00–17:00 on all five working days** — the copy is a starting point, not a
link. Raw: `dom/save-copy-then-edit.json`.

The draft was then restored to its documented shape by one more real save (revision 7 → 8): carrier
weekday 3 = 10:00–13:00, weekday 5 closed, weekdays 6–7 accepting 09:00–17:00, customer week untouched.
Raw: `dom/save-carrier-restore.json`. Final read: revision 8, `updatedAt` 2026-11-09T22:32:00Z.

### 4.4 Invalid save, rejection and unknown outcome (deterministic focused paths)

Not exercised as a browser write against the live draft (that would mean vandalising the reviewed
calendar to prove a rejection); they are pinned where they are deterministic:

- `authoring-save-carrier-only-256-r2.test.ts` — a rejected carrier-only save (`validation`) keeps
  `edits.carrierSchedule` byte-identical and stays `unsaved` for correction; the confirmed save clears
  the intent so a second Apply cannot resend it.
- `authoring-carrier-schedule-256.test.ts` — carrier rejections are placed on the carrier section and
  never on the customer week (existing server-field targeting), invalid calendars block Apply in their
  own tab, a typed-but-unconfigured time zone is real work that still blocks.
- Existing session suite — `conflict` and `outcomeUnknown` keep local edits, latch the unknown commit
  and never retry automatically (unchanged by this repair).

## 5. Verification actually executed

| Check | Result |
|---|---|
| `authoring-save-carrier-only-256-r2.test.ts` (new) | **4/4** |
| `session/editor-session.test.ts` + `editor-session-200-r2.test.ts` | **23/23** (incl. the 4 new) |
| `authoring-carrier-schedule-256.test.ts`, `authoring-timing-tabs-256-r1.test.ts` | 10/10 + 3/3 |
| `authoring-carrier-calendar.mount.test.ts` (mounted React) | **6/6** |
| `npm run typecheck` | 0 errors |
| `npm run build` | 0 errors, BUILD_ID `uXwhPh0ZnBqFWcpxujHPd`, served on FE3000 |
| Real browser save roundtrip (4 saves, 4 reloads) | as §4 |

No backend test suite was run because no backend file changed; no DB-dependent suite was run because
the task forbids reaching for another database.

## 6. Deployment and environment

- Source: `D:\Users\User\source\repos\SarvNewVer`, branch `main`.
- FE rebuilt and restarted on the canonical instance: old PID 38148 → new PID 20404,
  `next start -p 3000 -H 127.0.0.1`, `TOOBA_HOST_ORIGIN=http://127.0.0.1:5088`,
  served `BUILD_ID` equals the built `BUILD_ID` (`uXwhPh0ZnBqFWcpxujHPd`); `/fa` 200.
- Host 5088 untouched (PID 21536, `/health` 200) — no rebuild, no restart, no publish.
- Database: `tooba_alpha` only. No migration, no schema change, no DDL, no business SQL, no reset/reseed.
  All reads/writes went through the owning authoring API.
- DataProtection untouched; CapturedPreview local ON / checked-in OFF; Tipax deferred; 255 parked.
- No checkout, payment, packing, parcel, dispatch, delivery, cancel or publication mutation. No new
  draft, order or account. The user's draft was never written: it is still revision 2 with its original
  `updatedAt`, and no Publish/withdraw was invoked.

## 7. What is still open (and what this result does NOT claim)

- **Not** Architect acceptance and **not** the user's visual/functional acceptance. The next step is
  the user's own save on their draft
  `http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`
  (configure/copy the carrier calendar → save → reload).
- The backend roundtrip for a real published service (new commitments requiring a configured carrier
  calendar) is still code/fixture-proven only; the user's method has no configured carrier week.
- The save repair does not reopen delivery calculations, the MAX/cutoff/planned-dispatch evidence, or
  any operational UI.
- Five-item ledger (unchanged, NOT issued): tracking uniqueness; correct remaining quantity;
  non-delivery/return/resend; shared-group common-or-separate parcels; due workqueue/shared operational
  order view. Planned 257 (first two), 258, 259, 260 remain **NOT ISSUED** — the Architect issues the
  next task only after reviewing this result.
- Unrelated pre-existing site-wide `OPTIONS http://localhost:3000/fa` 400 CORS preflight (plain GET 200)
  was observed in network logs and was not "fixed" here.
- Pre-existing architecture guards (`frontend-source-size` 18-finding family) are untouched: no
  baseline JSON was edited and the changed file was already within that family at base.

## 8. Review URL

`http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
