# TB-275 — durable review intent and mobile proof

## D — one intent until an authoritative answer

`app/returns/return-review-intent.ts` (Returns-owned helper, no app-wide framework):

| Rule | Implementation |
| --- | --- |
| The reviewed intent is durable BEFORE the first possible request | `AdminReturnWorkspace.run` builds `{action, occurrenceKey, body, recordedAt}` and writes it with `storeReviewIntent` before `dispatch` is called |
| Storage failure ⇒ no send | a missing storage seam or a throwing `setItem` renders an honest notice («حافظهٔ موقت… اقدام ارسال نشد») and returns **without** dispatching |
| Same-tick double click ⇒ ONE executor | `inFlight` ref guards the whole action; the second click returns immediately |
| Unknown outcome / unmount / reload / route switch keeps the intent | the envelope stays in `sessionStorage` and is re-read on mount (`readReviewIntent`) and rendered as the unresolved state with an exact-body retry path; nothing auto-retries and there is no polling |
| A newer intent is never erased by an older answer | `clearReviewIntent` is compare-and-clear on `occurrenceKey`; `finish` clears only its own occurrence |
| Late responses cannot touch a newer case/dialog | `epoch` increments on mount, on `returnRequestId` change and on unmount; every async continuation checks it before `setResult` |
| Edits do not silently replace a possibly-sent intent | the operation keys are the reviewed occurrence (`occurrenceKey` for a receipt, the exact `expectedRefundAttemptId`, the exact `expectedFailedAttemptId`, the exact rejection reason, the same approval idempotency key), so a retry repeats the SAME review rather than mutating it |
| A conflict/authorization loss keeps the uncertainty | only an owning `state==="ok"` answer or a genuinely proven no-write refusal resolves; a 4xx renders the localized reason and keeps the stored envelope |

Frontend facts proven in `admin-returns-work-queue.test.ts`:

- `case workspace is the single action owner and never sends before persisting the intent`
- `durable review intent round-trips through the storage seam`
- `compare-and-clear only removes the exact reviewed occurrence`
- `a corrupt or nameless envelope is not an intent`
- `each reviewed occurrence gets its own identity`

Server-side, the reviewed identity is what makes the replay safe: the receipt occurrence key (exact-key replay), the
stored pending attempt id (money recovery), the reviewed failed attempt id (deterministic child), and the approval
idempotency key.

## E — mobile and readability (design-system only)

| Requirement | State |
| --- | --- |
| 1440: one work strip, readable shared grid, focused columns | 7 strips in one `role=tablist`, each 44px; shared `AppDataGrid` (no `AgGridReact`), 64px rows from the existing grid density, columns: عملیات / مرجوعی / سفارش / مشتری / وضعیت / مرحلهٔ کار / مبالغ / … |
| 390: header and actions wrap, strip scrolls or wraps locally, grid scoped horizontal scroll | measured: document overflow `0`, strip overflow `0` (wraps), grid text viewport `340` with `1652` content |
| New controls ≥44px | strip tabs 44px, row action and detail action buttons `min-h-11`, receipt inputs/selects `min-h-11`, «بازگشت به صف کار» `min-h-11` |
| No global toolbar restyle | the pre-existing 36px shared toolbar buttons (`فیلتر پیشرفته`, `ستونها`, `خروجی CSV/Excel`, pager) were deliberately left untouched |
| Dialog/keyboard semantics | the workspace uses the existing `MessageBox` + `ToastRegion`; no `confirm()`/`prompt()`, no nested modal |
| Persian/Jalali + digits | dates through the existing `formatReturnDate`, quantities through `formatQuantityDisplay`, digits through `toPersianDigits`/`Intl` `fa-IR`; wire/query values stay unlocalized |
| Unknown states are explicit | a 404 renders «پروندهای برای نمایش نیست» + «این پروندهٔ مرجوعی یافت نشد.» (verified live at 390 and 1440, `hasLoading=false`), an error renders its localized reason with «تلاش دوباره», denied renders «دسترسی به این پرونده مجاز نیست», and a missing counts block stays UNKNOWN rather than 0 |

No screenshot was taken and no screenshot artifact exists (the task forbids it); all geometry above is measured from the
served DOM through `Runtime.evaluate`.
