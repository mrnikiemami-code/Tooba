# RESULT — TB-SHIPPING-ADMIN-SALES-READY-UI-246-R2

Status: **PASS** within issued scope (NOT Architect / User Visual ACCEPT)

## Summary

Rebuilt the rejected Shipping authoring composition on the same canonical route without changing business rules, payloads, publication, slot semantics, or geography/rate handlers.

## Landed

- Removed the viewport-fixed bottom Save overlay; exactly one Save (`ذخیره تغییرات`) and one Cancel (`انصراف`) live in a compact sticky content toolbar with the title/method/status.
- Freight/calculation and slot mode use segmented controls: native radio glyphs `16×16`, label hit areas `≥44px`.
- Slot UX: one segmented choice `بازه کاری کامل` / `بازه‌های ساعتی`; duration `<select>` appears only when hourly (maps existing `SLOT_DURATION_OPTIONS`); one short example under the selected mode; restore action stays in the schedule header.
- Seven weekdays render once as compact rows; closed Friday has no time inputs and no duplicate closed label; HH:mm inputs use bounded width classes.
- Calculation merchant labels shortened to دستی/خودکار/رایگان for the segmented control.
- Focused authoring tests updated/added (246 + R1 + R2); typecheck clean; production build exit 0; FE3000 restarted from that build.

## Live (mandatory)

- FE `127.0.0.1:3000` → Host `5088` SingleStore/store-alpha/tooba_alpha
- Authorized admin BFF session `admin-actor@tooba.local` → userId `01a036c2…`, tenant `store-alpha`
- Chrome CDP `:9339` profile `D:\temp\tb246r1-chrome`
- Route left open: `http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`
- Method `آزمایش ارسال۲۳۰`; languages fa-IR/en-US/ru-RU; breadcrumb `ایران`
- apply=1 cancel=1 sticky=0; full-window hides duration select; hourly reveals it; giantRadios=0; shortLabels=0; no horizontal overflow at 1440/1024/390
- Local tab/slot/day/destination modal open-close without Save/publish/PUT
- Host `editor-document` after exercise: HTTP 200, revision **2**, bytes **2852** (unchanged)

## Stop

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
