# 13 — Frontend tests proof (TB-P06-T017)

## Tests

| File | Asserts |
|---|---|
| `app/stories/story-api.test.ts` | `mapPublicStory` PascalCase payload; `mapAdminStory` numeric status → `Active` |
| `app/storefront/home-structure.guard.test.ts` | Home has `home-stories` marker path; `stories` → `<HomeStoriesSection`; **no** `STORY_IMAGES` in home source; stories source includes live marker |

## Guard excerpt intent

```text
home stories use live Host binding without fake STORY_IMAGES
```

## Command (Worker fills exact pass counts if empty)

```text
node --test app/stories/story-api.test.ts app/storefront/home-structure.guard.test.ts
```

(from `src/frontend`; or project’s usual frontend test script)

See `17-final-validation.md` / final runtime if exact numbers pending.
