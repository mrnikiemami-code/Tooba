PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
TaskId: TB-P08-T010-R1
WorkerId: worker-01
AgentType: cursor
HEAD: (after commit/push origin/main)
USER_VISUAL_ACCEPTED=NO

## Summary
Repaired T010 blockers: scoped Content development seed + EF migrate, public Content APIs no longer 500, locale 308 self-loop fixed, demo fa/en articles available, required HTTP smoke 200, T010 visual copy preserved, SoT corrected to Last Architect-accepted TB-P08-T009-R2 / Current repair TB-P08-T010-R1. Committed and pushed. Worker IDLE. No T011.

## Preserved-T010-Visual-Fixes
Article loading «در حال بارگذاری…»; media «انتخاب از کتابخانه» / «کتابخانه رسانه» / متن جایگزین / توضیح تصویر; language banner Persian; author ?mode=edit; article list error map; public blog RTL/LTR chevrons/arrows and padding.

## Seed-Scope-Repair
ContentDevelopmentSeedHost CreateAsyncScope + CommerceContext store-alpha, then ContentDevelopmentSeed.ApplyAsync(scoped). Program no longer resolves ContentDbContext from root. Seed wrote 23 entities. Host.Tests: source guard + idempotency Testcontainers (2 passed).

## Public-API-500-Root-Cause
Missing Content (and Localization) schema/demo while Catalog legacy bootstraps skipped; scoped seed never ran. Not a query catch-empty. After migrate+seed: GET /v1/content/articles?locale=fa-IR 200 totalCount=4; en-US 200 totalCount=4; categories 200; authors 200.

## Migration-Readiness
EF MigrateAsync Localization+Content+Media on tenant connection. Localization added to ModuleMigrationRegistry before Content. No destructive reset.

## Locale-Loop-Repair
planLocaleMiddleware: prefixed rewrite; rewrite follow-up with x-tooba-locale passes without 308. Tests in middleware-locale.test.ts. HTTP 200 on /fa /en /fa/blogs /en/blogs and article slugs.

## Demo-Content
fa Published: guide-online-shopping articleId 01a03f17-3720-7000-94ab-ffa91a1ac02a
en Published: guide-online-shopping articleId 01a062aa-b346-7000-8a21-16b4336c3c7b
Authors include tooba-editorial; en category slug guides. Covers use existing demo DAM ids d0d0d0d0-0001…0004.

## Runtime-Health
Host http://127.0.0.1:5088 /health 200 (restarted for seed). FE :3000 left running. Shopeiva :3001 down (non-blocking). Postgres :5432 existing listener.

## Admin-Smoke
200: /admin/content, /admin/content/articles/new, VIEW /admin/content/articles/01a03f17-3720-7000-94ab-ffa91a1ac02a, EDIT ?mode=edit, /admin/content/categories, /admin/content/authors, /admin/languages.

## Public-Smoke
200: /fa /en /fa/blogs /en/blogs /fa/blogs/guide-online-shopping /en/blogs/guide-online-shopping /fa/blogs/author/tooba-editorial /en/blogs/category/guides. Public APIs 200 with items.

## Visual-Gate
No browser MCP. HTTP shells only. USER_VISUAL_ACCEPTED=NO.

## Review-Routes
http://127.0.0.1:3000/admin/content
http://127.0.0.1:3000/admin/content/articles/new
http://127.0.0.1:3000/admin/content/articles/01a03f17-3720-7000-94ab-ffa91a1ac02a
http://127.0.0.1:3000/admin/content/articles/01a03f17-3720-7000-94ab-ffa91a1ac02a?mode=edit
http://127.0.0.1:3000/admin/content/categories
http://127.0.0.1:3000/admin/content/authors
http://127.0.0.1:3000/admin/languages
http://127.0.0.1:3000/fa/blogs/guide-online-shopping
http://127.0.0.1:3000/en/blogs/guide-online-shopping
http://127.0.0.1:3000/fa/blogs/author/tooba-editorial
http://127.0.0.1:3000/en/blogs/category/guides

## Focused-Validation
Host compile + ContentDevelopmentSeedHostSourceTests + ContentDevelopmentSeedIdempotencyTests pass. test:content, test:i18n, article/author/language visual tests pass. recovery guard pass. git diff --check clean (CRLF warnings only).

## Recovery-SoT
Last Architect-accepted: TB-P08-T009-R2
Last Implementation: TB-P08-T010-R1
Current Issued: (none)
Current Repair: TB-P08-T010-R1
USER_VISUAL_ACCEPTED=NO
Worker IDLE — no T011
docs/evidence/TB-P08-T010/r1-*.md

## Git
Commit+push origin/main required by task; working tree expected clean after.

## Architectural-Concerns
Article VIEW import path and blogs author/category page imports were wrong relative paths (compile 500 until fixed). isSelfOrDescendant re-exported from design-system barrel.

## Visual-Concerns
No interactive browser pass; human visual acceptance still NO.

## Blockers
None for stated PASS criteria after repair. Shopeiva optional/down.
