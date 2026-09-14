# User inspection guide — TB-P10-T014

Host `:5088`, storefront/Admin `:3000`. Dev actor is the usual local Admin session.

## A. Appearance
Open `http://127.0.0.1:3000/admin/settings` → tab **ظاهر فروشگاه**. Inspect all 7 palettes, 4 theme modes, 4 card skins. Default after this Task: آبی توبا + فقط روشن + کلاسیک.

## B. Landing Pages
`http://127.0.0.1:3000/admin/landing-pages`

## C. Menus
`http://127.0.0.1:3000/admin/menus` — منوی دموی فروشگاه

## D. Published demo URLs
- `http://127.0.0.1:3000/landing-demo`
- `http://127.0.0.1:3000/landing-campaign`

## E. Draft preview
In the list, open **صفحهٔ دموی پیش‌نویس** → **پیش‌نمایش** (`/admin/landing-pages/{id}/preview`). The public URL `/landing-demo-draft` stays 404.

## F. Select a demo Landing as Home
On a Published row, **صفحه اصلی**. Confirm the replace dialog if another Home is set.

## G. Restore canonical Home
On the same row, **لغو خانه** / toggle off صفحه اصلی. Canonical Home (`/fa`) returns.

## H. Switch palette / theme / card skin
Appearance tab → pick another palette (e.g. سبز جنگلی), تم فقط تاریک, پوسته شیشه‌ای → **ذخیره ظاهر**. Reload `/fa` or the demo Landing. Restore آبی توبا + فقط روشن + کلاسیک when done.

## I. What to look at
Palette/theme/card on Home and Landing, Header menu after assigning منوی دموی فروشگاه, L1–L3 in the menu editor, Composer section order difference between دمو and کمپین, Draft staying private.

USER_VISUAL_ACCEPTED remains NO until you accept the look.
