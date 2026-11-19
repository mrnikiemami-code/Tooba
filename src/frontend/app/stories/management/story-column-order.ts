/**
 * Enforce Orders-style Story list: title first data column, actions last.
 * Preserves unrelated filters/sort/widths; only remaps column order/pinning.
 */

const TITLE_ID = "title";
const ACTIONS_ID = "actions";

export type StoryColumnOrderState = {
  columnOrder?: string[] | null;
  pinned?: Record<string, "left" | "right" | null | undefined> | null;
};

/** Required merchant column sequence constraint (ids only). */
export function normalizeStoryColumnOrder(order: string[] | null | undefined): string[] {
  const seen = new Set<string>();
  const rest: string[] = [];
  for (const id of order ?? []) {
    if (!id || seen.has(id) || id === TITLE_ID || id === ACTIONS_ID) continue;
    seen.add(id);
    rest.push(id);
  }
  return [TITLE_ID, ...rest, ACTIONS_ID];
}

/**
 * Drop title pin (Orders does not pin the first data column) and keep actions
 * end-pin to the AppDataGrid logical-end edge handled by the bridge.
 */
export function normalizeStoryColumnPins(
  pinned: Record<string, "left" | "right" | null | undefined> | null | undefined,
): Record<string, "left" | "right" | null> {
  const next: Record<string, "left" | "right" | null> = {};
  if (pinned) {
    for (const [key, value] of Object.entries(pinned)) {
      if (key === TITLE_ID) continue;
      if (key === ACTIONS_ID) continue;
      if (value === "left" || value === "right") next[key] = value;
    }
  }
  return next;
}

export function applyStoryListColumnConstraints<T extends StoryColumnOrderState>(state: T): T {
  return {
    ...state,
    columnOrder: normalizeStoryColumnOrder(state.columnOrder ?? undefined),
    pinned: normalizeStoryColumnPins(state.pinned ?? undefined),
  };
}
