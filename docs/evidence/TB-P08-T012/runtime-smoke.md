# TB-P08-T012 — Runtime smoke

## Status

**PASS (focused API + page smoke after Host restart on T012 bits).**

## Environment

- Host restarted after push; `GET /health` → 200
- Languages registry was empty on this Postgres volume → seeded fa-IR + en-US via Admin Languages API (active/default)
- FE: Next.js available (`/admin/content` 200 on `:3000` and `:3002`)

## Checks performed

| Check | Result |
|-------|--------|
| Create Draft fa-IR (no author) | 201 / id returned |
| Update fa body + title | 200 |
| Create Draft en-US (no author) | 201 |
| Update en body + title | 200 |
| GET Article media workspace | 200 |
| FE Article EDIT page (`/admin/content/articles/{id}?mode=edit`) | 200 (`:3002`) |
| FE Article list | 200 |

## Not fully browser-exercised in this pass

DAM insert click-path, Featured/SEO picker UI, Category picker UI — covered by code fixes + focused source-assert tests; live Media Library click path deferred to Architect visual review (`USER_VISUAL_ACCEPTED=NO`).

## Notes

Earlier smoke failed with `content.create.rejected` / detail `localization.language.inactive` because Language table was empty. Create endpoint now also promotes known domain codes (same resolver as Update) so inactive language surfaces as `localization.language.inactive` for FE mapping.
