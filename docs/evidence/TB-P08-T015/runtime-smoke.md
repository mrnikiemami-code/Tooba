# Runtime smoke (TB-P08-T015)

## Automated

- FE source contracts: `content-article-comments-help.test.ts`, `content-article-admin-screen.test.ts`, `content-article-publication.test.ts` — **pass**
- Recovery guard: `docs/ai/recovery-staleness.guard.test.mjs` — **pass**
- BE: `ContentArticleCommentModerationTests` — **2 passed** (domain transitions + directory paging/moderation with Testcontainers)
- `git diff --check` — clean (CRLF warnings only)

## Manual / API smoke checklist

1. Open Article workspace → Comments tab loads
2. Create Pending via admin seed → Approve; create another → Reject/Hide; reload persists
3. Help `?` opens; `/admin/content/help` loads topics
4. SEO/Media/Home wording visible
5. Save / Preview / Publish still functional
6. CKEditor / readiness / history / category / tags preserved

## Notes

Host process was briefly stopped to unlock DLLs for focused compile/test; domain+directory smoke covered by automated tests. Full browser UI smoke may be re-checked after Host restart by parent.
