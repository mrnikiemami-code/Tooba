# TB-P08-T001-R1 — Language Invariants

- Unique `code` and `url_prefix` on create.
- Exactly one active default (`SaveWithInvariantChecksAsync`).
- Default must stay active (`Language.SetDefault`, `UpdateMutableFields`).
- Cannot deactivate default without replacement (Patch/Update throws `default_must_be_active`).
- Stable error codes in `LanguageErrorCodes` (e.g. `localization.language.inactive`).
- `ContentLanguageReferenceGuard` blocks code/urlPrefix mutation once referenced by `ContentArticle.Locale`.
