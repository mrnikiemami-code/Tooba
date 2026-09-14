# TB-P10-T005 — Palette registry

| Key | Known | Primary | Strong | Notes |
| --- | --- | --- | --- | --- |
| `tooba-blue` | yes | `#2563EB` / `37 99 235` | `#1d4ed8` / `29 78 216` | canonical default |
| any other | no | fallback `tooba-blue` | fallback | `IsKnown=false` on projection when a row stored an unknown key |

Backend: `StoreAppearancePaletteRegistry`. Frontend: `lib/storefront-appearance/palette-registry.ts`. No Admin color picker. No extra presets exposed.
