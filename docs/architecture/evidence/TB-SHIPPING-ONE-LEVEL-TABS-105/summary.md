# TB-SHIPPING-ONE-LEVEL-TABS-105 — Evidence index

Task `TB-SHIPPING-ONE-LEVEL-TABS-105` (parent 104) · Channel `tooba-main` · Worker `tooba-worker-01`
Mode `BOUNDED_CATALOG_PUBLISH_READINESS_DIAGNOSTICS` · Base-SHA `222628615f7769ad276c1b3b6e76dc9c6e3dcd8b`
Stop-State `STOP_FOR_ARCHITECT` · **Status: PARTIAL** (diagnostic verified; scratch seed's exact code NOT YET OBSERVED)

| File | What it proves |
| --- | --- |
| `task.md`, `task-receipt.json`, `claim-task.js` | Received artifact, claim record via `GET /api/tasks/next?channelId=tooba-main`, receipt kept UNSTAGED |
| `source-diagnostic-inventory.txt` | The full read-path map: `Missing[]` type/semantics and its six possible codes, the exact frame where they are dropped (`ProductLifecycleDirectory.PublishAsync` → `CatalogDirectory.UnwrapLifecycle` line 849), the proof that this is a validation refusal whose reasons already exist in a returned-success contract, what was added, the deliberately unobserved fact, and the untouched second publish site |
| `code-diff-seed.patch` | The complete source diff: the helper call replacing the single publish line in the Development seed |
| `focused-test-run.txt` | Raw `dotnet test` output for the new class: `Passed! Failed: 0, Passed: 3, Skipped: 0` |
| `neighbour-publish-test-run.txt` | Neighbour run of `ProductPublishTests` + `ProductPublishReadinessCapabilityTests` (18 passed, 1 failed — the pre-existing failure below) |
| `preexisting-failure-control-run.txt` | The control run: same failure with this task's seed edit reverted to HEAD, proving the failure is independent of this change |
| `test-evidence.txt` | Test-by-test meaning, the pre-existing-failure attribution with chronology and the empirical control, and the no-seed/no-shared-write statement |
| `environment-and-parity.txt` | Base-SHA/ancestry, zero-scratch decision, read-only shared parity, working-tree preservation and the exact rollback |
| `RESULT.bridge.txt` | The complete Bridge result contract posted for this task |

## Verdict in one paragraph

The opaque `«محصول برای انتشار آماده نیست»` was traced to its exact loss point: the lifecycle port refuses with a
coarse code and `CatalogDirectory.UnwrapLifecycle` throws it as an `InvalidOperationException`, discarding the six
readiness rule codes that the existing `GetProductPublishReadinessAsync` contract already returns. A single small
Catalog-developed, Development-only helper now re-reads that existing contract on a refused publish and emits the real
codes with the seed stage and a non-sensitive product reference, rethrowing the original refusal **unchanged**; the
only call-site change is the one publish line in `CatalogAttributeSchemaDevelopmentSeed.ApplyAsync` that 102's stack
trace pointed at. Three focused tests are green, including a real-Postgres proof that the emitted codes equal what the
real reader returns for a real rejected product, that the refusal is byte-identical, and that nothing is emitted in
Production. One pre-existing, unrelated `ProductPublishTests` failure was found, explained from source and proven
independent by a revert-and-rerun control. The exact code rejecting the schema demo product remains
**NOT YET OBSERVED** on purpose — its only reproduction starts the full Development legacy bootstrap, which the task
forbids — and this diagnostic is precisely what will print it in the next bounded scratch start.
