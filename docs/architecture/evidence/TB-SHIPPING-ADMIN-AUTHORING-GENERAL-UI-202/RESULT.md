# TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202 — RESULT

Mode: `BOUNDED_EXISTING_DRAFT_GENERAL_UI_AND_ATOMIC_APPLY`
Parent: `TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R2`
Base (verified `HEAD == origin/main`): `439c43017ab38bdd6fb6999e26976796365e8140`
Channel: `tooba-main` — Worker: `tooba-worker-01`

## 1. What is now actually rendered

- One thin Next route: `src/frontend/app/admin/shipping-services/authoring/[draftId]/page.tsx`.
  - Server component only: GUID/zero-id route identity validation (`notFound()` otherwise), UI-only
    `?mode=view|edit` (never permission/owner authority), interface locale from the canonical
    `tooba_locale` cookie, no token/secret in HTML, no business/transport code.
- Composed public screen `ShippingAuthoringScreen` exported from the feature `index.ts`
  (`src/frontend/features/admin-shipping-authoring/ui/authoring-screen.tsx`), two-part
  General / Regions layout on existing design-system primitives.
- Reachable additive entry `ShippingAuthoringDraftEntry` in `flat-method-detail-dialog.tsx`, ONLY when
  `mode === "view"` AND the general tab is active AND the server-resolved binding exists. It reads the
  authorized `GET /v1/admin/shipping-services/authoring-drafts` on deliberate click, matches the EXACT
  `(serviceCode, optionCode)` binding (0 = honest no-draft, >1 = ambiguous/untrusted, auth/network =
  error), and navigates to `?mode=view`. No methodCode split, no owner/tenant picker, no automatic
  POST/create, no per-row N+1 read, no new grid/row action, legacy view/edit/delete/rate/policy flow
  untouched.

## 2. Exact UI / data / state boundary

- Decoder (`api/editor-document-decode.ts`) now strictly validates the ADDITIVE `destinationFacts`:
  only selected `(kind,id)` keys, no duplicates, no unselected entries, canonical per-kind write shape,
  terminal identity proven, `Resolved ⇔ area present` (contradictory payload fails closed), and every
  selected key must have exactly one fact (no corrupt fact is filtered away). Absent field ⇒
  `destinationFacts: null` = explicitly unavailable; no fact is ever invented.
- `model/editor-destination-ancestry.ts` is the ONLY ancestry source: synchronous lookup over those
  validated facts, `labelOf` (never a raw GUID as a name), `unresolvedSelections`, and
  `destinationFactsBlockApply`. No world scan, no guessed parent, no global cache.
- `model/editor-form.ts` holds local general-form intent and emits only changed active-language
  name/description, both-or-none settings, and an explicit `{shippingTypeId:null}` clear. Inactive
  stored text is displayed read-only and never emitted.
- `session/editor-controller.ts` orchestrates load / edit / ONE apply / explicit reconcile with a
  synchronous in-flight gate and epoch+requestId scoping; `session/editor-session.ts` (accepted 200)
  stays the single state authority. No second store, no localStorage draft, no automatic retry.
- AUTHORIZED compatibility fix in `api/editor-document-request.ts`: the admin-area requirement is now
  narrowed to an `AdministrativeArea` selection itself, so City and ServiceArea with a lawful
  `administrativeAreaId: null` serialize; deeper-field/terminal identity validation is unchanged and
  no backend rule/DTO changed.
- Read-only regions show real stored group order/currency/titles, proven geography labels, and every
  stored rate row (condition axes, exact decimal text, delivery days) without duplicated geography.
  Read-only week shows the real timezone and the seven stored day states; an unconfigured schedule is
  shown as unconfigured, never a fabricated seven-day default.
- Resolved owner is displayed read-only (Store/Marketplace/Seller). No activation toggle, no invented
  readiness, no cutoff/carrier/funding control pretending persistence.

## 3. Commands actually run (all inside the timebox)

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test features/admin-shipping-authoring/authoring-general-ui.test.ts` | PASS 9/9 (new focused suite) |
| `node --experimental-strip-types --test ... repair-200-r1 / editor-document-adapter / editor-session / editor-session-200-r2 / authoring-general-ui` | PASS 38/38 |
| `node --experimental-strip-types --test lib/architecture/frontend-import-boundary.guard.test.ts lib/architecture/frontend-feature-boundary.guard.test.ts lib/architecture/frontend-test-discovery.guard.test.ts` | PASS 5/5 |
| `npm run typecheck` | PASS (clean) |

Focused suite coverage (`authoring-general-ui.test.ts`):

1. 201 facts → canonical ancestry with a null-admin city AND a null-admin service area.
2. Null-admin city/service-area atomic PUT roundtrip: retained groups/rows/currency/geography and
   `expectedRevision` preserved; one `methodSettings` write.
3. Facts fail closed: missing selected fact, duplicate, contradictory `Resolved` + `null` area,
   wrong terminal, absent field ⇒ `null` (unavailable).
4. Form intent: untouched ⇒ `{}`; one active language only; never the inactive stored raw text;
   explicit description clear → `null`; one-sided settings → `settingsIncomplete`; explicit type clear.
5. Controller: load → edit name/settings/type → ONE PUT → adopt the real response; `loads === 1`
   (no unnecessary post-save GET).
6. Double Apply while in flight ⇒ exactly ONE request.
7. 403 and 409 keep edits, no automatic retry.
8. `outcomeUnknown` stays latched, blocks Apply, and only an explicit reconcile read (revision 7)
   unblocks; `saves` stays 1.
9. Binding entry: exact 1 / 0 / ambiguous>1, and no create path exists.

## 4. Visual verification

`Visual: NOTRUN`. No already-running dev app/session was confirmed for this task, and starting one
would have crossed the hard bounds and the deadline. No Visual ACCEPT is claimed; layout was only
validated through source/type checks and the accepted 186-R2 two-part reference structure.

## 5. Files

Added (`src/frontend/features/admin-shipping-authoring/`):
`authoring-core.ts`, `messages/authoring-messages.ts`, `model/editor-destination-ancestry.ts`,
`model/editor-form.ts`, `api/authoring-lookup-api.ts`, `session/editor-controller.ts`,
`ui/authoring-format.ts`, `ui/use-editor-controller.ts`, `ui/authoring-general-tab.tsx`,
`ui/authoring-regions-panel.tsx`, `ui/authoring-screen.tsx`, `ui/authoring-draft-entry.tsx`,
`authoring-general-ui.test.ts`.
Added: `src/frontend/app/admin/shipping-services/authoring/[draftId]/page.tsx`.
Modified: `index.ts` (public boundary now also exports the rendered capability; non-rendered surface
moved to `authoring-core.ts` so Node tests stay loadable), `model/editor-document.ts`,
`api/editor-document-decode.ts`, `api/editor-document-request.ts` (authorized null-admin narrowing),
`api/editor-document-api.ts` (shared header/failure helper only),
`app/admin/shipping-services/flat-method-detail-dialog.tsx` (VIEW-only entry composition), and the four
existing feature tests' import path (`./index.ts` → `./authoring-core.ts`) — no assertion changed.

## 6. Deviations / limits

- Working tree contained unrelated pre-existing untracked `bin/obj` artifacts and evidence files; none
  were touched, added or removed.
- `authoring-core.ts` indirection exists only to keep the rendered boundary in `index.ts` while the
  repository's Node test loader cannot parse `.tsx`; behavior is unchanged.
- Region/rate/week editing, whole-method creation, activation, cutoff/carrier/funding controls remain
  deliberately unimplemented in this slice (read-only display only).

Production-Functional-Acceptance: PENDING
Schema-Data-Flag-Changes: NONE
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

## R1 correction (2026-10-23, append-only)

The Architect review of this result is in `ARCHITECT-REVIEW.md` (REPAIR_REQUIRED): the form/session
dirty state above was derived from serializable edits only, so blank/incomplete/description-only input
looked clean; Cancel Edit bypassed the discard decision; and the hook captured the first draft id with
no cleanup. See `docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202-R1/RESULT.md`
for the repairs (raw local dirt separated from the wire patch, one explicit exit plan, per-draft
controller lifecycle and a seeded form). The statements above remain as the historical 202 claim.
