# Fashion demo data

- Module: `src/frontend/lib/storefront-composition/fashion-demo-preview.ts`
- In-memory only — no Host/DB writes
- 8 top-level three-level category trees (`demo-fashion-cat-*`)
- 15 Fashion products (`demo-fashion-prod-*`) with fashion media URLs
- Fashion banners via `BannerShowcase` `imageUrl` (Unsplash apparel imagery)
- 6 demo brands (`demo-fashion-brand-*`)
- Shared reviews + articles in render context
- Stories: shared `HomeStoriesSection` Host binding (no Fashion-specific story seed)
- Isolation: IDs/prefixes + `demoOrigin: fashion-template-preview-pilot` in every section config
- Cleanup: not implemented; Admin actions remain disabled
