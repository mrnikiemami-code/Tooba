# Disabled reasons (TB-P07-T039)

Category Attributes UI (`category-attributes-panel.tsx` + `variant-axis-messages.ts`):

| Cause | FA title | When |
|-------|----------|------|
| ValueKind | این نوع ویژگی برای ساخت تنوع مناسب نیست. | Text, Instant, Boolean (e.g. `five_g`) |
| Capability | امکان استفاده از این ویژگی برای تنوع در تعریف اصلی آن فعال نشده است. | Number/Enumeration with `IsVariantAxisAllowed=false` (e.g. `screen_size`) |

English equivalents in `VARIANT_AXIS_DISABLED_BY_KIND` / `VARIANT_AXIS_DISABLED_BY_CAPABILITY`.
