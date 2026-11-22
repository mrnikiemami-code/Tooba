# ARCHITECTURE-PLAN — TB-STORY-SLIDE-PRODUCT-PICKER-MOBILE-UX-282-R23

Protocol: TOOBA-DELIVERY-001
Base-SHA: ec9fb9d6574167622948f2f9dfd0245849f8fdf0
Task-SHA256: b80fcaeba6c84102a3c6cac0866daf422fbdb4e6f818ee529491168a114f3a31
Prepared-before-production-edit: source review and OpenSpec artifacts written before the first production edit (2026-11-22, local runtime)
OpenSpec: `openspec/changes/story-slide-product-picker-mobile-282-r23/`
Design: `openspec/changes/story-slide-product-picker-mobile-282-r23/design.md`

## Outcome and fixed requirements

| REQ | Actor / entry | Trigger / expected effect | Owner and source | Invariants | Acceptance scenarios |
|---|---|---|---|---|---|
| REQ-01 | Admin operator, `/fa/admin/stories/{storyId}` → edit slide | Select product B and Save; reload/GET shows B | Existing `StoryManagementScreen` update-item envelope; `PUT /v1/admin/stories/{storyId}/items/{itemId}` | One PUT; expectedVersion; occurrence; media asset/url retained; zero upload/add/delete | S-01, S-02, S-03 |
| REQ-02 | Admin operator | Select B then Cancel/Escape/discard; reopen shows A | Existing dialog close/discard path; no command | Zero mutation; A retained | S-04, S-05 |
| REQ-03 | Admin operator | Product lookup 503; error+retry; current A remains | `loadStoryProductOptions`; product epoch | Error is not empty; retry does not select/save | S-06 |
| REQ-04 | Admin operator at 360×640 / 360×800 | A has long Persian name, outside first page; search/paging keeps A; Save reachable | Existing picker/list/dialog | No horizontal document overflow; normal scroll; no overlay; selection preserved | S-07, S-08 |
| REQ-05 | Admin at 1440×900; seller route; fa/en | Same shared picker, seller fixed shop, RTL/LTR and labels | Existing VendorShell route and scope | Seller request never contains another shopId | S-09, S-10, S-11 |

## Architecture and real route

`StoryManagementScreen` (admin or VendorShell seller) → existing slide `Dialog` → product search state (`productSearch`) and result list (`loadStoryProductOptions`) → `productOptions` mapped to `StoryProductOptionRow` → selection state `slideCtaTarget` + `selectedProductLabel` → existing durable `update_item` envelope built from the selected slide and detail version → `PUT /v1/{admin|seller}/stories/{storyId}/items/{itemId}` → owning Story command validates version/occurrence/media/CTA → response mapped by `mapAdminStory`/`verifyStorySuccessPayload` → `reloadToken` refresh → independent GET/reload.

Presentation-only addition: selected card and option markers. No new endpoint, mapper, command, permission, schema or translation authority.

## Contract and lifecycle

- Product lookup remains `GET /v1/{admin|seller}/stories/product-options` with `search/page/pageSize` and seller/admin shop scope.
- Save remains the existing `update_item` PUT body: `mediaType`, `mediaAssetId`, `mediaUrl`, `caption`, `durationMs`, `ctaType`, `ctaTarget`, `expectedVersion`, `occurrenceKey`.
- `selectedProductLabel` is display state; the authoritative target is `slideCtaTarget`.
- A changed selection marks the existing slide form dirty; Cancel/Escape uses the existing discard dialog.
- Error/loading must not clear the selected target or label.
- The seller picker keeps `sellerShopId` and never accepts a query/body shop override from the client.

## Scenario matrix

| ID | Requirement | Entry/role/provider | Preconditions and action | Expected result + side effects | Proof level | Before-fix observation |
|---|---|---|---|---|---|---|
| S-01 | REQ-01 | Browser, admin, controlled HTTP | Open slide A, choose B, Save | One PUT with B/version/occurrence; media retained; no upload/add/delete | Browser transport | Selected option has no marker; selection sentence is below list |
| S-02 | REQ-01 | Browser, admin, controlled HTTP | Reload after S-01 | GET/reload shows B | Browser independent reread | No browser save proof existed |
| S-03 | REQ-01 | Browser, admin, controlled HTTP | Inspect PUT capture | Correct route/body; media identity retained | Browser transport | Current presentation gives no explicit confirmation |
| S-04 | REQ-02 | Browser, admin, controlled HTTP | Choose B then Cancel/discard | Zero mutation | Browser transport | No connected cancel proof existed |
| S-05 | REQ-02 | Browser, admin, controlled HTTP | Reopen after cancel; Escape dirty path | A again; Escape opens same dirty exit | Browser UI | No connected Escape proof existed |
| S-06 | REQ-03 | Browser, admin, controlled HTTP | First product call 503, retry | Error+retry, no empty, A preserved, no mutation | Browser transport/UI | Error/empty distinction exists only in mounted helper |
| S-07 | REQ-04 | Browser, admin, 360×640/800 | Long A outside page, search/paging | A retained, no horizontal overflow, Save reachable | Browser geometry | Selected row not marked; long-name mobile not proven |
| S-08 | REQ-04 | Browser, admin, 360 | `boundingBox` / `elementFromPoint` on Save and selected card | No overlay, normal click works | Browser geometry | No browser geometry proof existed |
| S-09 | REQ-05 | Browser, seller route, controlled HTTP | Open same picker, choose/cancel | Seller request has only fixed shop id | Browser transport | Seller picker shares component but no browser proof |
| S-10 | REQ-05 | Browser, admin, 1440×900 | Open picker | Layout and labels readable | Browser geometry | Desktop picker not browser-proven |
| S-11 | REQ-05 | Browser, fa/en | Inspect `dir` and labels | RTL fa / LTR en; catalog labels | Browser UI | New copy absent |

## Internal waves, same task

| Wave | Depends on | REQs/scenarios | Small connected change | Focused checks | Exit result |
|---|---|---|---|---|---|
| W1 | none | REQ-01, REQ-02, REQ-03 | Selected card, option markers, preserved selection and distinct states | Existing picker mount tests + tsc | Presentation and state facts green |
| W2 | W1 | REQ-04, REQ-05 | Mobile wrapping/hit targets and localized copy | Mount tests + browser first run | REQ-04/05 started with geometry |
| W3 | W2 | REQ-01..05 | Playwright controlled-HTTP acceptance | Browser suite + tsc + build | All tests pass or explicit PARTIAL |

## Controlled data, regression and served handoff

- Browser server: existing `http://127.0.0.1:3000`; no restart/port change.
- Transport: Playwright `page.route` for `/v1/admin/dev-context`, `/v1/admin/access-control/me/capabilities`, `/v1/admin/stories/query`, `/v1/admin/stories/{id}`, `/v1/admin/stories/product-options`, locale options, and update-item PUT. Seller routes mirror `/v1/seller/...`.
- No live Story/Media row, database, auth, permission, provider, money, stock or notification write.
- Existing picker mount suite: `npx tsx --tsconfig tsconfig.mount-tests.json --test app/stories/management/story-product-picker-282r21.mount.test.ts`.
- Production build: `npm run build`; served app starts on existing port. Backend build not required.
- Regression: other Story tests and `tsc`; no auth or media replacement change.

## Checkpoint log

- W0: repo/base verified; OpenSpec change, proposal, spec, design and tasks written; plan and manifest created before production edit.
- W1 pending: reproduce missing selected marker/bottom caption via mount/browser, then change only the picker presentation/state emphasis.
- W2 pending: mobile and locale proof.
- W3 pending: one Playwright acceptance run, one correction round maximum, then evidence/review gate.
