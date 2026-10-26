# TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225 — RESULT

Base: `fb2999cfa880ceafa985c57eede47ff5107ba44a` → work committed on `main` (fast-forward, no reset/stash/rebase/
force-push). Task text: `TASK.md` in this folder.

## A — Destinations: ONE modal, FOUR fixed cascading selects, ONE terminal path

- The inline picker and the level selector are GONE. `افزودن منطقه` opens the existing design-system `Dialog`
  (`ui/authoring-destination-modal.tsx`) with ALWAYS four labelled fields: کشور → استان → شهر → ناحیه.
- Country loads on open; every descendant starts `disabled` with only «انتخاب کنید». Picking a country loads
  ONLY its administrative areas; city stays disabled until an area is picked; the service-area list is read
  ONLY for the picked city. No eager country-wide city read is issued before an area choice
  (`destination-read-coordinator.ts` generation guard + `test 225-A4`).
- Changing a parent clears every dependent stage (`chooseCountry`/`chooseAdministrativeArea`/`chooseCity` in
  `model/destination-choice.ts`, `test 225-A2`); stale/out-of-order completions of a superseded parent are
  discarded by the existing generation-settled decoder, not a second weaker coordinator.
- `Add` submits ONLY the deepest verified terminal as ONE `(kind, id)` (`choiceTerminal`, `test 225-A1`;
  live: the staged path is NOT an addition). Ancestors describe the path and never become coverage chips.
- Duplicate/overlap is refused ONLY on a deliberate `Add`, through the existing MessageBox
  (`ui/authoring-message-box.tsx`), with the modal and the chosen path kept (`test 225-A3`). Staging an
  already-stored country shows no error.
- **Breadcrumb after reload**: `model/destination-trail-resolver.ts` rebuilds each stored chip's path from the
  EXISTING Geography reads — bounded SET reads, one per distinct parent level (`test 225-A6`, `225-A7`), never
  per chip; an unproven level publishes no fabricated segment (`225-A9`), a country-wide null-admin city keeps
  its honest two-segment path (`225-A8`), a country selection is never shown as `ایران › ایران` (`225-A11`).
  Display names stay display-only: identity/ancestry still come from the validated facts, never pricing truth.
  Hook: `ui/use-stored-trails.ts` → `indexWithTrails`.
- HONEST GAP (live): the real `تهران` city in this installation has NO service areas. The modal states
  «برای این شهر ناحیهای ثبت نشده است.» and the ناحیه select stays empty — this is the representation gap
  TASK.md anticipated, reported instead of inventing a province or silently re-enabling a country-wide branch.
  No Geography schema/data change and no seed.

## B — Rate rows: isolated modal Add/Edit, compact summaries, local validation mirror

- The area card keeps the ONE shared delivery range and now shows each row as a compact SUMMARY
  (conditions + freight/packaging + delivery), with clear Edit/Delete actions (`ui/authoring-groups-editor.tsx`).
- `افزودن ردیف` / `ویرایش` open `ui/authoring-rate-modal.tsx` with an isolated draft. No blank row is appended
  to the main form until a successful local confirmation; Cancel mutates nothing; a confirmed Edit replaces
  exactly that stable row key and preserves order/identity (`tests 225-B1`, `225-B2`).
- Local validation mirror in `model/editor-rates.ts`, consistent with the accepted Domain rules: weight/count/
  merchandise axes are AND within a row, no-axis is unconditional, half-open `[min,max)`, touching endpoints
  are lawful (`225-B4`), an EXACT duplicate condition tuple is ambiguity even with different prices (`225-B3`),
  and sibling checks exclude SELF by stable key — never by index, never «ignore all stored rows».
- Zero freight and zero packaging are VALID; empty is different from zero (`225-B5`). Invalid local state keeps
  the parent `Apply` blocked; no invalid text is converted into an accepted number.
- A failed Confirm shows the MessageBox with the short owning-field reason and leaves the modal open with the
  exact typed values. LIVE: a duplicate condition (weight `1000–2000`, freight `111,000`) was refused with
  «شرط این ردیف دقیقاً با ردیف دیگری در همین محدوده یکسان است.» and the typed values were preserved.

## C — Money: typing repair, IRR integer UI, no precision loss

- `model/money-text.ts` now separates the two proven root causes: `interpretMoneyTyping` (a keystroke edits the
  logical value, so the component's own grouping can never be re-read as malformed input → `1,200,000`, not
  `1,200000`; `test 225-C2`) and the strict paste/commit path that keeps unsupported text exactly as typed
  (`-100`, `1e3`, `12,34` stay themselves, `aria-invalid`; `test 225-C3`).
- IRR is integer-rial in the UI: `moneyHasFraction` drives display, so a stored `120000.00` renders `120,000`
  with the RAW wire token untouched (`225-C1`, `225-C1b`; LIVE read shows `120,000 · 5,000`, never `.00`), and
  a whole value is never reported as a visible fraction.
- A non-zero stored fraction is tolerated while untouched, refused honestly on edit, and never blocks the whole
  area (`225-C4`, `225-C5`); nothing is rounded, truncated or silently rewritten. No Number/parseFloat money
  math, no FX, currency remains server-owned.

## D — Mobile: weekly view/edit and modals

- `ui/authoring-week-editor.tsx`: at 360/390 CSS widths each day is a compact aligned `day + status` row; a
  working day shows its hours paired on ONE line with visible From/To context; a closed day is a compact
  «تعطیل» badge with NO empty From/To blocks (live edit mode exposes exactly 2 time inputs — the one open day).
  View mode stays truthful text. Desktop shared column alignment was not changed.
- Both modals fit the viewport and scroll internally: measured at 390×844 the rate modal is 352×566
  (top 139 / bottom 705) and the destination modal at 1440×900 is 672×359; `scrollWidth == clientWidth`
  (overflow 0) at 390 AND at 360 (360/360/360).

## Focused proof

- `destination-rate-modals-225.test.ts` — **22/22** real-helper facts (A1–A11, B1–B5, C1–C5).
- ALL feature tests, enumerated recursively: **188/188** pass, 0 fail, 0 skipped.
- ONE clean `npx tsc --noEmit`. Architecture guards: 10/11 files green; `FE-SIZE` fails only on the FOUR
  documented pre-existing drifts (`category-admin-screen`, `admin-landing-page-composer`,
  `storefront-landing-blocks`, `shared-composition-renderer`) — none touched here. Edited screen: 798 LOC.

## Live (mounted, real traffic, no `/v1` interception)

FE `127.0.0.1:3000` and Host `127.0.0.1:5088` (`/health` 200) reused, no restart; user's other tabs untouched.
Real draft `01a16eeb-70b3-7000-87c2-564c82835496` (revision 2, «پیشتاز»): the four-stage cascade resolved
ایران → تهران → تهران with real DB options and disabled dependents; the staged path produced NO chip and left
`Apply` disabled; the stored chip after reload shows the proven breadcrumb (`کشور: ایران`, no duplicate);
duplicate rate condition refused by MessageBox with values kept; `-100` kept as typed, `aria-invalid`, refused
with «مبلغ باید عددی نامنفی باشد…»; a clean Cancel raised NO discard dialog and the stored baseline stayed
`1 محدوده`, `1..3`, `120,000 / 5,000`, one destination.

Screens: `screens/225-1440-destination-modal.png`, `225-390-rate-modal.png`, `225-390-rate-refusal.png`,
`225-390-week-view.png`, `225-390-week-edit.png`.

## NOT claimed

- NO Apply / PUT / publication / catalog / migration / seed / reset. The draft revision and the publication
  pointer were not touched by this task.
- The LIVE service-area Add flow was NOT exercised because this installation has no service areas under the
  real `تهران` city; it is proven only through the focused helper tests (A1–A5).
- Not proven: legacy/conflicting stored rows rendered live; multi-area live geometry at 360; any Storefront /
  checkout consumer (`Selected` is still selection intent only), price/activation/readiness.
- User Visual ACCEPT is NOT granted and must not be assumed; Architect review comes first.
- Elapsed: this task spanned a chat-loss and several correction rounds; the truthful statement is that the
  45-minute box was EXCEEDED (measured minutes cannot be reconstructed) — recorded, NOT a process PASS.

Active task: NONE. Next: Architect review, then the user trial. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

## Correction appended by 225-R1 (2026-10-26)

Architect review (ARCHITECT-REVIEW.md) confirmed three defects in the money/modal layer of this task; 225 is
NOT accepted and the user trial still waits. `../TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R1/RESULT.md`
repairs exactly those: typing edits now know the PRIOR committed token (an invalid `12,34` can no longer be
healed into `12345`), the caret is mapped through the separator removal (middle insertion into `1,234` lands
after the typed digit), and the rate modal re-derives stored-fraction tolerance from the exact stored tokens on
every confirm plus refuses a view/busy confirm in the handler itself. The statements above about money
behaviour and modal confirmation therefore describe the REPAIRED behaviour, not what was reviewed at
`2e6aed095`.
