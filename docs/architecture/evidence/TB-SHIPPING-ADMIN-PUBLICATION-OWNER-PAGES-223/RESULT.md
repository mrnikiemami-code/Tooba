# TB-SHIPPING-ADMIN-PUBLICATION-OWNER-PAGES-223 — RESULT

Parent: TB-SHIPPING-ADMIN-USER-FEEDBACK-222
Base: `d62bab9a4f31dc1966aed8c06b4f7ea08a366df3` (fast-forwarded from `063aa05f`; the untracked 223 evidence
folder collided with the tracked `TASK.md` of that commit, so it was moved aside, the tree fast-forwarded, and
the artifact restored. No reset/stash/rebase/force-push.)

Mode: BOUNDED_RESTORE_COMPACT_PUBLICATION_AND_SEPARATE_OWNER_ENTRY.

## A — publication restored, compact (recovered from 221, semantics unchanged)

Recovered byte-exact from `443c75817` (not re-invented):
- `features/admin-shipping-authoring/model/authoring-publication.ts` — `stale` / `unknown` latches, synchronous
  in-flight gate (`beginWrite` returns `null`), phase comparison against the CONFIRMED baseline revision.
- `features/admin-shipping-authoring/api/authoring-publication-api.ts` — three real routes
  (`GET …/publication`, `POST …/publication`, `POST …/publication/withdraw`), strict decode, exact
  `expectedRevision` + `expectedSelectionVersion` tokens, `outcomeUnknown` never retried.

New presentation `ui/authoring-publication-panel.tsx` — SMALL: one status line, at most one short reason,
one honest note, three buttons and a confirmation dialog. Deliberately ABSENT from the normal UI: snapshot id,
selection-version counter, format version, revision prose card. Writes are refused outside clean VIEW; a
390/403/409 latches `stale`, an unestablished outcome latches `unknown`, and only an explicit successful
`GET` clears either. `onPublished` only feeds the screen's existing notice: the panel never reseeds the
document, advances the draft revision or discards typed work.

Composed in `ui/authoring-screen.tsx` only when `session.load === "loaded"`, with the confirmed baseline
revision as the expected token.

Trial (real local runtime, no `/v1` interception): existing draft `01a16eeb-70b3-7000-87c2-564c82835496`
reported `state = Selected`, `selectedMatchesCurrentDraftRevision = true` ⇒ status «نسخهٔ منتشرشده انتخاب شده است.»,
Publish disabled with the honest reason «همین بازبینی از قبل منتشر شده است.» (same capture is NOT republished),
Withdraw available. NO write was sent: draft revision/pointer are untouched by this task (read-only trial).

## B — separate owner surface, same form, no dummy combo

- The TB-222 single-option `<select data-testid="admin-shipping-authoring-owner">` and its note are REMOVED
  (live DOM: `combo:false`, `ownerNote:false`); page title now states the surface
  («روش‌های ارسال — همین فروشگاه»).
- New route `app/admin/shipping-services/marketplace/page.tsx` renders the SAME
  `ShippingAuthoringMethodListScreen` with `surface="marketplace"` through the SAME app wrapper; the feature
  is not copied and no parallel subsystem exists.
- Saved views / grid identity stay per-surface AND per authorized owner
  (`admin-shipping-services` vs `admin-shipping-services-marketplace`, plus `:${ownerKey}` in the grid id).
- Route prefix is NOT permission: the screen reads the authorized owner FIRST
  (`GET /v1/admin/shipping-services/authoring-owner`) and refuses to read drafts when that owner is not the
  one this surface names. No owner id is taken from query/body; no seller/tenant is inferred; edition is not
  changed. SingleStore live result: `/admin/shipping-services/marketplace` renders
  «مارکت‌پلیس در این نصب در دسترس نیست» + explanation, with `hasList:false` (no store drafts under a
  marketplace heading) and no fabricated second owner.
- The reverse mismatch (a marketplace-authorized actor opening the store route) has its own honest state
  (`listSurfaceMismatchTitle/Detail`), so no surface silently borrows the other owner's methods.
- Primary list behaviour preserved: real drafts, direct `?mode=view|edit` draftId navigation, names from the
  DB default language, `?legacy=1` only as an explicit secondary link.

## C — order invariant

`docs/architecture/shipping/SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md` is
preserved and referenced from the TOP recovery files. No order schema, projection or consumer change was made
here; no full authored JSON is copied per order by this task.

## D — executed proof

- `npx tsc --noEmit` — clean (once, after the final source edit).
- All `features/admin-shipping-authoring` tests: **144 / 144 pass**, including the restored publication model
  facts (decode, exact bodies, in-flight gate, stale/unknown latches, phase) and three NEW owner-surface facts:
  `223-01` lawful owner decode, `223-02` contradictory/unknown owner is untrusted (never repaired),
  `223-03` per-owner grid namespace.
- Frontend architecture guards: **7 / 7** (import boundary, feature boundary, flat folder, test discovery).
- Not replayed: the 127/194/full backend suites; the four pre-existing `frontend-source-size` drifts were not
  touched or repaired.

## Live facts (real FE :3000 / Host :5088, no restarts)

| Surface | URL | Rendered |
| --- | --- | --- |
| Store list | `/admin/shipping-services` | list, `data-surface="store"`, surface link → marketplace, overflowX 0 |
| Marketplace | `/admin/shipping-services/marketplace` | honest unavailable (SingleStore), overflowX 0 |
| Editor (view) | `/admin/shipping-services/authoring/01a16eeb…?mode=view` | compact publication: Selected / publish disabled+reason / withdraw enabled |
| Editor (edit) | same `?mode=edit` | publish+withdraw disabled with «انتشار فقط از حالت مشاهده اعمال می‌شود.»; closed day still `disabled`; type options عادی/سنگین/فوق‌سنگین |

Screenshots: `view-publication-1440.png`, `view-publication-390.png` (390px panel width 326, page overflowX 0).

## NOT claimed / limits

- Publication is selection INTENT: there is still no Storefront/checkout consumer, no projection/selection/
  commit, and the UI says so in one short line. Nothing is labelled as live/active for customers.
- Multi-owner live proof is impossible on this SingleStore installation: the backend returns exactly one
  authorized owner. The Marketplace page therefore has NO lawful cross-owner path here and was not faked;
  a real Marketplace listing needs a server route returning the actor's authorized owner SET plus owner-keyed
  list reads — an authorization-surface change, not a UI task.
- Creating a whole new method remains the legacy catalog behind `?legacy=1`; arbitrary seller-owner
  administration is out of scope.
- Storefront consumer + compact per-order provenance (decision doc) remain the next implementation gate.
- No DB reset/seed/reapply, no flag flip, no edition change, no user-draft Apply, no catalog write.
- Actual elapsed ≈ 40 min vs the HARD30 box — recorded, NOT a process PASS.

Active task: NONE. Next task: NOT ISSUED — STOP_FOR_ARCHITECT.
