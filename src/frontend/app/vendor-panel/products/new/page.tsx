"use client";

import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { useLocale } from "../../../../lib/i18n/locale-context";
import { offerLocaleFromAppLocale } from "../../../../lib/offer-workspace/i18n";
import { ErrorState, Skeleton } from "../../../../design-system";
import { useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { OfferWorkspace } from "../../../../lib/offer-workspace";
import type { OfferWorkspaceAdapter } from "../../../../lib/offer-workspace";
import {
  createSellerOffer,
  findSellerActiveOffer,
  loadSellerProductVariants,
  readSellerPartyId,
  searchSellerCatalogProducts,
} from "../../seller-api";
import { clearOfferIntent, readOrCreateOfferIntent } from "../../offer-create-intent";

const PAGE_SIZE = 10;

export default function VendorCreateOfferPage() {
  const router = useRouter();
  const localizePath = useLocalizedPath();
  const offerLocale = offerLocaleFromAppLocale(useLocale());
  const [sellerPartyId, setSellerPartyId] = useState<string | null>(null);

  useEffect(() => {
    setSellerPartyId(readSellerPartyId(window.location.search) ?? "");
  }, []);

  const adapter = useMemo<OfferWorkspaceAdapter | null>(() => {
    if (!sellerPartyId) return null;
    return {
      role: "seller",
      mode: "create",
      fixedSeller: { sellerPartyId, displayName: "" },
      searchProducts: async (search, page) => {
        const result = await searchSellerCatalogProducts(sellerPartyId, search, page, PAGE_SIZE, offerLocale.code);
        return {
          rows: result.rows,
          totalCount: result.totalCount,
          page: result.page,
          pageSize: result.pageSize,
          hasMore: result.hasMore,
        };
      },
      loadProductVariants: async (productId) => {
        const rows = await loadSellerProductVariants(sellerPartyId, productId, offerLocale.code);
        return rows.map((row) => ({
          catalogVariantId: row.catalogVariantId,
          productId: row.productId,
          productTitle: "",
          displayLabel: row.displayLabel,
          axisLabels: row.axisLabels,
          catalogCode: row.catalogCode,
          status: row.status,
        }));
      },
      findActiveOffer: async (_sellerId, catalogVariantId) =>
        findSellerActiveOffer(sellerPartyId, catalogVariantId),
      createDraft: async ({ catalogVariantId, sellerSku }) => {
        const scope = `${sellerPartyId}:${catalogVariantId}`;
        let requestId: string;
        try {
          requestId = readOrCreateOfferIntent(scope);
        } catch {
          return { ok: false, errorCode: "offer.create.intent_not_durable" };
        }
        const created = await createSellerOffer(sellerPartyId, {
          catalogVariantId,
          sellerSku,
          status: "Draft",
          requestId,
        });
        if (!created.ok) return { ok: false, errorCode: created.errorCode, denied: created.denied };
        clearOfferIntent(scope);
        return { ok: true, offerId: created.detail.offerId };
      },
      saveCommercial: async () => ({ ok: false, errorCode: "offer.workspace.not_created" }),
      savePrice: async () => ({ ok: false, errorCode: "offer.workspace.not_created" }),
      saveInventory: async () => ({ ok: false, errorCode: "offer.workspace.not_created" }),
      onCreated: (offerId) => router.push(localizePath(`/vendor-panel/products/${offerId}`)),
      onCancel: () => router.push(localizePath("/vendor-panel/products")),
    };
  }, [sellerPartyId, router, localizePath, offerLocale.code]);

  if (sellerPartyId === null) {
    return (
      <main className="mx-auto w-full max-w-6xl">
        <Skeleton className="h-16 w-full" />
      </main>
    );
  }
  if (sellerPartyId.length === 0) {
    return (
      <main className="mx-auto w-full max-w-6xl">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="هویت فروشنده در نشست مشخص نیست."
          onRetry={() => window.location.reload()}
          retryLabel="تلاش دوباره"
        />
      </main>
    );
  }
  if (!adapter) return null;

  return (
    <main className="mx-auto w-full max-w-6xl">
      <OfferWorkspace adapter={adapter} />
    </main>
  );
}
