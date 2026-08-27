# 23 — Final runtime (TB-P06-T015)

## Probes

| URL | Result |
|---|---|
| `http://127.0.0.1:5088/health/live` | 200 |
| `http://127.0.0.1:5088/health/ready` | 200 |
| `http://127.0.0.1:3000/` Home | 200 — composition-driven |
| `http://127.0.0.1:3000/admin/page-composition` | 200 |
| `http://127.0.0.1:3000/blogs` | 200 |
| `http://127.0.0.1:3000/customer-panel` | 200 |
| `http://127.0.0.1:3000/vendor-panel` | 200 |
| `http://127.0.0.1:3000/admin` | 200 |
| `http://127.0.0.1:3001/` Shopeiva | 200 |
| `GET /v1/storefront/home/composition` | 200 |

## USER-PREVIEW

- Home: http://127.0.0.1:3000/
- Admin composition: http://127.0.0.1:3000/admin/page-composition
- Shopeiva reference Home: http://127.0.0.1:3001/

## Captures

See `captures/01`–`08` PNG + `16-composition-e2e-proof.md`.

Keep Host :5088 + Frontend :3000 + Shopeiva :3001 running after Result where possible.
