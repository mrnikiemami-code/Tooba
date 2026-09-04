# Browser validation (TB-P08-T016-R5)

## Attempt
- cursor-ide-browser: tab/navigate unavailable in this worker session (no usable tab after create/list).
- HTTP smoke instead:
  - `GET http://127.0.0.1:3000/admin/content/articles/01a06ac1-135f-7000-b45f-7089ee3d1add?mode=edit` → **200** (EN article shell; markers present in HTML).
  - Host `/health` → **200**.
  - Admin article API without auth cookie → 401 (expected without prepared actor session).

## Covered by unit/source-assert
- Back link language query, category picker locale (en≠hardcoded fa), history totalCount label, fonts `shouldNotGroupWhenFull`, video unwrap, category tree polish.

USER_VISUAL_ACCEPTED=NO
