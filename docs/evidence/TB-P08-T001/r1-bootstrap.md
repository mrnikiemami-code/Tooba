# TB-P08-T001-R1 — Bootstrap

- `LanguageDirectory.BootstrapAsync` seeds fa-IR (RTL/Jalali/default) + en-US (LTR/Gregorian) when table empty.
- Idempotent: second call is no-op (does not overwrite admin edits).
- `LanguageBootstrapHostedService` runs on Host startup after migrations.
