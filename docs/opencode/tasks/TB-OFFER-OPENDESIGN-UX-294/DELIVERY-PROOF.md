# DELIVERY PROOF — TB-294-R4 (final artifact delivery)

## Origin (authoritative OpenDesign project)
`C:\OpenDesign\.od\projects\routine-1a39bd38-5ca2-4610-973b-f9a7737e71ac\toba-offer-workspace.html`

## Delivery
`D:\Tooba-Offer-OpenDesign\FINAL\index.html`

## Hash proof (source == destination)
- SRC  SHA-256: `3F61B3C5D24361008F6577C3965BF04ADB9386FC8734116EC28B7F0FA1757F7C`
- DST  SHA-256: `3F61B3C5D24361008F6577C3965BF04ADB9386FC8734116EC28B7F0FA1757F7C`
- **HASH_MATCH = true** — byte-identical; 618,637 bytes.

## Dependent resources
- Self-contained artifact: 7 embedded `data:image/*`; **0 external asset refs** (`assets/…`), verified. Nothing external to ship.
- `artifact.json` (OpenDesign artifact descriptor, status=complete) included in `FINAL\`.

## Real browser verification (Chromium, RTL/fa-IR)
- HTTP status 200; title «توبا — فضای کاری پیشنهادها».
- Tabs present: «مدیر/جدول پیشنهادها»، **«مدیر/ایجاد و ویرایش»**، «فروشنده/پیشنهادهای من»، **«فروشنده/فرم پیشنهاد»**.
- Form verified visible: heading «ویرایش پیشنهاد» + sections: مشخصات پیشنهاد · قیمت‌گذاری · تخفیف · موجودی انبارها ·
  ارسال · مقدار و واحد · مرجوعی · وضعیت و چرخه عمر · تاریخچه تغییرات.
- Visible controls (form view): inputs=32, selects=11, textarea=1, buttons=25; hasCreate/edit/save = true.
- **tableOnly = false → verdict = PASS** (the artifact is not table-only; create/edit form exists and is interactive).
- Screenshots: `FINAL\overview.png`, `FINAL\form-view.png` (also in repo evidence).

## Old outputs separated from the delivery path
Moved to `D:\Tooba-Offer-OpenDesign\_legacy-pre-r4\`: previous `index.html`, `screens\` (stale R2/292 renders),
`opendesign-output\` (stale 219 KB intermediate), `opendesign-render.png`. The delivery path is now only `FINAL\`.

## Constraints honored
No design change to the OpenDesign output; no engine re-run; no hand-written HTML; no old output used; no Tooba source change.
STOP_FOR_DESIGN_APPROVAL
