# TB-P10-T006 — Store isolation

Local runtime has one configured store (`tenant:store-alpha`). Second scope is covered by Host tests.

`StoreAppearanceAdminTests.Save_invalidates_only_changed_store_cache`:

- Store A catalog saved to `amber-gold`
- Store B cached projection remains `tooba-blue` (same cache instance, same object after save)
- A read is `amber-gold`; B read is `tooba-blue`

`StoreAppearanceFoundationTests.Store_A_cache_cannot_be_read_as_Store_B` still asserts distinct `StoreScope` keys.

Disabled/other host must not receive alpha palette (T005-R1: `Host: disabled.localhost` → 404).
