/**
 * نقش توکن‌های معنایی Design System.
 * لایهٔ مرجع (ref-*) از نقش محصول جدا است؛ قرمز قالب خطر Tooba نیست.
 */
export const tokenRoles = {
  color: ["background", "surface", "surface-elevated", "foreground", "muted", "border", "primary", "secondary", "success", "warning", "danger", "info", "focus"],
  space: ["1", "2", "3", "4", "6", "8"],
  radius: ["sm", "md", "lg"],
  shadow: ["sm", "md"],
  z: ["header", "overlay", "modal"],
  type: ["display", "title", "body", "caption"],
  density: ["control"],
  motion: ["fast"],
} as const;
