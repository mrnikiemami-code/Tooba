# 02 — Dead / Fake UX sweep (TB-P06-T029)

## Findings repaired

| Location | Issue | Fix |
| --- | --- | --- |
| `customer-panel/page.tsx` SummaryRow | Labelled Wallet/Tickets/Gift as «فعلاً در دسترس نیست» while routes are LIVE | Replaced with honest «فعال» rows + quick actions for wallet/tickets/gift/notifications |

## Honest / non-blocking

| Location | Note |
| --- | --- |
| `vendor-panel/analytics` | Charts explicitly unavailable until Host capability; metrics from live dashboard API |
| Wishlist/addresses | Show «غیرفعال» when Host capability missing — truthful |
| Mixed tender | Labeled DEFERRED on checkout — not claimed LIVE |

## Still watch

| Item | Classification |
| --- | --- |
| Product card placeholder images («Tooba» gradient) | PLACEHOLDER media — commercial seed; not fake price |
| `/blog` vs `/blogs` | `/blog` 404; canonical route is `/fa/blogs` — document in inventory (not nav-linked) |
| Next.js Dev Issues badge | Dev overlay only; not user-facing production UX |

No fake balances, fake charts claiming revenue, or cosmetic Save found on primary commercial path after repair.
