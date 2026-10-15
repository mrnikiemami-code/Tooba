# TB-SHIPPING-ONE-LEVEL-TABS-102 — Evidence index

Task `TB-SHIPPING-ONE-LEVEL-TABS-102` (parent 101) · Channel `tooba-main` · Worker `tooba-worker-01`
Program `TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE` · Mode `BOUNDED_ISOLATED_STOREFRONT_LABEL_PROJECTION_PROOF`
Base-SHA `09870abc0140c0366696a07fa868f79307f25221` · Stop-State `STOP_FOR_ARCHITECT`
**Status: BLOCKED** — Blocker code `BLOCKED_ISOLATED_CART_PROOF`

| File | What it proves |
| --- | --- |
| `task.md`, `task-receipt.json` | Received artifact + claim record (task receipt also kept UNSTAGED as `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-102.task.md`) |
| `claim-task.js` | The claim used the protocol's `GET /api/tasks/next?channelId=tooba-main` after a `Working` heartbeat |
| `isolated/prepare-isolated-target-102.js` | The guarded overlay builder: exactly one tenant and two connection references, both the task-owned scratch target; the effective merged config is the exact configuration that was launched |
| `isolated/launch-isolated-102.ps1` | The launcher: stages the guarded config over the build output, binds `http://127.0.0.1:5192`, sets the one-level gate only as a process environment value when asked (`-Gate on`), never sets `ShippingRateAuthority` / `ShippingOperationalShadow` |
| `isolated-host-102-startup-excerpt.txt` | Isolated startup: Development environment, content root inside the task-owned output dir, legacy bootstraps explicitly skipped in the schema-only path |
| `legacy-bootstrap-crash-excerpt.txt` | Verbatim capture of the **first** launch's unhandled exception with `RunLegacyBootstraps=true` on the empty scratch database (Catalog-owned post-migration attribute-schema step cannot publish) |
| `probe-isolated-102.js` / `probe-isolated-102.txt` | The genuine isolated chain: anonymous projection `401`, customer `register` (first call `201`; repeat `500 platform.unexpected` duplicate-identifier), `login 200`, authenticated guest cart created via the API, **projection reached with the owned cart → `400 shipping.cart.empty`**, and the cart-line attempt → `400 cart.offer.unavailable` |
| `shared-parity.sql` / `.txt` | Read-only shared `tooba_alpha` parity after cleanup: 7 methods, 14 translations, `post:express`/`snapp_courier` inactive, 50 carts, 78 lines, 14 drafts, 26 seller orders — unchanged |
| `isolation-and-cleanup-facts.txt` | Scratch container/DB/schema/user, isolated provenance, gate facts, cleanup results and shared-host state |

All identifiers created in scratch are redacted in the stored output; the scratch and probe passwords live only in
untracked ephemeral files and were never written to evidence or git.

## Verdict in one paragraph

Isolation was established exactly as in 099 on a **new** disposable container (`tooba-102-scratch-pg`,
`127.0.0.1:55442`, DBs `tooba_102_scratch` / `tooba_102_messaging`, no `tooba_alpha` inside) with the current code
copied to a task-owned output dir and an isolated Host on `:5192` whose provenance is
`isolated-102 / store-alpha / tenant-102-scratch`. The genuine authenticated path worked: real customer
registration, real credential login, and an API-created owned cart — and the storefront projection was **reached**
with that cart, returning `400 shipping.cart.empty` instead of any identity/ownership error, which proves the
identity gate and cart access rules were satisfied legitimately. The one thing that could not be produced was a
**sellable offer**: the supported Development catalog bootstrap terminates the host with an unhandled
`InvalidOperationException` (`محصول برای انتشار آماده نیست`) when run against a genuinely empty database, and the
only remaining way to get an offer would have been hand-built raw catalog fixtures, which is precisely the
shortcut this task forbade. The cart-line attempt on the same isolated host therefore returned
`400 cart.offer.unavailable`. With no line, `RequireCartAsync` rejects the projection before any method list is
built, so the gate-OFF vs gate-ON label parity could not be exercised at all. Reported BLOCKED with the exact gap,
full cleanup and unchanged shared state.
