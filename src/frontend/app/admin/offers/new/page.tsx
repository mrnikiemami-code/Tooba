"use client";

import { useRouter } from "next/navigation";
import { useMemo } from "react";
import { useRegistryLocale } from "../../../../lib/i18n/registry-locale";
import { offerWorkspaceMessagesForRegistry } from "../../../../lib/offer-workspace/messages-ru";
import { useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { OfferWorkspace } from "../../../../lib/offer-workspace";
import type { OfferWorkspaceAdapter } from "../../../../lib/offer-workspace";
import { queryAdminSellersGrid } from "../../../../features/admin-sellers";
import {
  assignAdminOfferFulfillment,
  createAdminOffer,
  ensureAdminInventoryPosition,
  findAdminActiveOffer,
  loadAdminAllowedOfferChannels,
  loadAdminInventoryLocations,
  loadAdminOfferDetail,
  loadAdminShippingTypes,
  loadAdminProductVariants,
  patchAdminOffer,
  searchAdminCatalogProducts,
  writeAdminOfferInventory,
  writeAdminOfferPrice,
} from "../admin-offer-api";
import { adminOfferIntent, clearAdminOfferIntent } from "../offer-create-intent";
import { createAndApplyOffer } from "../../../../features/offer-create/offer-create-and-apply.ts";
import { offerIntentScope } from "../../../../lib/offer-workspace/owner-write-intent.ts";

const PAGE_SIZE = 10;

export default function AdminCreateOfferPage() {
  const router = useRouter();
  const localizePath = useLocalizedPath();
  const registry = useRegistryLocale();
  const offerLocale = { code: registry.locale.code, culture: registry.locale.culture, language: registry.locale.language, direction: registry.locale.direction, calendar: registry.locale.calendar };
  const workspaceMessages = offerWorkspaceMessagesForRegistry(registry.locale.language);

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
      findActiveOffer: async (sellerPartyId, catalogVariantId, channel) =>
        findAdminActiveOffer(sellerPartyId, catalogVariantId, channel),
      loadShippingTypes: () => loadAdminShippingTypes(),
      loadChannels: () => loadAdminAllowedOfferChannels(),
      loadInventoryLocations: (sellerId, responsibility) => responsibility === "Platform" ? loadAdminInventoryLocations() : sellerId ? loadAdminInventoryLocations(sellerId) : Promise.resolve([]),
      createAndSaveDraft: async (input) => {
        const scope = offerIntentScope("admin", input.sellerPartyId ?? "", input.catalogVariantId, input.channel);
        let requestId: string;
        try {
          requestId = adminOfferIntent(scope);
        } catch {
          return { ok: false, errorCode: "offer.create.intent_not_durable" };
        }
        return createAndApplyOffer({
          scope,
          create: async () => {
            const created = await createAdminOffer({
              sellerPartyId: input.sellerPartyId ?? "",
              catalogVariantId: input.catalogVariantId,
              sellerSku: input.sellerSku,
              channel: input.channel,
              returnPolicyChoice: input.returnPolicyChoice,
              customReturnWindowDays: input.customReturnWindowDays,
              minimumOrderQuantity: input.minimumOrderQuantity,
              maximumOrderQuantity: input.maximumOrderQuantity,
              requestId,
            });
            return created.ok
              ? { ok: true, offerId: created.detail.offerId }
              : { ok: false, errorCode: created.errorCode, denied: created.denied };
          },
          load: async (offerId) => {
            const result = await loadAdminOfferDetail(offerId);
            return result.ok ? { ok: true, detail: result.detail } : { ok: false, errorCode: result.errorCode };
          },
          savePrice: async (offerId, amount, currency, version, operationId) => {
            const result = await writeAdminOfferPrice(offerId, { amount, currency, operationId }, version);
            return result.ok ? { ok: true } : { ok: false, errorCode: result.errorCode };
          },
          saveConditions: async (offerId, value, version) => {
            const result = await patchAdminOffer(offerId, {
              sellerSku: value.sellerSku,
              status: "Draft",
              returnPolicyChoice: value.returnPolicyChoice,
              customReturnWindowDays: value.customReturnWindowDays,
              minimumOrderQuantity: value.minimumOrderQuantity,
              maximumOrderQuantity: value.maximumOrderQuantity,
            }, version);
            return result.ok ? { ok: true } : { ok: false, errorCode: result.errorCode };
          },
          provisionPosition: async (offerId, locationId) => {
            const result = await ensureAdminInventoryPosition(offerId, locationId);
            return result ? { ok: true, stockItemId: result.stockItemId } : { ok: false, errorCode: "inventory.provision.failed" };
          },
          saveInventory: async (offerId, stockItemId, onHand, reason, version, operationId) => {
            const result = await writeAdminOfferInventory(offerId, { stockItemId, onHand, reason, operationId }, version);
            return result.ok ? { ok: true } : { ok: false, errorCode: result.errorCode };
          },
          saveFulfillment: async (offerId, value, version) => {
            const result = await assignAdminOfferFulfillment(offerId, {
              responsibility: value.responsibility,
              shippingTypeId: value.shippingTypeId,
            }, version);
            return result.ok ? { ok: true } : { ok: false, errorCode: result.errorCode };
          },
          onCommitted: () => clearAdminOfferIntent(scope),
        }, input);
      },
      createDraft: async ({ sellerPartyId, catalogVariantId, sellerSku, channel }) => {
        if (!sellerPartyId) return { ok: false, errorCode: "offer.seller.required" };
        const scope = offerIntentScope("admin", sellerPartyId, catalogVariantId, channel ?? "");
        let requestId: string;
        try {
          requestId = adminOfferIntent(scope);
        } catch {
          return { ok: false, errorCode: "offer.create.intent_not_durable" };
        }
        const created = await createAdminOffer({ sellerPartyId, catalogVariantId, sellerSku, channel, requestId });
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
      <OfferWorkspace adapter={adapter} localeDefinition={offerLocale} messages={workspaceMessages} />
    </main>
  );
}
