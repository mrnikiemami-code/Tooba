# TB-277 — modal and quantity proof

## 1. One dialog per reviewed action (mounted, real component)

Mounted suite: `src/frontend/app/admin/admin-return-workspace-277.mount.test.ts` (8/8), on the REAL
`AdminReturnWorkspace` with a controlled HTTP stub.

```
test 1  no review form is rendered inline
        - before any click: 0 open dialogs, 0 elements matching [data-testid$='-form']
        - for each of record-receipt, approve, reject, start-refund, resume-refund, retry-refund, settle-physical:
            · exactly ONE dialog[open]
            · the form root's closest('dialog') is that dialog (never on the page body)
            · zero POSTs while the dialog is open
            · «انصراف» closes it, still zero POSTs, and no intent was stored
```

The dialogs are the shared `Dialog` primitive (`design-system/primitives/overlays.tsx`), the same one the shipment-plan
modal uses; the form content differs per action.

```
test 5  approve → POST /v1/admin/orders/{checkoutId}/operations {code:"approve_return"}
        · zero calls whose URL matches /refund|recover/
        · the approve dialog states «فقط تصمیم تأیید» and «هیچ وجهی جابه‌جا نمی‌شود»
        start refund → POST /v1/admin/returns/{caseId}/start-refund {kind:"start_refund"}
        · the dialog states «اولین تلاش بازگشت وجه»
```

## 2. Quantity is bounded locally by the owning unit and the remaining ceiling

```
test 2  requested 1, unit pcs
        5 entered → confirm.disabled === true, zero POSTs, no stored intent,
                    field carries aria-invalid="true", in-dialog message /بیشتر است/,
                    ceiling line reads «باقی‌ماندهٔ قابل دریافت»

test 3  pcs (0 decimals) vs g (3 decimals), requested 3 and 5
        1.5 pcs  → refused locally with «فقط مقدار صحیح» and zero POSTs
        2 pcs    → accepted
        «۱٫۲۵۰» g → sent as 1.25 (Persian digits + Persian decimal separator normalized exactly)

test 4  requested 2, one line
        usable 2 + damaged 1 → confirm disabled, zero POSTs, message /مجموع سالم و معیوب/
        usable 1 + damaged 1 → sent as two inspected facts [(1,"Usable"),(1,"Damaged")]
```

The unit precision is not guessed in the UI: `ReturnItem.unitDecimalPlaces` is published by
`AdminReturnDetailQueryEngine` from the owning Order enrichment (`QuantityDecimalPlacesSnapshot`), capped at the persisted
scale 6 (proven: owning line with 7 → item 6) and left `null` when the owning line did not answer (proven: no label and no
precision invented). `quantityStep(0) === "1"`, `quantityStep(3) === "0.001"`.

## 3. Notification presentation

- Success: shared `ToastRegion` with a per-outcome sequence key (`key={toast.seq}`), so the same success is announced
  again instead of being swallowed, and the feature clears it after 6 s so no stale success sits on the page.
- Command refusal: shared `MessageBox` (`testId="admin-return-notice"`) with the localized mapped message — one surface,
  never a Toast **and** a MessageBox for the same failure.
- Field refusal: in-dialog `role="alert"` + `aria-invalid`, values preserved, nothing sent.
- Wording is truthful: approve's success reads «تصمیم تأیید ثبت شد؛ بازگشت وجه هنوز آغاز نشده است.»

## 4. Retained intent through the modal lifecycle (unchanged from 276, still fenced)

The stored envelope is read from `sessionStorage` and rendered before/independently of the detail read; the dialog path
uses the same `epoch` fence, one in-flight lock, and `clearReturnReviewIntentIfCurrent` on the exact occurrence. Closing a
dialog clears only the local editing state — it never deletes a possibly-sent intent. The proven-no-effect list now
contains exactly one new token, `return.refund.first_not_lawful`, which is raised before any attempt row or provider call.

## 5. Server-side truth (not a UI claim)

- `ReturnReceivingSummary` + the directory's post-`FOR UPDATE` re-read still reject over-receipt, incoherent evidence,
  undefined inspection and loss of precision with a typed error and zero rows.
- `usable + damaged` ceiling is re-derived from the stored receipts, so a client maximum is never authority.
- An exact occurrence replay is still answered from storage when the remaining amount is already 0.

## 6. Live surface (read-only, DOM text)

- Case detail at 1440/390: product + «واحد: عدد», per-line requested/received/usable/damaged, disposition, Jalali dates,
  Persian digits, `document overflow 0`, no raw ISO/IANA string, zero review actions rendered (settled history), zero open
  dialogs.
- Queue at 1440: `۰ پروندهٔ باز`, all stage counts 0, `سابقه ۱`, «ردیفی برای نمایش نیست», overflow 0.
- The dialog variants could not be exercised on live data because the only live case is settled; the mounted suite is the
  proof of the real component's dialog behavior.
