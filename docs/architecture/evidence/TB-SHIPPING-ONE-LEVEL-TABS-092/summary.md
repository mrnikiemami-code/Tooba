# Evidence index — TB-SHIPPING-ONE-LEVEL-TABS-092

Task: `TB-SHIPPING-ONE-LEVEL-TABS-092` (Development deploy + real five-tab smoke)
Status: **PARTIAL** · Stop-State: `STOP_FOR_ARCHITECT`

Narrative report: `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-092.md`

## Contents

| File | What it proves |
| --- | --- |
| `log-092-run.txt` | Task-scoped process PIDs (Host `:5100` = 37528, FE `:3100` = 32540) |
| `host-5100-stdout-excerpt.txt` | Redacted tail of the current-code Host startup on the real Development config |
| `host-5100-stderr.txt` | Empty — no startup error on the successful run |
| `log-http-gate-on-5100.txt` | Unauthorized `401`; authorized `200` with `gate=Enabled`, `ready=true`, 7 flat method codes |
| `log-tabs-5100.txt` | Binding resolver matrix, tab 2 rates, tab 3 packaging, tab 4 schedule/capacity, tab 5 read-only quotes |
| `log-fe-3100-live.txt` | Frontend BFF authentication against the task-scoped Host (login/me `200`) |
| `log-fe-3100-page.txt` | Authenticated server-rendered page fetch containing all seven method codes (fa + en) |
| `fe-page-fa-admin-shipping-services.html` | Rendered fa page payload: `gate:{state:"enabled",source:"server-flag"}` + seven methods, RTL |
| `fe-page-en-admin-shipping-services.html` | Same for the en locale |
| `01-five-tabs-services-rtl.png` | Browser screenshot: five-tab strip + seven-method list + inactive badges (RTL) |
| `02-cost-review-tab-rtl.png` | Browser screenshot: cost-review tab with real server-backed evaluation |
| `log-db-parity.txt` | Read-only DB parity: migration once, 7 methods / 14 translations, legacy 5/4, historical rows untouched |
| `db-parity-queries.sql` | The exact read-only SQL used (no DML) |
| `fe-3100-stdout.txt` | Next dev server ready on `:3100` |

## Not evidenced here (honest gaps)

- No proof of a five-tab UI on the **shared** Development `:5088` stack — its binary is stale (`404`) and
  the frontend dev actor is rejected there (`403`). See report §5 and §10.
- No coverage-grant **read** list exists (route is create-only), so tab 2 coverage is evidenced through
  per-method rate policies instead.
- No capacity reservation, COD ledger entry, promised delivery or packed-weight value was produced,
  by design.

## Redaction

No credentials, connection strings, tokens or session ids are recorded. The admin actor id used for
authorized probes is the non-secret Development bootstrap actor id. The oversized Host stdout log was
replaced by a trimmed excerpt.
