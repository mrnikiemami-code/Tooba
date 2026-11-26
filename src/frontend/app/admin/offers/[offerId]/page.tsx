"use client";

import { useParams, useRouter } from "next/navigation";
import { useMemo } from "react";
import { useRegistryLocale } from "../../../../lib/i18n/registry-locale";
import { offerWorkspaceMessagesForRegistry } from "../../../../lib/offer-workspace/messages-ru";
import { useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { OfferWorkspace } from "../../../../lib/offer-workspace";
import type { OfferWorkspaceAdapter } from "../../../../lib/offer-workspace";
import {
  assignAdminOfferFulfillment,
  loadAdminOfferDetail,
  loadAdminOfferHistory,
  loadAdminProductVariants,
  loadAdminShippingTypes,
  patchAdminOffer,
  writeAdminOfferInventory,
  writeAdminOfferPrice,
} from "../admin-offer-api";

export default function AdminOfferDetailPage() {
  const params = useParams<{ offerId: string }>();
  const router = useRouter();
  const localizePath = useLocalizedPath();
  const registry = useRegistryLocale();
  const offerLocale = { code: registry.locale.code, culture: registry.locale.culture, language: registry.locale.language, direction: registry.locale.direction, calendar: registry.locale.calendar };
  const workspaceMessages = offerWorkspaceMessagesForRegistry(registry.locale.language);
  const offerId = params?.offerId ?? "";

  const adapter = useMemo<OfferWorkspaceAdapter>(
    () => ({
      role: "admin",
      mode: "edit",
      offerId,
      loadHistory: (page, pageSize) => loadAdminOfferHistory(offerId, page, pageSize),
      loadDetail: async () => {
        const result = await loadAdminOfferDetail(offerId);
        return result.ok
          ? { ok: true, detail: result.detail }
          : { ok: false, errorCode: result.errorCode, denied: result.denied };
      },
      searchProducts: async () => ({ rows: [], totalCount: 0, page: 1, pageSize: 10, hasMore: false }),
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
      createDraft: async () => ({ ok: false, errorCode: "offer.workspace.edit_mode" }),
      saveCommercial: (patch, version) => patchAdminOffer(offerId, patch, version),
      savePrice: (input, version) => writeAdminOfferPrice(offerId, input, version),
      saveInventory: (input, version) => writeAdminOfferInventory(offerId, input, version),
      saveFulfillment: (input, version) => assignAdminOfferFulfillment(offerId, input, version),
      loadShippingTypes: () => loadAdminShippingTypes(),
      onCreated: () => undefined,
      onCancel: () => router.push(localizePath("/admin/offers")),
    }),
    [offerId, router, localizePath, offerLocale.code],
  );

  return (
    <main className="mx-auto w-full max-w-6xl">
      <OfferWorkspace adapter={adapter} localeDefinition={offerLocale} messages={workspaceMessages} />
    </main>
  );
}
