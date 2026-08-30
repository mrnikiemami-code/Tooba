# Final idempotency — TB-P07-T034-R1

Two consecutive `POST /v1/admin/catalog/demo/reset-and-seed`:

| Run | productsRemoved | seed products | grid total | Published |
|-----|----------------:|--------------:|-----------:|----------:|
| r1-first | 287 | 283 | 283 | 0 |
| r1-second | 283 | 283 | 283 | 0 |

Counts identical on rerun; residuals do not accumulate.
Evidence: `live-seed-r1-first.json`, `live-seed-r1-second.json`, `live-status-r1-*.json`.
