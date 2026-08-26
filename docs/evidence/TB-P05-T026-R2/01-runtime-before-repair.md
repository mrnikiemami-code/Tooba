# 01 — Runtime before repair (TB-P05-T026-R2)

Predecessor: `7baf6eb4f45d16db9429cbaa332e3e67d22fcc48` on `main` (`HEAD == origin/main`).

All three runtimes verified **before** any tracked Home fidelity code change.

| Runtime | PID | URL | Status |
|---|---:|---|---|
| Tooba Backend | 29940 | http://127.0.0.1:5088/health | HTTP 200 `{"status":"ok"}` |
| Tooba Frontend | 26076 | http://127.0.0.1:3000/ | HTTP 200 |
| Original Shopeiva | 2560 | http://127.0.0.1:3017/ | HTTP 200 |

**Runtime-before: PASS**
