# R20 accessibility

- Pay / retry buttons have readable names («پرداخت», «بررسی موجودی و پرداخت مجدد»).
- Countdown is numeric text (`00:16`) plus hold sentence (not color-only).
- Keyboard: primary CTAs are native `<button>`.
- Touch: `px-4 py-2` pending CTAs; quantity steppers on mobile.
- Pending section `dir={locale === "fa" ? "rtl" : "ltr"}`; html `dir=rtl` on FA.
- Status copy is text, not badge-color-only.
- No new focus traps.
