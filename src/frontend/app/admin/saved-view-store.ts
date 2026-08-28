import { adminHeaders } from "./admin-api";
import type { SavedGridView, SavedViewStore } from "../../design-system/data-grid";

/** کلیدهای ترجیح UI برای نمایش‌های ذخیره‌شدهٔ Admin. */
export const ADMIN_PRODUCT_GRID_VIEW_KEY = "grid.admin.products";
export const ADMIN_ORDER_GRID_VIEW_KEY = "grid.admin.orders";

type UiPreferencePayload = {
  views?: SavedGridView[];
};

/**
 * آداپتر SavedViewStore که نمایش‌ها را در `/v1/admin/ui-preferences/{key}` نگه می‌دارد.
 * شکست شبکه را خاموش می‌بلعد تا گرید بدون persistence هم کار کند.
 */
export function createHostSavedViewStore(preferenceKey: string): SavedViewStore {
  let cache: SavedGridView[] | null = null;

  async function readViews(): Promise<SavedGridView[]> {
    if (cache) {
      return cache.map(cloneView);
    }
    try {
      const response = await fetch(`/v1/admin/ui-preferences/${encodeURIComponent(preferenceKey)}`, {
        headers: adminHeaders(),
      });
      if (!response.ok) {
        cache = [];
        return [];
      }
      const body = (await response.json()) as { json?: UiPreferencePayload | null };
      const views = Array.isArray(body.json?.views) ? body.json!.views! : [];
      cache = views.map(cloneView);
      return cache.map(cloneView);
    } catch {
      cache = [];
      return [];
    }
  }

  async function writeViews(views: SavedGridView[]): Promise<void> {
    cache = views.map(cloneView);
    try {
      await fetch(`/v1/admin/ui-preferences/${encodeURIComponent(preferenceKey)}`, {
        method: "PUT",
        headers: adminHeaders({ "Content-Type": "application/json" }),
        body: JSON.stringify({ json: { views: cache } }),
      });
    } catch {
      // گرید باید بدون Host هم بماند؛ persistence بعداً دوباره تلاش می‌شود.
    }
  }

  return {
    async list() {
      return readViews();
    },
    async save(view) {
      const views = await readViews();
      const next = views.filter((item) => item.id !== view.id);
      next.push(cloneView(view));
      await writeViews(next);
    },
    async remove(id) {
      const views = await readViews();
      await writeViews(views.filter((item) => item.id !== id));
    },
  };
}

function cloneView(view: SavedGridView): SavedGridView {
  return JSON.parse(JSON.stringify(view)) as SavedGridView;
}
