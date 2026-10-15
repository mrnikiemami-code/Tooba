# TB-SHIPPING-ONE-LEVEL-TABS-091 — evidence index

Status: **PARTIAL** — safe offline/isolated/live-DB proof complete; live UI on the shared Development Host
remains unproven. Code-SHA = `NONE` (no production source file changed).

| Artifact | Contents |
| --- | --- |
| `probe-live-post-091.txt` | read-only live `tooba_alpha` re-verification: migration history, 7 methods, 14 translations, legacy catalog and historical commerce row counts |
| `log-http-probes-091.txt` | DB-side isolation barrier, real-HTTP route/auth outcomes on the isolated current-code Host, and the read-only `:5088` baseline |
| `log-prepare-isolated-091.txt` | isolated target preparation and the offline isolation guard PASS record (fingerprint, CONFIG_SHA256, no credentials) |
| `log-isolated-build-091.txt` | `dotnet build Tooba.Host.csproj -o .tmp-091-host-out` result (exit 0, 0 errors) |
| `isolated-host-091-stdout.txt`, `isolated-host-091-stderr.txt` | task-owned isolated Host logs; stdout shows `Applying migration '20261015120000_AddShippingOneLevelCatalog'` and `Now listening on: http://127.0.0.1:5099` |
| `log-isolated-host-startup-091.txt` | the extracted migration/listening/environment lines from that log |
| `log-frontend-one-level-tests-091.txt` | focused five-tab frontend suite: 56 pass / 0 fail |
| `log-backend-one-level-tests-091.txt` | focused one-level backend suite: 45 pass / 0 fail |
| `log-cleanup-091.txt` | isolated Host stopped, scratch container removed, user-managed `:5088` process and binary re-verified unchanged, run directory removed |
| `isolated/assert-isolated-target-091.js` | offline isolation guard for the effective launch configuration |
| `isolated/prepare-isolated-target-091.js` | builds the isolated overlay + effective config + launch environment, then gates them |
| `isolated/launch-isolated-091.ps1` | task-owned isolated launch script (Development, `:5099`) |
| `RESULT.bridge.txt`, `post-result.js` | Bridge result envelope and its posting script |

Planning document: `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-091.md`.

No credential value is contained in any evidence file. The isolated run directory that held the scratch
password (`.tmp-091-host-run`) was deleted at cleanup; it was never tracked by git.

## Key numbers

* Isolated `Tooba.Host.dll` SHA-256 `DC25E1C063F616C40778700CC60EFFC5977A73393A9C5EE70C438C006E04D88A`
* Isolated `Tooba.Fulfillment.Endpoints.dll` SHA-256 `77B707F15A2FCF88820BA0C9733C2A1E7FA0EF82C5F432D7513872E26E3B52AE`
* Stale shared `:5088` `Tooba.Host.dll` SHA-256 `025CD55FFD1A967648058F996C4090D5D45186C9AB691CEAA1B6F80D5F1DEBF1` (unchanged)
* 090 backup `.tmp-090-backup/tooba_alpha-pre-090.dump` — `2492453` bytes, SHA-256
  `A3D924BA51449604506A27016C337FBF8D9E4B4776F3C18D22CF92BA822D9439` (matches 090 manifest)

## The decisive delta

| Target | `GET …/shipping-methods/one-level` | Interpretation |
| --- | --- | --- |
| shared Dev Host `:5088` (stale binary) | `404` | route absent from the running binary |
| isolated current-code Host `:5099` | `401 admin.actor.missing` | route mounted, admin authorization enforced, fail-closed |
