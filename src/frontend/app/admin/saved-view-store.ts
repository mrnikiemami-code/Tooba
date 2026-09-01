import { adminHeaders } from "./admin-api";
import type { SavedGridView, SavedViewStore } from "../../design-system/data-grid";
import { migrateSavedView } from "../../design-system/app-data-grid/saved-view-state";

/** کلیدهای ترجیح UI برای نمایش‌های ذخیره‌شدهٔ Admin. */
export const ADMIN_PRODUCT_GRID_VIEW_KEY = "grid.admin.products";
export const ADMIN_ORDER_GRID_VIEW_KEY = "grid.admin.orders";
export const ADMIN_FULFILLMENT_GRID_VIEW_KEY = "grid.admin.fulfillments";
export const ADMIN_RETURN_GRID_VIEW_KEY = "grid.admin.returns";
export const ADMIN_SELLER_GRID_VIEW_KEY = "grid.admin.sellers";
export const ADMIN_CUSTOMER_GRID_VIEW_KEY = "grid.admin.customers";
export const ADMIN_SETTLEMENT_GRID_VIEW_KEY = "grid.admin.settlement";
export const ADMIN_REVIEW_GRID_VIEW_KEY = "grid.admin.reviews";
export const ADMIN_PROMOTION_GRID_VIEW_KEY = "grid.admin.promotions";
export const ADMIN_PAYOUT_GRID_VIEW_KEY = "grid.admin.payouts";
export const ADMIN_RECEIPT_GRID_VIEW_KEY = "grid.admin.receipts";
export const ADMIN_CONTENT_GRID_VIEW_KEY = "grid.admin.content";
export const ADMIN_STORY_GRID_VIEW_KEY = "grid.admin.stories";
export const ADMIN_ATTRIBUTE_DEF_GRID_VIEW_KEY = "grid.admin.catalog.attributes";
export const ADMIN_CATEGORY_SCHEMA_GRID_VIEW_KEY = "grid.admin.catalog.category-schema";
export const ADMIN_GIFT_CARD_GRID_VIEW_KEY = "grid.admin.gift-cards";

export const SAVED_VIEW_COLLECTION_SCHEMA_VERSION = 1;

type UiPreferencePayload = {
  schemaVersion?: number;
  defaultViewId?: string | null;
  views?: SavedGridView[];
};

/**
 * آداپتر SavedViewStore که نمایش‌ها را در `/v1/admin/ui-preferences/{key}` نگه می‌دارد.
 * شکست شبکه را خاموش می‌بلعد تا گرید بدون persistence هم کار کند.
 */
export function createHostSavedViewStore(preferenceKey: string): SavedViewStore {
  let cache: UiPreferencePayload | null = null;

  async function readCollection(): Promise<UiPreferencePayload> {
    if (cache) {
      return {
        schemaVersion: cache.schemaVersion ?? SAVED_VIEW_COLLECTION_SCHEMA_VERSION,
        defaultViewId: cache.defaultViewId ?? null,
        views: (cache.views ?? []).map(cloneView),
      };
    }
    try {
      const response = await fetch(`/v1/admin/ui-preferences/${encodeURIComponent(preferenceKey)}`, {
        headers: adminHeaders(),
      });
      if (!response.ok) {
        cache = { schemaVersion: SAVED_VIEW_COLLECTION_SCHEMA_VERSION, defaultViewId: null, views: [] };
        return readCollection();
      }
      const body = (await response.json()) as { json?: UiPreferencePayload | null };
      const raw = body.json ?? {};
      const views = Array.isArray(raw.views) ? raw.views.map(cloneView) : [];
      cache = {
        schemaVersion: raw.schemaVersion ?? SAVED_VIEW_COLLECTION_SCHEMA_VERSION,
        defaultViewId: raw.defaultViewId ?? null,
        views,
      };
      return readCollection();
    } catch {
      cache = { schemaVersion: SAVED_VIEW_COLLECTION_SCHEMA_VERSION, defaultViewId: null, views: [] };
      return readCollection();
    }
  }

  async function writeCollection(next: UiPreferencePayload): Promise<void> {
    cache = {
      schemaVersion: SAVED_VIEW_COLLECTION_SCHEMA_VERSION,
      defaultViewId: next.defaultViewId ?? null,
      views: (next.views ?? []).map(cloneView),
    };
    try {
      await fetch(`/v1/admin/ui-preferences/${encodeURIComponent(preferenceKey)}`, {
        method: "PUT",
        headers: adminHeaders({ "Content-Type": "application/json" }),
        body: JSON.stringify({ json: cache }),
      });
    } catch {
      // گرید باید بدون Host هم بماند؛ persistence بعداً دوباره تلاش می‌شود.
    }
  }

  return {
    async list() {
      const collection = await readCollection();
      return collection.views ?? [];
    },
    async save(view) {
      const collection = await readCollection();
      const views = (collection.views ?? []).filter((item) => item.id !== view.id);
      views.push(cloneView(view));
      await writeCollection({ ...collection, views });
    },
    async remove(id) {
      const collection = await readCollection();
      const views = (collection.views ?? []).filter((item) => item.id !== id);
      const defaultViewId = collection.defaultViewId === id ? null : collection.defaultViewId ?? null;
      await writeCollection({ ...collection, views, defaultViewId });
    },
    async getDefaultViewId() {
      const collection = await readCollection();
      return collection.defaultViewId ?? null;
    },
    async setDefaultViewId(id) {
      const collection = await readCollection();
      await writeCollection({ ...collection, defaultViewId: id });
    },
  };
}

function cloneView(view: SavedGridView): SavedGridView {
  return migrateSavedView(JSON.parse(JSON.stringify(view)) as SavedGridView);
}
