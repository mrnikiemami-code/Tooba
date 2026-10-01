# Provider parity

MemoryToobaCache and DisabledToobaCache both reject blank tag/namespace.
Both call CachePolicy.EnsureBounded on Set/GetOrCreate.
None no-ops storage/invalidation only after validation.
