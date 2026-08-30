# Live verification — TB-P07-T029

- Host `:5088` health/live 200, health/ready 200
- FE `:3000/admin` 200
- Upload real PNG via `POST /v1/admin/media/upload` → asset created
- Invalid MIME rejected with `media.type.unsupported`
- `GET /v1/admin/media` paging works
- `GET /v1/storefront/media/{id}` returns image/png bytes
- Product media attach/unassign; asset remains in DAM after unassign
- Category image/icon/banner assign + clearBanner

Raw log: `live-verify.log`
