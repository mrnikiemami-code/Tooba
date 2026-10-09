# TB-TMAR-SUPPORT-AMSC-001-W3-R1 — Validation Record

Starting head `c632da653d4a34cc6172313a9ef3ef61346e99c0`, branch `main`,
`HEAD == origin/main`, tracked working tree clean.

## 1. Preflight results

```text
branch = main
HEAD = c632da653d4a34cc6172313a9ef3ef61346e99c0
origin/main = c632da653d4a34cc6172313a9ef3ef61346e99c0
HEAD == origin/main : YES
tracked modified/deleted files : 0
W0 567ac4004eb464f489695f078e794fe7645716ab ancestor : YES
W1 5e8c86efacb0e4f45ee10a066d6cb869a21c1aec ancestor : YES
W2 aaa15b03e5cfa2ed1bef1c8244e0c621c3786980 ancestor : YES
W3 c632da653d4a34cc6172313a9ef3ef61346e99c0 ancestor : YES
```

## 2. Focused test results (exact counts)

| Command | Passed | Failed | Skipped | Total |
| --- | --- | --- | --- | --- |
| `dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~SupportModuleAmsc001"` | **30** | 0 | 0 | 30 |
| `dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~TmarCompleteReferenceStructureGateTests\|FullyQualifiedName~TmarDurableGuardTests"` | 7 | **3** | 0 | 10 |

The Support-focused family (W1 9 + W2 8 + W3 13) is green. The 3 failures are the pre-existing,
unrelated repository-global pins:

```text
TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment
TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable
TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative
```

## 3. Pre-existing vs regression separation

| Failure | Pre-existing at `c632da65`? | Cause | Regression? |
| --- | --- | --- | --- |
| `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment` | YES (documented in the gate source; recorded by Support W3 and by Returns/Promotion/Pricing AMSC waves) | `Tooba.Catalog.Contracts.Cart` namespace deviation | NO |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | YES (Support W3 baseline; stale `certifiedModules` literal pin) | repository-global recovery pin | NO |
| `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` | YES (same baseline) | same stale repository-global recovery pin | NO |

This wave changes **no production file** and **no SoT field**, so the repository-global failure set
is byte-identical to the Support W3 baseline. No test result is invented; no guard was run with
reduced scope to manufacture a pass.

## 4. Change inventory

| Change | Path |
| --- | --- |
| Task receipt | `docs/ai/tasks/TB-TMAR-SUPPORT-AMSC-001-W3-R1.task.md` |
| Task receipt metadata | `docs/architecture/evidence/TB-TMAR-SUPPORT-AMSC-001-W3-R1/task-receipt.json` |
| Bounded review | `docs/architecture/evidence/TB-TMAR-SUPPORT-AMSC-001-W3-R1/review.md` |
| Proposed repair (not implemented) | `docs/architecture/evidence/TB-TMAR-SUPPORT-AMSC-001-W3-R1/proposed-repair.md` |
| Validation record | `docs/architecture/evidence/TB-TMAR-SUPPORT-AMSC-001-W3-R1/validation.md` |
| Module-local recovery note (additive) | `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` |

Production `.cs` files changed: **0**. Shared factory edits: **0**. Schema/migration/endpoint/
contract changes: **0**. Tests weakened: **0**. Baselines widened: **0**. Global pins changed: **0**.

## 5. Preservation

- Support 17-route / 9 `VALIDATOR_REQUIRED` + 8 `NO_VALIDATOR_REQUIRED` proof: intact.
- Manifest structural state: NOT_TOUCHED.
- `structureLock.certifiedModules` still lists `Support` exactly once.
- Global Host checkpoint: `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`,
  `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001` PRESERVED.
- `automaticNextImplementationTask = NONE`.
- Workflow stop: `USER_REVIEW_SUPPORT_AMSC_001_W3_R1`.
