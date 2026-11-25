# Design — offer-workspace-visual-parity-296-r7

Reference: `D:\Tooba-Offer-OpenDesign\295\FINAL\index.html` (SHA-256 DA8AAFBF960B4494C2EE2F24E177F33ED55F9CBAC51648240B909B7DD36F01A2).

- Header: 4-column sticky summary grid (identity · meta · price · actions), 72px minimum row, 46px product thumb, eyebrow identity line, 16px product title, compact meta, price with market and currency, actions at the logical end. Mobile hides header actions and uses a sticky bottom action bar.
- Tabs: numbered RTL tab bar (۱..۷), 44px, active green underline, dirty dot, disabled lock; active tab scrolled into view.
- Content: cards with header/description/body/footer; two-column form grid; 37px controls; split main+316px aside for identity; 3-tile inventory summary; table rows become labelled cards on mobile.
- Colors: scoped semantic tokens derived from the artifact (green accent) while typography uses `var(--font-sans)` and the existing RTL/LTR registry. The list continues to use the Orders/admin design tokens.
- No parallel grid, shell, menu or form framework; the shared `AppDataGrid` remains the only list infrastructure.
