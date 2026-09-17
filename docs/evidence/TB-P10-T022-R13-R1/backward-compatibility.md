# backward-compatibility — TB-P10-T022-R13-R1

| Legacy | Adapter |
|--------|---------|
| `displayHeightPx` numeric | Maps to nearest Medium/Large/ExtraLarge for editing; may still influence render when present |
| `seoTitle` | Promoted to visible `title` when title empty |
| `seoDescription` / `subtitle` | Mapped to visible `description` |
| raw `href` | Inferred into `destinationType` (none / all-products / product / category / custom-url) |
| `hero.full-width|contained|side-promos` | Mapped to fullscreen/shapes/diagonal |
| single-image Hero config (no slides[]) | `LandingHero` still wraps into one `HeroSlider` slide |

No mass-delete of saved sections; no silent wipe of slide arrays on open.
