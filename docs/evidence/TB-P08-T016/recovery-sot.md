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
- Recovery guard: **3 pass / 0 fail**

## Repairs in this gate

1. FE `content-article-crud.test.ts` — assert `openPublishDialog` + publish/unpublish/republish kinds (T014 dialog model)
2. FE `content-article-media.test.ts` — SEO label matches T015 **تصویر اشتراک‌گذاری** (not longer OpenGraph-style phrase)
3. BE `ContentArticleEditorTests` — supply body before Publish so readiness gate can pass
4. FE language label fallback for corrupted / `?-only` `nativeName` in `content-list.tsx`, `content-article-admin-screen.tsx`, `content-article-new-screen.tsx` (gate session; may need commit beyond `41bc5036`)

## Runtime / commit notes

- Host `:5088` + FE `:3000` healthy (also saw `:3002`); **keep alive after result**
- Prior repair commit: `41bc5036` `fix(content): TB-P08-T016 final gate repairs` (ahead of origin)

## SoT docs

- `docs/PROJECT-STATE.md` — Last Architect-accepted T015; Last Implementation T016; Issued/Repair none; USER_VISUAL_ACCEPTED=NO (already aligned)
- `docs/ai/TOOBA-RECOVERY-CONTEXT.md` — same pattern (already aligned)
