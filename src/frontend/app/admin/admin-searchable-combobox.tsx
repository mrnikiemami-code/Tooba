"use client";

import { useEffect, useId, useMemo, useRef, useState } from "react";

export type AdminComboboxOption = {
  value: string;
  label: string;
};

/**
 * Combobox جستجو‌پذیر یکپارچه Admin — جستجو داخل dropdown، بدون textbox جدا.
 */
export function AdminSearchableCombobox({
  value,
  options,
  onChange,
  noneOption,
  placeholder = "جستجو و انتخاب…",
  disabled = false,
  testId,
  emptyLabel = "موردی یافت نشد",
}: {
  value: string | null;
  options: AdminComboboxOption[];
  onChange: (next: string | null) => void;
  noneOption?: { value: ""; label: string };
  placeholder?: string;
  disabled?: boolean;
  testId?: string;
  emptyLabel?: string;
}) {
  const listId = useId();
  const rootRef = useRef<HTMLDivElement | null>(null);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");

  const selectedLabel = useMemo(() => {
    if (!value) return noneOption?.label ?? "";
    return options.find((o) => o.value === value)?.label ?? value;
  }, [noneOption?.label, options, value]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    const base = options.filter((o) => (q ? o.label.toLowerCase().includes(q) : true));
    return base;
  }, [options, query]);

  useEffect(() => {
    if (!open) return;
    function onDoc(ev: MouseEvent) {
      if (!rootRef.current?.contains(ev.target as Node)) {
        setOpen(false);
        setQuery("");
      }
    }
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [open]);

  return (
    <div className="relative" ref={rootRef} data-testid={testId}>
      <button
        type="button"
        disabled={disabled}
        className="flex min-h-11 w-full items-center justify-between gap-2 rounded-ds border border-border bg-surface px-3 text-start text-base disabled:opacity-50"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={listId}
        data-testid={testId ? `${testId}-trigger` : undefined}
        onClick={() => {
          if (disabled) return;
          setOpen((v) => !v);
          setQuery("");
        }}
      >
        <span className={selectedLabel ? "text-foreground" : "text-muted"}>
          {selectedLabel || placeholder}
        </span>
        <span className="text-muted" aria-hidden>
          ▾
        </span>
      </button>
      {open ? (
        <div
          className="absolute z-40 mt-1 w-full overflow-hidden rounded-ds border border-border bg-surface shadow-lg"
          data-testid={testId ? `${testId}-panel` : undefined}
        >
          <input
            autoFocus
            className="min-h-10 w-full border-b border-border bg-surface px-3 text-sm outline-none"
            placeholder={placeholder}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            data-testid={testId ? `${testId}-search` : undefined}
          />
          <ul id={listId} role="listbox" className="max-h-56 overflow-auto py-1">
            {noneOption ? (
              <li>
                <button
                  type="button"
                  role="option"
                  aria-selected={!value}
                  className={`block w-full px-3 py-2 text-start text-sm hover:bg-secondary ${!value ? "bg-secondary/60 font-medium" : ""}`}
                  onClick={() => {
                    onChange(null);
                    setOpen(false);
                    setQuery("");
                  }}
                  data-testid={testId ? `${testId}-none` : undefined}
                >
                  {noneOption.label}
                </button>
              </li>
            ) : null}
            {filtered.map((opt) => (
              <li key={opt.value}>
                <button
                  type="button"
                  role="option"
                  aria-selected={value === opt.value}
                  className={`block w-full px-3 py-2 text-start text-sm hover:bg-secondary ${value === opt.value ? "bg-secondary/60 font-medium" : ""}`}
                  onClick={() => {
                    onChange(opt.value);
                    setOpen(false);
                    setQuery("");
                  }}
                >
                  {opt.label}
                </button>
              </li>
            ))}
            {filtered.length === 0 ? (
              <li className="px-3 py-2 text-sm text-muted">{emptyLabel}</li>
            ) : null}
          </ul>
        </div>
      ) : null}
    </div>
  );
}
