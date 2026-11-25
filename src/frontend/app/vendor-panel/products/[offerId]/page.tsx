"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { ErrorState, Skeleton } from "../../../../design-system";
import { useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { OfferWorkspace } from "../../../../lib/offer-workspace";
import type { OfferTabId, OfferWorkspaceAdapter } from "../../../../lib/offer-workspace";
import {
  loadSellerOfferDetail,
  loadSellerReservationPolicy,
  patchSellerOffer,
  readSellerPartyId,
  writeSellerOfferInventory,
  writeSellerOfferPrice,
} from "../../seller-api";
import { SellerProductAttributesPanel } from "../../../admin/catalog-attribute-ui";
import { ReservationPolicyEditor, draftFromView, type ReservationPolicyDraft } from "../../../admin/reservation-policy-editor.tsx";
import type { ReservationPolicyEditorView } from "../../../admin/reservation-policy-api";

export default function VendorProductDetailPage() {
  const params = useParams<{ offerId: string }>();
  const router = useRouter();
  const localizePath = useLocalizedPath();
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
      loadDetail: async () => {
        const result = await loadSellerOfferDetail(sellerPartyId, offerId);
        return result.detail
          ? { ok: true, detail: result.detail }
          : { ok: false, errorCode: result.message ?? "seller.offer.missing", denied: result.denied };
      },
      searchVariants: async () => ({ rows: [], totalCount: 0, page: 1, pageSize: 10, hasMore: false }),
      createDraft: async () => ({ ok: false, errorCode: "offer.workspace.edit_mode" }),
      saveCommercial: (patch) => patchSellerOffer(sellerPartyId, offerId, patch),
      savePrice: (input) => writeSellerOfferPrice(sellerPartyId, offerId, input),
      saveInventory: (input) => writeSellerOfferInventory(sellerPartyId, offerId, input),
      onCreated: () => undefined,
      onCancel: () => router.push(localizePath("/vendor-panel/products")),
    };
  }, [sellerPartyId, offerId, router, localizePath]);

  function renderExtras(tab: OfferTabId, detail: { productId: string | null }): ReactNode {
    if (tab === "identity" && detail.productId) {
      return <SellerProductAttributesPanel productId={detail.productId} />;
    }
    if (tab === "conditions" && reservation) {
      return (
        <section className="rounded-ds border border-border p-3" data-testid="seller-reservation-policy-readonly">
          <ReservationPolicyEditor
            view={reservation}
            draft={reservationDraft}
            onDraftChange={setReservationDraft}
            canEdit={false}
            locale="fa"
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
      <OfferWorkspace adapter={adapter} renderExtras={renderExtras} />
    </main>
  );
}
