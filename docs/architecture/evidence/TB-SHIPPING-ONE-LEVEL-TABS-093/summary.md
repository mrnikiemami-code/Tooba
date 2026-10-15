# Evidence index — TB-SHIPPING-ONE-LEVEL-TABS-093

Task: `TB-SHIPPING-ONE-LEVEL-TABS-093` (reproducible Development Host + real admin auth + shared-stack UI proof)
Status: **PASS** for the bounded task scope · Stop-State: `STOP_FOR_ARCHITECT`

Narrative report: `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-093.md`
Operator script: `scripts/dev/host-stack.ps1`

## Contents

| File | What it proves |
| --- | --- |
| `log-auth-diagnosis-pre.txt` | Pre-change matrix on the old shared `:5088`: real login session 200, guest actor 403, bootstrapped admin 200, no credential 401; one-level route 404 everywhere (stale binary) |
| `log-host-stack-start.txt` | Controlled takeover: provenance gate, prior-process record, graceful stop of the orphan, current-code start, health + provenance verification, PID file |
| `log-shared-5088-gate-on.txt` | Shared `:5088` on current code: health, provenance, real BFF login, and the one-level matrix — 401 unauth / 403 guest actor / 200 real session with `gate=Enabled`, `ready=true`, 7 flat method codes |
| `log-rollback-gate-off.txt` | Rollback proof: restart without the gate → `{"gate":"Disabled","ready":false,"methods":[]}`, legacy catalog still 200, shared FE page has no one-level tab strip, `status` shows `gate=defaults (all gates off)` |
| `log-final-operator-state.txt` | Final deliberate operator state (current code + ephemeral gate ON) |
| `log-db-parity-after.txt` | Read-only parity after all work: methods 7, translations 14, legacy 5/4, seller_orders 26, order_lines 37, fulfillments 9 |
| `01-shared-3000-five-tabs-rtl.png` | Shared `:3000` browser screenshot: five RTL tabs, seven flat methods, inactive badges |
| `02-shared-3000-cost-review-rtl.png` | Shared `:3000` browser screenshot: read-only cost review with a real server-backed evaluation |

## Redaction

No password, token, session id, connection string or secret is recorded. The admin actor id shown is the
non-secret Development bootstrap actor id. The Development login password appears only in the transient
probe commands, never in any evidence file.

## Honest gaps

- No coverage-grant read list exists (create-only route), so tab 2 coverage is evidenced through
  per-method rate policies.
- The anonymous server-side admin fallback still uses the storefront guest actor id and therefore reads
  as disabled/403; this is a documented finding, deliberately **not** changed.
- No capacity reservation, COD ledger entry, promised delivery or packed-weight value was produced.
