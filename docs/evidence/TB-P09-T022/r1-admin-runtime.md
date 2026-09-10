# TB-P09-T022-R1 — Admin runtime

## Preconditions

- Host: `http://127.0.0.1:5088`
- FE: `http://127.0.0.1:3000` — Next **Ready**
- `TOOBA_HOST_ORIGIN=http://127.0.0.1:5088`

## HTTP 200 (after FE Ready)

| Order | Role | URL | HTTP |
| --- | --- | --- | --- |
| `01a08973-d831-7000-ae48-d6f8a6bc3fcf` | Single-seller | `http://127.0.0.1:3000/fa/admin/orders/01a08973-d831-7000-ae48-d6f8a6bc3fcf` | **200** |
| `01a08973-dd8c-7000-b205-cc7f15358dfb` | Multi-seller delivered | `http://127.0.0.1:3000/fa/admin/orders/01a08973-dd8c-7000-b205-cc7f15358dfb` | **200** |
| `01a0898a-0d7d-7000-b5ed-7f5d85a29241` | Fresh multi-seller | `http://127.0.0.1:3000/fa/admin/orders/01a0898a-0d7d-7000-b5ed-7f5d85a29241` | **200** |

## Rendered surfaces (browser)

On multi-seller Detail → اقلام و ارسال:

- Order Detail shell loads (no endless spinner / no render crash)
- بسته‌بندی مرکزی present when multi-seller
- Member shipments still visible under central package hierarchy

Single-seller: no بسته‌بندی مرکزی heading (see `r1-visual-smoke.md`).

## Note on prior 500

When FE was down/restarting, the same Admin URLs returned 500 (`Internal Server Error` / `ECONNRESET`). After Ready, routes are healthy — see `r1-fe500-discovery.md`.
