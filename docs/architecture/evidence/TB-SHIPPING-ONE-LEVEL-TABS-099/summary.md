TB-SHIPPING-ONE-LEVEL-TABS-099 — isolated real activation write / read-back / reversal (evidence index)

Mode: BOUNDED_ISOLATED_ACTIVATION_WRITE_READBACK_ROLLBACK
Status: PASS (bounded). Shared tooba_alpha and shared :5088/:3000 were never mutated.

| File | Meaning |
| --- | --- |
| `activation-write-proof.txt` | full transcript: baseline, auth denials, real authorized PUT, scratch DB truth after write/revert/repeat, both refusals, 13/13 checks, OVERALL=PASS |
| `isolated/activation-write-proof-099.js` | the proof runner (real Bearer session, scratch-only reads) |
| `log-prepare-isolated-099.txt` | isolated launch directory preparation (no credentials) |
| `log-isolation-guard-099.txt` | offline isolation guard PASS: one tenant, two scratch references, no tooba_alpha |
| `isolated/assert-isolated-target-099.js` | the offline guard itself |
| `isolated/prepare-isolated-target-099.js` | builds the guarded overlay + effective config + launch env |
| `isolated/launch-isolated-099.ps1` | task-owned isolated launch (Development, :5099, process-only gate) |
| `isolated-host-099-startup-excerpt.txt` | startup excerpt: `Now listening on: http://127.0.0.1:5099`, Development, content root, migration count (raw 3.1 MB stdout trimmed) |
| `fixtures-099.sql`, `fixtures-refusal-099.sql` | synthetic scratch fixtures (active parent + two active option siblings + aligned projection; optionless and inactive-parent refusal rows) |
| `log-fixtures-099.txt` | fixture load results |
| `log-isolated-catalog-read-099.txt` | authenticated catalog + binding reads on the isolated host (gate Enabled, resolved binding, optionless has no optionCode) |
| `log-cleanup-099.txt` | teardown (0 listeners, container removed, scratch files removed) plus shared-stack and shared-DB read-only parity |

No credential value appears in any file here: the scratch password and session token lived only in
untracked files that were deleted during teardown. The `Password=` occurrences are template
placeholders inside the two preparation scripts, never a literal secret.
