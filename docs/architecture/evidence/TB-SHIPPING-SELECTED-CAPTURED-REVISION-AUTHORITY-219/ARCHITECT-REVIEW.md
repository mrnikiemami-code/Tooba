# Architect review — TB219
Exact source eca53dae9c56bfb4b7f2be91eb54da64e1464319; base4972c6c8af39c000bceaebe4f0846540ec37f107.
Main verified at full source SHA; the worker HEAD line has a transcription typo, not evidence of different code.

## Decision
Retain the architecture/table/Contracts surface, but require bounded219-R1 before acceptance of write correctness. No220 issued.
Reviewed source diff: Domain pointer, internal port, Infrastructure adapter, owner resolver extraction, Contracts, EF/migration and real storage-test code. No root test/build/runtime execution.
Worker reports nine real PostgreSQL storage facts plus nine affected Contracts edge facts/build0 errors; these remain valid bounded evidence, not coverage for the two cases below.

## Defect1 — tracked pointer can supply stale concurrency state
SelectAsync and ClearAsync materialize ScopedPointer without AsNoTracking or an explicit database refresh. EF returns the already tracked instance unchanged. RequireCleanTracker rejects dirty changes, but a stale clean instance is allowed.
Reproduction: contextA tracks selected v1/captureA; contextB selects captureB v2. A.Select(captureA,expected1) returns success v1 via same-target no-op although the database is v2/B. A.Select(captureB,expected2) instead rejects a valid current token because tracked version remains1.
Clear analogue: A tracks cleared v2; B selects captureB v3; A.Clear(expected2) returns false cleared/no-op success. Updating cases can hit database concurrency protection, but early no-op/version decisions do not.
Required fix: read fresh persisted pointer before any expected-version or no-op decision, preserve real CAS and caller tracking. A minimal explicit reload of the one clean pointer is acceptable; preserve instance identity, do not clear/detach unrelated state or fabricate a second context/framework. Handle missing row safely.

## Defect2 — domain rejection mutates pointer
Select assigns SelectedSnapshotId and then computes NextVersion; Clear nulls SelectedSnapshotId then computes NextVersion. With stored version long.MaxValue, NextVersion throws after target changed.
The throw happens before TrySaveAsync, so its detach cleanup never runs. An EF-tracked pointer can then be flushed by a later SaveChanges with the old max version and changed snapshot, contradicting no-leak/no-ABA guarantees.
Required fix: compute/validate all potentially throwing next-state values before any assignment (strong exception safety). Rejected Select/Clear must leave target, version and UpdatedAt unchanged, tracking clean, later SaveChanges0 and database unchanged.
Real disposable DB may set max token as a test fixture only; no shared schema/data work.

## Proof scope
Add actual stale contextA tests across fresh B updates, both no-op and current-token controls, preserve unrelated caller tracked identity. Add overflow Select and Clear tests proving unchanged state + later SaveChanges0. Execute red first, then one focused provider run and the directly affected storage class as needed; no214/218/119/full-suite replay.
No schema/migration/API/Contracts/flags/Host/FE changes required. 219 schema stays intact and NOT shared-applied.
Runtime publication/customer eligibility not accepted; selected pointer remains intent only. Actual29min total versus30, but implementation stop exceeded; retain recorded deviation.
