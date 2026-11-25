"use client";

import { useRouter } from "next/navigation";
import { useMemo } from "react";
import { useLocale } from "../../../../lib/i18n/locale-context";
import { offerLocaleFromAppLocale } from "../../../../lib/offer-workspace/i18n";
import { useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { OfferWorkspace } from "../../../../lib/offer-workspace";
import type { OfferWorkspaceAdapter } from "../../../../lib/offer-workspace";
import { queryAdminSellersGrid } from "../../../../features/admin-sellers";
import {
  createAdminOffer,
  findAdminActiveOffer,
  loadAdminProductVariants,
  searchAdminCatalogProducts,
} from "../../admin-offer-api";
import { adminOfferIntent, clearAdminOfferIntent } from "../../offer-create-intent";

const PAGE_SIZE = 10;

export default function AdminCreateOfferPage() {
  const router = useRouter();
  const localizePath = useLocalizedPath();
  const offerLocale = offerLocaleFromAppLocale(useLocale());

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
      <OfferWorkspace adapter={adapter} />
    </main>
  );
}
