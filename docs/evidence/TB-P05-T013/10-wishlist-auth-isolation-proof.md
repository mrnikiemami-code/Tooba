# TB-P05-T013 — Wishlist authentication and isolation proof

## Runtime

Verified on 2026-08-25 with the normal Development Host at `http://127.0.0.1:5088` and the normal Next application at `http://127.0.0.1:3000`. Browser calls traversed the Next `/v1` rewrite. Development actors were supplied only through the controlled `X-Tooba-Dev-Actor-User-Id` seam; request bodies and routes contained no owner identifier.

## Separate actors

Product under test: `01a030d1-4056-7000-baf1-99951569bc6b`.

```json
{"SeedActor":"aaaaaaaa-aaaa-4aaa-8aaa-000000000009","SeedBefore":3,"IsolatedActor":"cccccccc-cccc-4ccc-8ccc-000000000013","IsolatedBefore":0,"IsolatedAdd":201,"SeedAfter":3,"IsolatedAfter":1}
```

The isolated actor began empty and reached one item. The seeded actor remained at three throughout, proving the mutation did not cross the owner boundary. The isolated row was removed after the probe.

## Idempotency, membership and safe removal

The same product was exercised against actor `bbbbbbbb-bbbb-4bbb-8bbb-000000000013`:

```json
{"Before":0,"FirstAdd":201,"SecondAdd":200,"Membership":true,"After":1,"FirstRemove":204,"SafeRemove":204,"Final":0}
```

- First add created one row (`201`).
- Repeated add was idempotent (`200`) and the list still contained one row.
- Batched membership returned the product.
- First remove and repeated absent remove both returned `204`.
- Final list count was zero.

## Browser proof

- `04-customer-wishlist-empty.png` uses a distinct empty actor.
- `08-wishlist-remove-action.png` uses another isolated actor, adds one real product through the API, removes it through the visible card heart, waits for the live empty response, and captures the resulting state.
- `03`, `05`, `06`, `07`, and `09` use the deterministic seeded customer actor.
