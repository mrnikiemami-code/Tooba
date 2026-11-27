"use client";

import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { useRegistryLocale } from "../../../../lib/i18n/registry-locale";
import { offerWorkspaceMessagesForRegistry } from "../../../../lib/offer-workspace/messages-ru";
import { ErrorState, Skeleton } from "../../../../design-system";
import { useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { OfferWorkspace } from "../../../../lib/offer-workspace";
import type { OfferWorkspaceAdapter } from "../../../../lib/offer-workspace";
import { readSellerPartyId } from "../../seller-api";
import {
  createSellerOffer,
  ensureSellerInventoryPosition,
  findSellerActiveOffer,
  loadSellerAllowedOfferChannels,
  loadSellerInventoryLocations,
  loadSellerOfferDetail,
  loadSellerProductVariants,
  patchSellerOffer,
  searchSellerCatalogProducts,
  writeSellerOfferInventory,
  writeSellerOfferPrice,
} from "../seller-offer-api";
import { clearOfferIntent, readOrCreateOfferIntent } from "../offer-create-intent";
import { createAndApplyOffer } from "../../../../features/offer-create/offer-create-and-apply.ts";
import { offerIntentScope } from "../../../../lib/offer-workspace/owner-write-intent.ts";

const PAGE_SIZE = 10;

export default function VendorCreateOfferPage() {
  const router = useRouter();
  const localizePath = useLocalizedPath();
  const registry = useRegistryLocale();
  const offerLocale = { code: registry.locale.code, culture: registry.locale.culture, language: registry.locale.language, direction: registry.locale.direction, calendar: registry.locale.calendar };
  const workspaceMessages = offerWorkspaceMessagesForRegistry(registry.locale.language);
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
      findActiveOffer: async (_sellerId, catalogVariantId, channel) =>
        findSellerActiveOffer(sellerPartyId, catalogVariantId, channel),
      loadChannels: () => loadSellerAllowedOfferChannels(sellerPartyId),
      loadInventoryLocations: () => loadSellerInventoryLocations(sellerPartyId),
      createAndSaveDraft: async (input) => {
        const scope = offerIntentScope("seller", sellerPartyId, input.catalogVariantId, input.channel);
        let requestId: string;
        try {
          requestId = readOrCreateOfferIntent(scope);
        } catch {
          return { ok: false, errorCode: "offer.create.intent_not_durable" };
        }
        return createAndApplyOffer({
          scope,
          create: async () => {
            const created = await createSellerOffer(sellerPartyId, {
              catalogVariantId: input.catalogVariantId,
              sellerSku: input.sellerSku,
              channel: input.channel,
              returnPolicyChoice: input.returnPolicyChoice,
              customReturnWindowDays: input.customReturnWindowDays,
              minimumOrderQuantity: input.minimumOrderQuantity,
              maximumOrderQuantity: input.maximumOrderQuantity,
              status: "Draft",
              requestId,
            });
            return created.ok
              ? { ok: true, offerId: created.detail.offerId }
              : { ok: false, errorCode: created.errorCode, denied: created.denied };
          },
          load: async (offerId) => {
            const result = await loadSellerOfferDetail(sellerPartyId, offerId);
            return result.source === "host" && result.detail
              ? { ok: true, detail: result.detail }
              : { ok: false, errorCode: result.message ?? "host-unreachable" };
          },
          savePrice: async (offerId, amount, currency, version, operationId) => {
            const result = await writeSellerOfferPrice(sellerPartyId, offerId, { amount, currency, operationId }, version);
            return result.ok ? { ok: true } : { ok: false, errorCode: result.errorCode };
          },
          provisionPosition: async (offerId, locationId) => {
            const result = await ensureSellerInventoryPosition(sellerPartyId, offerId, locationId);
            return result ? { ok: true, stockItemId: result.stockItemId } : { ok: false, errorCode: "inventory.provision.failed" };
          },
          saveInventory: async (offerId, stockItemId, onHand, reason, version, operationId) => {
            const result = await writeSellerOfferInventory(sellerPartyId, offerId, { stockItemId, onHand, reason, operationId }, version);
            return result.ok ? { ok: true } : { ok: false, errorCode: result.errorCode };
          },
          saveConditions: async (offerId, value, version) => {
            const result = await patchSellerOffer(sellerPartyId, offerId, {
              sellerSku: value.sellerSku,
              status: "Draft",
              returnPolicyChoice: value.returnPolicyChoice,
              customReturnWindowDays: value.customReturnWindowDays,
              minimumOrderQuantity: value.minimumOrderQuantity,
              maximumOrderQuantity: value.maximumOrderQuantity,
            }, version);
            return result.ok ? { ok: true } : { ok: false, errorCode: result.errorCode };
          },
          onCommitted: () => clearOfferIntent(scope),
        }, input);
      },
      createDraft: async ({ catalogVariantId, sellerSku, channel }) => {
        const scope = offerIntentScope("seller", sellerPartyId, catalogVariantId, channel ?? "");
        let requestId: string;
        try {
          requestId = readOrCreateOfferIntent(scope);
        } catch {
          return { ok: false, errorCode: "offer.create.intent_not_durable" };
        }
        const created = await createSellerOffer(sellerPartyId, {
          catalogVariantId,
          sellerSku,
          channel,
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
      <OfferWorkspace adapter={adapter} localeDefinition={offerLocale} messages={workspaceMessages} />
    </main>
  );
}
