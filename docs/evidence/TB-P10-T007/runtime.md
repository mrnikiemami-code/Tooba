# TB-P10-T007 — Runtime A–L

Host `:5088` PID 35708 (T006 recycle). FE `:3000`. Actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`. No manual DB write. Raw: `runtime-raw.json`.

B script flag was a false positive on `suppressHydrationWarning` in layout (not a hydration mismatch). First HTML still has `data-storefront-palette` + `--color-primary` before paint.

| Step | Proof |
| --- | --- |
| A tooba-blue | Admin GET + `/v1/storefront/appearance` `paletteKey=tooba-blue` tokens `37 99 235` |
| B inspect | `/fa` `/fa/products` `/fa/products/demo-prod-av-audio-headphones-3` `/fa/cart` `/fa/shipping` all `data-storefront-palette=tooba-blue` `--color-primary:37 99 235`; no `--color-danger` on html |
| C forest-green | one PUT 200 `{paletteKey:"forest-green"}` |
| D–E fresh nav | Home/PLP/PDP/Cart/Shipping `forest-green` `--color-primary:21 128 61` |
| F status | appearance style omits danger; `text-red-600` probe stays `rgb(220, 38, 38)` / `bg-red-50` `rgb(254, 242, 242)` while brand is green |
| G wine-burgundy | PUT 200 |
| H | Home/PDP/Shipping `159 18 57` |
| I slate-navy | PUT 200 (high-contrast fourth) |
| J | Home/PDP/Shipping `30 58 95` |
| K restore | PUT `tooba-blue` 200 |
| L | appearance + Home/PDP/Shipping `37 99 235` |

Screenshots (copied from browser capture):

- `home-tooba-blue.png` / `home-forest-green.png` (header + slider dot follow palette)
- `pdp-tooba-blue.png` / `pdp-forest-green.png` (header chrome; product media image stays decorative blue)
- `shipping-tooba-blue.png` / `shipping-forest-green.png` (hero + «آدرس جدید» CTA follow palette)
- `status-isolation-forest-green.png` (brand green; Next overlay / danger remain red)

Shopeiva `:3001` 200 (424091 bytes). Console: Next.js `N 1 Issue` overlay present on both palettes (pre-existing overlay, not a palette/hydration mismatch). Appearance network: Admin PUT + public GET only; no per-card appearance.
