# TB-SHIPPING-ONE-LEVEL-TABS-101 — Evidence index

Task-ID `TB-SHIPPING-ONE-LEVEL-TABS-101` (Parent 100) · Channel `tooba-main` · Worker `tooba-worker-01`
Program `TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE` · Mode `BOUNDED_SHARED_DEVELOPMENT_STOREFRONT_LABEL_READ_ONLY_PROOF`
Base-SHA `4fc2c4b0dcaac565124fe16420955103e314a1ad` · Stop-State `STOP_FOR_ARCHITECT`
Status: **PARTIAL** (deployment proven; storefront projection not reachable read-only without DML)
Blocker code: `BLOCKED_REAL_STOREFRONT_PROOF`

| File | What it proves |
| --- | --- |
| `baseline-preflight.sql` / `.txt` | Time-stamped read-only baseline before any change: 7 one-level methods, 14 translations, `post:standard` active with fa name «معمولی», legacy service/option state, languages |
| `host-restart-101.txt` | Full `host-stack.ps1 -Action restart -OutDir .tmp-dev-host-out-5088-101 -GateShippingOneLevelCatalog` log: build `0 Error(s)` (33s), ephemeral process-only gate, `/health` OK, provenance tuple OK, PID file written, rollback instructions |
| `host-restart-101-intermediate-superseded.txt` | Disclosed earlier rollout inside the same task (script default outDir `.tmp-dev-host-out-5088`, `pid=41824`, gate ON, current code) that was superseded by the task-named isolated outDir above so the rollout owns a task-specific build directory; its process was replaced by the script itself (graceful, no blind kill) |
| `deployment-facts.txt` | Post-rollout ownership/hash/gate facts and deployed-binary seam verification |
| `live-http-proof.txt` | Live read-only HTTP captures: `/v1/auth/me` 200, projection fail-closed for guest/foreign/unknown cart, live one-level read with `gate=Enabled` and `post:standard → «معمولی»` |
| `probe-readonly-101.js` / `.txt` | Anonymous fail-closed probes on `:5088` (all `401 checkout.authentication_required`) |
| `reachability-facts.txt` | Why no safe read path exists: session user owns 0 carts; no Active Authenticated cart; guest secrets unrecoverable |
| `no-write-proof.sql` / `.txt` | Newest `cart.carts` row = 2026-10-15 09:48Z, drafts = 2026-10-14Z, shipping methods = 14:45Z — all before the task window ⇒ zero DML |
| `parity-post.sql` / `.txt` | Post-run parity: still 7 methods / 14 translations, `post:standard` still active with «معمولی», `post:express`/`snapp_courier` still inactive, 26 seller orders, 50 carts / 78 lines / 14 drafts |

## Result in one paragraph

The shared Development Host on `:5088` did **not** contain the task-100 Order label seam (its output
was built 01:55, before commit 02:45), so it was rebuilt once from current `main` and restarted through
`scripts/dev/host-stack.ps1` under script-managed ownership, keeping the pre-existing
`ShippingOneLevelCatalog=true` gate state, and it now serves the seam symbol in
`Tooba.Fulfillment.Infrastructure/Contracts` and `Tooba.Order.Application` with `/health` 200 and the
exact provenance tuple `SingleStore / dev-singlestore / store-alpha / tenant-alpha`. The live process
returns the Fulfillment-owned one-level name «معمولی» for `post:standard` with `gate=Enabled`,
`ready=true`. The **storefront projection itself could not be exercised**: the identity gate needs a
session (present) *and* a cart owned by that session (none exists — 0 owned carts, 0 Active
Authenticated carts, 8 Active Guest carts whose secrets are unrecoverable), and producing one would
require forbidden DML. No data was written (row timestamps prove it), `:3000` was never restarted, and
every failure mode observed was correctly fail-closed.
