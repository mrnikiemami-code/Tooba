# R20 runtime URLs

Kept running for user review:

- Host: `http://127.0.0.1:5088` (`dotnet run --project src/backend/Host/Tooba.Host/Tooba.Host.csproj --urls http://127.0.0.1:5088`)
- Frontend: `http://127.0.0.1:3000` (`TOOBA_HOST_ORIGIN=http://127.0.0.1:5088`)
- Postgres: `127.0.0.1:5432` / `tooba_alpha`
- Host header: `alpha.localhost`

Storefront:

- Cart FA: http://127.0.0.1:3000/fa/cart
- Cart EN: http://127.0.0.1:3000/en/cart

Admin:

- Orders: http://127.0.0.1:3000/fa/admin/orders
- Order detail: http://127.0.0.1:3000/fa/admin/orders/01a098eb-7669-7000-9fc2-1654986b455b
- Receipts / payments: http://127.0.0.1:3000/fa/admin/receipts
- Store Settings مهلت‌ها: http://127.0.0.1:3000/fa/admin/settings
- Category reservation: http://127.0.0.1:3000/fa/admin/catalog/categories/01a05387-e545-7000-bc52-64f4c8f82baa/reservation
- Product / offer workspace: http://127.0.0.1:3000/fa/admin/products/01a05388-87f9-7000-a893-23eb5060ccee

Seller read-only:

- http://127.0.0.1:3000/fa/vendor-panel/products/47801c5c-7490-4782-912f-5c6cd5f3f879

Existing local Dev credentials (already in `appsettings.Development.json`): `Username=admin;Password=123456`. Admin actor header `X-Tooba-Dev-Actor-User-Id=01a036c2-970e-7000-8eb7-94bf5cc2d8db`.
