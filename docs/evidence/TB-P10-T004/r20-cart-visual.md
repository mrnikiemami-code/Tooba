# R20 cart visual

Real browser: Cursor IDE browser, `http://127.0.0.1:3000/fa/cart`.

Observed:

- Shopeiva Cart chrome + «در انتظار پرداخت» section above empty/active cart.
- Pending cards use storefront white/rounded cards, not Admin grids.
- Product title, payable amount (۹٬۴۸۵٬۷۷۱ ریال), countdown, Pay / retry CTAs.
- When Active Cart empty, pending section stays prominent; empty-cart copy points to pending above.
- State I: Active Cart line (نایک شلوار مردانه سری 1) + pending cards together.
- Mobile 390×844: «منوی موبایل» + same pending section + quantity steppers.
- EN: heading «Awaiting payment», English status/CTA; chrome still mixed FA (pre-existing i18n, not redesigned).

Screenshot tool mirrors RTL pixels; `document.documentElement.dir=rtl` and a11y tree are correct Persian.

No page redesign. Spacing/copy-only repairs listed in r20-focused-validation.md.
