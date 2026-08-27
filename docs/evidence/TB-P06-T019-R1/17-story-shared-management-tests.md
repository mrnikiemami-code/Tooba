# 17 — Shared Story management tests (TB-P06-T019-R1)

## Frontend unit

- `app/stories/management/story-capabilities.test.ts` — seller forbids review/publish; admin allows; submit gates
- `app/stories/story-api.test.ts` — origin/review/ownership mapping
- `app/vendor-panel/panel-nav-integrity.test.ts` — live `استوری‌ها` → `/vendor-panel/stories`

## Backend

- `StoryFoundationTests` — public eligibility, seller isolation, submit/approve/reject, no seller enable/approve routes

## Commands

```text
npm run test:stories → 6 pass
dotnet test --filter FullyQualifiedName~StoryFoundation → 3 pass
```
