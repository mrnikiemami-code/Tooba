export const MENU_LINK_CHOICES = [
  { value: "Home", label: "خانه" },
  { value: "LandingPage", label: "صفحهٔ فرود" },
  { value: "Product", label: "کالا" },
  { value: "Category", label: "دسته‌بندی" },
  { value: "Brand", label: "برند" },
  { value: "Article", label: "مقاله" },
  { value: "External", label: "نشانی بیرونی" },
  { value: "Group", label: "عنوان گروهی" },
] as const;

export function menuLinkLabel(type: string): string {
  return MENU_LINK_CHOICES.find((item) => item.value === type)?.label ?? "مقصد";
}

export function summarizeMenuDestination(item: {
  linkType: string;
  targetLabel?: string | null;
  externalUrl?: string | null;
}): string {
  if (item.linkType === "Home") return "خانهٔ فروشگاه";
  if (item.linkType === "Group") return "بدون پیوند؛ فقط عنوان گروه";
  if (item.linkType === "External") return item.externalUrl ? `نشانی: ${item.externalUrl}` : "نشانی بیرونی";
  const kind = menuLinkLabel(item.linkType);
  return item.targetLabel ? `${kind} · ${item.targetLabel}` : kind;
}
