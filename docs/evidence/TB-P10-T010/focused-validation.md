# Focused validation

- Host `StoreLandingPageTests`: 6/6 (reserved, duplicate slug, draft vs published, home eligibility/clear, reserved public miss, two-catalog isolation). Output `.tmp-t010-test-out`.
- FE: reserved-slugs, landing guard, routing public/excluded + `/summer-sale`. 12/12 with recovery guard.
- Recovery staleness: Architect TB-P10-T009-R2, Impl TB-P10-T010, do NOT invent T011.
- `git diff --check` clean on task-owned files.

No matrix suites. Admin UI deferred (API + tests).
