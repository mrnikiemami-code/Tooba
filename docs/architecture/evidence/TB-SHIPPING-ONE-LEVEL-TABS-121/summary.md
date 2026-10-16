# TB-SHIPPING-ONE-LEVEL-TABS-121 — Evidence index

Parent: TB-SHIPPING-ONE-LEVEL-TABS-120 · Channel: `tooba-main` · Worker: `tooba-worker-01`
Status: **PASS** · Stop-State: `STOP_FOR_ARCHITECT` · Code-SHA: NONE (observation-only)

| File | Content |
| --- | --- |
| `task.md` | Received downloadable task contract |
| `task-receipt.json` | Task/bridge identifiers |
| `prepare-isolated-target-121.js` | Offline effective-config builder + fail-closed isolation guard |
| `prepare-output.txt` | Guard verdict (`PREPARE=PASS`) |
| `isolation-guard.json` | Effective tenancy/reference/port/gate proof, credential-free |
| `launch-isolated-host-121.ps1` | The single watchdog-bounded launch (one `Start-Process`) |
| `watchdog-result.txt` | Recorded PID, absolute exe path, health 200, stop proof |
| `run-isolated-proof-121.js` | The single bounded authorized observation pass |
| `isolated-proof-121.txt` | Raw probe record (catalog + projection labels) |
| `one-level-binding-readback.txt` | Authorized binding reads: identity for all seven methods |
| `scratch-legacy-activation-readback.txt` | Scratch DB read-only read-back of identity/activation |
| `startup-stages.txt` | Hosting lifecycle, migration markers, zero fail markers, empty stderr |
| `shipping-migration-markers.txt` | `AddShippingOneLevelCatalog` + fulfillment shipping DDL |
| `seed-execution-evidence.txt` | Batched `INSERT INTO fulfillment.shipping_methods` (seven rows) |
| `shared-preflight.txt` | Shared `:5088`/`:3000`/PID/container baseline |
| `shared-postflight.txt` | Identical shared parity after the run |
| `shared-alpha-preflight-counts.txt` | Read-only `tooba_alpha` counts before |
| `shared-alpha-postflight-counts.txt` | Read-only `tooba_alpha` counts after (identical) |
| `cleanup.txt` | Container/port/temp-dir/process removal proof |
| `summary.md` | Concise result summary |
| `RESULT.bridge.txt` | The exact Bridge result contract payload |
| `post-result.js` | Bridge submission script (receipt kept unstaged) |

## Key figures

- Single isolated launch; `/health` 200 after **41 s**; watchdog never hit.
- `2010-01-15`-free: migration `20261015120000_AddShippingOneLevelCatalog` applied by the ordinary startup.
- One-level catalog: `gate=Enabled`, `ready=true`, **7 rows**, Persian + English translations.
- Storefront projection: **7 documented labels, 7/7 sourced from the one-level catalog, 0 mismatches**.
- `fail:`/`crit:` markers: **0**; stderr: **0 lines**.
- Shared parity: identical PID/path/health/provenance and identical `tooba_alpha` counts.
- **Frontend DOM parity: NOT PROVEN** (API-level proof only).
