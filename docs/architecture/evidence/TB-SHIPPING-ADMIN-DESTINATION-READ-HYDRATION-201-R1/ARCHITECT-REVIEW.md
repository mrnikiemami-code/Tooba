# Architect review — TB201-R1 / 2026-10-23
Disposition: PARTIAL; original activity guard retained, no demonstrated second production defect.
Reviewed0c692f35732420c785916bb28e11bd02910270df and preserved repro text. The service-area IsActive condition is the intended narrow change. Activity regression never reached its assertion; no green activity/closure/EF/DI/listener/new-handler proof yet. Existing6green are narrow fixture cases only. Overrun/stale-assembly diagnostic and omitted proofs remain history.

Source-derived root cause of the saved reproduction:
Two requested service areas share City=33333333-3333-4333-8333-333333333333.
ReadAsync forms owningCityIds by Select(CityId).ToArray(), so it legitimately passes [City,City] to the bulk seam.
CountingSnapshots.GetCitiesAsync maps EVERY requested id, returning TWO identical city rows.
ReadVerifiedAsync.TryAdd correctly rejects the duplicate response and returns configuration_unavailable.
The actual GeographyPlaceSnapshotReader de-duplicates requested ids via DistinctIdentifiers before querying, as its public contract requires.
Thus the saved fake violates output uniqueness. A source-derived JS counterexample confirms duplicated collection shape; this is NOT a C# test execution. No dotnet runtime is available to Architect here.

The claimed 'terminal-only closure is broken' is UNPROVEN. Adding ancestor keys cannot itself remove this duplicated owning-city result in the current algorithm; old6green use different fixture shapes. Do not authorize an unlocated second production fix. Correct lawful fixture output to distinct ids and retain a separate deliberately duplicated-response fail-closed negative. One-active-terminal-only control plus active/inactive shared-city pair will distinguish closure from fake corruption.

Only next task201-R2: bounded test-only reproduction correction plus missing actual EF/DI/listener/new-handler integration in existing Shipping.Tests, using already-authorized test-only Geography.Infrastructure reference. No further production change; if lawful fixture/real reader still fails, preserve red test and report exact unexpected rows/call stage, no speculative patch.
One integrated real-module/EF harness should cover multiple obligations; reuse established listener and editor endpoint harness rather than chase diagnostics. No Postgres/fullsuite/Host. Then review and proceed UI.
