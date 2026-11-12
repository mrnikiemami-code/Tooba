/**
 * TB-256-R1/TB-264-R1: the scheduling tabs are ordinary tabs, so their keyboard model is pure arithmetic
 * (no DOM, no state) and can be proven directly.
 *
 * Direction matters: in a right-to-left document `ArrowLeft` moves FORWARD in reading order and
 * `ArrowRight` moves BACKWARD, which is the opposite of the left-to-right mapping. `Home`/`End` always
 * mean the first/last tab.
 */
export const TIMING_TAB_IDS = ["customer", "carrier", "processing"] as const;

export type TimingTabId = (typeof TIMING_TAB_IDS)[number];

export type TimingTabKey = "ArrowLeft" | "ArrowRight" | "Home" | "End";

/** The keys this tablist owns; anything else must keep its default browser behaviour. */
export function isTimingTabKey(key: string): key is TimingTabKey {
  return key === "ArrowLeft" || key === "ArrowRight" || key === "Home" || key === "End";
}

/**
 * Index of the tab that receives focus for `key`, wrapping around, or `current` when the key is not
 * owned by the tablist / the tab count is not positive.
 */
export function nextTimingTabIndex(
  current: number,
  key: string,
  isRtl: boolean,
  count: number = TIMING_TAB_IDS.length,
): number {
  if (!isTimingTabKey(key) || count <= 0) return current;
  if (key === "Home") return 0;
  if (key === "End") return count - 1;

  const start = current >= 0 && current < count ? current : 0;
  const forward = key === "ArrowLeft" ? isRtl : !isRtl;
  const step = forward ? 1 : -1;
  return (start + step + count) % count;
}
