# TB-P10-T006 — Cache invalidation

Save calls `_projector.Invalidate(_commerce.Current)` then re-reads. No `MemoryCache.Clear`, no `Thread.Sleep`, no TTL wait, no Host/FE restart required for the new value.

Runtime (Host recycled once so the new Admin endpoint existed; that recycle is not the apply mechanism):

| Step | Result |
| --- | --- |
| PUT forest-green | 200, `paletteKey=forest-green` |
| GET `/v1/storefront/appearance` immediately | `forest-green`, tokens `21 128 61` |
| first HTML `/fa` immediately | `data-storefront-palette="forest-green"` `--color-primary:21 128 61` |
| PUT tooba-blue | 200 |
| GET storefront + `/fa` immediately | `tooba-blue` / `--color-primary:37 99 235` |

Host tests: `Save_invalidates_only_changed_store_cache` — Store B cache object remains `tooba-blue` while A becomes `amber-gold`.
