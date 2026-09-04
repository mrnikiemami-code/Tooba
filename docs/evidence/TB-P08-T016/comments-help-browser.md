# TB-P08-T016 — Comments / Help / Wording

## Code / contract (worker)

- Comments tab: Pending/Approved/Rejected/Hidden filter + moderation actions (no hard-delete moderation)
- Contextual `ContentHelpAffordance` + `/admin/content/help` topics coverage
- Home wording: نمایش در بخش مقالات صفحه اصلی; no ویژه در ریل خانه; no DAM in normal UI
- FE comments-help tests pass; BE ContentArticleCommentModerationTests pass

## Browser (parent)

- [ ] Status filter + moderation persists across reload  
  Not exercised this session.
- [ ] Empty state polished  
  Not exercised this session.
- [ ] Help page loads all required topics  
  Not exercised this session.
- [ ] Contextual Help near confusing fields  
  Not exercised this session.

## Gate repairs (T016)
- Fixed comments API client missing `X-Tooba-Dev-Actor-User-Id` (showed «دسترسی مجاز نیست» while Host returned 200 with actor).
- API create+approve comment smoke: PASS after repair.
