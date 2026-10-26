# TB-SHIPPING-ADMIN-USER-FEEDBACK-222 — RESULT

Status: RESULT_POSTED / STOP_FOR_ARCHITECT. Automatic next task: NONE.
Base: `472384b81e5cde3e9e2b45b3e3c7aa2a5c1172f8` (verified with `origin/main`; the local branch was one
docs-only commit behind and was fast-forwarded — no reset/rebase/stash/force-push).

## 0. Pre-existing local work (preserved, not re-designed)

The uncommitted frontend cleanup described in the task was inspected BEFORE any change and kept as-is:
publication UI/API/model removed from the authoring editor, the owner line and repetitive explanatory
text removed, simple `نام`/`توضیح` labels, DB-native language labels, Saturday-first DISPLAY order, and an
unsaved-work warning that appears only when the session is actually dirty. This task additionally commits
that cleanup — no publication UI was restored and no backend publication/capture/pointer code, route,
migration or data was deleted.

## 1. Closed day really disables both bounds

Mounted defect reproduced first on the real editor (edit mode): the two `type="time"` inputs stayed
interactive for a day whose `isWorking` was false.

Repaired in two layers:
- presentation: both bounds receive `disabled={!day.isWorking}` (and a disabled style) in
  `ui/authoring-week-editor.tsx`;
- HANDLER BOUNDARY: `withDayTime` in `model/editor-week.ts` now refuses an unknown weekday or a day whose
  `isWorking` is false and returns the SAME state object (no dirty flag, no revision, no remembered-hour
  write). The greyed input is presentation; the guard is the protection.

Toggling closed still only flips `isWorking`: both remembered hour texts, the ISO weekday and the stored
time text are untouched, so re-enabling restores the exact previous bounds. No default hours, no clearing,
no fraction loss. Wire/validation order remains ISO Monday-first; the Saturday-first order is display only.

Live facts (real browser, real API, my own isolated tab, no Apply, discarded afterwards):
`original {closed, inputs disabled}` → attempt on a closed day leaves start/end `""` and no unsaved banner
→ open, type `08:30`/`12:45` → close: `startDisabled:true, endDisabled:true, start:"08:30", end:"12:45"`
→ reopen: `startDisabled:false, start:"08:30", end:"12:45"`.

Focused tests: `closed-day-222.test.ts` 3/3. Two pre-existing week facts in
`authoring-rates-week-203.test.ts` typed a bound BEFORE marking the day as working; they were corrected to
the truthful order (day opened first) without weakening any assertion.

## 2. Owner control in the primary list — real authority, honest limit

Source inspection first: the only owner authority is
`GET /v1/admin/shipping-services/authoring-owner` (and its seller twin). It binds NO owner/seller/tenant
from the transport — the scope is built server-side after `fulfillment.view` — and returns the SINGLE
authorized owner (`Store`, `Marketplace`, or `Seller` + its business `SellerPartyId`). There is no route
that accepts a client-chosen seller party id or a marketplace/seller LIST. `SellerPartyId` is business
seller identity, not a user account.

Delivered (no fake framework, no guessed GUID, no legacy row import):
- `loadAuthoringOwner` + strict `decodeAuthoringOwner` (+ `authoringOwnerKey`) in
  `api/authoring-method-list-api.ts`. A `Store`/`Marketplace` owner carrying a seller id, a `Seller` owner
  without a valid non-empty one, or an unknown kind is `untrusted`, never a partial owner;
- the list renders a visible owner control populated ONLY from that authorized read, labelled with the
  real business identity (`همین فروشگاه` / `مارکت‌پلیس` / seller business id);
- scope isolation: every load increments a generation counter, rows+context are cleared before the read,
  a superseded response is dropped instead of merged, and the grid identity is scope-keyed
  (`admin-shipping-services-authoring:<ownerKey>`), so saved views/export cannot leak across scopes.

Live facts: the installation is `SingleStore`/`store-alpha`, so exactly ONE lawful option exists —
`{ value: "store", label: "همین فروشگاه" }`, no fake Marketplace entry — with an explicit honest note, and
the one real draft still offers direct View/Edit actions.

Documented gap (not faked): representing an ALTERNATE authorized owner (e.g. Marketplace listing its real
authorized sellers/stores) needs a server route that returns the ACTOR's authorized owner set and an
owner-keyed list/context read. Today's owner route returns one resolved scope and the drafts/context routes
take no owner parameter; adding that is an authorization-surface change, not a frontend filter.

## 3. Real shipping type catalog

Read the current records first via the existing catalog API
(`/v1/admin/shipping-types`, `fulfillment.view`/`fulfillment.manage`), then created only the names recorded
in the approved type decision (`عادی، سنگین، فوق‌سنگین`; `سوپرمارکتی` is also recorded there but was NOT
created — only the two requested labels were authorized here). Codes follow the domain rule
(`a-z`, digits, `_`), so `extra-heavy` is illegal — the canonical codes are `heavy` and `extra_heavy`.

Exact writes on `127.0.0.1:5432/tooba_alpha` (SingleStore/store-alpha), real API only, no SQL seed:

| step | code | id | result |
| --- | --- | --- | --- |
| POST create | `heavy` | `01a1785e-01f3-7000-b50d-d6372e756f89` | 201, `isActive:false`, revision 1 |
| POST create | `extra_heavy` | `01a1785e-6b50-7000-a98f-2e1c80544815` | 201, `isActive:false`, revision 1 |
| PUT activate | `heavy` | same | 200, `isActive:true`, revision 2, sort 20 |
| PUT activate | `extra_heavy` | same | 200, `isActive:true`, revision 2, sort 30 |

Translations were written for all three DB-registry languages (`fa-IR` فارسی default, `en-US`, `ru-RU`);
descriptions are `null` because no policy text was authorized. Creation always lands inactive, so the
explicit update path was used to activate. `standard/عادی` (revision 2) and the draft's assignment were not
touched, and no type was auto-assigned to a draft.

Live facts: the editor type selector now lists `عادی، سنگین، فوق‌سنگین` (+`بدون نوع`) from the API — no
hardcoded frontend option list. Types are categories, not carrier services, and no weight/algorithm policy
was inferred from a name.

## 4. Verification actually executed

- `npx tsc --noEmit` (frontend): clean, run once after final source.
- ALL feature test files found recursively: **127 tests / 127 pass / 0 fail / 0 skipped**.
- Guards: import-boundary, feature-boundary, flat-folder, test-discovery → **7/7**.
- Live trial on the running FE:3000 with real traffic (no `/v1` mocks): closed-day disable + refusal +
  restore, owner control options, direct View/Edit, type options.
- Screenshots in this folder: `list-desktop-1440.png`, `list-mobile-390.png` (390 viewport reports
  `innerWidth 390`, `overflowX 0`, owner control visible). These are CHECKS, not user Visual ACCEPT.

User data preserved: draft `01a16eeb-70b3-7000-87c2-564c82835496` still at **revision 2**, `updatedAt`
`2026-10-24T19:05:52Z`, 1 region group, publication pointer still `selectionVersion 3`. No Apply, no
publication write, no migration (214/219 were already shared-applied by 221 and were NOT reapplied).

## 5. Limits and honesty

- NOT Storefront-integrated: there is still no live selected-plan/projection/selection/commit consumer, so
  no price, eligibility, activation or customer-readiness claim is made.
- The owner control is lawful but single-owner on this SingleStore installation; multi-owner is a server
  authorization gap, not delivered here.
- Backend was not changed by this task; the three backend files present in `git status` are
  line-ending-only local drift and were deliberately NOT staged.
- TB185 remains paused / default-OFF / PendingAllocation.
- Elapsed: actual ≈ 60 minutes vs the 30-minute box (20-minute implementation stop exceeded). Recorded
  honestly; this is NOT a process PASS.

## 6. Live handoff

`http://127.0.0.1:3000/admin/shipping-services` (list + owner control) and
`http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=edit`
(week day toggle, real type list). User Visual ACCEPT: PENDING.
