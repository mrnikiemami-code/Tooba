# R19 network / performance

| Surface | Requests |
| --- | --- |
| Cart pending list | one batched pending-payments + GetProjectionsAsync(checkoutIds) |
| Countdown | local interval; one refresh at zero |
| Orders/Payments grids | one GetProjectionsAsync per page; no per-row cycle fetch |
| Store Settings | reservation rides hold-policy GET |
| Category | one GET |
| Offer batch | PreviewManyAsync + existing lookups |
| Unpaid worker | server poll interval ≥5s; not storefront |

No per-second API polling. No Settings fetch loop. No supply queries on countdown tick.
