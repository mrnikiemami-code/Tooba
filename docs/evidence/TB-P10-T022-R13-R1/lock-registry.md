# lock-registry — TB-P10-T022-R13-R1

Preserved: LOCK-SF-001…366

Added (non-duplicate):

| Lock | Statement |
|------|-----------|
| LOCK-SF-367 | Hero Slider exposes exactly six canonical variants: fullscreen/الماس, shapes/سیمین, diagonal/کیمیا, cinematic/فاخته, split/صبا, editorial/عقیق. |
| LOCK-SF-368 | All six Hero Slider variants share one HeroSlider/Swiper engine, autoplay, RTL, navigation and persistence contract; parallel slider implementations/libraries are forbidden. |
| LOCK-SF-369 | Hero Slider height is user-selected only through Medium/Large/Extra-Large presets; responsive pixel behavior remains code-owned. |
| LOCK-SF-370 | Slider media UI provides code-owned variant/height-specific recommended aspect ratio and minimum image dimensions without making arbitrary pixels user-configurable. |
| LOCK-SF-371 | Slide-level SEO/accessibility uses semantic visible content plus image Alt; page SEO remains page-level and fake per-slide meta-title/meta-description controls are forbidden. |
| LOCK-SF-372 | Slide CTA destinations use a typed target model with canonical Product/Category/All-Products/Custom-URL resolution; raw GUID/internal identifiers are never exposed to ordinary users. |
| LOCK-SF-373 | Wizard validation must surface blocked progression through visible summary + inline field errors + slide-tab state + first-error navigation, and error state clears live once corrected. |
| LOCK-SF-374 | After Bridge Result delivery, Cursor stops completely; no heartbeat, polling, IDLE loop, or automatic next-task fetch is part of the Tooba worker protocol. |

SoT: `docs/architecture/TOOBA-LOCKS.md`
