import type { SavedGridView, SavedViewStore } from "../../../design-system/data-grid";

/**
 * Seller/admin Story grid saved views scoped by actor+role+shop in localStorage.
 * Seller must NOT call admin ui-preferences.
 */
export function createScopedStorySavedViewStore(args: {
  role: "admin" | "seller";
  actorUserId: string;
  shopId: string | null;
  gridKey: string;
}): SavedViewStore {
  const storageKey = `tooba.story.views.v1:${args.role}:${args.actorUserId || "anon"}:${args.shopId ?? "platform"}:${args.gridKey}`;

  function readAll(): SavedGridView[] {
    if (typeof localStorage === "undefined") return [];
    try {
      const raw = localStorage.getItem(storageKey);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as SavedGridView[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      throw new Error("story.saved_views.unreadable");
    }
  }

  function writeAll(views: SavedGridView[]): void {
    if (typeof localStorage === "undefined") throw new Error("story.saved_views.unavailable");
    localStorage.setItem(storageKey, JSON.stringify(views));
  }

  return {
    async list() {
      return readAll();
    },
    async save(view: SavedGridView) {
      const all = readAll().filter((row) => row.id !== view.id);
      all.push(view);
      writeAll(all);
    },
    async remove(id: string) {
      writeAll(readAll().filter((row) => row.id !== id));
    },
  };
}
