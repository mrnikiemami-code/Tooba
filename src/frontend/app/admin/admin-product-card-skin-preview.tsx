import { resolveProductCardSkin, resolveProductCardSkinChrome } from "../../lib/storefront-appearance/product-card-skin.ts";

export function AdminProductCardSkinPreview({
  skin,
  compact = false,
  testId,
}: {
  skin: string;
  compact?: boolean;
  testId?: string;
}) {
  const key = resolveProductCardSkin(skin);
  const chrome = resolveProductCardSkinChrome(key);
  return (
    <article
      className={`${chrome.article} pointer-events-none ${compact ? "max-w-[168px]" : "max-w-[220px]"}`}
      data-product-card-skin={key}
      data-testid={testId}
    >
      <div className={`${chrome.media} flex items-end justify-center`}>
        <span className="absolute inset-0 bg-gradient-to-br from-primary/25 via-primary/70 to-primary" />
        <span className={`${chrome.action} absolute top-2 right-2 text-[10px] text-foreground`}>♡</span>
        <span className="relative mb-2 text-[10px] font-bold text-white/90">نمونه تصویر</span>
      </div>
      <div className={`flex-1 flex flex-col ${compact ? "p-2 gap-1" : "p-3 gap-1.5"}`}>
        <p className={`font-bold text-foreground leading-5 ${compact ? "text-[11px]" : "text-xs"}`}>هدفون بی‌سیم</p>
        <span className={chrome.badge}>٪۲۰</span>
        <p className={`text-foreground ${compact ? "text-[11px]" : "text-xs"}`}>۲٬۴۷۰٬۰۰۰ ریال</p>
        <span className="mt-auto rounded-lg bg-primary text-primary-foreground text-center font-bold py-1.5 text-[11px]">
          افزودن به سبد
        </span>
      </div>
    </article>
  );
}
