# TB-P08-T001-R2 — Backend Immutability

- `UpdateLanguageCommand` carries optional `Code` / `UrlPrefix`.
- Referenced language: throws `localization.language.code.in_use` / `localization.language.url_prefix.in_use`.
- Unreferenced: `Language.UpdateIdentityFields` with duplicate checks.
- Safe fields (display/native/direction/culture/calendar/active/default/sort) still editable when referenced.
