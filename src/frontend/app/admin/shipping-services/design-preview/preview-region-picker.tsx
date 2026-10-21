"use client";

/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186 — انتخاب‌گر آبشاری مناطق (کشور ← استان ← شهر ← ناحیه).
 *
 * قواعد نمایش‌داده‌شده (بدون اعتبارسنجی سرور):
 * - افزودن در هر سطح مجاز است و همان سطح انتخاب می‌شود؛ صرف انتخاب والد برای مرور چیزی اضافه نمی‌کند.
 * - افزودن والد همهٔ زیرمجموعه‌ها را پوشش می‌دهد، پس گزینه‌های متعارض غیرفعال و علت آن خوانا می‌شود.
 * - تکرار و والد/فرزند در دامنهٔ همان مالک/روش مسدود است؛ هیچ حذف یا جایگزینی خودکار انجام نمی‌شود.
 * - نام خوانای کامل مسیر نمایش داده می‌شود، نه کد فنی.
 */

import { useMemo, useState } from "react";
import { X } from "lucide-react";
import { Field, Select } from "../../../../design-system";
import {
  PREVIEW_PLACES,
  previewChildPlaces,
  previewPlaceConflict,
  previewPlaceLabel,
  previewPlacePath,
  previewPlacesByLevel,
  type PreviewLocale,
  type PreviewPlaceLevel,
} from "./preview-model.ts";
import { previewFormat, type PreviewMessages } from "./preview-i18n.ts";

function levelLabel(level: PreviewPlaceLevel, messages: PreviewMessages): string {
  switch (level) {
    case "country":
      return messages.pickerLevelCountry;
    case "province":
      return messages.pickerLevelProvince;
    case "city":
      return messages.pickerLevelCity;
    default:
      return messages.pickerLevelArea;
  }
}

const LEVELS: readonly PreviewPlaceLevel[] = ["country", "province", "city", "area"];

export function PreviewRegionPicker({
  selected,
  methodSelected,
  locale,
  messages,
  readOnly,
  onAdd,
  onRemove,
}: {
  selected: readonly string[];
  methodSelected: readonly string[];
  locale: PreviewLocale;
  messages: PreviewMessages;
  readOnly: boolean;
  onAdd: (placeId: string) => void;
  onRemove: (placeId: string) => void;
}) {
  const [picker, setPicker] = useState<Record<PreviewPlaceLevel, string>>({
    country: "",
    province: "",
    city: "",
    area: "",
  });

  const optionsFor = (level: PreviewPlaceLevel): typeof PREVIEW_PLACES => {
    if (level === "country") return previewPlacesByLevel("country");
    if (level === "province") return picker.country ? previewChildPlaces(picker.country) : [];
    if (level === "city") return picker.province ? previewChildPlaces(picker.province) : [];
    return picker.city ? previewChildPlaces(picker.city) : [];
  };

  const candidate = picker.area || picker.city || picker.province || picker.country;
  const candidateConflict = candidate ? previewPlaceConflict(methodSelected, candidate) : null;

  const selectLevel = (level: PreviewPlaceLevel, value: string): void => {
    const index = LEVELS.indexOf(level);
    const next: Record<PreviewPlaceLevel, string> = { ...picker, [level]: value };
    // پاک‌شدن یک سطح، سطوح پایین‌تر را هم پاک می‌کند تا مسیر ناسازگار ساخته نشود.
    for (const deeper of LEVELS.slice(index + 1)) next[deeper] = "";
    setPicker(next);
  };

  const conflictExplanation = useMemo(() => {
    if (!candidateConflict) return "";
    const place = previewPlaceLabel(candidateConflict.placeId, locale);
    if (candidateConflict.kind === "duplicate") return messages.pickerBlockedDuplicate;
    if (candidateConflict.kind === "covers") return previewFormat(messages.pickerBlockedCovers, { place });
    return previewFormat(messages.pickerBlockedCovered, { place });
  }, [candidateConflict, locale, messages]);

  const canAdd = !readOnly && candidate.length > 0 && candidateConflict === null;

  return (
    <div className="space-y-3 rounded-xl border border-dashed border-border bg-surface p-3" data-testid="shipping-preview-region-picker">
      <div>
        <h4 className="text-sm font-black text-gray-900">{messages.pickerTitle}</h4>
        <p className="mt-1 text-[11px] text-gray-500">{messages.pickerHint}</p>
      </div>

      <div className="grid gap-2 md:grid-cols-4">
        {LEVELS.map((level) => {
          const options = optionsFor(level);
          const disabled = readOnly || options.length === 0;
          return (
            <Field key={level} id={`picker-${level}`} label={levelLabel(level, messages)}>
              <Select
                value={picker[level]}
                disabled={disabled}
                data-testid={`shipping-preview-picker-${level}`}
                onChange={(event) => selectLevel(level, event.target.value)}
              >
                <option value="">{messages.dash}</option>
                {options.map((place) => {
                  const conflict = previewPlaceConflict(methodSelected, place.id);
                  const label = previewPlaceLabel(place.id, locale);
                  return (
                    <option key={place.id} value={place.id} disabled={conflict !== null}>
                      {conflict ? `⛔ ${label}` : label}
                    </option>
                  );
                })}
              </Select>
            </Field>
          );
        })}
      </div>

      {optionsFor("province").length === 0 && picker.country ? (
        <p className="text-[11px] text-gray-500">{messages.pickerNoChildren}</p>
      ) : null}

      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          className="inline-flex min-h-9 items-center rounded-lg bg-primary px-3 text-xs font-bold text-primary-foreground disabled:opacity-50"
          disabled={!canAdd}
          data-testid="shipping-preview-picker-add"
          onClick={() => {
            if (!canAdd) return;
            onAdd(candidate);
            setPicker({ country: "", province: "", city: "", area: "" });
          }}
        >
          {messages.pickerAdd}
        </button>
        {candidateConflict ? (
          <p role="status" className="text-[11px] font-semibold text-amber-700" data-testid="shipping-preview-picker-conflict">
            {conflictExplanation}
          </p>
        ) : null}
      </div>

      <div>
        <p className="text-xs font-bold text-gray-900">{messages.pickerSelectedTitle}</p>
        {selected.length === 0 ? (
          <p className="mt-1 text-[11px] text-gray-500">{messages.pickerNoPlaces}</p>
        ) : (
          <ul className="mt-2 flex flex-wrap gap-2" data-testid="shipping-preview-selected-places">
            {selected.map((placeId) => (
              <li
                key={placeId}
                className="inline-flex items-center gap-1 rounded-full bg-secondary px-2.5 py-1 text-[11px] font-semibold text-gray-800"
              >
                <span>{previewPlacePath(placeId, locale).join(" › ")}</span>
                {readOnly ? null : (
                  <button
                    type="button"
                    className="rounded-full p-0.5 text-gray-500 hover:text-gray-900"
                    aria-label={`${messages.pickerRemove}: ${previewPlacePath(placeId, locale).join(" › ")}`}
                    data-testid={`shipping-preview-remove-place-${placeId}`}
                    onClick={() => onRemove(placeId)}
                  >
                    <X className="size-3" aria-hidden />
                  </button>
                )}
              </li>
            ))}
          </ul>
        )}
        <p className="mt-2 text-[11px] text-gray-500">{messages.pickerExplanation}</p>
      </div>
    </div>
  );
}
