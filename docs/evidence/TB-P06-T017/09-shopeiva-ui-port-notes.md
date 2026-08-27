# 09 — Shopeiva UI port notes (TB-P06-T017)

## Kept exact (parity)

| Chrome | Detail |
|---|---|
| Accent | `#E53935` (heading bar, borders, CTA, icons) |
| Rail | Swiper + FreeMode + Autoplay, `dir="rtl"`, circle covers, video Play badge, Eye hover |
| Heading | «استوری‌ها» with red vertical accent |
| Modal | `fixed inset-0 z-[200]`, progress bars, tap zones, RTL chevrons, mute/like/comment chrome |
| Media | image + video item playback; duration-aware progress |

## Intentionally omitted

| Shopeiva piece | Tooba decision |
|---|---|
| Customer **AddStory** / `addStoryModal.jsx` | Omitted — stories created in Admin only |
| Fake local story arrays | Replaced by live Host API |

## Files

- Port: `home-stories.tsx`, `story-modal.tsx`
- Source of truth for look/feel: external FrontStarter/shopeiva stories (see `02-shopeiva-story-source-map.md`)

## Zero redesign claim

No new story visual system; no Home/PDP shared chrome redesign; only data wiring + intentional AddStory omission.
