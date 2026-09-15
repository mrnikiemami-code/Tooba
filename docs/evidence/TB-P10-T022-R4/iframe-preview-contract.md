# Iframe preview contract

- Admin embeds `/template-preview/fashion` via `<iframe sandbox="allow-scripts allow-same-origin">`
- Device modes set frame `width`/`height` in px (920 / 768 / 390) — no CSS `zoom` / `transform: scale`
- Vertical scroll inside iframe frame
- Separate document contexts: Admin CSS does not cascade into iframe; Storefront CSS does not recolor Admin shell
- Preview page intercepts anchor clicks and form submits (`data-preview-safe`)
- No cart/auth mutation required for preview
- Non-Fashion templates keep wireframe canvas (pilot scope = Fashion only)
