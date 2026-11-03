"use client";

import { Minus, Plus, Trash2 } from "lucide-react";
import { useEffect, useState, type ReactNode } from "react";
import { useLocalizedPath } from "../../lib/i18n/locale-context.tsx";
import { formatQuantityDisplay, parseQuantityInput } from "../../lib/quantity-display.ts";
import { formatOfferAmount, storefrontMediaUrl } from "./storefront-api.ts";
import type { StorefrontCartLine } from "./storefront-cart-map.ts";
import { groupCartLinesBySeller } from "./storefront-cart-grouping.ts";

type Locale = "fa" | "en";

/**
 * Seller-grouped cart lines — one section per sellerPartyId.
 */
export function StorefrontCartSellerSections({
  lines,
  busy,
  locale,
  onRemove,
  onChangeQuantity,
}: {
  lines: StorefrontCartLine[];
  busy: boolean;
  locale: Locale;
  onRemove: (lineId: string) => void;
  onChangeQuantity: (lineId: string, quantity: number) => void;
}): ReactNode {
  const sections = groupCartLinesBySeller(lines);
  return (
    <div className="space-y-4" data-testid="cart-seller-sections">
      {sections.map((section) => (
        <section
          key={section.sectionKey}
          className="space-y-2"
          data-testid="cart-seller-section"
          data-seller-party-id={section.sellerPartyId ?? ""}
          data-seller-unknown={section.sellerPartyId ? "false" : "true"}
        >
          <header className="flex flex-wrap items-baseline justify-between gap-2 px-1">
            <h3 className="text-sm md:text-base font-bold text-gray-900" data-testid="cart-seller-name">
              {section.sellerDisplayName}
            </h3>
            {section.sellerSubtotalExclusiveOfTax != null ? (
              <p className="text-xs text-gray-500 tabular-nums" data-testid="cart-seller-subtotal">
                {formatOfferAmount(section.sellerSubtotalExclusiveOfTax, section.currency)}
              </p>
            ) : (
              <p className="text-xs text-amber-700" data-testid="cart-seller-subtotal-unavailable">
                {locale === "en" ? "Seller total unavailable" : "جمع فروشنده نامشخص"}
              </p>
            )}
          </header>
          <div className="space-y-2">
            {section.lines.map((line) => (
              <CartLineCard
                key={line.lineId}
                line={line}
                busy={busy}
                locale={locale}
                showSellerLabel={false}
                onRemove={() => onRemove(line.lineId)}
                onChangeQuantity={(qty) => onChangeQuantity(line.lineId, qty)}
              />
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}

function CartLineCard({
  line,
  busy,
  locale,
  showSellerLabel,
  onRemove,
  onChangeQuantity,
}: {
  line: StorefrontCartLine;
  busy: boolean;
  locale: Locale;
  showSellerLabel: boolean;
  onRemove: () => void;
  onChangeQuantity: (quantity: number) => void;
}) {
  const lp = useLocalizedPath();
  const productHref = lp(line.productSlug ? `/products/${line.productSlug}` : "/products");
  return (
    <div
      className="bg-surface rounded-2xl border border-gray-200 p-3 md:p-4 shadow-sm hover:shadow-md transition-all"
      data-storefront-surface-role="card"
      data-testid="cart-line"
      data-line-id={line.lineId}
      data-seller-party-id={line.sellerPartyId}
    >
      <div className="flex gap-3 md:gap-4">
        <div className="w-20 h-20 md:w-24 md:h-24 rounded-xl bg-gray-50 overflow-hidden shrink-0 border border-gray-100">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={storefrontMediaUrl(line.mediaAssetId)} alt="" className="w-full h-full object-contain p-2" />
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0">
              <a
                href={productHref}
                className="text-sm md:text-base font-bold text-gray-900 hover:text-primary line-clamp-1"
              >
                {line.title}
              </a>
              {showSellerLabel ? (
                <p className="text-[11px] text-gray-500 mt-1">فروشنده: {line.sellerDisplayName}</p>
              ) : null}
              {line.availability && line.availability !== "Available" ? (
                <p className="text-[11px] text-amber-700 mt-1" data-testid="cart-line-availability">
                  {line.availability === "Unavailable"
                    ? locale === "en"
                      ? "Currently unavailable"
                      : "این کالا فعلاً موجود نیست"
                    : locale === "en"
                      ? "Limited quantity available"
                      : "موجودی کمتر از تعداد سبد است"}
                </p>
              ) : null}
            </div>
            <button
              type="button"
              disabled={busy}
              onClick={onRemove}
              className="p-1.5 rounded-lg hover:bg-red-50 text-gray-400 hover:text-red-500 shrink-0"
              title="حذف کالا"
              aria-label="حذف کالا"
              data-testid="cart-line-remove"
            >
              <Trash2 className="w-4 h-4" />
            </button>
          </div>
          <div className="flex flex-wrap items-end justify-between gap-2 mt-2">
            <CartLineQuantityInput
              quantity={line.quantity}
              decimalPlaces={line.quantityDecimalPlaces}
              step={line.quantityStep}
              unitLabel={line.unitDisplayName ?? line.unitCode}
              busy={busy}
              onCommit={onChangeQuantity}
            />
            <div className="text-left">
              {line.unitAmountExclusiveOfTax != null ? (
                <p className="text-xs text-gray-500" data-testid="cart-line-unit-amount">
                  واحد: {formatOfferAmount(line.unitAmountExclusiveOfTax, line.currency)}
                </p>
              ) : null}
              <p className="text-sm font-black text-primary" data-testid="cart-line-amount">
                {line.lineAmountExclusiveOfTax != null
                  ? formatOfferAmount(line.lineAmountExclusiveOfTax, line.currency)
                  : "—"}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function CartLineQuantityInput({
  quantity,
  decimalPlaces,
  step,
  unitLabel,
  busy,
  onCommit,
}: {
  quantity: number;
  decimalPlaces: number;
  step: number | null;
  unitLabel: string | null;
  busy: boolean;
  onCommit: (next: number) => void;
}) {
  const [text, setText] = useState(formatQuantityDisplay(quantity, decimalPlaces));
  useEffect(() => {
    setText(formatQuantityDisplay(quantity, decimalPlaces));
  }, [quantity, decimalPlaces]);

  const bump = step && step > 0 ? step : 1;
  function commit(raw: string) {
    const parsed = parseQuantityInput(raw);
    if (parsed == null) {
      setText(formatQuantityDisplay(quantity, decimalPlaces));
      return;
    }
    onCommit(parsed);
  }

  return (
    <div className="flex items-center gap-2">
      <div className="flex items-center border border-gray-200 rounded-xl overflow-hidden" data-testid="cart-line-qty">
        <button
          type="button"
          disabled={busy || quantity <= bump}
          onClick={() => onCommit(Math.max(bump, quantity - bump))}
          className="w-8 h-8 flex items-center justify-center text-gray-500 hover:bg-gray-100 disabled:opacity-30"
          aria-label="کاهش"
        >
          <Minus className="w-3.5 h-3.5" />
        </button>
        <input
          className="w-16 h-8 text-center text-sm font-bold border-x border-gray-200 bg-surface tabular-nums"
          value={text}
          disabled={busy}
          inputMode="decimal"
          dir="ltr"
          aria-label="تعداد"
          data-testid="cart-line-qty-input"
          onChange={(event) => setText(event.target.value)}
          onBlur={() => commit(text)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.preventDefault();
              commit(text);
            }
          }}
        />
        <button
          type="button"
          disabled={busy}
          onClick={() => onCommit(quantity + bump)}
          className="w-8 h-8 flex items-center justify-center text-gray-500 hover:bg-gray-100"
          aria-label="افزایش"
        >
          <Plus className="w-3.5 h-3.5" />
        </button>
      </div>
      {unitLabel ? <span className="text-[11px] text-gray-500">{unitLabel}</span> : null}
    </div>
  );
}
