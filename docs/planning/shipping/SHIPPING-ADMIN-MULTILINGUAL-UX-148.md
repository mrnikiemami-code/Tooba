# SHIPPING-ADMIN-MULTILINGUAL-UX-148 — «عمومی» compact + true multilingual method-name editing

Task: `TB-SHIPPING-ADMIN-MULTILINGUAL-UX-148` (BOUNDED_REAL_ADMIN_GENERAL_UX_MULTILINGUAL_PERSISTENCE)
Base: `bcac6610ebd1edc7c7c5c246f1d2ac21b669c075`
Verdict: **PARTIAL** — real UI + authoritative languages + method-scoped save seam delivered; one
isolated successful save/reopen round-trip is explicitly UNVERIFIED (see «Unverified»).

## What was wrong before this task

The `پیشتاز` (`post:express`) General tab repeated identity/code/provider blocks in large stacked
sections, used one oversized warning card, forced heavy scrolling, and only ever edited the name of
the **active** language. Financial status and `enabled` were not distinguishable, so an enabled
method looked usable even though no monetary policy exists.

## What was implemented (code, not documentation)

1. `shipping-method-language-model.ts` (new, pure):
   - `methodLanguageDefinitions` — editable languages come **only** from the authoritative registry;
     rows without a real `languageId` are dropped (the save seam needs that GUID); label = server
     `nativeName` → `displayName` → `code` → id; server order (`sortOrder`, then input order) kept;
     no `fa`/`en`/`ru` literal exists in this file.
   - `methodTranslationStates` / `methodTranslationSummary` — per-language real name + explicit
     `translated` flag and «x از y» completeness; a missing translation stays an honest gap.
   - `methodNameDrafts` / `setMethodNameDraft` / `methodNameDraftFor` — drafts are kept per language,
     so switching the selected language before saving loses nothing.
   - `methodNameDraftIsDirty` / `dirtyMethodLanguages` / `serverMethodNameFor` — dirtiness is measured
     against the real server value; pure whitespace is not a change.

2. `flat-method-language-editor.tsx` (new): one editor bound to real languages.
   - Explicit loading / denied / error (with retry) / empty-registry states; **no** language is created
     client-side and no translation is fabricated.
   - A clickable per-language list showing `ثبتشده` / `بدون ترجمه` / `پیشنویس ذخیرهنشده`, plus a
     language `Select`, a per-language name input whose `dir` follows the language direction, the real
     server value line, an unsaved-draft counter, «بازگردانی» and «ذخیرهٔ نام این زبان».
   - Save goes only through the canonical seam
     `PUT /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/translations/{languageId}`
     (`saveOneLevelMethodName`, unchanged) and accepts the server answer **only** if the returned
     method+language equals the requested target (generation guard), so a stale response can never land
     on another method or language. Name limits/blank input use the existing validator (fail-closed).

3. `flat-general-tab.tsx` (rewritten, compact):
   - Single compact header: localized name, immutable canonical code, badges for
     «فعال/غیرفعال» and «وضعیت مالی: تنظیم نشده».
   - Provider identity → collapsed `<details>` «فرادادهٔ مشترک ارائهدهنده (خارج از دامنهٔ این روش)»
     with the authoritative `serviceCode`/`optionCode` and an explicit shared-metadata note.
   - New «وضعیت مالی مشتری» section states, verbatim, that the financial status is **not readable from
     the current API**, that missing price ≠ free, and that enabled ≠ quote-ready. No amount, no
     financial mode, no «آمادهٔ استعلام» claim and no zeros are produced. The valid locked financial
     modes are named only as *requiring an authorized server endpoint*.
   - Activation block keeps exactly the previous authorized behaviour (same test ids, read-only in
     view mode, confirm, server re-read).
   - Name save refreshes in place (`onSavedInPlace`) so multilingual work is not interrupted; activation
     keeps the close-after-save behaviour (`onSavedAndClose`).

4. `flat-method-detail-dialog.tsx`: loads the authoritative languages once per dialog
   (`loadAdminLanguages` from the existing `features/admin-languages` boundary), reads name/status from
   the **fresh** canonical method row (so anything saved shows without reopening), and the duplicated
   identity box was reduced to one compact line. The four non-nested tabs, the bindings and the three
   specialized tab components are untouched.

5. `flat-screen.tsx`: passes the fresh `method` row of the selected code to the dialog and adds the
   in-place refresh callback. Row list, three row actions, gate/auth behaviour from TB-147 unchanged.

## Verification

- Focused suite `app/admin/shipping-services/**`: **220/220 pass** (`focused-tests-148.txt`), including
  the new `flat-method-language.test.ts` cases for registry-only languages, explicit missing
  translation, lossless switching, per-method save identity, dirty detection and view/edit read-only.
- `npm run typecheck`: clean (`typecheck-148.txt`).
- Architecture guards: 10/11; the single failure is the pre-existing FE-SIZE baseline listing the same
  five `OVERSIZED_GROWTH` files reported in TB-146/147 — none of them belongs to this task
  (`architecture-guards-148.txt`).
- Full frontend suite: 1177/1196, with the same 19 unrelated pre-existing failures (landing-pages
  builder, storefront perf/color guards, FE-SIZE); no failing test references shipping-services.
- Live browser (READ-ONLY, ordinary Development Admin): real registry languages
  `فارسی / English / Русский`, «ترجمهشده: 2 از 3», Русский shown as «بدون ترجمه», per-language draft
  preserved across switches, all four tabs still present, no «انواع سرویس» anywhere
  (`browser-evidence-148.txt`, `01-general-multilingual.png`).
- Save seam fail-closed proof (rejected writes only, no mutation): unknown method code ⇒
  `shipping_method.code.invalid`; unknown language ⇒ `shipping_service.language_invalid`; catalog re-read
  shows `post:express` fa name still `پیشتاز`.

## Explicitly out of scope / untouched

Server financial policy, create-method endpoint, Checkout/Fulfillment/Settlement, PERF-CRIT-001,
storefront visuals, other detail tabs, the TB-147 auth fix, unrelated dirty
`admin-fulfillment-work-queue-screen.tsx` and the TB-075/079 evidence logs.

## Unverified (why this is PARTIAL, not PASS)

A **successful** name save followed by GET/reopen was not executed. The only reachable environments
(shared `:3000`/`:5088` and `store-alpha`/`tooba_alpha`) are READ-ONLY for this task, and no isolated
disposable authorized scratch environment was available inside the 45-minute timebox. Everything else
required by the PASS gate is proven: real multilingual UI backed by the authoritative language
registry, the existing method-scoped save API wired only to the selected method+language, an actually
improved browser-rendered General tab, focused tests, clean typecheck and committed source.

## Next (architect decision, not started)

1. Run the same dialog against an isolated disposable authorized scratch Host+DB to prove
   save → GET/reopen per language, per-method isolation and stale/error behaviour (completes E of the
   recovery backlog).
2. Only after that: backend financial mode/readiness endpoint (C) and real create-method API (D).
3. Shipping storefront per-group schedule (B) remains release-critical and must not be starved by
   further Admin polish.
