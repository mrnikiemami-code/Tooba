# TB-P08-T013 — Tag migration

- Migration `20260904070000_AddContentTags` creates `content.tags` + `content.article_tags`.
- SQL backfill: split TagsCsv, trim/dedupe by normalized name per Language, assign ArticleTag.
- TagsCsv remains compatibility projection synced on assign/remove; not canonical edit storage.
- Public article tags still rendered as string list from normalized relationships (CSV fallback).
