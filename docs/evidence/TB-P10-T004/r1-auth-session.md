# Auth Session — TB-P10-T004-R1

| Mechanism | Result |
|---|---|
| Register+Login Host | PASS (ephemeral `*.example.test`; password not committed) |
| Bearer accessToken | PASS |
| BFF `tooba_session` | PASS (cookie jar local only; not committed) |
| DevActor header as login | NOT used (`usedDevActorHeader: false`) |

Secrets omitted from evidence payloads.
