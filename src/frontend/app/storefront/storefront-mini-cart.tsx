"use client";

import { LocalizedLink } from "../../lib/i18n/LocalizedLink.tsx";
import { useLocale, useLocalizedPath } from "../../lib/i18n/locale-context.tsx";
import { useStorefrontNavigationPending } from "../../lib/storefront-navigation/navigation-pending-context.tsx";
import { normalizeNavigationHref } from "../../lib/storefront-navigation/navigation-pending.ts";
import { Loader2, Minus, Plus, ShoppingBag, Trash2, X } from "lucide-react";
import { useEffect, useState } from "react";
import { formatOfferAmount, storefrontMediaUrl } from "./storefront-api.ts";
import {
  CART_CHANGED_EVENT,
  changeCartLineQuantity,
  loadStorefrontCart,
  removeCartLine,
  toCustomerCartMessage,
  type StorefrontCartPage,
} from "./storefront-cart-api.ts";
import { noteCartOwnerRead } from "./storefront-cart-read-ownership.ts";

/**
 * کشوی مینی‌سبد Shopeiva روی حقیقت Cart Host.
 * برند از توکن primary؛ بدون قیمت جعلی و بدون پرش مستقیم به shipping.
 * TB-245-R4: CTA سبد بازخورد سطح‌اقدام فوری + یک خواندن در هر باز شدن.
 */
export function StorefrontMiniCartDrawer({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const [cart, setCart] = useState<StorefrontCartPage | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navPending = useStorefrontNavigationPending();
  const lp = useLocalizedPath();
  const locale = useLocale();
  const cartHref = lp("/cart");
  const origin = typeof window !== "undefined" ? window.location.origin : "";
  const ctaBusy =
    navPending.isPending
    && Boolean(navPending.pending.href)
    && normalizeNavigationHref(navPending.pending.href ?? "", origin)
      === normalizeNavigationHref(cartHref, origin);
  const ctaLabel = ctaBusy
    ? (locale === "en" ? "Opening cart…" : "در حال باز کردن سبد…")
    : (locale === "en" ? "Checkout" : "تکمیل خرید");

  useEffect(() => {
    if (!open) {
      return;
    }
    let cancelled = false;
    let generation = 0;
    const mine = ++generation;
    setLoading(true);
    noteCartOwnerRead("mini-cart-open-cart");
    void loadStorefrontCart()
      .then((page) => {
        if (cancelled || mine !== generation) {
          return;
        }
        setCart(page);
        setError(null);
      })
      .catch((cause: unknown) => {
        if (cancelled || mine !== generation) {
          return;
        }
        setError(toCustomerCartMessage(cause));
        setCart(null);
      })
      .finally(() => {
        if (!cancelled && mine === generation) {
          setLoading(false);
        }
      });
    return () => {
      cancelled = true;
      generation += 1;
    };
  }, [open]);

  useEffect(() => {
    if (!open) {
      return;
    }
    let generation = 0;
    const refresh = () => {
      const mine = ++generation;
      noteCartOwnerRead("mini-cart-event-cart");
      void loadStorefrontCart()
        .then((page) => {
          if (mine !== generation) return;
          setCart(page);
          setError(null);
        })
        .catch((cause: unknown) => {
          if (mine !== generation) return;
          setError(toCustomerCartMessage(cause));
        });
    };
    window.addEventListener(CART_CHANGED_EVENT, refresh);
    return () => {
      generation += 1;
      window.removeEventListener(CART_CHANGED_EVENT, refresh);
    };
  }, [open]);

  useEffect(() => {
    if (!open) {
      return;
    }
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  async function mutate(action: () => Promise<StorefrontCartPage>) {
    setBusy(true);
    setError(null);
    try {
      setCart(await action());
    } catch (cause) {
      setError(toCustomerCartMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  if (!open) {
    return null;
  }

  const lines = cart?.lines ?? [];
  const itemCount = cart?.itemCount ?? 0;
  const subtotal = cart?.subtotalExclusiveOfTax ?? 0;
  const currency = cart?.currency ?? "IRR";

  return (
    <div
      className="fixed inset-0 z-[160] bg-black/50 backdrop-blur-sm"
      data-testid="mini-cart-overlay"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="سبد خرید"
        data-testid="mini-cart-drawer"
        data-nav-busy={ctaBusy ? "true" : "false"}
        className="absolute left-0 top-0 bottom-0 w-full max-w-sm bg-surface shadow-2xl flex flex-col animate-in slide-in-from-left duration-300"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="p-4 border-b border-gray-200 flex justify-between items-center bg-gray-50">
          <h2 className="text-lg font-bold text-gray-900 flex items-center gap-2">
            <ShoppingBag className="w-5 h-5 text-primary" />
            سبد خرید ({itemCount.toLocaleString("fa-IR")})
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-gray-200 transition text-gray-500"
            aria-label="بستن سبد"
            data-testid="mini-cart-close"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="flex-1 overflow-auto p-4 space-y-3" data-testid="mini-cart-lines">
          {loading ? (
            <p className="text-center py-12 text-sm text-gray-500">در حال بارگذاری…</p>
          ) : lines.length === 0 ? (
            <div className="text-center py-12 text-gray-500" data-testid="mini-cart-empty">
              <ShoppingBag className="w-16 h-16 mx-auto mb-4 opacity-20" />
              <p>سبد خرید خالی است</p>
              {error ? <p className="text-xs text-red-600 mt-3">{error}</p> : null}
            </div>
          ) : (
            <>
              {error ? (
                <p className="text-xs text-red-600 bg-red-50 border border-red-100 rounded-lg p-2" role="alert">
                  {error}
                </p>
              ) : null}
              {lines.map((line) => (
                <div
                  key={line.lineId}
                  className="flex gap-3 p-3 bg-gray-50 rounded-xl border border-gray-100"
                  data-testid="mini-cart-line"
                >
                  <div className="w-16 h-16 bg-surface rounded-lg flex items-center justify-center overflow-hidden shrink-0">
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={storefrontMediaUrl(line.mediaAssetId)}
                      alt=""
                      className="w-full h-full object-contain p-1"
                    />
                  </div>
                  <div className="flex-1 min-w-0">
                    <h4 className="font-bold text-sm text-gray-900 line-clamp-2">{line.title}</h4>
                    <p className="text-sm text-primary font-bold">
                      {line.lineAmountExclusiveOfTax != null
                        ? formatOfferAmount(line.lineAmountExclusiveOfTax, line.currency)
                        : "—"}
                    </p>
                    <div className="flex items-center gap-2 mt-1">
                      <button
                        type="button"
                        disabled={busy || line.quantity <= 1}
                        onClick={() =>
                          void mutate(() => changeCartLineQuantity(line.lineId, Math.max(1, line.quantity - 1)))
                        }
                        className="p-1 bg-surface rounded-md text-gray-700 hover:bg-gray-100 transition disabled:opacity-40"
                        aria-label="کاهش"
                      >
                        <Minus className="w-3 h-3" />
                      </button>
                      <span className="text-sm text-gray-700 min-w-[20px] text-center tabular-nums">
                        {line.quantity.toLocaleString("fa-IR")}
                      </span>
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => void mutate(() => changeCartLineQuantity(line.lineId, line.quantity + 1))}
                        className="p-1 bg-surface rounded-md text-gray-700 hover:bg-gray-100 transition"
                        aria-label="افزایش"
                      >
                        <Plus className="w-3 h-3" />
                      </button>
                    </div>
                  </div>
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => void mutate(() => removeCartLine(line.lineId))}
                    className="p-2 text-red-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition self-start"
                    aria-label="حذف"
                    data-testid="mini-cart-line-remove"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              ))}
            </>
          )}
        </div>

        {lines.length > 0 ? (
          <div className="p-4 border-t border-gray-200 bg-gray-50" data-testid="mini-cart-footer">
            <div className="flex justify-between font-bold mb-3 text-base">
              <span className="text-gray-700">جمع کل</span>
              <span className="text-primary">{formatOfferAmount(subtotal, currency)}</span>
            </div>
            <LocalizedLink
              href="/cart"
              className="flex w-full items-center justify-center gap-2 py-2.5 bg-primary text-white text-center rounded-xl font-bold text-sm hover:bg-primary-strong transition-all shadow-md hover:shadow-lg hover:shadow-primary/25 disabled:opacity-80"
              data-testid="mini-cart-checkout-cta"
              aria-busy={ctaBusy || undefined}
              data-action-pending={ctaBusy ? "true" : "false"}
            >
              {ctaBusy ? <Loader2 className="w-4 h-4 animate-spin shrink-0" aria-hidden /> : null}
              {ctaLabel}
            </LocalizedLink>
          </div>
        ) : null}
      </div>
    </div>
  );
}
