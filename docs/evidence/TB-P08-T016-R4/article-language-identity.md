# Article language identity

- Existing articles: `draftLocale` starts empty; `applyArticle` always sets locale from `data.locale`.
- Language select includes current article locale even if inactive in registry.
- Badge/header show Article locale identity, not Admin UI locale.
- `searchParams.language` does not overwrite existing article locale.
- Admin shell may remain Persian; Article identity is persisted locale (`en-US`, `fa-IR`, …).
