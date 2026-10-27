/**
 * Search + keyboard selection of ONE destination field's dropdown (TB-227).
 *
 * Two honest limits are encoded here rather than hidden in the component:
 *
 * - the dropdown filters ONLY the rows already loaded for the current verified branch — the Geography
 *   routes return the whole branch array (no pagination), so `{shown} of {total}` is the operator's proof
 *   that no row was silently left out of the search;
 * - an INACTIVE place can never be chosen, so it stays visible (with its unavailable suffix) but is not
 *   part of the keyboard/Enter sequence.
 *
 * Framework-free so the real filter/keyboard transitions are executed by tests.
 */
import type { GeographyOption } from "../api/geography-lookup-api.ts";

/** One row of the loaded branch: identity plus the display text the operator actually reads. */
export interface DestinationSearchEntry {
  readonly option: GeographyOption;
  readonly text: string;
}

const NAVIGATION_KEYS = ["ArrowDown", "ArrowUp", "Home", "End"] as const;
export type DestinationSearchNavigationKey = (typeof NAVIGATION_KEYS)[number];

export function isDestinationSearchNavigationKey(value: string): value is DestinationSearchNavigationKey {
  return (NAVIGATION_KEYS as readonly string[]).includes(value);
}

/** Case-insensitive match on the DISPLAYED label and on the real code; never a fuzzy/guess match. */
export function filterDestinationEntries(
  entries: readonly DestinationSearchEntry[],
  query: string,
): readonly DestinationSearchEntry[] {
  const needle = query.trim().toLowerCase();
  if (needle.length === 0) return entries;
  return entries.filter(
    (entry) => entry.text.toLowerCase().includes(needle) || entry.option.code.toLowerCase().includes(needle),
  );
}

/** The rows a keyboard/Enter sequence may land on, in display order (inactive places excluded). */
export function selectableDestinationIds(entries: readonly DestinationSearchEntry[]): readonly string[] {
  return entries.filter((entry) => entry.option.isActive).map((entry) => entry.option.id);
}

/**
 * Next active index for one navigation key, clamped to the current filtered set and wrapping on the
 * arrow keys (a filtered list of one row keeps index 0, an empty list keeps 0 so Enter stays a no-op).
 */
export function moveDestinationActiveIndex(
  current: number,
  count: number,
  key: DestinationSearchNavigationKey,
): number {
  if (count <= 0) return 0;
  const base = Math.min(Math.max(current, 0), count - 1);
  switch (key) {
    case "ArrowDown":
      return (base + 1) % count;
    case "ArrowUp":
      return (base - 1 + count) % count;
    case "Home":
      return 0;
    case "End":
      return count - 1;
  }
}

/** The identity Enter would choose for a given active index, or null when nothing is selectable. */
export function destinationActiveId(entries: readonly DestinationSearchEntry[], active: number): string | null {
  const ids = selectableDestinationIds(entries);
  if (ids.length === 0) return null;
  return ids[Math.min(Math.max(active, 0), ids.length - 1)];
}

/**
 * The honest coverage line of the popup. `total` is the number of rows the branch actually returned, so
 * filtering never claims to have searched rows that were not loaded.
 */
export function destinationSearchCoverage(shown: number, total: number): { shown: number; total: number; complete: boolean } {
  return { shown, total, complete: shown === total };
}
