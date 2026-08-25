"use client";

import { createContext, type ReactNode, useCallback, useContext, useEffect, useRef, useState } from "react";
import {
  addWishlistProduct,
  loadWishlistMembership,
  removeWishlistProduct,
  WISHLIST_CHANGED_EVENT,
  wishlistErrorMessage,
} from "./storefront-wishlist-api.ts";

interface WishlistContextValue {
  membership: ReadonlySet<string>;
  pending: ReadonlySet<string>;
  register(productId: string): void;
  toggle(productId: string): Promise<string | null>;
}

const WishlistContext = createContext<WishlistContextValue | null>(null);

/** عضویت کارت‌ها را batch می‌خواند و mutation تأییدنشده را وارد UI نمی‌کند. */
export function StorefrontWishlistProvider({ children }: { children: ReactNode }) {
  const requested = useRef(new Set<string>());
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [membership, setMembership] = useState<Set<string>>(new Set());
  const [pending, setPending] = useState<Set<string>>(new Set());

  const refresh = useCallback(async () => {
    const ids = [...requested.current];
    if (ids.length === 0) return;
    try {
      setMembership(await loadWishlistMembership(ids));
    } catch {
      setMembership(new Set());
    }
  }, []);

  const register = useCallback((productId: string) => {
    if (requested.current.has(productId)) return;
    requested.current.add(productId);
    if (timer.current) clearTimeout(timer.current);
    timer.current = setTimeout(() => void refresh(), 0);
  }, [refresh]);

  useEffect(() => {
    const listener = () => void refresh();
    window.addEventListener(WISHLIST_CHANGED_EVENT, listener);
    return () => {
      window.removeEventListener(WISHLIST_CHANGED_EVENT, listener);
      if (timer.current) clearTimeout(timer.current);
    };
  }, [refresh]);

  const toggle = useCallback(async (productId: string): Promise<string | null> => {
    if (pending.has(productId)) return null;
    setPending((current) => new Set(current).add(productId));
    try {
      if (membership.has(productId)) await removeWishlistProduct(productId);
      else await addWishlistProduct(productId);
      setMembership((current) => {
        const next = new Set(current);
        if (next.has(productId)) next.delete(productId);
        else next.add(productId);
        return next;
      });
      return null;
    } catch (error) {
      return wishlistErrorMessage(error);
    } finally {
      setPending((current) => {
        const next = new Set(current);
        next.delete(productId);
        return next;
      });
    }
  }, [membership, pending]);

  return <WishlistContext.Provider value={{ membership, pending, register, toggle }}>{children}</WishlistContext.Provider>;
}

/** دسترسی مشترک کارت و PDP به عضویت batch و mutation ProductId را فراهم می‌کند. */
export function useStorefrontWishlist(): WishlistContextValue {
  const value = useContext(WishlistContext);
  if (!value) throw new Error("StorefrontWishlistProvider is required.");
  return value;
}
