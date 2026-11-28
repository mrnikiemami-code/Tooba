"use client";

import { useEffect, useId, useRef, useState } from "react";
import { ChevronDown, Search } from "lucide-react";
import { cn } from "../cn";

export interface SearchableSelectOption {
  value: string;
  label: string;
  searchText?: string;
}

/** Local searchable selection for an already authorized bounded option list. */
export function SearchableSelect({ options, value, onChange, label, placeholder, searchLabel, emptyLabel, disabled, testId, fitViewport = false }: {
  options: SearchableSelectOption[];
  value: string;
  onChange: (value: string) => void;
  label: string;
  placeholder: string;
  searchLabel: string;
  emptyLabel: string;
  disabled?: boolean;
  testId?: string;
  fitViewport?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [active, setActive] = useState(0);
  const [placement, setPlacement] = useState<{ above: boolean; maxHeight: number } | null>(null);
  const trigger = useRef<HTMLButtonElement>(null);
  const input = useRef<HTMLInputElement>(null);
  const id = useId();
  const filtered = options.filter(row => (row.searchText ?? row.label).toLocaleLowerCase().includes(query.trim().toLocaleLowerCase()));
  const selected = options.find(row => row.value === value);
  const current = Math.min(active, Math.max(0, filtered.length - 1));
  function close(focus = false) { setOpen(false); setQuery(""); setActive(0); setPlacement(null); if (focus) trigger.current?.focus(); }
  function measurePlacement() {
    if (!fitViewport || !trigger.current) return;
    const rect = trigger.current.getBoundingClientRect();
    const viewportHeight = window.visualViewport?.height ?? window.innerHeight;
    const viewportTop = window.visualViewport?.offsetTop ?? 0;
    const above = Math.max(0, rect.top - viewportTop - 12);
    const below = Math.max(0, viewportTop + viewportHeight - rect.bottom - 12);
    const flip = below < 310 && above > below;
    const maxHeight = Math.max(0, Math.min(310, flip ? above : below));
    setPlacement(previous => previous?.above === flip && previous.maxHeight === maxHeight ? previous : { above: flip, maxHeight });
  }
  function show() { measurePlacement(); setOpen(true); }
  function choose(next: string) { onChange(next); close(true); }
  useEffect(() => { if (open) input.current?.focus(); }, [open]);
  useEffect(() => { if (disabled) setOpen(false); }, [disabled]);
  useEffect(() => {
    if (!open || !fitViewport) return;
    measurePlacement();
    window.addEventListener("resize", measurePlacement);
    window.addEventListener("scroll", measurePlacement, true);
    window.visualViewport?.addEventListener("resize", measurePlacement);
    window.visualViewport?.addEventListener("scroll", measurePlacement);
    return () => {
      window.removeEventListener("resize", measurePlacement);
      window.removeEventListener("scroll", measurePlacement, true);
      window.visualViewport?.removeEventListener("resize", measurePlacement);
      window.visualViewport?.removeEventListener("scroll", measurePlacement);
    };
  }, [open, fitViewport]);
  return (
    <div className="relative min-w-0 w-full" onBlur={event => { if (!event.currentTarget.contains(event.relatedTarget as Node | null)) close(); }}>
      <button ref={trigger} type="button" disabled={disabled} aria-label={label} aria-haspopup="listbox" aria-expanded={open} aria-controls={open ? id : undefined}
        className="flex min-h-11 w-full min-w-0 items-center justify-between gap-2 rounded-ds border border-border bg-surface px-3 text-start text-sm disabled:opacity-50"
        data-testid={testId} onClick={() => open ? close() : show()}
        onKeyDown={event => { if (event.key === "ArrowDown") { event.preventDefault(); show(); } }}>
        <span className="min-w-0 break-words">{selected?.label ?? placeholder}</span>
        <ChevronDown size={16} className="shrink-0" aria-hidden />
      </button>
      {open && !disabled ? (
        <div className={cn("absolute inset-x-0 z-[var(--z-overlay)] min-w-0 rounded-ds border border-border bg-surface-elevated p-2 shadow-ds",
            placement?.above ? "bottom-full mb-1" : "top-full mt-1",
            fitViewport && "flex flex-col overflow-y-auto")}
          style={fitViewport && placement ? { maxHeight: placement.maxHeight } : undefined}
          data-testid={testId ? testId + "-popup" : undefined}
          onKeyDown={event => {
            if (event.key === "Escape") { event.preventDefault(); event.stopPropagation(); close(true); }
            if (event.key === "ArrowDown" || event.key === "ArrowUp") { event.preventDefault(); setActive(Math.max(0, Math.min(filtered.length - 1, current + (event.key === "ArrowDown" ? 1 : -1)))); }
            if (event.key === "Enter" && event.target === input.current && filtered[current]) { event.preventDefault(); choose(filtered[current].value); }
          }}>
          <div className="relative mb-2 shrink-0">
            <Search size={15} aria-hidden className="pointer-events-none absolute start-3 top-1/2 -translate-y-1/2" />
            <input ref={input} type="search" role="combobox" aria-expanded="true" aria-controls={id}
              aria-activedescendant={filtered[current] ? id + "-" + current : undefined} aria-label={searchLabel} placeholder={searchLabel}
              className="min-h-11 w-full rounded-ds border border-border bg-surface ps-9 pe-2 text-sm"
              value={query} onChange={event => { setQuery(event.target.value); setActive(0); }} data-testid={testId ? testId + "-search" : undefined} />
          </div>
          <div id={id} role="listbox" aria-label={label} className={cn("max-h-60 overflow-y-auto", fitViewport && "min-h-0 flex-1")}>
            {filtered.map((row, i) => <button type="button" role="option" aria-selected={row.value === value} id={id + "-" + i} key={row.value}
              data-testid={testId ? testId + "-option" : undefined} data-value={row.value}
              className={cn("block w-full min-h-11 rounded-ds px-3 py-2 text-start text-sm break-words", i === current && "bg-secondary", row.value === value && "font-semibold")}
              onMouseEnter={() => setActive(i)} onClick={() => choose(row.value)}>{row.label}</button>)}
            {!filtered.length ? <div className="px-3 py-2 text-sm text-muted" role="status">{emptyLabel}</div> : null}
          </div>
        </div>
      ) : null}
    </div>
  );
}
