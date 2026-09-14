# TB-P10-T009 — Skin registry

| Key | Name | Role |
| --- | --- | --- |
| classic | کلاسیک | Accepted default chrome (exact previous article/media/action/badge classes) |
| clean | ساده | Low-chrome border-on-hover |
| elevated | برجسته | Stronger shadow, elevated actions |
| glass | شیشه‌ای | Translucent surface + blur |

Host: `StoreAppearanceProductCardSkinRegistry` (`classic|clean|elevated|glass`). FE: `product-card-skin.ts`. Missing/legacy → `classic`. Arbitrary strings rejected (`appearance.skin.invalid`). No CSS/HTML from DB.
