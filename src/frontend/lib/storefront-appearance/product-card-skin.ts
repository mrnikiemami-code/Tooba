export const DEFAULT_PRODUCT_CARD_SKIN = "classic";

export type ProductCardSkinKey = "classic" | "clean" | "elevated" | "glass";

export interface ProductCardSkinChrome {
  article: string;
  media: string;
  action: string;
  badge: string;
}

export interface ProductCardSkinDefinition {
  key: ProductCardSkinKey;
  nameFa: string;
  nameEn: string;
  chrome: ProductCardSkinChrome;
}

export const CLASSIC_PRODUCT_CARD_CHROME: ProductCardSkinChrome = {
  article: "group relative flex flex-col rounded-2xl overflow-hidden bg-surface border border-border hover:shadow-xl hover:shadow-black/40 hover:-translate-y-1 transition-all duration-300",
  media: "relative aspect-[4/5] bg-background overflow-hidden",
  action: "w-7 h-7 flex items-center justify-center rounded-full bg-surface-elevated/90 backdrop-blur-sm shadow-md hover:scale-110 transition-transform",
  badge: "bg-primary text-white text-[10px] font-bold px-2.5 py-0.5 rounded-lg shadow-lg shadow-primary/30 flex items-center gap-1",
};

export const PRODUCT_CARD_SKINS: readonly ProductCardSkinDefinition[] = [
  {
    key: "classic",
    nameFa: "کلاسیک",
    nameEn: "Classic",
    chrome: CLASSIC_PRODUCT_CARD_CHROME,
  },
  {
    key: "clean",
    nameFa: "ساده",
    nameEn: "Clean",
    chrome: {
      article: "group relative flex flex-col rounded-2xl overflow-hidden bg-surface border border-transparent hover:border-border hover:-translate-y-1 transition-all duration-300",
      media: "relative aspect-[4/5] bg-background overflow-hidden",
      action: "w-7 h-7 flex items-center justify-center rounded-full bg-surface hover:scale-110 transition-transform",
      badge: "bg-primary text-white text-[10px] font-bold px-2.5 py-0.5 rounded-lg flex items-center gap-1",
    },
  },
  {
    key: "elevated",
    nameFa: "برجسته",
    nameEn: "Elevated",
    chrome: {
      article: "group relative flex flex-col rounded-2xl overflow-hidden bg-surface border border-border shadow-md hover:shadow-2xl hover:shadow-black/50 hover:-translate-y-1 transition-all duration-300",
      media: "relative aspect-[4/5] bg-surface overflow-hidden",
      action: "w-7 h-7 flex items-center justify-center rounded-full bg-surface-elevated shadow-lg hover:scale-110 transition-transform",
      badge: "bg-primary text-white text-[10px] font-bold px-2.5 py-0.5 rounded-lg shadow-lg shadow-primary/40 flex items-center gap-1",
    },
  },
  {
    key: "glass",
    nameFa: "شیشه‌ای",
    nameEn: "Glass",
    chrome: {
      article: "group relative flex flex-col rounded-2xl overflow-hidden bg-surface/70 backdrop-blur-md border border-border/80 hover:shadow-xl hover:shadow-black/30 hover:-translate-y-1 transition-all duration-300",
      media: "relative aspect-[4/5] bg-background/80 overflow-hidden",
      action: "w-7 h-7 flex items-center justify-center rounded-full bg-surface/60 backdrop-blur-md shadow-md hover:scale-110 transition-transform",
      badge: "bg-primary/90 text-white text-[10px] font-bold px-2.5 py-0.5 rounded-lg backdrop-blur-sm flex items-center gap-1",
    },
  },
];

const REGISTRY: Record<string, ProductCardSkinDefinition> = Object.fromEntries(
  PRODUCT_CARD_SKINS.map((item) => [item.key, item]),
);

export function resolveProductCardSkin(raw: string | null | undefined): ProductCardSkinKey {
  const key = raw?.trim().toLowerCase();
  return key && REGISTRY[key] ? key as ProductCardSkinKey : DEFAULT_PRODUCT_CARD_SKIN;
}

export function isKnownProductCardSkin(raw: string | null | undefined): boolean {
  const key = raw?.trim().toLowerCase();
  return Boolean(key && REGISTRY[key]);
}

export function resolveProductCardSkinChrome(raw: string | null | undefined): ProductCardSkinChrome {
  return REGISTRY[resolveProductCardSkin(raw)]?.chrome ?? CLASSIC_PRODUCT_CARD_CHROME;
}

export function listProductCardSkins(): readonly ProductCardSkinDefinition[] {
  return PRODUCT_CARD_SKINS;
}
