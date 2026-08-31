import type { CatalogAttributeValueKind } from "./catalog-attribute-api.ts";

export function valueKindBlocksVariantAxis(valueKind: CatalogAttributeValueKind): boolean {
  return valueKind === "Text" || valueKind === "Instant" || valueKind === "Boolean";
}

export const VARIANT_AXIS_DISABLED_BY_KIND = {
  fa: {
    title: "این نوع ویژگی برای ساخت تنوع مناسب نیست.",
    detail: "ویژگی‌های بله/خیر، متن آزاد و تاریخ/زمان نمی‌توانند محور تنوع باشند.",
  },
  en: {
    title: "This attribute type cannot be used for variants.",
    detail: "Boolean, free text, and date/time attributes cannot be variant axes.",
  },
} as const;

export const VARIANT_AXIS_DISABLED_BY_CAPABILITY = {
  fa: {
    title: "امکان استفاده از این ویژگی برای تنوع در تعریف اصلی آن فعال نشده است.",
    detail: "برای فعال‌سازی، تعریف اصلی ویژگی را ویرایش کنید.",
  },
  en: {
    title: "Variant axis capability is not enabled on this attribute definition.",
    detail: "Edit the canonical attribute definition to enable it.",
  },
} as const;

export const VARIANT_AXIS_CAPABILITY_LABEL = {
  fa: "قابل استفاده برای تنوع",
  en: "Usable for variants",
} as const;

export const VARIANT_AXIS_CAPABILITY_HELPER = {
  fa: "فعال بودن این گزینه فقط اجازه می‌دهد دسته‌ها از این ویژگی به‌عنوان محور تنوع استفاده کنند؛ با فعال‌سازی، هیچ دسته یا محصولی خودکار تغییر نمی‌کند.",
  en: "When enabled, categories may use this attribute as a variant axis; no category or product changes automatically.",
} as const;

export const VARIANT_AXIS_DISABLE_IN_USE_TITLE = {
  fa: "این ویژگی در تنوع‌های فعال استفاده می‌شود",
  en: "This attribute is used in active variants",
} as const;

export const VARIANT_AXIS_DISABLE_ZERO_USAGE_CONFIRM = {
  fa: "پس از غیرفعال‌سازی، این ویژگی دیگر در دسته‌های جدید قابل انتخاب به‌عنوان تنوع نخواهد بود.",
  en: "After disabling, this attribute will no longer be selectable as a variant axis on new categories.",
} as const;
