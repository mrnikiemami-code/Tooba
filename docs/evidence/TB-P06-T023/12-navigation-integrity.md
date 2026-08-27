# 12 — Navigation integrity (TB-P06-T023)

## Live nav (after backend + UI)

| Shell | Entry | `live` |
|---|---|---|
| `customer-panel-shell.tsx` | `/customer-panel/notifications` | `true` |
| `vendor-shell.tsx` | `/vendor-panel/notifications` | `true` |

## Deferred lists

`CUSTOMER_DEFERRED_NAV_HREFS` no longer includes notifications:

```text
/customer-panel/wallet
/customer-panel/tickets
/customer-panel/gift-cards
```

Vendor deferred remains customers / tickets / gift-cards (notifications not deferred).

## Tests

| File | Asserts |
|---|---|
| `customer-panel/panel-nav-integrity.test.ts` | notifications in LIVE_HREFS; not in deferred |
| `vendor-panel/panel-nav-integrity.test.ts` | notifications in LIVE_HREFS |

Reported: **4 nav integrity tests passed**.

## Dead links

None for notifications — routes map to real pages + Host APIs. No nav exposure before capability was live (T020 deferred stub removed).
