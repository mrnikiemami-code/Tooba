# Performance

- Sections loaded in one query per page.
- ProductCollection uses one bounded product query (Take ≤ 24), not per-product theme queries.
- Cache key unchanged; invalidate on section write.
- Max 40 sections; config size capped.
