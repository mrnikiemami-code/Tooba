# Live verification — TB-P07-T032

Runtimes:
- Host `:5088` health 200 (restarted with Tag migration loaded)
- FE `:3000` admin products/categories/edit 200
- Shopeiva `:3001` 200

Live API:
- `GET /v1/admin/catalog/tags` → list OK
- `POST /v1/admin/catalog/tags` → create OK
- `POST /v1/admin/catalog/products/{id}/tags/{tagId}` → assign OK; product tags count 1
- Brand options → 9

UI routes smoke: Product list, Product EDIT, Category Admin → 200

Manual checklist covered by implementation + unit gates:
- Global Edit / feedback / Media Persian / MegaMenu combobox / Brand combobox / Category membership picker / Tags cards
