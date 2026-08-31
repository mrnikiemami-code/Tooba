"use client";

import { Popover } from "../../design-system/primitives/overlays";

export const ADDITIONAL_CATEGORY_INLINE_LIMIT = 3;

/**
 * تراشه‌های دسته‌های نمایشی: حداکثر ۳ تراشهٔ درون‌خطی و +N بدون درخواست شبکه.
 */
export function AdditionalCategoryChipsCell({ names }: { names: string[] }) {
  const clean = names.map((n) => n.trim()).filter((n) => n.length > 0);
  if (clean.length === 0) {
    return (
      <span className="text-muted" data-testid="additional-category-empty">
        —
      </span>
    );
  }

  const inline = clean.slice(0, ADDITIONAL_CATEGORY_INLINE_LIMIT);
  const rest = clean.slice(ADDITIONAL_CATEGORY_INLINE_LIMIT);

  return (
    <span className="inline-flex max-w-full flex-wrap items-center gap-1" data-testid="additional-category-chips">
      {inline.map((name) => (
        <span
          key={name}
          className="inline-flex max-w-[9rem] truncate rounded-full border border-border bg-surface-elevated px-2 py-0.5 text-xs"
          title={name}
          data-testid="additional-category-chip"
        >
          {name}
        </span>
      ))}
      {rest.length > 0 ? (
        <Popover
          trigger={
            <button
              type="button"
              className="inline-flex rounded-full border border-border bg-surface px-2 py-0.5 text-xs font-medium text-foreground"
              data-testid="additional-category-more"
              aria-label={`${rest.length} دستهٔ دیگر`}
            >
              +{rest.length.toLocaleString("fa-IR")}
            </button>
          }
        >
          <ul className="flex flex-col gap-1" data-testid="additional-category-more-list">
            {rest.map((name) => (
              <li key={name} className="text-sm">
                {name}
              </li>
            ))}
          </ul>
        </Popover>
      ) : null}
    </span>
  );
}

/** نام‌های باقی‌مانده برای +N — فقط از payload ردیف، بدون API. */
export function remainingAdditionalCategoryNames(names: string[]): string[] {
  return names.map((n) => n.trim()).filter((n) => n.length > 0).slice(ADDITIONAL_CATEGORY_INLINE_LIMIT);
}
