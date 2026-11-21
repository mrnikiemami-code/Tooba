# TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R19

Finish audited Story workflow, operator UI, recovery and public playback in one delivery.

`channelId: tooba-main` · Bridge row `80faea2c-6881-40fa-a0d8-4bd3a4439179` · `Expected-Base`
`d3de64c2c6692b551c8193e842bdd04fe99ceded` · single worker, one task, no delegation, no historical subagent resume.

> Provenance note: the R19 envelope reached the worker through the `BRIDGE-WAKE` claim (row id and timestamps are
> recorded in `claimed.json`). No repo-tracked copy of the envelope existed at claim time, so this file is the
> byte-accurate restatement of the issued scope below; `issuance-receipt.json` records its SHA-256 and the authority.

## Fixed scope (A–G) — close all of them in this one delivery

**A. Route binding must name the owning command.**
- Admin stop must bind `POST /v1/admin/stories/{id}/disable` (`AdminDisableAsync`).
- Seller stop must bind `POST /v1/seller/stories/{id}/stop` (`SellerStopAsync`).
- Seller media upload must bind the real `POST /v1/seller/media` (the module maps the group root; no `/upload-story`
  suffix exists).
- Appending a foreign suffix, or using the other role's route, stays a client-side `route_binding` refusal.

**B. A 2xx is not success by itself.**
- A reply must show the effect its frozen action owns (approve ⇒ `reviewStatus=Approved`, publish ⇒ public/`Active`,
  schedule ⇒ `Scheduled`, stop ⇒ `Disabled`, return-to-draft ⇒ `Draft`, reject ⇒ `Rejected`, submit ⇒ `Submitted`).
- An unchanged-Draft snapshot returned to `approve`/publish is `story_state_unchanged`, never completion.
- Replies that carry no state fields keep the existing identity binding (partial-but-truthful stays valid).
- A non-platform 4xx (HTML/proxy/gateway) is NOT an authoritative no-effect refusal: it stays uncertain and the frozen
  intent is retained.

**C. Honest publication state.**
- `Approved` + not effectively public must read «تأییدشده، منتشر نشده» / "Approved, not published"; it must never read
  «در ویترین» nor «پیش‌نویس».

**D. Lawful seller return-to-draft.**
- `Approved` + `Draft` (never published) and `Approved` + `Disabled` (stopped) must offer and execute
  `POST /v1/seller/stories/{id}/return-to-draft`; the action code is only offered when the owning domain method accepts
  that state, and the seller still cannot `update`/`submit` until the story is a real editable draft.

**E. DB-native grid filters/sorts.**
- `StoryAdminGridPolicies` must admit `shopDisplayName`, `shopName`, `startAt`, `endAt`, `updatedAt` with their lawful
  operators, and `updatedAt` must be sortable.
- `AdminStoryGridQueryEngine` must translate nullable `StartAt`/`EndAt` day filters to SQL (no in-memory evaluation) and
  sort by `updatedAt`.

**F. Public player truthfulness.**
- Video detection belongs to the SLIDE, not the story: a JPEG cover of a story that contains a video slide must dwell as
  a photo instead of waiting for `ended`.
- A photo's dwell timer starts only after the image actually loaded (`onLoad`), so a slow/failed load cannot consume the
  slide's time.
- Caption renders; the overlay exposes dialog semantics (`role="dialog"`, `aria-modal`, accessible name) and restores
  focus/body scroll on close.

**G. Explicit slide media replacement with preservation.**
- Editing a slide may offer a deliberate file replacement. The existing media must survive until the new file uploads AND
  the owning `update_item` succeeds; a caption/CTA-only edit must upload nothing and keep the media byte-for-byte.
- The replacement media type must come from the upload receipt, never from the old slide.

## Out of scope / forbidden
- No migration, DDL, seed, backfill, reset, restore, config or grant change.
- No live Story/Media business write by the worker (no create/update/approve/publish/delete POST against Host).
- No parallel seller panel, menu or login: the seller surface stays inside the existing `VendorShell`
  (`/vendor-panel/stories`, `story.view`).
- Preserve R16 typing/selection behaviour, R17 toast presentation, R18 approval truthfulness, durable executor epochs,
  VendorShell/i18n/date layout, Returns/payout/user work, and the known `Html404` production-build blocker.

## Acceptance expectations
- Owning-method proof, mounted UI proof and served chunk proof for every repair above.
- Focused tests only; no broad suite loop, no screenshots, no delegation.
- Truthful PASS/PARTIAL; stop for Architect and owner review. `automaticNextImplementationTask: NONE`.
