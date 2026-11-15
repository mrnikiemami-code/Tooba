# TB-268 — tab counts and navigation (live, served build `vJWB2FG1oJ2-vHasmjtve`)

Everything below was read from the real served page, the same read-only Order B the architect named:

```
http://127.0.0.1:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations?fulfillmentId=01a1db2f-eb79-7000-b084-3d6b4add0fbb&shippingGroupId=01a1db2f-e351-7000-a516-71ab5b815d1c&from=queue
```

The order stayed read-only (GET only). No create / dispatch / deliver / return / refund / cancel / save / publish was invoked.

## 1 Counts on Order B (this group)

| Tab | Count | Rendered? | Why |
| --- | --- | --- | --- |
| همه | ۲ | yes | the group's whole parcel truth, cancelled history included |
| نیازمند اقدام | ۰ | hidden | no server-projected action and no retained unresolved attempt |
| در مسیر | ۰ | hidden | no `Dispatched` / `InTransit` parcel |
| در حال برگشت | ۰ | hidden | no `Returning` parcel |
| دریافت‌شده در مبدأ | ۱ | yes | the source parcel is `Returned` at the carrier origin |
| ارسال مجدد | ۱ | yes | durable `resendOfShipmentId`, and it is not a damaged-goods replacement |
| تحویل‌شده | ۱ | yes | the linked resend parcel is `Delivered` |
| لغوشده | ۰ | hidden | no cancelled parcel |

The overlap is deliberate and is the point of the design: the `Delivered` resend is counted **once** in
«ارسال مجدد» and **once** in «تحویل‌شده», because resend is lineage and the others are statuses. Counts are
parcels, never quantities or items, and both the badges and the panel come from one per-group snapshot, so
`count(همه) = visible cards` under «همه» and each category count equals the cards its tab shows.

The default tab on this order is «ارسال مجدد» (needsAction is 0), and the panel really renders only
`مرسولهٔ ۲ — ارسال مجدد از مرسولهٔ ۱ · تیپاکس · ۱ قلم · تحویل‌شده`.

## 2 Numbering

`مرسولهٔ ۱` = the source (`Returned` origin receipt), `مرسولهٔ ۲` = the linked resend (`Delivered`).
The number is the parcel's place in the **whole group**, so it is identical on every tab, in the lineage
chip and in the «نمایش مرسولهٔ …» control. Filtering never renumbers anything.

## 3 Cross-tab lineage navigation

A live click on the chip «ارسال مجدد از مرسولهٔ ۱» inside the child card while «ارسال مجدد» was active:

- the tab selection moved to «دریافت‌شده در مبدأ» (`aria-selected="true"`),
- the panel rendered the **existing** source card
  `admin-order-operational-parcel-01a1dc15-b0b7-7000-9118-953a3f6e92d2`,
- `document.activeElement` was that card, so the operator lands exactly on the counterpart,
- the document still measured `390 / 390` (no horizontal overflow) at that moment.

No card was cloned, no second action owner was created, and no request was sent by the navigation.

## 4 Keyboard (RTL)

`role="tablist"` owns the keydown handler; only the selected tab has `tabIndex 0`, every other tab `-1`.

| Key | Expected | Observed live |
| --- | --- | --- |
| ArrowLeft | next tab in RTL reading order | from «ارسال مجدد» → «تحویل‌شده», selection + focus moved together |
| ArrowRight | previous tab | asserted in the mounted case |
| Home / End | first / last shown tab | asserted in the mounted case |

## 5 Widths, overflow and touch targets

| Viewport | Document | Strip | Selected tab visible | Tab height | Card |
| --- | --- | --- | --- | --- | --- |
| 1440 | 1425 / 1425 (no overflow) | 1022 / 1022 (fits, no scroll) | yes | 44px | 1037 inside `main` 1169 |
| 1024 | 1009 / 1009 (no overflow) | 606 / 606 (fits) | yes | 44px | — |
| 390 (fresh load) | 390 / 390 (no overflow) | 502 scrollWidth / 306 clientWidth → scoped horizontal scroll | yes, 42..169 inside the 42..348 strip box | 44px | 306 |

At 390 the strip scrolls **by itself** — the page does not — and the selected tab is brought fully into view on
load (and again on a later viewport change), so a phone never opens on a half-hidden active tab.

The relationship chip is deliberately compact (24px visual box, 12px text) and carries an expanded hit area
(`::before` of 44px, computed `height: 44px`, `-10px` top and bottom), so the only sub-44 box on the page is
still a 44px touch target.

## 6 Emptiness and single-parcel cases

- A selected tab that reaches 0 on an authoritative refresh keeps its selection and prints
  «مرسوله‌ای در این بخش نیست.» — proven in the mounted suite, not by mutating Order B.
- One parcel, or one non-zero category, renders **no** strip at all: navigation that leads nowhere is noise.
- A package-locked child never funds «نیازمند اقدام» (its actions belong to the shared-package section) but it is
  still one real parcel under «همه» and under its own status.
