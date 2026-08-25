# TB-P05-T014 — Address authentication and isolation proof

## Runtime

Verified on 2026-08-25 with the normal Development Host at `http://127.0.0.1:5088` and the normal Next application at `http://127.0.0.1:3000`. Browser calls traversed the Next `/v1` rewrite. Development actors were supplied only through the controlled `X-Tooba-Dev-Actor-User-Id` seam; request bodies and routes contained no owner identifier.

## Separate actors

Seed actor `aaaaaaaa-aaaa-4aaa-8aaa-000000000009` listed two deterministic addresses (`200`). Isolated actor `cccccccc-cccc-4ccc-8ccc-000000000014` listed zero (`200`). GET of the seed default AddressId as the isolated actor returned `404`.

## Create / delete isolation

The isolated actor created one private address (`201`) and then listed count `1`. The row was deleted afterward so the actor remains empty for empty-state screenshots. The seed actor remained at two rows throughout.

## Checkout foreign AddressId

Checkout with the isolated actor and the seed AddressId returned `403` / `checkout.address.forbidden`. Own AddressId snapshot for the seed actor returned `200` with copied recipient fields.

## Browser proof

- `06-customer-address-empty-state.png` uses the isolated empty actor.
- `03`, `04`, `05`, `07`, `10`, and `11` use the deterministic seeded customer actor.
- Missing production actor continues to map to `401` at `/v1/customer/addresses` (covered by Host tests reading the endpoint source and runtime session gate).
