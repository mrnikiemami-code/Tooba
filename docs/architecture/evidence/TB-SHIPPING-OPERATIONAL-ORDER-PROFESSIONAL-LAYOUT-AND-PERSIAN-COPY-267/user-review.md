# TB-267 — user review (visual only, no acceptance claimed)

The user rejected the previous operational order surface: «الان جا نمایی ها اصلا حرفه ای نیست ui افتضاح هست این رو درستش کن واقعا نوشته ها قابل تحویل نیست بقیه اش کار کرد».

Read that literally: **the functional workflow worked for you; the presentation did not.** Nothing functional was re-exercised, and nothing about shipping, payment, returns or finance is claimed as accepted.

## The one URL to review

```
http://127.0.0.1:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations?fulfillmentId=01a1db2f-eb79-7000-b084-3d6b4add0fbb&shippingGroupId=01a1db2f-e351-7000-a516-71ab5b815d1c&from=queue
```

Your own Order B, unchanged and read-only. The source parcel is still `Returned`, the linked resend is still `Delivered`, the damaged decision and its case links are still where they were.

## What to look at

1. **Header** — «عملیات ارسال» with the order reference as secondary LTR text, and the two actions grouped at 44px. No empty banner space.
2. **Overview** — one card: the work target on the wide side, the recipient on the narrow side (stacked on a phone). Name, mobile and address are together; the mobile sits next to its own label in LTR; a long address wraps instead of pushing anything to the far edge.
3. **Operational group** — «تحویل مشتری» and «موعد ارسال حامل» as separate labelled rows, then one deadline block: «آمادهسازی از / خروج به سوی حامل / آخرین مهلت تحویل به حامل», with «به وقت تهران» written once as words, never `Asia/Tehran` and never a bare ISO date.
4. **Contents then parcels** — the product and its quantity sit in a compact block; each parcel is a full-width card: «مرسولهٔ ۱» / «مرسولهٔ ۲», its Persian status, `تیپاکس · ۱ قلم`, then a tracking line you can select and copy with the «کپی» button.
5. **Lineage** — «ارسال مجدد در مرسولهٔ ۲ · تحویلشده» and «ارسال مجدد از مرسولهٔ ۱» tell the relationship in human parcel numbers; the raw tracking code of the child is behind «پیگیری ارسال مجدد: مرسولهٔ ۲». The old amber paragraph no longer repeats a long `TRK-…` code and no longer ends in a dangling «·».
6. **Finance** — this shipping-only route no longer shows price, line total, group fee or refund columns; the product, quantity, packing, status, allocation, return window and every physical action are still there. The full order page still shows its money fields.

Check it at desktop width and on a phone; the layout stacks cleanly at 390px with no horizontal scrolling.

## Deliberately not changed

- No create / dispatch / deliver / return / refund / cancel / save / publish was performed on your record. Everything on this route was read, not written.
- Your stored names («TB264R2 تیپاکس اکسپرس آزمایشی», «TB266R1 گیرنده», «فروشگاه آرمان») are your data and were left exactly as stored.
- Toasts, MessageBox, retained-attempt recovery, server-projected permissions and the exact lineage of the parcels are untouched.

Tell the architect what still reads badly; nothing here is claimed as visually accepted.
