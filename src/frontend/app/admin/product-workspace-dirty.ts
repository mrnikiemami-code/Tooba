/**
 * Registry for Product Workspace section-level unsaved edits.
 * Pure helper — React wiring lives in product-workspace-dirty-context.tsx.
 */

export type ProductWorkspaceDirtySection = {
  sectionId: string;
  isDirty: boolean;
  discard: () => void;
};

export type ProductWorkspaceDirtyRegistry = {
  register: (sectionId: string, entry: { isDirty: boolean; discard: () => void }) => void;
  unregister: (sectionId: string) => void;
  isAnyDirty: () => boolean;
  isSectionDirty: (sectionId: string) => boolean;
  discardAll: () => void;
  discardSection: (sectionId: string) => void;
  dirtySectionIds: () => Set<string>;
};

type DirtyEntry = { isDirty: boolean; discard: () => void };

/**
 * Creates an in-memory dirty-section registry for workspace navigation guards.
 */
export function createProductWorkspaceDirtyRegistry(
  onChange?: () => void,
): ProductWorkspaceDirtyRegistry {
  const entries = new Map<string, DirtyEntry>();

  return {
    register(sectionId, entry) {
      entries.set(sectionId, entry);
      onChange?.();
    },
    unregister(sectionId) {
      if (!entries.has(sectionId)) return;
      entries.delete(sectionId);
      onChange?.();
    },
    isAnyDirty() {
      for (const entry of entries.values()) {
        if (entry.isDirty) return true;
      }
      return false;
    },
    isSectionDirty(sectionId) {
      return entries.get(sectionId)?.isDirty ?? false;
    },
    discardAll() {
      for (const entry of [...entries.values()]) {
        if (entry.isDirty) entry.discard();
      }
    },
    discardSection(sectionId) {
      const entry = entries.get(sectionId);
      if (entry?.isDirty) entry.discard();
    },
    dirtySectionIds() {
      const ids = new Set<string>();
      for (const [sectionId, entry] of entries) {
        if (entry.isDirty) ids.add(sectionId);
      }
      return ids;
    },
  };
}
