# TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062 — evidence index

Root cause: the canonical Development admin/authorization bootstrap was gated behind Party demo seller
data that the schema-only Development path never creates, so `platform-admin`, its catalogue grants, the
admin assignment and the admin capability tuples were never provisioned on a fresh database.

| Artifact | Contents |
| --- | --- |
| `log-root-cause-and-proof.txt` | symptom reproduction, source-traced root cause with the exact call chain, the minimal fix and its boundary proof, the fresh-database and restart results, real HTTP outcomes, cleanup |
| `log-guard-effective-target.txt` | copied 060 guard PASS record for the real isolated target (fingerprint, config SHA, one tenant, one reference, no credentials) |
| `log-isolated-host-after-fix-bounded.txt` | bounded excerpt of the task-owned isolated Host stdout after the fix (first records plus non-Outbox Error records) |
| `log-isolated-host-restart-bounded.txt` | the same for the idempotence restart |
| `isolated/assert-scratch-target.js` | the 060 preflight guard copy used to gate the isolated target offline |
| `isolated/prepare-isolated-target.js` | builds the isolated overlay, the effective merged config and the launch override, asserts one tenant / one reference / no live target, then gates with the guard |
| `isolated/run-fixture-proofs.js`, `isolated/fixtures/**` | the guard's synthetic fixture suite (proves the copy is not weakened) |
| `isolated/launch-config/` | redacted launch configuration and hashes |
| `RESULT.bridge.txt`, `post-result.js` | Bridge result envelope and its posting script |

Planning document: `docs/planning/shipping/SHIPPING-MVP-SCRATCH-BOOTSTRAP-062.md`.

Production code changed: exactly one Development-only file,
`src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Development/AccessControlDevelopmentSeedPrelude.cs`.
No file under the frozen Host/Admin or Host/Security trees was added or modified.
