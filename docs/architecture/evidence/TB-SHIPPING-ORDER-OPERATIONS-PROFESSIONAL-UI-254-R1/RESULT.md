# RESULT — TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254-R1

Status: **PASS within the issued UI-correction + FE-deploy scope**
Decision: **READABILITY_WRAP_ONE_PARCEL_OWNER_FEATURE_OWNED_DIALOG_AND_AUTHORIZED_REAL_PAGE_PROOF**

NOT Architect ACCEPT, NOT user Visual ACCEPT, NOT PSP/provider work, no backend/idempotency change.

- Parent: `TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254`
- Base: `5a11a4b8f98d00952bdd42fa1ab6da60c1b547f7`
- Code: `08935f55198078d478a1f250ed8b59387d045525` (this task's source + task/evidence commit)
- Channel: `tooba-main` · Bridge task row `e5cf0ca8-4ead-4dd3-800f-56cdd7c7afcc`
- Canonical FE3000 PID 45832 → Host5088 (untouched) SingleStore / store-alpha / `tooba_alpha`
- Final served BUILD_ID: `eQhOdt4_ez2jEs_E1Ke16` (previous `o7FdiG_1QEjT376HubPBj` now `404`)

## 1. The three Architect items — closed

### 1a. Essential titles wrap instead of truncating

`GroupItems` (normal card) and the workflow dialog's review rows no longer use `truncate`:

```startLine:558:566:src/frontend/app/admin/admin-order-operational-delivery-section.tsx
                    <li
                      key={lineKey(line)}
                      className="flex items-start justify-between gap-2 px-3 py-2"
                      data-testid={`admin-order-operational-item-${lineKey(line)}`}
                    >
                      <span className="min-w-0 flex-1 break-words text-sm leading-6 text-gray-800">{line.title}</span>
```

The same contract is applied to the dialog's line rows, the dialog's parcel-reuse labels and the group/seller
names in the fee breakdown. Measured on the real page: **0 / 4** item titles clipped (`scrollWidth` vs
`clientWidth`) and **0** carrying a `truncate` class at 1440, 1024 and 390, with `documentElement.scrollWidth -
clientWidth === 0` at every width.

Hit targets: the review quantity input is `min-h-11` (44px) inside a 44px label, the reuse label is `min-h-11`
around a normal 16px checkbox glyph, and every visible parcel action measured **44px** tall on the real page. No
fixed/sticky control was added; the section measured `position: static` and `elementFromPoint` inside the section
returned a node that the section contains at all three widths.

### 1b. The new dialog is no longer a flat-directory business file

The dialog moved out of `app/admin` into the owning feature with a public import surface:

- `src/frontend/features/order-operational-groups/ui/admin-operational-group-workflow-dialog.tsx`
- `src/frontend/features/order-operational-groups/index.ts` — the ONLY import surface the page uses

The file declares its own narrow input contract (`OperationalGroupWorkflowGroup`, `OperationalGroupDialogRow`,
`RetainedOperationalGroupEnvelope`) and receives `formatMoney` / `formatStatus` from the page, so the feature does
**not** import `app/admin/admin-api` or the occurrence store at all — no cross-layer inversion, no duplicated
money/status dictionary. The section now imports `../../features/order-operational-groups`.

Guard result (run once): **`FE-FOLDER-001` is back to the base-parent count of 11 novel flat files with the dialog
absent from the list** — the task-introduced 12th entry is gone and no allowlist/baseline was edited. The two
remaining guard failures are unchanged pre-existing reds: the same 11 flat files that already existed at
`5a11a4b8f`'s parent, and `FE-FOLDER-002` listing the five `admin-api` exports (`AdminAcceptedShippingGroup`,
`AdminOperationalDeliveryGroup`, `AdminOperationalDeliveryLine`, `AdminOperationalDeliveryMember`,
`AdminOperationalFeeBreakdown`) introduced by 252/253 — none of them added by this task.
`FE-BOUNDARY` and `FE-BOUNDARY-001` pass.

### 1c. The authorized canonical page was actually rendered and measured

Task-owned **fresh, sessionless** Chrome profile `D:\temp\tb254r1-chrome` (CDP 9346, PID 25776, window 1440×1000)
on the exact route, using the app's own sanctioned seam: the admin client calls `/v1/admin/dev-context` (same-origin
Next rewrite → Host5088 Development) and sends `X-Tooba-Dev-Actor-User-Id`; the actor returned was
`01a036c2-970e-7000-8eb7-94bf5cc2d8db`. No cookie/JWT injection, no role grant, no user tab touched.

Read-only DOM receipts (`D:\temp\tb254r1-dom.json`, `D:\temp\tb254r1-dom2.json`), **no screenshots**:

| Fact | 1440 | 1024 | 390 |
| --- | --- | --- | --- |
| Authorized render (operational section present, no denial banner) | yes | yes | yes |
| `documentElement` horizontal overflow | 0 | 0 | 0 |
| Group cards / parcel cards | 3 / 3 | 3 / 3 | 3 / 3 |
| Package-lock rows (no consolidated package on this order) | 0 | 0 | 0 |
| Section width | 1071 | 655 | 332 |
| Item titles clipped / with `truncate` class | 0 / 0 | 0 / 0 | 0 / 0 |
| Checkboxes INSIDE the operational section | 0 | 0 | 0 |
| Quantity inputs outside a dialog | 0 | 0 | 0 |
| Parcel actions / min height | 6 / 44px | 6 / 44px | 6 / 44px |
| Duplicated seller-aside parcel cards | 0 | 0 | 0 |
| Overlay probe inside the section | yes | yes | yes |

Real content read from the rendered order (operator comprehension, not testids): method «پیشتاز» / «آزمایش
ارسال۲۳۰», days `1405/08/18` and `1405/08/19`, windows `14:00–15:00` and `09:00–17:00`, sellers «فروشگاه آرمان» and
«فروشگاه نیلوا», fee `۱۲۵٬۰۰۰ ریال` per group, items «نایک شلوار مردانه سری 1», «نایک پیراهن مردانه سری 1»,
«سامسونگ هدفون سری 1». Each parcel appeared exactly once with its own identifier/status/tracking line and its own
`assign_tracking` + `cancel` actions; the seller aside rendered **zero** cards (`asideCount = 0`,
`asideParcelCardCount = 0`) because every parcel of this order is group-covered.

The only checkboxes that exist anywhere on the page belong to other purposeful surfaces: three inside the existing
shared-package create dialog (`admin-consolidated-package-eligible-*` inside
`admin-consolidated-package-create-dialog`) and two unlabelled controls elsewhere in the shell — the operational
shipping section itself contains none.

### Keyboard / focus (measured with real CDP key events on the same page)

39 `Tab` presses reach the first contextual parcel action
(`admin-order-shipment-assign-tracking-01a1b93f-…`, 44px, `outline-style: solid`); `Enter` opens the shared
design-system `Dialog` («ثبت کد رهگیری مرسوله», `admin-shipment-tracking-dialog`); `Escape` closes it and
`document.activeElement` returns to the **same trigger button**. This is the exact `Dialog` primitive
(`showModal`/`close`, no portal, no fixed overlay) that the operational group workflow dialog uses.

Honest limit: this canonical order is Store-owned — `isCrossSellerMarketplace=false`, `consolidateEligible=false`,
`prepareEntries = 0` — so the Marketplace group workflow dialog is not reachable on it and was NOT forced onto it
(no fake Marketplace conversion, no replacement order). Its review/reuse/retained cases are covered by the
controlled mounted Marketplace fixture (§2), and its dialog keyboard/close/focus-return contract is inherited from
the same native `Dialog` measured live above.

## 2. Focused proof (mounted Marketplace fixture)

`app/admin/admin-order-operational-delivery.mount.test.ts` — **25/25 pass** (22 retained + 3 new):

- new — **shared package has ONE owner**: both member parcels in `PKG-9` produce exactly one
  `admin-order-operational-package-lock-PKG-9` row, zero `admin-order-operational-parcel-<id>` cards for the locked
  children, zero child lifecycle buttons and zero seller-aside cards.
- new — **titles wrap**: a deliberately long product title renders in full in the group card and in the dialog,
  both without a `truncate` class and both with `break-words`; the review input carries `min-h-11`.
- new — **retained titles survive partial progress**: after a failed submit the retained envelope is reopened
  against a projection whose packed quantities are all consumed (`remaining = 0`, so the fresh review rows are
  empty) and the recorded request still reads «شال آرمان × ۲» and «پیراهن نیلوا × ۱»
  (`admin-order-operational-retained-line-<lineId>`), never «کالای سفارش»; the page outside the dialog still owns
  no checkbox or quantity input.
- retained — 253-R4/R5/R6 behaviour unchanged: exact prebound envelope before the first POST, same occurrence on
  every refusal/lost response, changed-review blocked while unresolved, reconcile of the recorded envelope,
  retained operations reachable when fresh-create readiness is false, one request on double submit, close/reopen
  never re-mints, viewer switch cannot inherit another viewer's refs.

Typecheck: `npx tsc --noEmit -p tsconfig.json` → 0. Production build: `npx next build` → 0.

## 3. Boundaries preserved

No change to `admin-api.ts`, the occurrence store, the panel's request composition, the server, migrations,
DataProtection, `CapturedPreview` (local ON, checked-in OFF), or storefront/global CSS. No pack/create/dispatch/
cancel/deliver/payment or any business write was performed — the live check only read the page and opened/closed an
already-read-only tracking dialog without submitting it. Host5088 was not restarted. Tipax remains deferred.

## 4. Review URL

`http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf` (FE3000, BUILD_ID
`eQhOdt4_ez2jEs_E1Ke16`). The Marketplace dialog itself is visible only on a Marketplace cross-seller group; it was
proven in the mounted fixture, not by converting this Store-owned order.

STOP_FOR_ARCHITECT · automaticNextImplementationTask: NONE
