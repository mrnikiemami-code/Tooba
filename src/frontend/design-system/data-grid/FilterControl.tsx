"use client";

import { useMemo, useState } from "react";
import { Button, Checkbox, Input, Select } from "../primitives/core";
import type { EntityFilterAdapter, GridColumnDef, GridFilterValue } from "./types";
import { filterOperatorLabelsFor, type FilterOperatorLabels } from "./messages";

/**
 * کنترل فیلتر تایپ‌شده برای یک ستون. برچسب عملگرها محلی‌سازی شده‌اند (نه contains خام).
 */
export function FilterControl<T>({
  column,
  value,
  onChange,
  entityLookup,
  operatorLabels,
}: {
  column: GridColumnDef<T>;
  value?: GridFilterValue;
  onChange: (value: GridFilterValue) => void;
  entityLookup?: EntityFilterAdapter;
  /** پیش‌فرض فارسی؛ locale انگلیسی از سند خوانده می‌شود. */
  operatorLabels?: FilterOperatorLabels;
}) {
  const kind = column.filterKind;
  const [entityHits, setEntityHits] = useState<{ id: string; label: string }[]>([]);
  const ops = useMemo(() => {
    if (operatorLabels) return operatorLabels;
    const lang =
      typeof document !== "undefined" && (document.documentElement.lang || "").toLowerCase().startsWith("en")
        ? "en"
        : "fa";
    return filterOperatorLabelsFor(lang);
  }, [operatorLabels]);

  if (kind === "text") {
    return (
      <label className="block text-sm">
        {column.header}
        <Select
          aria-label={`${column.header} — ${ops.contains}`}
          value={value?.kind === "text" ? value.operator : "contains"}
          onChange={(event) =>
            onChange({
              kind: "text",
              operator: event.target.value as "contains" | "equals" | "startsWith",
              query: value?.kind === "text" ? value.query : "",
            })
          }
        >
          <option value="contains">{ops.contains}</option>
          <option value="equals">{ops.equals}</option>
          <option value="startsWith">{ops.startsWith}</option>
        </Select>
        <Input
          value={value?.kind === "text" ? value.query : ""}
          onChange={(event) =>
            onChange({
              kind: "text",
              operator: value?.kind === "text" ? value.operator : "contains",
              query: event.target.value,
            })
          }
        />
      </label>
    );
  }

  if (kind === "number" || kind === "money") {
    const operator = value?.kind === "number" || value?.kind === "money" ? value.operator : "equals";
    const amount = value?.kind === "number" ? value.value : value?.kind === "money" ? value.money.amount : 0;
    const amountTo = value?.kind === "number" ? value.valueTo : value?.kind === "money" ? value.money.amountTo : undefined;
    return (
      <label className="block text-sm">
        {column.header}
        <Select
          aria-label={`${column.header} — ${ops.equals}`}
          value={operator}
          onChange={(event) => {
            const next = event.target.value as typeof operator;
            onChange(
              kind === "money"
                ? { kind: "money", operator: next, money: { amount, currency: "IRR", amountTo } }
                : { kind: "number", operator: next, value: amount, valueTo: amountTo },
            );
          }}
        >
          <option value="equals">{ops.equals}</option>
          <option value="greaterThan">{ops.greaterThan}</option>
          <option value="lessThan">{ops.lessThan}</option>
          <option value="between">{ops.between}</option>
        </Select>
        <Input
          type="number"
          value={Number.isFinite(amount) ? amount : ""}
          onChange={(event) => {
            const nextAmount = Number(event.target.value);
            onChange(
              kind === "money"
                ? { kind: "money", operator, money: { amount: nextAmount, currency: "IRR", amountTo } }
                : { kind: "number", operator, value: nextAmount, valueTo: amountTo },
            );
          }}
        />
        {operator === "between" ? (
          <Input
            type="number"
            aria-label={`${column.header} — ${ops.lessThan}`}
            value={amountTo ?? ""}
            onChange={(event) => {
              const nextTo = Number(event.target.value);
              onChange(
                kind === "money"
                  ? { kind: "money", operator, money: { amount, currency: "IRR", amountTo: nextTo } }
                  : { kind: "number", operator, value: amount, valueTo: nextTo },
              );
            }}
          />
        ) : null}
      </label>
    );
  }

  if (kind === "date") {
    const operator = value?.kind === "date" ? value.operator : "on";
    return (
      <label className="block text-sm">
        {column.header}
        <Select
          aria-label={`${column.header} — ${ops.on}`}
          value={operator}
          onChange={(event) =>
            onChange({
              kind: "date",
              operator: event.target.value as "on" | "before" | "after" | "between",
              iso: value?.kind === "date" ? value.iso : "",
              isoTo: value?.kind === "date" ? value.isoTo : undefined,
            })
          }
        >
          <option value="on">{ops.on}</option>
          <option value="before">{ops.before}</option>
          <option value="after">{ops.after}</option>
          <option value="between">{ops.between}</option>
        </Select>
        <Input
          type="date"
          value={value?.kind === "date" ? value.iso : ""}
          onChange={(event) =>
            onChange({
              kind: "date",
              operator,
              iso: event.target.value,
              isoTo: value?.kind === "date" ? value.isoTo : undefined,
            })
          }
        />
        {operator === "between" ? (
          <Input
            type="date"
            aria-label={`${column.header} — ${ops.between}`}
            value={value?.kind === "date" ? (value.isoTo ?? "") : ""}
            onChange={(event) =>
              onChange({
                kind: "date",
                operator,
                iso: value?.kind === "date" ? value.iso : event.target.value,
                isoTo: event.target.value,
              })
            }
          />
        ) : null}
      </label>
    );
  }

  if (kind === "boolean") {
    return (
      <label className="block text-sm">
        {column.header}
        <Select
          value={value?.kind === "boolean" ? value.state : "all"}
          onChange={(event) => onChange({ kind: "boolean", state: event.target.value as "all" | "true" | "false" })}
        >
          <option value="all">{ops.all}</option>
          <option value="true">{ops.yes}</option>
          <option value="false">{ops.no}</option>
        </Select>
      </label>
    );
  }

  if (kind === "enum" || kind === "status") {
    const selected = value && (value.kind === "enum" || value.kind === "status") ? value.values : [];
    return (
      <fieldset className="text-sm">
        <legend>{column.header}</legend>
        {(column.enumOptions ?? []).map((option) => (
          <Checkbox
            key={option.value}
            label={option.label}
            checked={selected.includes(option.value)}
            onChange={() => {
              const next = selected.includes(option.value)
                ? selected.filter((item) => item !== option.value)
                : [...selected, option.value];
              onChange({ kind, values: next });
            }}
          />
        ))}
      </fieldset>
    );
  }

  if (kind === "entity") {
    return (
      <div className="text-sm">
        <p>{column.header}</p>
        <Input
          value={value?.kind === "entity" ? (value.search ?? value.ids.join(",")) : ""}
          onChange={(event) => {
            const term = event.target.value;
            onChange({
              kind: "entity",
              ids: term
                .split(",")
                .map((item) => item.trim())
                .filter(Boolean),
              search: term,
            });
            if (entityLookup) {
              void entityLookup.search(term).then(setEntityHits);
            }
          }}
        />
        {entityHits.length > 0 ? (
          <ul className="mt-2 space-y-1">
            {entityHits.map((hit) => (
              <li key={hit.id}>
                <Button
                  type="button"
                  tone="ghost"
                  onClick={() => onChange({ kind: "entity", ids: [hit.id], search: hit.label })}
                >
                  {hit.label}
                </Button>
              </li>
            ))}
          </ul>
        ) : null}
      </div>
    );
  }

  return null;
}
