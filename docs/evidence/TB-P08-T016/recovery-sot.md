# Recovery SoT (TB-P08-T016)

Phase: P08 — Content / i18n Foundation

- Last Architect-accepted: **TB-P08-T015**
- Last Implementation: **TB-P08-T016**
- Current Issued: **(none)**
- Current Repair: **(none)**
- USER_VISUAL_ACCEPTED: **NO**
- Worker Next State: **IDLE**
- Do **NOT** invent TB-P08-T017

Evidence: `docs/evidence/TB-P08-T016/`

## Focused validation (worker)

- FE Content suite: **58 pass / 0 fail**
- BE `FullyQualifiedName~Content`: **36 pass / 0 fail**
- Recovery guard: run after SoT update

## Repairs in this gate

1. FE `content-article-crud.test.ts` — assert `openPublishDialog` + publish/unpublish/republish kinds (T014 dialog model)
2. FE `content-article-media.test.ts` — SEO label matches T015 **تصویر اشتراک‌گذاری** (not longer OpenGraph-style phrase)
3. BE `ContentArticleEditorTests` — supply body before Publish so readiness gate can pass
