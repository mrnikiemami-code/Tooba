# RECEIPTS — TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254-R1

Base: `5a11a4b8f98d00952bdd42fa1ab6da60c1b547f7`
Bridge row: `e5cf0ca8-4ead-4dd3-800f-56cdd7c7afcc`
Worker: `tooba-worker-01` · Agent: cursor · Channel: `tooba-main`
Claim: `2026-11-08T15:47:45Z` · Result written: `2026-11-08T16:2xZ` (elapsed ≈ 35–40 min wall clock, measured from
the claim timestamp; no speed promise implied).

## 1. Guidance actually used

- `docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md` (owner/dependency/rendering rules).
- `.cursor/skills/tooba-frontend-feature/SKILL.md` — the repo's mandatory frontend-feature skill; read and
  followed for preflight → implement → proof. No skill was installed or created.
- Design-system conventions read and reused: `design-system/primitives/overlays.tsx` `Dialog` (native `<dialog>`,
  `showModal`/`close`, focus return, `size="lg"`, no portal/fixed overlay) and
  `design-system/primitives/core.tsx` `Button` (`min-h-11`, tone variants).

## 2. Source changes

| File | Change |
| --- | --- |
| `src/frontend/features/order-operational-groups/ui/admin-operational-group-workflow-dialog.tsx` | NEW home of the dialog (moved from `app/admin`), own narrow prop contract, wrap-not-truncate titles, `min-h-11` review input, `lineTitles` for retained summaries, `formatMoney`/`formatStatus` injected |
| `src/frontend/features/order-operational-groups/index.ts` | NEW public import surface (component + types) |
| `src/frontend/app/admin/admin-order-operational-delivery-section.tsx` | imports the feature surface; wraps essential titles; `groupLineTitles()`; passes `lineTitles` + the two formatters |
| `src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts` | +3 TB-254-R1 mounted tests (shared-package owner, wrap readability, retained-title survival) |
| `src/frontend/app/admin/admin-operational-group-workflow-dialog.tsx` | DELETED (moved) |

No baseline/allowlist edits, no guard sweep, no unrelated refactor.

## 3. Commands actually run

```
cd src/frontend
npx tsc --noEmit -p tsconfig.json                     -> exit 0
npx tsx --tsconfig tsconfig.mount-tests.json --test \
  app/admin/admin-order-operational-delivery.mount.test.ts   -> tests 25 / pass 25 / fail 0
npx tsx --tsconfig tsconfig.mount-tests.json --test \
  lib/architecture/frontend-flat-folder.guard.test.ts \
  lib/architecture/frontend-import-boundary.guard.test.ts \
  lib/architecture/frontend-feature-boundary.guard.test.ts
    -> FE-BOUNDARY pass, FE-BOUNDARY-001 pass
    -> FE-FOLDER-001 fail: 11 novel flat files, `admin-operational-group-workflow-dialog.tsx` ABSENT
       (base parent count = 11 => no new violation; 11 !== 0 is the pre-existing red)
    -> FE-FOLDER-002 fail: 5 pre-existing admin-api exports (252/253), unchanged by this task
npx next build                                        -> exit 0, BUILD_ID eQhOdt4_ez2jEs_E1Ke16
```

## 4. Runtime / deployment

- FE3000 restarted from `D:\Users\User\source\repos\SarvNewVer\src\frontend`:
  `node node_modules/next/dist/bin/next start -p 3000 -H 127.0.0.1`, PID **45832**, listener PID **45832**,
  `Ready in 569ms`, `GET /fa` → 200.
- Environment preserved: `TOOBA_HOST_ORIGIN=http://127.0.0.1:5088`, `NEXT_PUBLIC_API_BASE_URL=http://127.0.0.1:5088`,
  `Storefront__CapturedPreview__{Enabled,SelectionEnabled,CheckoutEnabled}=true` and the
  `Tooba__StorefrontCapturedPreview__*` aliases (local ON; the checked-in value stays OFF).
- Build identity proven over HTTP, not from source:
  - `GET /_next/static/eQhOdt4_ez2jEs_E1Ke16/_buildManifest.js` → **200** (6612 bytes)
  - `GET /_next/static/o7FdiG_1QEjT376HubPBj/_buildManifest.js` → **404** (no stale process)
  - Served order HTML references `eQhOdt4_ez2jEs_E1Ke16` and not the previous build.
  - `GET /_next/static/chunks/778-eb4e0f3de11a86f0.js` → 200, 231577 bytes, contains
    `admin-order-operational-dialog-`, `admin-order-operational-retained-line-` and the new
    `min-w-0 break-words text-sm leading-6 text-gray-800` title class, and no longer contains
    `min-w-0 flex-1 truncate text-sm text-gray-800`.
- Host5088 not restarted, not rebuilt; no migration, no SQL, no reseed.

## 5. Authorized rendered check (task-owned browser, NO screenshots)

- Chrome `C:\Program Files\Google\Chrome\Application\chrome.exe`, fresh profile `D:\temp\tb254r1-chrome`,
  `--remote-debugging-port=9346`, PID 25776, window 1440×1000.
- Sanctioned seam: `GET http://127.0.0.1:3000/v1/admin/dev-context` → 200
  `{"actorUserId":"01a036c2-970e-7000-8eb7-94bf5cc2d8db","tenantId":"store-alpha"}`; the app stores it in
  `localStorage.tooba.adminActorUserId` and sends `X-Tooba-Dev-Actor-User-Id` (Host `AdminPanelAccess`
  Development-only header). No cookie/JWT injection and no authenticated user session was used or created.
- Route: `http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf`; operational section rendered,
  no denial banner.
- Raw receipts: `D:\temp\tb254r1-dom.json`, `D:\temp\tb254r1-dom2.json`. Values reproduced in RESULT §1c:
  overflow 0 at 1440/1024/390; 3 groups / 3 parcels; 0 package rows; 0 clipped / 0 `truncate` titles; 0 checkboxes
  inside the operational section; 0 quantity inputs outside a dialog; 6 parcel actions all 44px; 0 seller-aside
  parcel cards; overlay probe inside the section at all widths.
- Keyboard (CDP `Input.dispatchKeyEvent`): 39 Tabs → `admin-order-shipment-assign-tracking-01a1b93f-71d4-7000-9500-e21af4a160e3`
  (44px, solid outline); Enter → `dialog[open]` «ثبت کد رهگیری مرسوله»; Escape → dialog closed and focus returned to
  the same button. Nothing was submitted; no tracking code was written.
- Store-owned order disclosed: `prepareEntries = 0` (no Marketplace cross-seller group), so the group workflow dialog
  was proven in the mounted fixture instead of being forced onto this order.

## 6. Cleanup (owned resources only)

- Task Chrome process 25776 stopped; profile `D:\temp\tb254r1-chrome` retained until the Architect/session no longer
  needs re-verification, then removable. No user browser/tab/profile was read, reloaded or closed.
- No container was created; no database was touched.

## 7. Disclosed, not fixed

- `FE-FOLDER-001` / `FE-FOLDER-002` remain RED with the same entries that existed at the 254 base parent. This task
  removes its own introduced entry and adds no new one; it does not sweep unrelated reds.
- The Marketplace dialog is not reachable on the canonical Store-owned order; its live appearance at 390 inside a
  real Marketplace group remains to be seen by the user/Architect on a lawful Marketplace order.
- Tipax/live provider remains deferred; no CarrierApi path was exercised.
