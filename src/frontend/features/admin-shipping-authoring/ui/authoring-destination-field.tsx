"use client";

/**
 * ONE searchable destination field of the four fixed cascade fields (TB-227).
 *
 * Same public contract and markup idea as the existing admin searchable combobox
 * (`app/admin/admin-searchable-combobox.tsx`): a trigger button that opens a popup whose FIRST element is
 * the search input — the search is never a separate input outside the dropdown, it filters ONLY the rows
 * already loaded for the current verified branch, and the count line states how many of the branch's rows
 * are shown so an incomplete branch is never presented as a complete search.
 *
 * It is NOT a fork of that component for its own sake: its popup is absolutely positioned inside its
 * parent, which inside this modal (a native `dialog` with `overflow: hidden` plus an inner scroll
 * container) is clipped and, worse, changes the dialog's content height while the operator types. Here the
 * popup is portalled to `document.body` with fixed coordinates measured from the trigger, so it is never
 * clipped, never adds height to the dialog, flips above the field when there is no room below, and always
 * scrolls internally inside the viewport (360/390 included).
 *
 * The popup keeps the four field identities stable: opening/closing it never remounts a field, never
 * scrolls the page, and never moves the dialog.
 */
import { useCallback, useEffect, useId, useLayoutEffect, useMemo, useRef, useState, type KeyboardEvent as ReactKeyboardEvent } from "react";
import { createPortal } from "react-dom";
import type { GeographyOption } from "../api/geography-lookup-api.ts";
import { optionDisplayName } from "../model/destination-choice.ts";
import {
  destinationActiveId,
  destinationSearchCoverage,
  filterDestinationEntries,
  isDestinationSearchNavigationKey,
  moveDestinationActiveIndex,
  selectableDestinationIds,
  type DestinationSearchEntry,
} from "../model/destination-search.ts";

const PANEL_GAP = 4;
const VIEWPORT_MARGIN = 8;
/** Below this much room the popup prefers opening upwards. */
const MIN_BELOW = 168;
const PANEL_MAX_HEIGHT = 288;

type Placement = { top?: number; bottom?: number; left: number; width: number; maxHeight: number };

export interface DestinationFieldLabels {
  readonly label: string;
  readonly placeholder: string;
  readonly searchPlaceholder: string;
  readonly noResult: string;
  /** The branch itself has no recorded row at all (distinct from "this query matched nothing"). */
  readonly emptyBranch: string;
  readonly loading: string;
  readonly clearChoice: string;
  readonly count: (shown: number, total: number) => string;
  readonly unavailableName: string;
}

export function AuthoringDestinationField({
  labels,
  value,
  options,
  disabled,
  loading,
  testId,
  onChange,
}: {
  labels: DestinationFieldLabels;
  /** Selected identity, or "" for no selection. */
  value: string;
  /** ONLY the rows the current verified branch actually returned. */
  options: readonly GeographyOption[];
  disabled: boolean;
  /** The branch read for this field is in flight (the popup says so instead of pretending to be empty). */
  loading: boolean;
  testId: string;
  onChange: (option: GeographyOption | null) => void;
}) {
  const listId = useId();
  const labelId = useId();
  const triggerRef = useRef<HTMLButtonElement | null>(null);
  const panelRef = useRef<HTMLDivElement | null>(null);
  const searchRef = useRef<HTMLInputElement | null>(null);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [active, setActive] = useState(0);
  const [placement, setPlacement] = useState<Placement | null>(null);
  /**
   * The popup renders INSIDE the open native `dialog`, never on `document.body`: a modal dialog makes
   * everything outside it inert, so a body-level popup could be seen but neither focused nor clicked.
   * `position: fixed` still anchors it to the viewport (the dialog has no transform), so it is never
   * clipped by the dialog's `overflow: hidden` and never adds height to it.
   */
  const [container, setContainer] = useState<HTMLElement | null>(null);

  const labelled = useMemo<DestinationSearchEntry[]>(
    () => options.map((option) => ({ option, text: optionDisplayName(option, labels.unavailableName) })),
    [labels.unavailableName, options],
  );

  const selectedLabel = useMemo(() => {
    if (value === "") return "";
    return labelled.find((entry) => entry.option.id === value)?.text ?? "";
  }, [labelled, value]);

  const filtered = useMemo(() => filterDestinationEntries(labelled, query), [labelled, query]);
  const coverage = destinationSearchCoverage(filtered.length, options.length);

  const close = useCallback(
    (restoreFocus: boolean) => {
      setOpen(false);
      setQuery("");
      if (restoreFocus) triggerRef.current?.focus();
    },
    [],
  );

  const place = useCallback(() => {
    const trigger = triggerRef.current;
    if (!trigger) return;
    const rect = trigger.getBoundingClientRect();
    const below = window.innerHeight - rect.bottom - PANEL_GAP - VIEWPORT_MARGIN;
    const above = rect.top - PANEL_GAP - VIEWPORT_MARGIN;
    const openUp = below < MIN_BELOW && above > below;
    const room = openUp ? above : below;
    const width = Math.max(rect.width, 192);
    const left = Math.min(Math.max(rect.left, VIEWPORT_MARGIN), Math.max(VIEWPORT_MARGIN, window.innerWidth - width - VIEWPORT_MARGIN));
    setPlacement({
      left,
      width,
      maxHeight: Math.min(PANEL_MAX_HEIGHT, Math.max(96, room)),
      ...(openUp ? { bottom: window.innerHeight - rect.top + PANEL_GAP } : { top: rect.bottom + PANEL_GAP }),
    });
  }, []);

  // Measured before paint, so the popup is never drawn at a wrong place first.
  useLayoutEffect(() => {
    if (!open) {
      setPlacement(null);
      setContainer(null);
      return;
    }
    setContainer(triggerRef.current?.closest("dialog") ?? document.body);
    place();
  }, [open, place]);

  // The popup is mounted only after its placement exists, so the search input is focused on that commit:
  // typing immediately filters the list instead of going to the trigger button.
  useLayoutEffect(() => {
    if (!open || placement === null) return;
    searchRef.current?.focus();
  }, [open, placement]);

  useEffect(() => {
    if (!open) return;
    const onPointerDown = (event: MouseEvent) => {
      const target = event.target as Node;
      if (triggerRef.current?.contains(target) || panelRef.current?.contains(target)) return;
      setOpen(false);
      setQuery("");
    };
    const onReflow = () => place();
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        // The popup owns Escape: it closes the dropdown, never the whole destination dialog.
        event.preventDefault();
        event.stopPropagation();
        close(true);
      }
    };
    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onKey, true);
    window.addEventListener("scroll", onReflow, true);
    window.addEventListener("resize", onReflow);
    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onKey, true);
      window.removeEventListener("scroll", onReflow, true);
      window.removeEventListener("resize", onReflow);
    };
  }, [close, open, place]);

  useEffect(() => {
    setActive(0);
  }, [query, options]);

  const choose = useCallback(
    (option: GeographyOption) => {
      if (!option.isActive) return;
      onChange(option);
      close(true);
    },
    [close, onChange],
  );

  const openPopup = () => {
    if (disabled) return;
    setQuery("");
    setActive(0);
    setOpen(true);
  };

  const onSearchKeyDown = (event: ReactKeyboardEvent<HTMLInputElement>) => {
    const pressed = event.key;
    if (isDestinationSearchNavigationKey(pressed)) {
      event.preventDefault();
      setActive((current) => moveDestinationActiveIndex(current, selectableDestinationIds(filtered).length, pressed));
      return;
    }
    if (event.key === "Enter") {
      event.preventDefault();
      const id = destinationActiveId(filtered, active);
      const option = id === null ? undefined : options.find((candidate) => candidate.id === id);
      if (option) choose(option);
      return;
    }
    if (event.key === "Tab") {
      setOpen(false);
      setQuery("");
    }
  };

  const activeId = useMemo(() => {
    const id = destinationActiveId(filtered, active);
    return id === null ? undefined : `${listId}-option-${id}`;
  }, [active, filtered, listId]);

  return (
    <div className="grid gap-0.5">
      <span className="text-xs font-bold text-gray-700" id={labelId}>
        {labels.label}
      </span>
      <button
        type="button"
        ref={triggerRef}
        role="combobox"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={open ? listId : undefined}
        aria-labelledby={labelId}
        className="flex min-h-10 w-full items-center justify-between gap-2 rounded-lg border border-border bg-surface px-2 text-start text-sm text-gray-900 disabled:cursor-not-allowed disabled:opacity-60"
        disabled={disabled}
        data-testid={testId}
        onClick={() => (open ? close(false) : openPopup())}
      >
        <span className={selectedLabel === "" ? "truncate text-gray-500" : "truncate"}>{selectedLabel === "" ? labels.placeholder : selectedLabel}</span>
        <span aria-hidden className="shrink-0 text-gray-500">
          ▾
        </span>
      </button>

      {open && placement !== null && container !== null
        ? createPortal(
            <div
              ref={panelRef}
              className="fixed z-[var(--z-overlay)] flex flex-col overflow-hidden rounded-lg border border-border bg-surface shadow-lg"
              style={{ left: placement.left, width: placement.width, maxHeight: placement.maxHeight, ...(placement.top !== undefined ? { top: placement.top } : { bottom: placement.bottom }) }}
              data-testid={`${testId}-panel`}
            >
              <input
                ref={searchRef}
                className="min-h-9 w-full shrink-0 border-b border-border bg-surface px-2 text-sm outline-none"
                placeholder={labels.searchPlaceholder}
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                onKeyDown={onSearchKeyDown}
                role="combobox"
                aria-expanded
                aria-controls={listId}
                aria-activedescendant={activeId}
                aria-autocomplete="list"
                aria-label={labels.searchPlaceholder}
                data-testid={`${testId}-search`}
              />
              <ul id={listId} role="listbox" aria-label={labels.label} className="min-h-0 flex-1 overflow-y-auto py-1">
                {filtered.map((entry) => {
                  const optionId = `${listId}-option-${entry.option.id}`;
                  const chosen = entry.option.id === value;
                  const isActiveRow = activeId === optionId;
                  return (
                    <li key={entry.option.id} id={optionId} role="option" aria-selected={chosen} aria-disabled={!entry.option.isActive}>
                      <button
                        type="button"
                        tabIndex={-1}
                        className={`block w-full truncate px-2 py-2 text-start text-sm ${entry.option.isActive ? "hover:bg-secondary" : "cursor-not-allowed text-gray-400"} ${chosen ? "bg-secondary/60 font-medium" : ""} ${isActiveRow ? "outline outline-1 outline-primary" : ""}`}
                        data-testid={`${testId}-option-${entry.option.id}`}
                        disabled={!entry.option.isActive}
                        onClick={() => choose(entry.option)}
                      >
                        {entry.text}
                      </button>
                    </li>
                  );
                })}
                {filtered.length === 0 ? (
                  <li className="px-2 py-2 text-sm text-gray-500" data-testid={`${testId}-empty`}>
                    {loading ? labels.loading : options.length === 0 && query.trim() === "" ? labels.emptyBranch : labels.noResult}
                  </li>
                ) : null}
              </ul>
              <div className="flex shrink-0 items-center justify-between gap-2 border-t border-border px-2 py-1 text-[11px] text-gray-500">
                <span data-testid={`${testId}-count`}>{labels.count(coverage.shown, coverage.total)}</span>
                {value !== "" ? (
                  <button
                    type="button"
                    className="font-bold text-gray-700"
                    data-testid={`${testId}-clear`}
                    onClick={() => {
                      onChange(null);
                      close(true);
                    }}
                  >
                    {labels.clearChoice}
                  </button>
                ) : null}
              </div>
            </div>,
            container,
          )
        : null}
    </div>
  );
}
