# 20 — Final runtime (TB-P06-T019-R1)

| Probe | Expected | Result |
|---|---|---|
| Host `GET /health` | 200 | 200 |
| Host `GET /v1/storefront/stories?locale=fa` | live stories | 200, ≥2 seed + approved seller after enable |
| Host `GET /v1/admin/stories` + Dev Actor | list + origin/review | 200 |
| Host `GET /v1/seller/stories` + SellerParty + Actor | own only | 200 |
| Seller create → submit | Draft → Submitted | OK |
| Seller `/approve` | absent | 404 |
| Admin approve → enable | Approved + Active; public includes | OK |
| Frontend `/admin/stories` | shared management + review | 200 + CDP `admin-stories` / review filter / approve |
| Frontend `/vendor-panel/stories` | shared management + create/submit | 200 + CDP `seller-stories`; no review filter; no approve |
| `/fa` Home Story rail | unchanged viewer | CDP `home-stories`; no `STORY_IMAGES` |
| Shopeiva `:3001` | reference | 200 + capture |

Browser proof JSON: `browser-proof.json`  
Captures: `captures/01-admin-stories-shared.png` … `04-shopeiva-home.png`

## USER-PREVIEW URLs

- Seller Stories: http://127.0.0.1:3000/vendor-panel/stories
- Admin Stories: http://127.0.0.1:3000/admin/stories
- Persian Home: http://127.0.0.1:3000/fa
- English Home: http://127.0.0.1:3000/en
- Shopeiva Home: http://127.0.0.1:3001/

Preview flow: Seller create → submit → Admin reject/approve → activate → Home rail.
