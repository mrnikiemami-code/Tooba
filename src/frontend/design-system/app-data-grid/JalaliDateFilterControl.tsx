"use client";

import { Input, Select } from "../primitives/core";
import type { GridFilterValue } from "../data-grid/types";
import { filterOperatorLabelsFor } from "../data-grid/messages";
import { jalaliInputToIso } from "./jalali";

/** فیلتر تاریخ با ورودی جلالی → ISO برای API. */
export function JalaliDateFilterControl({
  header,
  value,
  onChange,
  locale = "fa",
}: {
  header: string;
  value?: GridFilterValue;
  onChange: (value: GridFilterValue) => void;
  locale?: "fa" | "en";
}) {
  const ops = filterOperatorLabelsFor(locale);
  const operator = value?.kind === "date" ? value.operator : "on";
  const iso = value?.kind === "date" ? value.iso : "";
  const isoTo = value?.kind === "date" ? value.isoTo : undefined;

  function jalaliDisplay(isoValue: string): string {
    if (!isoValue) return "";
    const d = new Date(isoValue);
    if (Number.isNaN(d.getTime())) return isoValue.slice(0, 10);
    return isoValue.slice(0, 10);
  }

  return (
    <label className="block text-sm">
      {header}
      <Select
        aria-label={`${header} — ${ops.on}`}
        value={operator}
        onChange={(event) =>
          onChange({
            kind: "date",
            operator: event.target.value as "on" | "before" | "after" | "between",
            iso: iso || new Date().toISOString(),
            isoTo,
          })
        }
      >
        <option value="on">{ops.on}</option>
        <option value="before">{ops.before}</option>
        <option value="after">{ops.after}</option>
        <option value="between">{ops.between}</option>
      </Select>
      <Input
        placeholder={locale === "fa" ? "۱۴۰۴/۰۱/۰۱" : "YYYY-MM-DD"}
        value={iso ? jalaliDisplay(iso) : ""}
        onChange={(event) => {
          const parsed = locale === "fa" ? jalaliInputToIso(event.target.value) : event.target.value;
          onChange({
            kind: "date",
            operator,
            iso: parsed ?? event.target.value,
            isoTo,
          });
        }}
      />
      {operator === "between" ? (
        <Input
          placeholder={locale === "fa" ? "تا تاریخ" : "To date"}
          aria-label={`${header} — ${ops.between}`}
          value={isoTo ? jalaliDisplay(isoTo) : ""}
          onChange={(event) => {
            const parsed = locale === "fa" ? jalaliInputToIso(event.target.value) : event.target.value;
            onChange({
              kind: "date",
              operator,
              iso: iso || parsed || new Date().toISOString(),
              isoTo: parsed ?? event.target.value,
            });
          }}
        />
      ) : null}
    </label>
  );
}
