# Demo traceability

- Origin marker: `fashion-template-preview-pilot` (`FASHION_DEMO_ORIGIN`)
- Deterministic IDs: `demo-fashion-*` / `demo-shared-*`
- Section configs include `demoOrigin`
- Media: `demo-fashion-media-*` resolved only via `fashionDemoMediaUrl` (never Catalog Media)
- User-owned Host Catalog remains untouched
- Future cleanup can filter on origin marker / ID prefix without scanning user content
