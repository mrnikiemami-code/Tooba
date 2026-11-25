"use client";

import { useRouter } from "next/navigation";
import { useMemo } from "react";
import { useOfferLocaleOverride } from "../../../../lib/offer-workspace/offer-locale";
import { useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { OfferWorkspace } from "../../../../lib/offer-workspace";
import type { OfferWorkspaceAdapter } from "../../../../lib/offer-workspace";
import { queryAdminSellersGrid } from "../../../../features/admin-sellers";
import {
  assignAdminOfferFulfillment,
  createAdminOffer,
  findAdminActiveOffer,
  loadAdminOfferDetail,
  loadAdminShippingTypes,
  loadAdminProductVariants,
  patchAdminOffer,
  searchAdminCatalogProducts,
  writeAdminOfferPrice,
} from "../../admin-offer-api";
import { adminOfferIntent, clearAdminOfferIntent } from "../../offer-create-intent";

const PAGE_SIZE = 10;

export default function AdminCreateOfferPage() {
  const router = useRouter();
  const localizePath = useLocalizedPath();
  const localeResolution = useOfferLocaleOverride("admin");
  const offerLocale = localeResolution.locale;

  const adapter = useMemo<OfferWorkspaceAdapter>(
    () => ({
      role: "admin",
      mode: "create",
      searchSellers: async (search, page) => {
        const result = await queryAdminSellersGrid({
          page,
          pageSize: PAGE_SIZE,
          sorts: [],
          filters: {},
          search: search.trim() || undefined,
        });
        const total = result.page.total;
        return {
          rows: result.page.rows.map((row) => ({
            sellerPartyId: row.sellerPartyId,
            displayName: row.displayName,
          })),
          totalCount: total,
          page,
          pageSize: PAGE_SIZE,
          hasMore: page * PAGE_SIZE < total,
        };
      },
      searchProducts: async (search, page) => {
        const result = await searchAdminCatalogProducts(search, page, PAGE_SIZE, offerLocale.code);
        return {
          rows: result.rows,
          totalCount: result.totalCount,
          page: result.page,
          pageSize: result.pageSize,
          hasMore: result.hasMore,
        };
      },
      loadProductVariants: async (productId) => {
        const rows = await loadAdminProductVariants(productId, offerLocale.code);
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
      findActiveOffer: async (sellerPartyId, catalogVariantId) =>
        findAdminActiveOffer(sellerPartyId, catalogVariantId),
      loadShippingTypes: () => loadAdminShippingTypes(),
      createAndSaveDraft: async (input) => {
        const created = await createAdminOffer({
          sellerPartyId: input.sellerPartyId ?? "",
          catalogVariantId: input.catalogVariantId,
          sellerSku: input.sellerSku,
          requestId: adminOfferIntent(`admin:${input.sellerPartyId}:${input.catalogVariantId}`),
        });
        if (!created.ok) return { ok: false, errorCode: created.errorCode, denied: created.denied };
        const offerId = created.detail.offerId;
        try {
          if (input.amount != null) {
            const price = await writeAdminOfferPrice(offerId, { amount: input.amount, currency: input.currency });
            if (!price.ok) return { ok: false, errorCode: price.errorCode, createdOfferId: offerId, partial: true };
          }
          const conditionsChanged = input.returnPolicyChoice !== "Default"
            || input.customReturnWindowDays != null
            || input.minimumOrderQuantity != null
            || input.maximumOrderQuantity != null;
          if (conditionsChanged) {
            const patch = await patchAdminOffer(offerId, {
              sellerSku: input.sellerSku,
              status: "Draft",
              returnPolicyChoice: input.returnPolicyChoice,
              customReturnWindowDays: input.customReturnWindowDays,
              minimumOrderQuantity: input.minimumOrderQuantity,
              maximumOrderQuantity: input.maximumOrderQuantity,
            });
            if (!patch.ok) return { ok: false, errorCode: patch.errorCode, createdOfferId: offerId, partial: true };
          }
          if (input.responsibility) {
            const fulfillment = await assignAdminOfferFulfillment(offerId, { responsibility: input.responsibility, shippingTypeId: input.shippingTypeId });
            if (!fulfillment.ok) return { ok: false, errorCode: fulfillment.errorCode, createdOfferId: offerId, partial: true };
          }
          const reread = await loadAdminOfferDetail(offerId);
          if (!reread.ok) return { ok: false, errorCode: reread.errorCode, createdOfferId: offerId, partial: true };
          clearAdminOfferIntent(`admin:${input.sellerPartyId}:${input.catalogVariantId}`);
          return { ok: true, offerId };
        } catch {
          return { ok: false, errorCode: "host-unreachable", createdOfferId: offerId, partial: true };
        }
      },
      createDraft: async ({ sellerPartyId, catalogVariantId, sellerSku }) => {
        if (!sellerPartyId) return { ok: false, errorCode: "offer.seller.required" };
        const scope = `admin:${sellerPartyId}:${catalogVariantId}`;
        let requestId: string;
        try {
          requestId = adminOfferIntent(scope);
        } catch {
          return { ok: false, errorCode: "offer.create.intent_not_durable" };
        }
        const created = await createAdminOffer({ sellerPartyId, catalogVariantId, sellerSku, requestId });
        if (!created.ok) return { ok: false, errorCode: created.errorCode, denied: created.denied };
        clearAdminOfferIntent(scope);
        return { ok: true, offerId: created.detail.offerId };
      },
      saveCommercial: async () => ({ ok: false, errorCode: "offer.workspace.not_created" }),
      savePrice: async () => ({ ok: false, errorCode: "offer.workspace.not_created" }),
      saveInventory: async () => ({ ok: false, errorCode: "offer.workspace.not_created" }),
      onCreated: (offerId) => router.push(localizePath(`/admin/offers/${offerId}`)),
      onCancel: () => router.push(localizePath("/admin/offers")),
    }),
    [router, localizePath, offerLocale.code],
  );

  return (
    <main className="mx-auto w-full max-w-6xl">
      <OfferWorkspace adapter={adapter} localeDefinition={localeResolution.locale} messages={localeResolution.workspaceMessages} />
    </main>
  );
}
