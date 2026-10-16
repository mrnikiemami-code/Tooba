# TB-SHIPPING-ONE-LEVEL-TABS-103 — Evidence index

Task `TB-SHIPPING-ONE-LEVEL-TABS-103` (parent 102) · Channel `tooba-main` · Worker `tooba-worker-01`
Mode `BOUNDED_CATALOG_SCRATCH_BOOTSTRAP_PREREQUISITE_REPAIR` · Base-SHA `cbbdc7cbe196914d0df2af7925e9492d22ebfe5e`
Stop-State `STOP_FOR_ARCHITECT` · **Status: PARTIAL**

| File | What it proves |
| --- | --- |
| `task.md`, `task-receipt.json` | Received artifact and claim record (receipt also kept UNSTAGED at `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-103.task.md`) |
| `claim-task.js` | The claim used `GET /api/tasks/next?channelId=tooba-main` after a `Working` heartbeat |
| `isolated/prepare-isolated-target-103.js` | Guarded overlay for the new scratch target (`tenant-103-scratch`, `127.0.0.1:55443`, `tooba_103_scratch`), `AllowResetAndSeed=true`, `RunLegacyBootstraps=false` |
| `isolated/launch-isolated-103.ps1` | Launcher: staged guarded config, bound `http://127.0.0.1:5292`, gate only as a process env value, `ShippingRateAuthority`/`ShippingOperationalShadow` never set |
| `bootstrap-demo-103.js` / `bootstrap-demo-103.txt` | The official path, end to end: real admin credential login `200`, `GET demo/status` `200` (empty scratch, `allowResetAndSeed=true`), `POST demo/reset-and-seed` aborted at the client's 300 s header timeout |
| `scratch-seed-state.sql` / `.txt` | Server-side truth at abort: 545 Draft products, 116 categories, 41 attribute definitions, **offers=0, prices=0, stock=0** |
| `legacy-bootstrap-crash-excerpt.txt` | Carried forward from 102: the `RunLegacyBootstraps=true` startup crash (`WorkspaceProductPublishRejected`) that this task had to work around |
| `shared-parity.sql` / `.txt` | Read-only shared `tooba_alpha` parity after cleanup — unchanged (7 methods / 14 translations / 50 carts / 78 lines / 14 drafts / 26 seller orders) |
| `isolation-and-cleanup-facts.txt` | Scratch container/DB/provenance, gate facts, seed-attempt result, cleanup and shared-host state |

## Verdict in one paragraph

No source change was made because the failure could not be attributed to a provable single prerequisite without
instrumenting the readiness gate, and the task forbids guessing. The supported alternative — the authorized
Development demo seed API — was exercised instead on a genuinely isolated scratch database with a real admin
credential login, and it reached **545 Draft publish-ready products** with 116 categories and 41 attribute
definitions before the HTTP request was aborted by the client's 300-second header timeout (which also cancels the
server-side work). The sellable phase therefore never ran: `offers=0, prices=0, stock=0`, so there was still no
sellable offer, no cart line, and no storefront label comparison. The precise remaining gap is now identified and
actionable: the seed must be allowed to run to completion without a client-side abort. The isolated environment was
fully destroyed, credentials are redacted everywhere, and shared `tooba_alpha` / `:5088` / `:3000` are unchanged.
