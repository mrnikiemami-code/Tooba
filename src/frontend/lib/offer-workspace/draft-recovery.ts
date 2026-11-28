import type { OfferWorkspaceDraft } from "./model.ts";
import type { OfferWorkspaceAdapter, OfferWorkspaceDetail, OfferSellerOption, OfferProductOption, OfferVariantOption, OfferLocalInventoryDraft, OfferFulfillmentResponsibility } from "./types.ts";
export type PersistedOfferWorkspace = {
  draft?: Partial<OfferWorkspaceDraft>;
  selectedSeller?: OfferSellerOption | null;
  selectedProduct?: OfferProductOption | null;
  selectedVariant?: OfferVariantOption | null;
  responsibility?: OfferFulfillmentResponsibility | null;
  shippingTypeId?: string;
  selectedChannel?: string;
  localInventory?: OfferLocalInventoryDraft[];
  baseVersion?: string;
};

type DraftScope = Pick<OfferWorkspaceAdapter, "role" | "fixedSeller">;

function scope(adapter: DraftScope) {
  if (typeof window === "undefined") return null;
  try {
    const actor = window.localStorage.getItem(adapter.role === "admin" ? "tooba.adminActorUserId" : "tooba.sellerActorUserId");
    if (!actor) return null;
    const tenant = window.localStorage.getItem("tooba.tenantId") ?? "";
    return "tooba:offer-workspace:" + JSON.stringify([tenant, actor, adapter.role, adapter.fixedSeller?.sellerPartyId ?? ""]) + ":";
  } catch { return null; }
}
export function draftStorageKey(adapter: OfferWorkspaceAdapter, detail: OfferWorkspaceDetail | null, seller: OfferSellerOption | null, variant: OfferVariantOption | null, channel: string) {
  const prefix = scope(adapter);
  return prefix ? prefix + JSON.stringify([adapter.mode, detail?.sellerPartyId ?? seller?.sellerPartyId ?? adapter.fixedSeller?.sellerPartyId ?? "", detail?.catalogVariantId ?? variant?.catalogVariantId ?? "", detail?.channel ?? channel, adapter.offerId ?? "new"]) : null;
}
export function draftVersion(detail: OfferWorkspaceDetail) {
  return JSON.stringify([detail.offerRevision, detail.priceRevision, detail.allocationRevision, detail.revisions, (detail.prices ?? []).map(row => [row.currency, row.revision]).sort(), detail.inventoryLocations.map(row => [row.stockItemId, row.revision]).sort()]);
}
export function readDraft(key: string | null, adapter: OfferWorkspaceAdapter): PersistedOfferWorkspace | null {
  const prefix = scope(adapter);
  if (!key || !prefix) return null;
  try {
    const storage = window.sessionStorage;
    const pointer = adapter.mode === "create" ? storage.getItem(prefix + "latest-create") : null;
    const raw = storage.getItem(pointer?.startsWith(prefix) ? pointer : key);
    return raw ? JSON.parse(raw) as PersistedOfferWorkspace : null;
  } catch { return null; }
}
export function writeDraft(key: string | null, adapter: OfferWorkspaceAdapter, draft: PersistedOfferWorkspace) {
  const prefix = scope(adapter);
  if (!key || !prefix) return;
  try {
    const storage = window.sessionStorage;
    storage.setItem(key, JSON.stringify(draft));
    if (adapter.mode === "create") {
      const previous = storage.getItem(prefix + "latest-create");
      if (previous && previous !== key && previous.startsWith(prefix)) storage.removeItem(previous);
      storage.setItem(prefix + "latest-create", key);
    }
  } catch { /* Optional local draft recovery, never a domain write. */ }
}
export function discardDraft(key: string | null, adapter: OfferWorkspaceAdapter) {
  const prefix = scope(adapter);
  if (!key || !prefix) return;
  try {
    const storage = window.sessionStorage;
    storage.removeItem(key);
    if (adapter.mode === "create" && storage.getItem(prefix + "latest-create") === key) storage.removeItem(prefix + "latest-create");
  } catch { /* Storage may be disabled. */ }
}

/** Explicit New starts a new form; reload or language navigation does not call this. */
export function discardCreateDrafts(adapter: DraftScope) {
  const prefix = scope(adapter);
  if (!prefix) return;
  try {
    const storage = window.sessionStorage;
    const keys = Array.from({ length: storage.length }, (_, i) => storage.key(i));
    for (const key of keys) {
      if (key === prefix + "latest-create" || key?.startsWith(prefix + '["create",')) storage.removeItem(key);
    }
  } catch { /* Optional local recovery. */ }
}
