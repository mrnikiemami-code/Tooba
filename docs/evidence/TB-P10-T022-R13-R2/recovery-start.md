# Recovery Start — TB-P10-T022-R13-R2

- branch: main
- HEAD at claim: 0fe2dfdbddc4ecc9bd2e338f62447a85b6b5d24c
- origin/main: 0fe2dfdbddc4ecc9bd2e338f62447a85b6b5d24c
- HEAD==origin/main: True
- Expected prior tip in task: 44e5a680 (superseded; tip advanced via R13-R1-R1)
- Claimed Bridge task: fcb605f7-ff03-4eb0-ad8b-261817b12f87 / TB-P10-T022-R13-R2
- Existing Product Showcase variants audited: product.card-carousel (آریا), product.amazing (شگفت‌انگیز), product.zohreh (زهره), product.mahoor (ماهور), plus grid/compact/tabbed/etc.
- ProductCard: StorefrontProductCardView unchanged
- Rails: Amazing/Zohreh/Mahoor Swiper intact; generic ProductRailSection overflow rails intact
- Embla: added embla-carousel-react@8.5.2 scoped to new rails only
- SSR: client components still SSR product markup via props
- Unrelated .tmp-* / stashes unrelated-pre-r10 + temp-before-push: untouched
- Unrelated local dirty hero/selector work: preserved unstaged
