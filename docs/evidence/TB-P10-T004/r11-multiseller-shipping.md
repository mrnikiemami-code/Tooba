# TB-P10-T004-R11 — Multi-seller + shipping

Required cases proven in focused tests and `_r11_runtime.mjs`:

| Case | Proof |
| --- | --- |
| One seller + shipping | Runtime A |
| Two sellers + shipping | Runtime B (`KG` + `ARMAN`) |
| Shipping = 0 | Runtime `shipping-zero` + unit `207100` without shipping |
| Manual confirm | Runtime C (`confirm_deposit`) |
| Sandbox online | Runtime D |
| R10 retry after timeout | Runtime E |
| R10 late captured | Runtime F |
| Duplicate success | Runtime G (inbox count unchanged) |

Seller settlement-visible amount = seller allocation sum (excludes StoreShipping) in every money snapshot.
