# 20 — Zero redesign declaration (TB-P06-T017)

## Declaration

This Task **does not redesign** Shopeiva-locked Home/PDP visual systems.

| Change | Redesign? |
|---|---|
| New Story backend module + APIs | No (data plane) |
| Port exact Shopeiva rail + modal chrome | No (parity port) |
| Live Host fetch instead of `STORY_IMAGES` | No (data binding) |
| Omit customer AddStory | Intentional product/admin boundary — not a visual redesign |
| Admin `/admin/stories` | New admin surface only; storefront chrome unchanged aside from live data |

## Locked surfaces

- Accent `#E53935`, Swiper rail, modal progress/tap RTL preserved from Shopeiva source.
- PageComposition `stories` section continues to render the stories slot without inventing foreign section chrome.
- No unrelated storefront/component redesign.

**Worker PASS ≠ Architect Visual ACCEPT** for critical storefront; this Task’s claim is story **live bind + exact chrome port**, not global Home visual re-ACCEPT.
