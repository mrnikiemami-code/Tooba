"use client";

import { useParams, useRouter } from "next/navigation";
import { useMemo } from "react";
import { useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { OfferWorkspace } from "../../../../lib/offer-workspace";
import type { OfferWorkspaceAdapter } from "../../../../lib/offer-workspace";
import {
  assignAdminOfferFulfillment,
  loadAdminOfferDetail,
  loadAdminShippingTypes,
  patchAdminOffer,
  writeAdminOfferInventory,
  writeAdminOfferPrice,
} from "../../admin-offer-api";

export default function AdminOfferDetailPage() {
  const params = useParams<{ offerId: string }>();
  const router = useRouter();
  const localizePath = useLocalizedPath();
  const offerId = params?.offerId ?? "";

  const adapter = useMemo<OfferWorkspaceAdapter>(
    () => ({
      role: "admin",
      mode: "edit",
      offerId,
      loadDetail: async () => {
        const result = await loadAdminOfferDetail(offerId);
        return result.ok
          ? { ok: true, detail: result.detail }
          : { ok: false, errorCode: result.errorCode, denied: result.denied };
      },
      searchVariants: async () => ({ rows: [], totalCount: 0, page: 1, pageSize: 10, hasMore: false }),
      createDraft: async () => ({ ok: false, errorCode: "offer.workspace.edit_mode" }),
      saveCommercial: (patch) => patchAdminOffer(offerId, patch),
      savePrice: (input) => writeAdminOfferPrice(offerId, input),
      saveInventory: (input) => writeAdminOfferInventory(offerId, input),
      saveFulfillment: (input) => assignAdminOfferFulfillment(offerId, input),
      loadShippingTypes: () => loadAdminShippingTypes(),
      onCreated: () => undefined,
      onCancel: () => router.push(localizePath("/admin/offers")),
    }),
    [offerId, router, localizePath],
  );

  return (
    <main className="mx-auto w-full max-w-6xl">
      <OfferWorkspace adapter={adapter} />
    </main>
  );
}
