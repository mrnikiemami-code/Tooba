# TB-P10-T008 — Runtime A–O

Host `:5088` rebuilt after ThemeMode C# (Postgres `postgres-db` recycled). FE `:3000` restarted. Actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`. No manual DB write. Raw: `runtime-raw.json` (`ok: true`).

| Step | Proof |
| --- | --- |
| A LightOnly + tooba-blue | PUT 200 `themeMode=LightOnly` `paletteKey=tooba-blue` tokens `37 99 235` |
| B Home/PDP/Cart/Shipping light | all `data-storefront-theme-mode=LightOnly` `scheme=light` no `html.dark` |
| C DarkOnly | PUT 200 `themeMode=DarkOnly` |
| D first paint dark | `/fa` `class=dark` `scheme=dark` bootstrap script present |
| E Home/PDP/Cart/Shipping dark | all `darkClass=true` |
| F System | PUT 200 `themeMode=System` |
| G system SSR fallback light | fetch HTML `scheme=light` no dark class |
| H OS dark | static `THEME_BOOTSTRAP_SCRIPT` applies `prefers-color-scheme` before paint |
| I UserChoice | PUT 200 |
| J toggle cookie dark | missing cookie = light; `Cookie: tooba-storefront-color-scheme=dark` → dark |
| K nav stays dark | PLP/PDP/Cart/Shipping keep dark with same cookie |
| L reload dark | cookie persists |
| M toggle light reload | cookie light → no dark class |
| N palette while dark | PUT forest-green DarkOnly; Home `21 128 61` + dark class |
| O restore | PUT tooba-blue LightOnly; Home light `37 99 235` |
| invalid theme | PUT Sepia → 400 `appearance.theme.invalid` |
| unauthorized | PUT without actor → 401 |

Screenshots:

- `admin-theme-mode.png` — Appearance tab ThemeMode cards (LightOnly/DarkOnly/System)
- `home-light.png` / `home-dark.png`
- `pdp-dark.png` / `cart-dark.png` / `shipping-dark.png`
- `userchoice-toggle.png` — moon toggle in header (UserChoice)
- `home-forest-green-dark.png` — green chrome + dark header
- `status-error-dark.png` — Next overlay + shipping dark surfaces (danger stays off PaletteKey)

Console: Next.js N overlay (2–3 issues) pre-existing, not a hydration/theme mismatch. Appearance network: Admin PUT + public GET only; toggle writes cookie/localStorage only.
