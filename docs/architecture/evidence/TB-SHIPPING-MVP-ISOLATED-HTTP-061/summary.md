# TB-SHIPPING-MVP-ISOLATED-HTTP-061 — evidence index

Status: **PARTIAL** — isolation gate PASS, real-HTTP missing-scope regression NOT MEASURABLE, cleanup PASS.

| Artifact | Contents |
| --- | --- |
| `log-summary.txt` | command and exit-code record, isolated target description, artifact map |
| `log-http-probes.txt` | the mandatory isolation gate evidence, every real-HTTP status/code, and the blocker analysis |
| `log-guard-effective-target.txt` | the copied 060 guard's PASS record for the real isolated target (fingerprint, CONFIG_SHA256, no credentials) |
| `log-guard-copy-fixture-proofs.txt` | `FIXTURE_PROOFS=PASS` (12/12) proving the copied guard is not weakened |
| `log-prepare-isolated-target.txt` | prepare/gate exit record: 1 tenant, 1 reference, guard exit 0 |
| `isolated/assert-scratch-target.js` | the copied 060 guard, extended to also emit `CONFIG_SHA256` |
| `isolated/run-fixture-proofs.js` | the fixture suite run against that copy |
| `isolated/prepare-isolated-target.js` | builds the isolated overlay + effective merged config + launch override file, asserts one tenant / one reference / no live target, then gates with the guard |
| `isolated/launch-config/` | redacted launch configuration and a hashes record |
| `log-isolated-host-final-run.txt` | task-owned isolated Host log (final run) |
| `log-run1-stderr.txt`, `log-host061-stderr-run2.txt`, `log-host061-stdout-run2.txt` | earlier isolated runs, including the legacy-seed crash |
| `RESULT.bridge.txt`, `post-result.js` | Bridge result envelope and its posting script |

Planning document: `docs/planning/shipping/SHIPPING-MVP-ISOLATED-HTTP-061.md`.

No production source file was modified: `Code-SHA = NONE`.
