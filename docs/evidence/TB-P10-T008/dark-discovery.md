# TB-P10-T008 — Dark discovery

| Area | Current behavior | Store-controlled? | User-controlled? | Dark-ready? | Required action |
| --- | --- | --- | --- | --- | --- |
| ThemeProvider | client default light; used to toggle `html.dark` | no | accidental | partial | reuse; stop forcing light after SSR |
| Tailwind darkMode | `class` | n/a | n/a | yes | keep |
| globals `.dark` | surface/text/status tokens | n/a | n/a | yes | reuse; do not override brand primary |
| ThemeMode persist | enum Light/Dark unused on Storefront | yes | no | no | extend LightOnly/DarkOnly/System/UserChoice |
| Admin Appearance | PaletteKey only | yes | no | no | add ThemeMode cards + atomic save |
| Appearance API | GET themeMode string; PUT palette only | yes | no | no | PUT themeMode |
| User preference | locale cookie; no theme key | no | locale only | no | cookie+localStorage for UserChoice |
| System preference | unused | no | OS | no | bootstrap + matchMedia |
| T007 surfaces | brand tokens; leftover gray/white | yes | no | partial | canonical `html.dark` remaps + body tokens |
| Shopeiva | class-based dark utilities in reference | n/a | n/a | reference | color only |

No second ThemeProvider.
