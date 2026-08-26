# 01 — Runtime before fidelity review (TB-P06-T010-R1)

| Service | Command / URL | PID / Port | Status |
| --- | --- | --- | --- |
| PostgreSQL | local instance `:5432` | existing | available |
| Backend | `dotnet run --urls http://127.0.0.1:5088` | 33524 / 5088 | running |
| Tooba Frontend | `npm run dev -- --port 3000` | restarted after build | running |
| Original Shopeiva | reference dev on `:3001` during capture | n/a | used for CDP captures |

Pre-change HEAD: `3317f4771fbc4c1f0505c442482ea409892c066a`

Capture script: `scripts/capture-t010-r1-fidelity-evidence.mjs`
