# TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219-R1 — RESULT

Mode: BOUNDED_FRESH_POINTER_AND_EXCEPTION_ATOMIC_SELECTION_REPAIR
Base: `e4e3a1cf7b6fcc0e9ba7e84c77eb1526a49bb101` (fetch verified, `HEAD == origin/main` before work)
Parent: TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219 (`eca53dae9c…`, review in
`../TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219/ARCHITECT-REVIEW.md`)

## Verdict

`BOUNDED_CORRECTION_EXECUTED / STILL_NOT_WIRED / STILL_NOT_LIVE`.

Exactly the two source-confirmed defects were repaired. The 219 schema, table/index/CHECK shape, the
selection architecture, the Contracts surface, the owner resolver, the CAS/unique matcher, the no-op
semantics and every previously accepted behaviour are unchanged. The new facts were RED on the unchanged
production code and GREEN only after the repair.

Task text is persisted verbatim in `TASK.md` in this folder.

## Defect 1 (confirmed) — a clean TRACKED pointer could decide from a stale token

`SelectAsync`/`ClearAsync` materialized the pointer through a tracking query and then decided
`SelectionVersion != expectedSelectionVersion` and the same-target / already-cleared no-ops from it. EF's
identity map returns the already-tracked instance **without** refreshing its current values, so after
another context advanced the row the early decisions were taken from the OLD token (a stale
same-target select or a stale cleared no-op could report success), while a genuinely current token could be
rejected.

Repair — `ShippingSelectedCapturedRevisionDirectory` (only this adapter):
- New `LoadPersistedPointerAsync(...)` runs **before any** version comparison or no-op decision. It reads the
  one owner-scoped pointer through the tracked query (so identity is preserved) and then explicitly
  `context.Entry(tracked).ReloadAsync(ct)`s **only that entry**, so the token/target are authoritative.
- A row that vanished between the query and the reload (`DbUpdateConcurrencyException`) is treated as
  **absent**, never as a usable token: only that entry is detached and the ordinary
  creation/conflict paths run.
- When the query itself proves no row exists, `DetachVanishedPointer(...)` detaches a stale leftover
  instance for that same owner+canonical binding only, so it can never be used as a live token.
- `RequireCleanTracker()` still runs **first** and still refuses unrelated dirty caller work without any
  refresh/flush/clear; unrelated caller entities, their state and identity are untouched; the real column
  concurrency token and the exact unique-violation matcher still guard the actual write.

## Defect 2 (confirmed) — a rejected Select/Clear mutated the row

`Select` assigned `SelectedSnapshotId` and only then computed `NextVersion`; `Clear` nulled the target first.
At a stored version of `long.MaxValue` the version check threw **after** the target had changed and **before**
`TrySaveAsync` could detach, so a later `SaveChanges` could publish the rejected target/clear with the old
version — contradicting no-leak/no-ABA.

Repair — `ShippingSelectedCapturedRevision` (Domain, only `Select`/`Clear`):
- The whole next state is computed and validated **before any assignment** (`var nextVersion =
  NextVersion(SelectionVersion);`), then assigned. A rejection now leaves target, version and `UpdatedAt`
  exactly as they were, the tracked entry `Unchanged`, a later `SaveChangesAsync` at 0 and the database
  unchanged. No-op semantics at the current version and the stable error code are unchanged.
- The adapter maps the domain's `SemanticException` from `Select`/`Clear` to a normal
  `Result.Failure(selected_capture.invalid)` instead of letting it escape the seam; because nothing was
  mutated there is nothing to detach.

## Executed proof (RED first, then GREEN)

Target: task-owned disposable `127.0.0.1:15432` / `shipping_tb214` / `tb214` (`postgres:17`, one container
created for this task, live identity `current_database()|current_user|host(inet_server_addr())` verified
before any DDL, normal migration chain applied to that DB only; container removed and `15432` verified silent
afterwards). Older 55432 family and shared 5432 never touched; no InMemory substitute; no Docker engine
restart.

**RED (production unchanged at `e4e3a1cf7…` + new facts only): 3 failed / 9 passed**
- `A_clean_tracked_pointer_cannot_report_a_stale_noop_after_another_context_advances` — failed: the stale
  `Select(oldSnapshot, expected 1)` returned success instead of conflicting.
- `A_clean_tracked_cleared_pointer_cannot_report_a_stale_clear_noop` — failed: the stale
  `Clear(expected 2)` returned the already-cleared no-op success.
- `A_max_version_pointer_rejects_select_and_clear_without_mutating_or_leaking` — failed:
  `SemanticException : shipping.selected_capture.invalid` escaped instead of a typed refusal.

**GREEN (after the two repairs): 12/12 passed** — the 9 previously accepted 219 facts plus:
- **A** — context A tracks a CLEAN v1/old-snapshot pointer (asserted `Assert.Same` on the tracked instance
  and `SelectionVersion == 1`); independent context B selects the newer capture at v2; `Select(old, 1)` now
  conflicts with `shipping.selected_capture.conflict`; the control `Select(old, 2)` succeeds to v3 with the
  old snapshot (deliberate rollback) and a fresh independent read proves DB v3/old-snapshot; the caller's
  unrelated tracked draft keeps its identity, values and `Unchanged` state.
- **B** — A tracks a CLEAN cleared v2; B selects at v3; `Clear(expected 2)` conflicts (not a stale
  cleared no-op); `Clear(expected 3)` succeeds to v4 `Cleared`; a fresh independent read proves v4/no target.
- **C** — the task DB sets `selection_version = long.MaxValue` on a selected pointer; `Select(different
  capture, expectedMax)` and `Clear(expectedMax)` both reject with `shipping.selected_capture.invalid`; after
  **each** attempt the same tracked instance keeps target/version/`UpdatedAt`, the entry stays `Unchanged`,
  `SaveChangesAsync()` returns 0, and an independent DB read proves the row unchanged (no Dispose/
  ChangeTracker.Clear/detach/restore masking).

`dotnet build` of `Tooba.Shipping.Tests`: 0 errors. Only the directly affected storage class was executed
(12/12); no 214/216/218/119/194/whole-shipping/solution/Host/snapshot wildcard replay.

## Bounds respected

Production changes are limited to the existing 219 adapter (one private fresh-load helper + vanished-row
detachment + domain-rejection mapping) and the Domain pointer (`Select`/`Clear` ordering). No Contracts,
schema, migration, DI, flags, Order, FE, HTTP or browser change; no shared migration (214 and 219 remain
shared-pending); no user draft/capture write; no Host/FE restart; `TB185` paused / default-OFF /
`PendingAllocation` and §29 preserved.

Timebox: actual ≈26 min vs HARD20 / implementation-stop12 / final8 (over both; recorded honestly, not a
timing PASS). One correction round was not needed on production logic beyond the two scoped repairs; the red
probe was executed before them. The lost-task-artifact recovery (read-only extraction of the verbatim task
text from the Bridge store after the claim response was consumed) took part of the elapsed time.
