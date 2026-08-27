# 00 — Recovery & runtime before work (TB-P06-T029)

## Recovery

| Check | Value |
| --- | --- |
| toplevel | `D:\Users\User\source\repos\SarvNewVer` |
| branch | `main` |
| HEAD | `1c38ddb3d7d4f7490aa568a5274d4b95ad0dfdfc` |
| origin/main | `1c38ddb3d7d4f7490aa568a5274d4b95ad0dfdfc` |
| expected predecessor | `5dc0204fd2e928bb98c9e881956c99503bf6893b` |
| relation | HEAD is docs-meta sync commit immediately after predecessor; tree synchronized |
| tracked tree | clean (local untracked logs/junk only) |

## Runtime before work

| Service | Probe | Result |
| --- | --- | --- |
| PostgreSQL | via Host ready | included in ready checks |
| Backend | `http://127.0.0.1:5088/health/live` | 200 `{"status":"ok"}` |
| Backend | `http://127.0.0.1:5088/health/ready` | 200 |
| Tooba FE | `http://localhost:3000` | responds (locale may 308→/fa) |
| Shopeiva | `http://127.0.0.1:3001` | 200 |

Worker: `tooba-worker-01` · Channel: `tooba-main` · status Working · Bridge task `75378a8b-53f0-4192-afae-40e426f0e52f`

Runtimes MUST remain alive after Result.
