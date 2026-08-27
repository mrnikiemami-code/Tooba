# 12 — Final runtime (TB-P06-T016-R1)

## Triad health

| Probe | Result |
|---|---|
| `http://127.0.0.1:5088/health/live` | **200** |
| `http://127.0.0.1:5088/health/ready` | **200** |
| `http://127.0.0.1:3001/` Shopeiva | **200** |

## Prefixed routes

| URL | Result |
|---|---|
| `http://127.0.0.1:3000/fa` | 200 — `lang=fa` `dir=rtl` |
| `http://127.0.0.1:3000/en` | 200 — `lang=en` `dir=ltr` |
| `http://127.0.0.1:3000/fa/blogs` | 200 — fa/rtl |
| `http://127.0.0.1:3000/en/blogs` | 200 — en/ltr |
| `http://127.0.0.1:3000/fa/products/demo-game-3` | 200 — fa/rtl + canonical/hreflang |
| `http://127.0.0.1:3000/en/products/demo-game-3` | 200 — en/ltr + canonical/hreflang |
| `http://127.0.0.1:3000/fa/blogs/guide-online-shopping` | 200 — fa/rtl; no fake hreflang |
| `http://127.0.0.1:3000/en/blogs/guide-online-shopping` | 200 — honest fallback; no fake hreflang |

## Redirects

| URL | Result |
|---|---|
| `http://127.0.0.1:3000/` | **308** → `/fa` |
| `http://127.0.0.1:3000/blogs` | **308** → `/fa/blogs` |
| `http://127.0.0.1:3000/products` | **308** → `/fa/products` |
| `http://127.0.0.1:3000/products?q=demo&page=1` | **308** → `/fa/products?q=demo&page=1` |
| `http://127.0.0.1:3000/fr/products` | **404** |

## SEO / composition

| Check | Result |
|---|---|
| Sitemap | 200; ~30 locale-prefixed entries; no unprefixed `/products` duplicates |
| Composition | 11 sections for `fa` and `en` |
| Link preservation | `/fa` stays `/fa`; `/en` stays `/en`; `unprefixedPublic=[]` on primary surfaces |

## USER-PREVIEW

- Persian Home: http://127.0.0.1:3000/fa
- English Home: http://127.0.0.1:3000/en
- Persian Blog: http://127.0.0.1:3000/fa/blogs
- English Blog: http://127.0.0.1:3000/en/blogs
- Persian PDP: http://127.0.0.1:3000/fa/products/demo-game-3
- English PDP: http://127.0.0.1:3000/en/products/demo-game-3
- Persian Article: http://127.0.0.1:3000/fa/blogs/guide-online-shopping
- Original Shopeiva: http://127.0.0.1:3001/

## Machine proof

`docs/evidence/TB-P06-T016-R1/_acceptance-proof.json`  
Captures: `docs/evidence/TB-P06-T016-R1/captures/01-*.png` … `10-*.png`

## Operational note

Keep Host `:5088` + Frontend `:3000` + Shopeiva `:3001` running after Result where possible for Architect preview.

## SoT snapshot

```text
TB-P06-T016 = REPAIR_REQUIRED
TB-P06-T016-R1 = AWAITING_ARCHITECT_ACCEPT
PUBLIC_LOCALE_ROUTING = PREFIXED
```
