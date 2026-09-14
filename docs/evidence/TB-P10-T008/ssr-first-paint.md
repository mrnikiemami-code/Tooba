# TB-P10-T008 — SSR first paint

`html` gets `data-storefront-theme-mode`, `data-storefront-color-scheme`, and `class=dark` when the server-effective scheme is dark.

LightOnly/DarkOnly resolved on the server. UserChoice reads `tooba-storefront-color-scheme` cookie (missing cookie = light). System server fallback is light; a static blocking script applies `prefers-color-scheme` before paint.

Script is `THEME_BOOTSTRAP_SCRIPT` in repo, not a DB payload.
