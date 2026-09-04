# TB-P08-T013 — Category current state

- Content-owned `content.categories` with ParentCategoryId, LanguageCode, Status.
- Prior tree rules allowed unlimited depth (cycle/language only).
- Seed/demo inspection via focused tests + migrate: no Content Category rows deeper than Level 2 observed; enforcement applied directly without reparent/delete.
- Public category slug routes unchanged.
