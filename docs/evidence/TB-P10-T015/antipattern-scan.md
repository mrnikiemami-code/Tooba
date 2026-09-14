# Anti-pattern scan — TB-P10-T015

CLEAN.

- No color picker / arbitrary hex input
- Tint tokens curated in palette registry; not primary-alpha
- No page-local Home/Landing override
- Cards/inputs/modals/mini-cart not recolored by BackgroundStyle
- Admin copy uses خنثی / رنگی ملایم
- Single appearance fetch at root layout; no polling
- Preview uses canonical tokens (`appearancePreviewStyle`)
