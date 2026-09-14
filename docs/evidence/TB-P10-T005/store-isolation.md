# TB-P10-T005 — Store isolation

`StoreAppearanceFoundationTests.Store_A_cache_cannot_be_read_as_Store_B` proves distinct `StoreScope` keys (`tenant:store-a` vs `tenant:store-b`) and that B is not served from A's cache entry.

Authoritative store resolution remains domain/tenant middleware. Appearance never uses a process-global mutable theme object.

Same contract for Marketplace (`marketplace:{connection}`) and Single-store (`tenant:{id}`).
