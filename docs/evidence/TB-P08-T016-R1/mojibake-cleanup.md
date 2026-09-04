# TB-P08-T016-R1 — Mojibake / `????` cleanup

## Root cause

Official Content seed (`ContentDevelopmentSeed`) and Language bootstrap store clean UTF-8 Persian labels. Stale smoke/test rows in the current development DB had corrupted display names (`?` / mojibake). Seed previously created missing rows only and did **not** rewrite existing bad names.

## Durable fix (source)

`ContentDevelopmentSeed.SanitizeCorruptedLabelsAsync` runs on every Apply:

- Categories with mojibake names → rename to `Archived seed N` + `ContentCategoryStatus.Archived`
- Authors with mojibake display names → rename to `Sample author N` + `Deactivate`
- Articles with mojibake title/excerpt/`authorDisplayName` → replace with clean FA/EN placeholders (keeps body/locale)
- Unused Tags with mojibake names → removed

Detection: ≥2 `?`/`？`/`U+FFFD` and question-mark ratio ≥ 30% of trimmed length.

## Current DB repair

Host restart applied sanitizer. API probe (actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`):

- FA/EN category trees: **0** names containing `?`
- Authors picker (active+inactive): **0** `?`
- Languages: **0** `?`
- Tags FA: **0** `?`
- Articles pageSize 100: **0** title/excerpt/authorDisplayName with `?`
- Article `01a06a9a-570c-7000-b7d7-6bd1bbb267f2` (`Ready b39688`): `authorDisplayName` → `نویسنده نمونه`

## Visual

- `articles-admin.png` — list shows clean Persian/English; sample author on Ready row
- `categories-admin.png` — active FA categories + archived seed labels; no `????`
- `authors-admin.png` — author grid without mojibake

## Scope notes

- No CKEditor redesign; no feature expansion
- `USER_VISUAL_ACCEPTED=NO`
- FE hide-only was **not** used as the primary fix; persisted/seed path repaired
