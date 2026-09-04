# TB-P08-T012 — Category save root cause

## Root cause

1. Host `AdminUpdateAsync` mapped most `InvalidOperationException`s to **`errorCode: content.update.rejected`**, putting the real code (`localization.language.inactive`, `content.category.language_mismatch`) only in `detail`.
2. FE `parseAdminProblemErrorCode` read only `errorCode` → UI showed generic / unmapped rejection.
3. Changing Article Language reloaded the category tree but **did not clear** `draftCategoryId`, so a prior-language category id could still be submitted → `content.category.language_mismatch`.

## Repair

- Host promotes known domain codes to `errorCode` (`ResolveArticleUpdateErrorCode`).
- FE also unwraps `detail` when errorCode is generic `content.update.rejected`.
- Locale change clears category draft; Save pre-checks active language + category language match.
- Mapped Persian/English messages in `admin-error-map.ts`.
