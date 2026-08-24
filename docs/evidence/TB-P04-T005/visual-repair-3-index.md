# TB-P04-T005 visual repair 3 index

Functional ACCEPT is preserved. Visual ACCEPT is pending Architect review of these live captures.

Live Next: `http://127.0.0.1:3012` rewriting `/v1/*` to Host `http://127.0.0.1:5088`. Tenant store-alpha. Product `01a030d1-4056-7000-baf1-99951569bc6b` (پیراهن مردانه لینن).

ImageMagick was not used; Architect should open the twelve named PNGs. Dimension and overflow proofs:

- `visual-repair-3/screenshot-dimensions.md`
- `visual-repair-3/overflow-check.md`

| file | route | CSS viewport | PNG | theme | direction | live API | state | overflow |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| visual-repair-3/01-list-1440x900-rtl-light.png | `/admin/products` | 1440×900 | 1440×900 | light | rtl | yes | default columns, no header range sliders | false |
| visual-repair-3/02-overview-1440x900-rtl-light.png | workspace overview | 1440×900 | 1440×900 | light | rtl | yes | Persian identity, SEO warning copy | n/a (desktop workspace) |
| visual-repair-3/03-variants-1440x900-rtl-light.png | variants | 1440×900 | 1440×900 | light | rtl | yes | 1 variant / 2 offers | n/a |
| visual-repair-3/04-commercial-1440x900-rtl-light.png | commercial | 1440×900 | 1440×900 | light | rtl | yes | فروشگاه آرمان + دیجی‌استایل نمونه | n/a |
| visual-repair-3/05-inventory-1440x900-rtl-light.png | inventory | 1440×900 | 1440×900 | light | rtl | yes | انبار تهران / اصفهان / فروشنده ب | n/a |
| visual-repair-3/06-seo-content-1440x900-rtl-light.png | seo | 1440×900 | 1440×900 | light | rtl | yes | incomplete search title | n/a |
| visual-repair-3/07-publication-1440x900-rtl-light.png | publication | 1440×900 | 1440×900 | light | rtl | yes | readiness groups | n/a |
| visual-repair-3/08-mobile-overview-390x844-rtl-light.png | overview | 390×844 | 390×844 | light | rtl | yes | drawer nav, single column | false |
| visual-repair-3/09-mobile-commercial-390x844-rtl-light.png | commercial | 390×844 | 390×844 | light | rtl | yes | seller cards | false |
| visual-repair-3/10-ltr-1440x900-light.png | publication LTR | 1440×900 | 1440×900 | light | ltr | yes | direction toggle | n/a |
| visual-repair-3/11-dark-1440x900-rtl.png | overview dark | 1440×900 | 1440×900 | dark | rtl | yes | dark tokens | n/a |
| visual-repair-3/12-conflict-1440x900-rtl.png | overview 409 | 1440×900 | 1440×900 | light | rtl | yes | concurrent PATCH then UI Save | n/a |

TB-P04-T006 is not issued.
