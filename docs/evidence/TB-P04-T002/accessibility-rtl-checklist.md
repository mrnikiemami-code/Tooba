# TB-P04-T002 — Accessibility / RTL checklist

| Check | Status |
| --- | --- |
| Keyboard-operable buttons/inputs | Yes (native) |
| Visible focus | `:focus-visible` on document |
| Icon-only label | `IconButton` requires non-empty `label`; invariant helper |
| Form error association | `Field` clones `aria-describedby` / `aria-invalid` |
| Dialog Escape / modal | native `<dialog showModal>` |
| Drawer logical placement | `start-0`; invariant `drawerUsesLogicalStart` |
| LTR islands | `Input ltrIsland` |
| Live region | `ToastRegion` polite |
| Reduced motion | global media query |
| Touch target | min-h-11 / min-w-11 |
| Contrast | semantic tokens; not a WCAG lab certification |
| Screenshots | Not captured in-repo (no committed binaries). Showcase at `/design-system` with noindex. Visual ACCEPT is not claimed. |
| Component test harness | None in Tooba frontend; documented gap. Relied on tsc/lint/build + invariants. |
