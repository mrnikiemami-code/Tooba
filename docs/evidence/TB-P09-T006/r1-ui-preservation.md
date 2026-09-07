# R1 UI Preservation

USER_VISUAL_ACCEPTED=NO — no redesign.

Preserved markers verified in source + FE smoke:

- Admin اقلام و ارسال panel + compact kebab
- Create Shipment modal dynamic `shippingMethodCode` forms (پست / تیپاکس / پیک)
- Offer editor FA labels: `استفاده از سیاست پیش‌فرض فروشگاه` / `مهلت اختصاصی` / `غیرقابل مرجوعی`
- Shipping FA labels from registry; no raw enum as primary UI label
- Grid width/scroll/filter/kebab refinements not reverted
- FE `:3000` HTTP 200; Host `:5088` health ok
- Focused `admin-t006-return-shipping.test.ts` green
