"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { useRegistryLocale } from "../../../../lib/i18n/registry-locale";
import { offerWorkspaceMessagesForRegistry } from "../../../../lib/offer-workspace/messages-ru";
import { ErrorState, Skeleton } from "../../../../design-system";
import { useLocale, useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { OfferWorkspace, offerWorkspaceMessages } from "../../../../lib/offer-workspace";
import type { OfferTabId, OfferWorkspaceAdapter } from "../../../../lib/offer-workspace";
import { loadSellerReservationPolicy, readSellerPartyId } from "../../seller-api";
import {
  loadSellerOfferDetail,
  loadSellerOfferHistory,
  loadSellerProductVariants,
  patchSellerOffer,
  writeSellerOfferInventory,
  writeSellerOfferedQuantity,
  writeSellerOfferPrice,
} from "../seller-offer-api";
import { SellerProductAttributesPanel } from "../../../admin/catalog-attribute-ui";
import { ReservationPolicyEditor, draftFromView, type ReservationPolicyDraft } from "../../../admin/reservation-policy-editor.tsx";
import type { ReservationPolicyEditorView } from "../../../admin/reservation-policy-api";

export default function VendorProductDetailPage() {
  const params = useParams<{ offerId: string }>();
  const router = useRouter();
  const localizePath = useLocalizedPath();
  const registry = useRegistryLocale();
  const offerLocale = { code: registry.locale.code, culture: registry.locale.culture, language: registry.locale.language, direction: registry.locale.direction, calendar: registry.locale.calendar };
  const resourceLocale = useLocale();
  const workspaceMessages = offerWorkspaceMessagesForRegistry(registry.locale.language) ?? offerWorkspaceMessages[resourceLocale];
  const offerId = params?.offerId ?? "";
  const [sellerPartyId, setSellerPartyId] = useState<string | null>(null);
  const [reservation, setReservation] = useState<ReservationPolicyEditorView | null>(null);
  const [reservationDraft, setReservationDraft] = useState<ReservationPolicyDraft>({
    initial: "",
    retry: "",
    max: "",
    inheritInitial: true,
    inheritRetry: true,
    inheritMax: true,
  });

  useEffect(() => {
    setSellerPartyId(readSellerPartyId(window.location.search) ?? "");
  }, []);

  useEffect(() => {
    if (!sellerPartyId || !offerId) return;
    let active = true;
    void loadSellerReservationPolicy(sellerPartyId, offerId).then((policy) => {
      if (!active || !policy.ok) return;
      setReservation(policy.data);
      setReservationDraft(draftFromView(policy.data));
    });
    return () => {
      active = false;
    };
  }, [sellerPartyId, offerId]);

  const adapter = useMemo<OfferWorkspaceAdapter | null>(() => {
    if (!sellerPartyId) return null;
    return {
      role: "seller",
      mode: "edit",
      offerId,
      loadHistory: (page, pageSize) => loadSellerOfferHistory(sellerPartyId, offerId, page, pageSize),
      loadDetail: async () => {
        const result = await loadSellerOfferDetail(sellerPartyId, offerId);
        return result.detail
          ? { ok: true, detail: result.detail }
          : { ok: false, errorCode: result.message ?? "seller.offer.missing", denied: result.denied };
      },
      searchProducts: async () => ({ rows: [], totalCount: 0, page: 1, pageSize: 10, hasMore: false }),
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
      createDraft: async () => ({ ok: false, errorCode: "offer.workspace.edit_mode" }),
      saveCommercial: (patch, version) => patchSellerOffer(sellerPartyId, offerId, patch, version),
      savePrice: (input, version) => writeSellerOfferPrice(sellerPartyId, offerId, input, version),
      saveInventory: (input, version) => writeSellerOfferInventory(sellerPartyId, offerId, input, version),
      saveOfferedQuantity: (quantity, version, operationId) => writeSellerOfferedQuantity(sellerPartyId, offerId, quantity, version, operationId),
      onCreated: () => undefined,
      onCancel: () => router.push(localizePath("/vendor-panel/products")),
    };
  }, [sellerPartyId, offerId, router, localizePath, offerLocale.code]);

  function renderExtras(tab: OfferTabId, detail: { productId: string | null }): ReactNode {
    if (tab === "identity" && detail.productId) {
      return (
        <details className="rounded-ds border border-border bg-surface" data-testid="seller-product-attributes">
          <summary className="cursor-pointer px-4 py-3 text-sm font-semibold">{workspaceMessages.productAdditionalInformation}</summary>
          <div className="border-t border-border p-3">
            <SellerProductAttributesPanel productId={detail.productId} />
          </div>
        </details>
      );
    }
    if (tab === "conditions" && reservation) {
      return (
        <section className="rounded-ds border border-border p-3" data-testid="seller-reservation-policy-readonly">
          <ReservationPolicyEditor
            view={reservation}
            draft={reservationDraft}
            onDraftChange={setReservationDraft}
            canEdit={false}
            locale={resourceLocale}
          />
        </section>
      );
    }
    return null;
  }

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
          title={workspaceMessages.denied}
          detail={workspaceMessages.sellerSessionHint}
          onRetry={() => window.location.reload()}
          retryLabel={workspaceMessages.retry}
        />
      </main>
    );
  }
  if (!adapter) return null;

  return (
    <main className="mx-auto w-full max-w-6xl">
      <OfferWorkspace adapter={adapter} renderExtras={renderExtras} localeDefinition={offerLocale} messages={workspaceMessages} />
    </main>
  );
}
