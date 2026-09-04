# TB-P08-T013 — Runtime smoke

Attempted after Host rebuild (prior Host process was stopped for compile).

**Automated (authoritative for this Worker pass):**
- Backend focused: ContentCategoryTreeRulesTests + ContentTaxonomyTagsTests + ContentCategoryDirectoryTests — 9/9 passed (Docker Testcontainers).
- Covers: L1/L2 create, reject L3, reject deep move, cross-language parent, Article L1/L2 assign, language mismatch, tag create/search/dedupe/assign/idempotent/remove, tag language mismatch.

**Browser/manual UI smoke:** not fully re-run in this Worker session after Host stop; Host must be restarted and migration applied on the local DB before interactive FA/EN smoke. Documented honestly — no false PASS claimed for live UI clicks.

Recommended operator smoke after Host up + migrate:
- fa: main+sub category, no L3 add, Draft article L1 then L2, tags create/search/assign/remove, reload.
- en: picker language isolation.
