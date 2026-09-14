"use client";

import { createContext, useContext, type ReactNode } from "react";
import { DEFAULT_PRODUCT_CARD_SKIN, resolveProductCardSkin, type ProductCardSkinKey } from "./product-card-skin.ts";

const ProductCardSkinContext = createContext<ProductCardSkinKey>(DEFAULT_PRODUCT_CARD_SKIN);

export function StorefrontProductCardSkinProvider({
  skin,
  children,
}: {
  skin: string | null | undefined;
  children: ReactNode;
}) {
  return (
    <ProductCardSkinContext.Provider value={resolveProductCardSkin(skin)}>
      {children}
    </ProductCardSkinContext.Provider>
  );
}

export function useProductCardSkin(): ProductCardSkinKey {
  return useContext(ProductCardSkinContext);
}
