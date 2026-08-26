# 10 — Final three-runtime proof (TB-P05-T026-R1)

**All three runtimes left running at Result for user side-by-side preview.**

| Runtime | PID | URL | Final check |
|---|---:|---|---|
| Tooba Backend | 29940 | http://127.0.0.1:5088/health | HTTP 200 `{"status":"ok"}` |
| Tooba Frontend | 26076 | http://127.0.0.1:3000/ | HTTP 200 Home; HTTP 200 PDP `/products/demo-game-2` |
| Original Shopeiva | 2560 | http://127.0.0.1:3017/ | HTTP 200 Home; HTTP 200 PDP |

## Post-build FE recovery

After `next build`, dev Home returned 500. Recovery: cleared `.next`, restarted `next dev` → Home/PDP 200 again.

## USER-SIDE-BY-SIDE-PREVIEW (working URLs)

| Label | URL |
|---|---|
| Tooba Backend Health | http://127.0.0.1:5088/health |
| Tooba Home | http://127.0.0.1:3000/ |
| Tooba PDP | http://127.0.0.1:3000/products/demo-game-2 |
| Original Shopeiva Home | http://127.0.0.1:3017/ |
| Original Shopeiva PDP | http://127.0.0.1:3017/product/1/%DA%AF%D9%88%D8%B4%DB%8C-%D9%85%D9%88%D8%A8%D8%A7%DB%8C%D9%84-%D8%A7%D9%BE%D9%84-%D8%A2%DB%8C%D9%81%D9%88%D9%86-%DB%B1%DB%B5-%D9%BE%D8%B1%D9%88-%D9%85%DA%A9%D8%B3 |

**Final three-runtime: PASS**
