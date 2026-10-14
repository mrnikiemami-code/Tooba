# TB-SHIPPING-MVP-AUTHORIZED-ADMIN-059 — evidence index

| File | Contents |
| --- | --- |
| `log-authorization-provenance.txt` | The legitimate provisioning mechanism chain, proof of the `platform-admin` role/assignments/actor, the Development-actor-header comparison, the RESTRICTED-VALUE INCIDENT with root cause, measured exposure, rollback and cleanup proof |
| `log-authorized-roundtrip.txt` | Every real authenticated HTTP result (PASS / DENIED tables), the reproduced `NullReferenceException` defect, the TEST-ONLY data marker |
| `probe/probe-roundtrip.ps1` + `probe/probe-phase2-roundtrip.txt` | Driver + raw output of the authorized CRUD roundtrip |
| `probe/probe-guards.ps1` + `probe/probe-phase3-guards.txt` | Duplicate-scope, unknown-id, missing-scope, no-credential and dev-header probes |
| `probe/probe-crossservice.ps1` + `probe/probe-phase4-crossservice.txt` | Cross-service mutation guard and scope-immutability proof on a fresh area-scoped row |
| `probe/rollback-alpha-accidental-writes.sql` + `probe/rollback-output.txt` | Exact rollback of the 3 rate policies, 2 grants and 5 sessions; result `0 / 0 / 170` |
| `probe/post-rollback-delta.txt` | Post-rollback full-table diff attempt vs the pre-task snapshot (note: the diff query itself had an identifier-quoting bug; the direct counts `0/0/170` in `rollback-output.txt` are the authoritative verification) |
| `probe/final-alpha-state.txt` | Final read-only state of `tooba_alpha` |
| `probe/host059-stdout.txt`, `probe/host059-stderr.txt` | Isolated instance startup log and the `NullReferenceException`/`BadHttpRequestException` detail |

Verdict: authenticated real-HTTP admin roundtrip **achieved**; scratch-only containment **violated and
reverted**; no product source changed; release verdict still `NOT_READY_FOR_CHECKOUT_CUTOVER`.
