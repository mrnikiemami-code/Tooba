"use client";

/**
 * Local capability of the ONE delivery range of one delivery AREA (TB-224).
 *
 * It owns the operator's latest STAGED pair per area (needed when an area has no rows yet, so its first
 * row can inherit it) and applies a lawful pair to exactly that area's rows. The typed text itself lives
 * in the area control; this hook only keeps the value that may still have to be inherited.
 *
 * A fresh confirmed baseline (`seed`) forgets every staged pair, so a discard/save can never leave a range
 * behind that the rows do not carry.
 */
import { useCallback, useEffect, useRef } from "react";
import { applyAreaRange, areaRangeIssue, type AreaRangePair } from "../model/editor-area-range.ts";
import type { RatesFormState } from "../model/editor-rates.ts";

export interface AreaRangeEdits {
  /** Deliberate shared-range entry of one area. */
  change(groupKey: string, min: string, max: string): void;
  /** Last staged pair of one area (used only for inheriting a first row), or `null`. */
  staged(groupKey: string): AreaRangePair | null;
}

export function useAreaRangeEdits(input: {
  /** Local rows of every area. */
  readonly rates: RatesFormState;
  /** Writes the next rows of every area (the screen's ONE commit path). */
  readonly writeRates: (next: RatesFormState) => void;
  /** Handler-boundary guard: view/saving/reconciling must not mutate. */
  readonly blocked: () => boolean;
  /** Confirmed baseline identity; a new value forgets every staged pair. */
  readonly seed: unknown;
}): AreaRangeEdits {
  const staged = useRef<Record<string, AreaRangePair>>({});
  const { rates, writeRates, blocked, seed } = input;

  useEffect(() => {
    staged.current = {};
  }, [seed]);

  const change = useCallback(
    (groupKey: string, min: string, max: string) => {
      if (blocked()) return;
      // The pair stays staged even while it is partial, so the first row can inherit the real choice.
      staged.current = { ...staged.current, [groupKey]: { min, max } };
      const rows = rates[groupKey] ?? [];
      if (rows.length === 0) return;
      if (areaRangeIssue(min, max) !== null) return;
      const next = applyAreaRange(rows, min, max);
      if (next === rows) return;
      writeRates({ ...rates, [groupKey]: next });
    },
    [blocked, rates, writeRates],
  );

  const stagedRange = useCallback((groupKey: string) => staged.current[groupKey] ?? null, []);

  return { change, staged: stagedRange };
}
