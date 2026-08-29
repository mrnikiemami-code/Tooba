export type ProductPublishMissingRequirement = {
  code: string;
  messageFa: string;
  workspaceTab: string;
};

export type ProductPublishReadiness = {
  isReady: boolean;
  categoryReady: boolean;
  translationReady: boolean;
  attributeReady: boolean;
  variantReady: boolean;
  mediaReady: boolean;
  seoReady: boolean;
  missingRequirements: ProductPublishMissingRequirement[];
  messageFa: string;
};

export type PublishChecklistItem = {
  code: string;
  label: string;
  ready: boolean;
  workspaceTab: string;
  messageFa?: string;
};

/** برچسب فارسی وضعیت چرخهٔ عمر Catalog. */
export function formatProductLifecycleLabelFa(status: string): string {
  switch (status) {
    case "Draft":
      return "پیش‌نویس";
    case "Published":
      return "منتشرشده";
    case "Archived":
      return "بایگانی‌شده";
    default:
      return status;
  }
}

/** چک‌لیست انسانی آمادگی انتشار از projection تجمیعی. */
export function buildPublishChecklist(readiness: ProductPublishReadiness | null): PublishChecklistItem[] {
  if (!readiness) {
    return [];
  }
  const missingByCode = new Map(readiness.missingRequirements.map((m) => [m.code, m]));
  return [
    {
      code: "category",
      label: "دسته‌بندی معتبر",
      ready: readiness.categoryReady,
      workspaceTab: "general",
      messageFa: missingByCode.get("category")?.messageFa,
    },
    {
      code: "identity",
      label: "اطلاعات اصلی",
      ready: readiness.translationReady,
      workspaceTab: "general",
      messageFa: missingByCode.get("identity")?.messageFa,
    },
    {
      code: "attributes",
      label: "ویژگی‌های الزامی",
      ready: readiness.attributeReady,
      workspaceTab: "attributes",
      messageFa: missingByCode.get("attributes")?.messageFa,
    },
    {
      code: "variants",
      label: "تنوع‌ها",
      ready: readiness.variantReady,
      workspaceTab: "variants",
      messageFa: missingByCode.get("variants")?.messageFa,
    },
    {
      code: "media",
      label: "تصویر اصلی",
      ready: readiness.mediaReady,
      workspaceTab: "media",
      messageFa: missingByCode.get("media")?.messageFa,
    },
    {
      code: "seo",
      label: "سئو",
      ready: readiness.seoReady,
      workspaceTab: "seo",
      messageFa: missingByCode.get("seo")?.messageFa,
    },
  ];
}

export function mapPublishReadiness(raw: unknown): ProductPublishReadiness | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Record<string, unknown>;
  const read = (a: string, b: string) => item[a] ?? item[b];
  const missingRaw = read("missingRequirements", "MissingRequirements");
  const missing = Array.isArray(missingRaw)
    ? missingRaw.map((row) => {
        const r = (row ?? {}) as Record<string, unknown>;
        return {
          code: String(r.code ?? r.Code ?? ""),
          messageFa: String(r.messageFa ?? r.MessageFa ?? ""),
          workspaceTab: String(r.workspaceTab ?? r.WorkspaceTab ?? "general"),
        };
      })
    : [];
  return {
    isReady: Boolean(read("isReady", "IsReady")),
    categoryReady: Boolean(read("categoryReady", "CategoryReady")),
    translationReady: Boolean(read("translationReady", "TranslationReady")),
    attributeReady: Boolean(read("attributeReady", "AttributeReady")),
    variantReady: Boolean(read("variantReady", "VariantReady")),
    mediaReady: Boolean(read("mediaReady", "MediaReady")),
    seoReady: Boolean(read("seoReady", "SeoReady")),
    missingRequirements: missing,
    messageFa: String(read("messageFa", "MessageFa") ?? ""),
  };
}
