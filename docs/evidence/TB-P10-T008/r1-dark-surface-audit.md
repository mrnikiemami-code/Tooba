# TB-P10-T008-R1 — Dark surface audit

| Surface | Light-looking leftover | Class |
| --- | --- | --- |
| Header / mega menu | `bg-white`, `bg-gray-50`, gray text/borders | remapped in `globals.css` — correct semantic |
| Mega offers chip | `hover:bg-blue-50` | remapped `html.dark .hover\:bg-blue-50:hover` — was defect, now elevated surface |
| Home rails / brands / blog | `bg-white`, `bg-gray-50`, `bg-white/95` on media tags | remapped; `/95` now elevated. Photo wells intentional media |
| Product Card | `bg-white/90` actions | repaired to semantic tokens |
| PLP | same card | repaired |
| PDP | `bg-white` panels, `bg-gray-50` gallery | remapped; gallery is media well |
| Cart / mini-cart | `bg-white` drawer/panels | remapped |
| Shipping / forms | `bg-white` / `bg-gray-50` inputs | remapped; inputs semantic via gray remaps |
| Pending hold chip | was `#EFF6FF` | `bg-surface` (UI chrome, not media) |
| Account menu | gray hover utilities | remapped |
| Stories | `bg-white/20` on dark story chrome | intentional media overlay |

No page-local hex dark branches added.
