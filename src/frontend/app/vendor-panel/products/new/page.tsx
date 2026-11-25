"use client";

import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { ErrorState, Skeleton } from "../../../../design-system";
import { useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { OfferWorkspace } from "../../../../lib/offer-workspace";
import type { OfferWorkspaceAdapter } from "../../../../lib/offer-workspace";
import {
  createSellerOffer,
  loadSellerCatalogVariants,
  readSellerPartyId,
} from "../../seller-api";
import { clearOfferIntent, readOrCreateOfferIntent } from "../../offer-create-intent";

const PAGE_SIZE = 10;

export default function VendorCreateOfferPage() {
  const router = useRouter();
  const localizePath = useLocalizedPath();
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
      searchVariants: async (search, page) => {
        const result = await loadSellerCatalogVariants(sellerPartyId, search, page, PAGE_SIZE);
        return {
          rows: result.rows.map((row) => ({
            catalogVariantId: row.catalogVariantId,
            productId: row.productId,
            productTitle: row.productTitle,
            catalogCode: row.catalogCode,
            productStatus: row.productStatus,
          })),
          totalCount: result.totalCount,
          page: result.page,
          pageSize: result.pageSize,
          hasMore: result.hasMore,
        };
      },
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
  }, [sellerPartyId, router, localizePath]);

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
