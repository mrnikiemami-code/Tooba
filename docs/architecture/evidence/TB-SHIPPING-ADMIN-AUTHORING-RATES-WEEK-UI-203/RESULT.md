# TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203 — Worker result

- Task: `TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203`
- Parent: `TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202-R1`
- Mode: `BOUNDED_EXISTING_GROUP_RATES_AND_METHOD_WEEK_EDITOR_UI`
- Base: `44a220dea77c87e3d8c1a80e07b22af4e2846e45` (verified `HEAD == origin/main`, fast-forward only)
- Production-Functional-Acceptance: `PENDING`
- Schema/Data/Flag changes: `NONE`
- Stop-State: `STOP_FOR_ARCHITECT`
- Automatic-Next-Task: `NONE`

## Delivered (visible, existing draft only)

1. **Existing-group rate rows are editable** under every stored group, with add/remove per group.
   Group id/order/areas/currency and the proven geography stay read-only and are rendered once.
   A new row gets a client-local key and `rateRowId: null`; removing the last row emits `rows: []`
   for that group only. No group create/delete/reorder and no geography editing.
2. **Exact-token pipeline**: money fields are TEXT inputs (`0.50`, `1850000.00`) and never pass
   through `input[type=number]`, `Number()` rounding or `toFixed`; untouched tokens are re-sent
   verbatim. Integer fields accept only safe integers within the real contract domain. A blank
   optional bound serializes as `null`; required/malformed/unsafe/out-of-range values stay visible,
   dirty and blocking (no zero default, no silent clamp). Free/Manual calculation mode never clears rows.
3. **ONE method-level weekly schedule**, mode-aware at the previous read-only location:
   configured drafts initialize the real timezone + seven ISO days; unconfigured drafts stay
   explicitly unconfigured until the operator types a timezone and clicks Configure (seven closed,
   blank-hour days, no guessed browser timezone/default hours). Work/closed toggles retain remembered
   hours; a working day needs a lawful explicit `start < end` pair; untouched `TimeText` tokens
   (including seconds/fractions) are preserved, while a deliberate `HH:mm` edit becomes `HH:mm:00`.
   A configured all-closed week is distinct from unconfigured; there is no clear operation.
4. **Composed single intent**: General, rate rows and week are merged into ONE typed `EditorEdits`
   (`model/authoring-intent.ts`) and dispatched through `controller.edit` once, so changing General
   after a rate/week edit (or the reverse) keeps every pending capability. Untouched capabilities
   are omitted wrappers; only a changed capability is serialized. 202-R1 `localDirty`, `formSeed`,
   discard/exit flow and inert saving/reconciling controls are reused unchanged.
5. **One Apply → one PUT**: the existing request builder writes `expectedRevision`, the full retained
   group tree (with 201 ancestry resolution for all retained areas) and the optional week wrapper in
   a single body. No per-capability requests, no post-save reload, no retry; server ids/revision are
   adopted on acceptance and the fresh form seed re-initializes rows/week from the new baseline.

## Validation actually executed

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test features/admin-shipping-authoring/authoring-rates-week-203.test.ts` | PASS 7/7 |
| `node --experimental-strip-types --test` (6 existing authoring suites) | PASS 48/48 |
| `npx tsc --noEmit` | PASS (exit 0) |
| `node --experimental-strip-types --test` feature-boundary / import-boundary / test-discovery / flat-folder guards | PASS 7/7 |
| `frontend-source-size.guard.test.ts` | FAIL — 4 PRE-EXISTING `OVERSIZED_GROWTH` entries in files untouched by this task (`category-admin-screen.tsx` 2230>2215, `admin-landing-page-composer.tsx` 1442>1440, `storefront-landing-blocks.tsx` 1033>1026, `shared-composition-renderer.tsx` 918>907). No new oversized feature file was introduced; not modified here. |

Not run (proportionate, no harness invention): mounted/browser/visual acceptance, build, whole
frontend discovery, backend/Host/solution suites. **Visual: NOT RUN** — no mounted harness/session
was available and none was started/installed.

## Focused facts covered by the new test file

- Two populated groups: editing/adding/removing in one leaves the other group's id/order/areas/
  currency/rows and every untouched row id/value exact; last-row removal affects only that group;
  a new row's id is `null` on the wire and its local key never appears in the body.
- Exact decimal tokens (`1850000.00`, `0.50`, `0.00`) are written literally; blank optional bound
  becomes `null`; required blank, 3-decimal money, unsafe integer and half-written/reversed intervals are
  local blocking issues (no throw); reverting to the baseline is clean again.
- Week: untouched unconfigured is omitted; deliberate Configure yields 7 unique ISO days; a
  configured all-closed week is dirty and serializable; independent Saturday/Sunday times; partial
  and reversed pairs invalid; a fractional stored `TimeText` survives untouched.
- General → rates → week → General keeps all three in one intent; the built body carries
  `expectedRevision`, `regionGroups` and `weeklySchedule`; one accepted save = one PUT, no post-save
  GET, and the returned server child id appears in the new baseline.

## Deviations / limits

- Overlap/price policy is intentionally NOT duplicated client-side; the server stays authoritative
  for overlap/currency mismatch (canonical server errors retained).
- Visual/RTL rendering was implemented with existing design-system primitives and responsive
  wrapping but is NOT visually certified (no harness).
- Recovery pointer added; no certification or delivery-complete claim is made.


## Correction (2026-10-23, R1)

The four Architect-observed defects in this result are repaired in TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1 (docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1/RESULT.md): per-bound time provenance, whole-document Cancel Edit reseed, pending-timezone dirtiness, and truthful all-axis read-only rate conditions. This section is append-only; the statements above describe the reviewed pre-R1 state.
