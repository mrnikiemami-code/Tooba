# Checkout invariants — evidence-backed

| Invariant | Enforcement | Modules | Failure if broken | Strong? | Compensation OK? |
|---|---|---|---|---|---|
| Cart converts at most once for a checkout | IdempotencyKey/CartId lookup + ConvertAsync + version | Order, Cart | Duplicate orders / double convert | Strong at commit | Partial (return existing) |
| Only Active cart converts | Cart.Status check pre-TX | Cart, Order | Convert non-active cart | Strong | N/A reject |
| Optimistic cart version | ExpectedCartVersion vs cart.Version | Cart, Order | Lost update / stale checkout | Strong | N/A reject |
| Quoted price must match live Pricing quote | QuoteSellerOrdersAsync PRICE_CHANGED | Pricing, Order | Under/over charge | Strong at submit | Reject |
| Offer Active + seller match | Offer lookup | Offer, Order | Invalid commercial facts | Strong | Reject |
| Tax must resolve applicable rule | TaxOutcome checks | Tax, Order | Untaxed illegal sale | Strong | Reject |
| Inventory reserved before order lines bound | Reserve then BindReservationsToOrders | Inventory, Order | Oversell | Strong at commit | ReleaseAcquiredAsync |
| Reservation bound 1:1 to order lines | BindReservationsToOrders | Order, Inventory | Orphan reserve / unbound line | Strong | Release |
| Abuse reservation/commit limits | ICheckoutAbuseGate | Order/Host | Abuse spam reservations | Policy | Reject |
| Payment not inside submit TX | Separate Host payment path | Payment, Wallet | N/A for submit | Financial strong later | Refund/credit post-capture |

Promotion evaluation adjusts line amounts inside quote; no separate durable promotion-quota write observed inside Submit TransactionScope (evaluate-only at commit path).
