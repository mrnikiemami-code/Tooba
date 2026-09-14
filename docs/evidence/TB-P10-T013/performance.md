# Performance

One public projection per menu, cached 2 minutes. Header and menu writes invalidate `store-menu:{scope}:{id}` and `store-header-menu:{scope}` only. No polling. No per-item fetch. Depth bounded at 3.
