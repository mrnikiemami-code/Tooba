"use client";

import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { useOfferLocaleOverride } from "../../../../lib/offer-workspace/offer-locale";
import { ErrorState, Skeleton } from "../../../../design-system";
import { useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { OfferWorkspace } from "../../../../lib/offer-workspace";
import type { OfferWorkspaceAdapter } from "../../../../lib/offer-workspace";
import {
  createSellerOffer,
  findSellerActiveOffer,
  loadSellerOfferDetail,
  loadSellerProductVariants,
  patchSellerOffer,
  readSellerPartyId,
  searchSellerCatalogProducts,
  writeSellerOfferPrice,
} from "../../seller-api";
import { clearOfferIntent, readOrCreateOfferIntent } from "../../offer-create-intent";

const PAGE_SIZE = 10;

export default function VendorCreateOfferPage() {
  const router = useRouter();
  const localizePath = useLocalizedPath();
  const localeResolution = useOfferLocaleOverride("seller");
  const offerLocale = localeResolution.locale;
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
      createAndSaveDraft: async (input) => {
        const scope = `${sellerPartyId}:${input.catalogVariantId}`;
        const created = await createSellerOffer(sellerPartyId, { catalogVariantId: input.catalogVariantId, sellerSku: input.sellerSku, status: "Draft", requestId: readOrCreateOfferIntent(scope) });
        if (!created.ok) return { ok: false, errorCode: created.errorCode, denied: created.denied };
        const offerId = created.detail.offerId;
        try {
          if (input.amount != null) {
            const price = await writeSellerOfferPrice(sellerPartyId, offerId, { amount: input.amount, currency: input.currency });
            if (!price.ok) return { ok: false, errorCode: price.errorCode, createdOfferId: offerId, partial: true };
          }
          const conditionsChanged = input.returnPolicyChoice !== "Default"
            || input.customReturnWindowDays != null
            || input.minimumOrderQuantity != null
            || input.maximumOrderQuantity != null;
          if (conditionsChanged) {
            const patch = await patchSellerOffer(sellerPartyId, offerId, {
              sellerSku: input.sellerSku,
              status: "Draft",
              returnPolicyChoice: input.returnPolicyChoice,
              customReturnWindowDays: input.customReturnWindowDays,
              minimumOrderQuantity: input.minimumOrderQuantity,
              maximumOrderQuantity: input.maximumOrderQuantity,
            });
            if (!patch.ok) return { ok: false, errorCode: patch.errorCode, createdOfferId: offerId, partial: true };
          }
          const reread = await loadSellerOfferDetail(sellerPartyId, offerId);
          if (reread.source !== "host" || !reread.detail) return { ok: false, errorCode: "host-unreachable", createdOfferId: offerId, partial: true };
          clearOfferIntent(scope);
          return { ok: true, offerId };
        } catch {
          return { ok: false, errorCode: "host-unreachable", createdOfferId: offerId, partial: true };
        }
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
      <OfferWorkspace adapter={adapter} localeDefinition={localeResolution.locale} messages={localeResolution.workspaceMessages} />
    </main>
  );
}
