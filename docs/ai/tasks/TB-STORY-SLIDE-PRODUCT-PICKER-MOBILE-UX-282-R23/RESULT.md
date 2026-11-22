# RESULT — TB-STORY-SLIDE-PRODUCT-PICKER-MOBILE-UX-282-R23

Status: **PASS** within the issued picker UX scope.
Readiness: **READY_FOR_ARCHITECT_REVIEW**. This is worker self-review, not Architect ACCEPT or owner acceptance.

## Changes

- Selected product is now a prominent card above search with a check marker, `aria-live`, the current name, a selected badge and an explicit “choose another” hint.
- Selected options expose `aria-pressed`, a check icon and a visible selected badge; long names wrap instead of truncating.
- Selection remains independent of search, paging, loading, empty and error states; an outside-results note explains when the current product is not in the visible page.
- Loading, empty and error are separate states; retry and load-more controls are 44px and focus-visible.
- fa/en copy added for the new labels; the picker carries `dir=rtl` for fa and `dir=ltr` for en.
- Escape now routes through the controlled `Dialog.onCancel` before native close, so the existing dirty-slide discard guard runs instead of silently closing the owning state.
- Existing lookup, update-item command, media retention, version/occurrence and seller shop scope are unchanged.

## Acceptance

| REQ | Test | Viewport | Result | Evidence |
|---|---|---|---|---|
| REQ-01 | `REQ-01 admin selects B, one PUT preserves media, reload shows B` | 1280×720 default | PASS | `browser/assertions.json` REQ_01 |
| REQ-02 | `REQ-02 cancel and Escape send no mutation and reopen A` | 1280×720 default | PASS | `browser/assertions.json` REQ_02 |
| REQ-03 | `REQ-03 503 shows error and retry keeps A without mutation` | 1280×720 default | PASS | `browser/assertions.json` REQ_03 |
| REQ-04 | `REQ-04 mobile long A outside first page, no overflow and Save reachable` | 360×640 and 360×800 | PASS | `browser/assertions.json` REQ_04 |
| REQ-05 | `REQ-05 seller uses its fixed shop and fa/en direction/labels` | seller default and 1440×900 | PASS | `browser/assertions.json` REQ_05 |

## Transport evidence

`REQ_01` captured exactly one owning PUT:

- method/path: `PUT /v1/admin/stories/{storyId}/items/{itemId}`
- `ctaTarget`: B
- `expectedVersion`: 7
- occurrence key present
- `mediaAssetId` and `mediaUrl` retained
- upload/add/delete count: 0

The controlled PUT updates the fixture state and the later GET/reload shows B.

## Tool evidence

- OpenSpec `2.1.1`: change `story-slide-product-picker-mobile-282-r23` validates with `--strict`; proposal, spec, design and tasks exist.
- Impeccable `4.10.0` guidance was read from the installed skill; the project already has no PRODUCT/DESIGN files, so the existing project tokens/components were reused without creating a parallel design system.
- Playwright `1.65.1` with installed Chromium: 5/5 browser tests pass. Command is recorded in `RECEIPTS.md`.
- Browser transport is controlled by Playwright `page.route`; this is not live backend/PostgreSQL proof.

## Verification

- `npm run test:stories`: 24 unit + 67 mount = **91 passed**
- `npx tsc --noEmit`: 0
- `npm run build`: success; BUILD_ID `UAHu-Q2GiefOjIEroMkVb`
- Served route `/fa/admin/stories/{id}`: 200; Next is running on existing port 3000.
- No backend build/publish was needed; no backend source changed.

## Honest limits

- Browser tests use controlled HTTP fixtures, not live Story/Media persistence.
- The browser suite covers 1440×900 in the REQ-05 test, while REQ-01/02/03 use the Playwright default viewport (1280×720); the task's mandatory mobile checks are 360×640 and 360×800.
- No screenshot was captured (not required).
- The shared `Dialog` change is limited to routing Escape through the existing controlled close; no dialog redesign.
- The Story screen remains oversized; this task does not restructure it.
