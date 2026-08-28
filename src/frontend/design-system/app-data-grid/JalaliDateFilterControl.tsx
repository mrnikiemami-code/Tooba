"use client";

import { Select } from "../primitives/core";
import type { GridFilterValue } from "../data-grid/types";
import { filterOperatorLabelsFor } from "../data-grid/messages";
import { JalaliDatePicker } from "./jalali-date-picker";

/** فیلتر تاریخ با تقویم/ورودی جلالی → ISO برای API. */
export function JalaliDateFilterControl({
  header,
  value,
  onChange,
  locale = "fa",
  compact = false,
}: {
  header: string;
  value?: GridFilterValue;
  onChange: (value: GridFilterValue) => void;
  locale?: "fa" | "en";
  /** بدون برچسب/عملگر تکراری — برای کارت فیلتر پیشرفته. */
  compact?: boolean;
}) {
  const ops = filterOperatorLabelsFor(locale);
  const operator = value?.kind === "date" ? value.operator : "on";
  const iso = value?.kind === "date" ? value.iso : "";
  const isoTo = value?.kind === "date" ? value.isoTo : undefined;

  if (compact) {
    if (operator === "between") {
      return (
        <div className="grid gap-2">
          <JalaliDatePicker
            ariaLabel={`${header} — ${locale === "fa" ? "از تاریخ" : "From"}`}
            locale={locale}
            value={iso || undefined}
            panelMinWidth={320}
            onChange={(next) =>
              onChange({
                kind: "date",
                operator,
                iso: next ?? iso ?? new Date().toISOString(),
                isoTo,
              })
            }
          />
          <JalaliDatePicker
            ariaLabel={`${header} — ${locale === "fa" ? "تا تاریخ" : "To"}`}
            locale={locale}
            value={isoTo || undefined}
            panelMinWidth={320}
            onChange={(next) =>
              onChange({
                kind: "date",
                operator,
                iso: iso || new Date().toISOString(),
                isoTo: next,
              })
            }
          />
        </div>
      );
    }
    return (
      <JalaliDatePicker
        ariaLabel={header}
        locale={locale}
        value={iso || undefined}
        panelMinWidth={320}
        onChange={(next) =>
          onChange({
            kind: "date",
            operator,
            iso: next ?? iso ?? new Date().toISOString(),
            isoTo,
          })
        }
      />
    );
  }

  return (
    <div className="grid gap-2 text-sm">
      <span className="font-medium">{header}</span>
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
      {operator === "between" ? (
        <div className="grid gap-2 sm:grid-cols-2">
          <label className="grid gap-1">
            <span className="text-xs text-muted">{locale === "fa" ? "از تاریخ" : "From"}</span>
            <JalaliDatePicker
              ariaLabel={`${header} — ${locale === "fa" ? "از تاریخ" : "From"}`}
              locale={locale}
              value={iso || undefined}
              panelMinWidth={320}
              onChange={(next) =>
                onChange({
                  kind: "date",
                  operator,
                  iso: next ?? iso ?? new Date().toISOString(),
                  isoTo,
                })
              }
            />
          </label>
          <label className="grid gap-1">
            <span className="text-xs text-muted">{locale === "fa" ? "تا تاریخ" : "To"}</span>
            <JalaliDatePicker
              ariaLabel={`${header} — ${locale === "fa" ? "تا تاریخ" : "To"}`}
              locale={locale}
              value={isoTo || undefined}
              panelMinWidth={320}
              onChange={(next) =>
                onChange({
                  kind: "date",
                  operator,
                  iso: iso || new Date().toISOString(),
                  isoTo: next,
                })
              }
            />
          </label>
        </div>
      ) : (
        <JalaliDatePicker
          ariaLabel={header}
          locale={locale}
          value={iso || undefined}
          panelMinWidth={320}
          onChange={(next) =>
            onChange({
              kind: "date",
              operator,
              iso: next ?? iso ?? new Date().toISOString(),
              isoTo,
            })
          }
        />
      )}
    </div>
  );
}
