# Role visual verification — TB-P10-T017

PaletteTint four-role tokens were slightly stepped so Page / Section / Alternate / Accent read as distinct bands without becoming a primary wash.

Computed Home PaletteTint (tooba-blue):

- PageBackground `rgb(228, 236, 248)`
- SectionSurface `rgb(247, 250, 253)`
- SectionAlternate `rgb(216, 228, 244)`
- CardSurface `rgb(255, 255, 255)`

SectionAccent is below the Home first viewport (flash/promo). Landing Hero/Promo uses accent; sampled landing screenshot `08-landing-tinted.png`.

Cards remain elevated white/dark-surface. Status chips still use success/warning/danger. Header/Footer stay independent.

Adjustment is central palette tint tokens only (C# + FE registry). No page-local colors.
