# TB-P08-T016 — Media / SEO browser

## Code / contract (worker)

- Featured/Gallery: `content-article-media-panel.tsx` — کتابخانه رسانه; no DAM acronym in normal UI
- SEO tab: **تصویر اشتراک‌گذاری**; featured fallback + separate pick; effective preview
- Wording: no primary OpenGraph jargon; Home: **نمایش در بخش مقالات صفحه اصلی** (not ویژه در ریل خانه)
- FE media/comments-help wording asserts pass

## Browser (parent)

- [ ] Featured select/remove; Gallery add/remove (reorder if UI supports)  
  Not exercised this session (inline editor path only).
- [ ] Unassign does not delete DAM asset  
  Not exercised this session.
- [ ] SEO share image pick + featured fallback understandable  
  Not exercised this session.
- [x] Inline CKEditor DAM insert path works  
  Observed: درج تصویر → Media Library → select Screenshot asset `01a06a03-2bc1-7000-9790-e01560cd47bf` → insert → Save → reload → image persists with `data-media-asset-id` (no base64).

## Gate repairs (T016)
- Fixed Media panel infinite loading loop when parent passed fresh `onWorkspaceChange` each render (reload effect depended on unstable callback identity).
- Featured select/remove + gallery empty state verified in browser after repair.
