"use client";

/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186 — زبانهٔ «مناطق و هزینه‌ها».
 *
 * ساختار هدف: گروه مناطق (یک انتخاب جغرافیایی مشترک) ← چند ردیف شرط/هزینه زیر همان گروه.
 * - ردیف‌ها هرگز جغرافیا را تکرار نمی‌کنند و ویرایشگر ردیف انتخاب‌گر منطقه ندارد.
 * - گروه‌ها جمع‌شونده‌اند و جدول ردیف‌ها فقط اسکرول افقی محدود دارد (نه اسکرول تودرتو در مودال بلند).
 * - عنوان گروه اختیاری و به زبان محتوای فعال ذخیره می‌شود؛ تعویض زبان دادهٔ مشترک را از بین نمی‌برد.
 */

import { useState } from "react";
import { ChevronDown, ChevronUp, Pencil, Plus, Trash2 } from "lucide-react";
import { Field, Input } from "../../../../design-system";
import {
  previewMethodSelectedPlaces,
  previewNewRateRow,
  previewNextId,
  previewTextFor,
  previewTranslationStates,
  type PreviewLocale,
  type PreviewMethod,
  type PreviewRegionGroup,
  type PreviewRateRow,
} from "./preview-model.ts";
import {
  describePreviewDelivery,
  describePreviewRange,
  formatPreviewMoney,
  formatPreviewNumber,
  previewContentLanguageLabel,
  previewFormat,
  type PreviewMessages,
} from "./preview-i18n.ts";
import { PreviewRegionPicker } from "./preview-region-picker.tsx";
import { PreviewRateRowEditor } from "./preview-rate-row-editor.tsx";

export function PreviewRegionsTab({
  draft,
  readOnly,
  locale,
  messages,
  contentLanguageId,
  onChange,
}: {
  draft: PreviewMethod;
  readOnly: boolean;
  locale: PreviewLocale;
  messages: PreviewMessages;
  contentLanguageId: string;
  onChange: (next: PreviewMethod) => void;
}) {
  const [openGroups, setOpenGroups] = useState<readonly string[]>(() =>
    draft.regionGroups.length > 0 ? [draft.regionGroups[0]!.id] : [],
  );
  const [pickerFor, setPickerFor] = useState<string | null>(null);
  const [editingRowFor, setEditingRowFor] = useState<{ groupId: string; rowId: string | null } | null>(null);

  const methodSelected = previewMethodSelectedPlaces(draft);

  const patchGroup = (groupId: string, updater: (group: PreviewRegionGroup) => PreviewRegionGroup): void => {
    onChange({
      ...draft,
      regionGroups: draft.regionGroups.map((group) => (group.id === groupId ? updater(group) : group)),
    });
  };

  const toggleGroup = (groupId: string): void => {
    setOpenGroups((current) =>
      current.includes(groupId) ? current.filter((id) => id !== groupId) : [...current, groupId],
    );
  };

  const saveRow = (groupId: string, row: PreviewRateRow): void => {
    patchGroup(groupId, (group) => {
      const exists = group.rows.some((current) => current.id === row.id);
      return {
        ...group,
        rows: exists
          ? group.rows.map((current) => (current.id === row.id ? row : current))
          : [...group.rows, row],
      };
    });
    setEditingRowFor(null);
  };

  const removeRow = (groupId: string, rowId: string): void => {
    patchGroup(groupId, (group) => ({ ...group, rows: group.rows.filter((row) => row.id !== rowId) }));
    setEditingRowFor(null);
  };

  return (
    <div className="flex min-w-0 flex-col gap-3" data-testid="shipping-preview-regions-tab">
      <div>
        <h3 className="text-sm font-black text-gray-900">{messages.regionGroupsTitle}</h3>
        <p className="mt-1 text-xs text-gray-500">{messages.regionGroupsHint}</p>
      </div>

      {draft.regionGroups.length === 0 ? (
        <p className="rounded-xl border border-dashed border-border px-4 py-5 text-center text-xs text-gray-500">
          {messages.pickerNoPlaces}
        </p>
      ) : null}

      {draft.regionGroups.map((group, groupIndex) => {
        const open = openGroups.includes(group.id);
        const title = previewTextFor(group.titles, contentLanguageId);
        const titleStates = previewTranslationStates(group.titles);
        return (
          <section
            key={group.id}
            className="min-w-0 rounded-2xl border border-border bg-surface-elevated"
            data-testid={`shipping-preview-group-${group.id}`}
          >
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border px-3 py-2">
              <div className="flex min-w-0 items-center gap-2">
                <button
                  type="button"
                  className="inline-flex size-8 items-center justify-center rounded-lg border border-border text-gray-600"
                  aria-expanded={open}
                  aria-label={open ? messages.scheduleCollapse : messages.scheduleExpand}
                  data-testid={`shipping-preview-group-toggle-${group.id}`}
                  onClick={() => toggleGroup(group.id)}
                >
                  {open ? <ChevronUp className="size-4" aria-hidden /> : <ChevronDown className="size-4" aria-hidden />}
                </button>
                <div className="min-w-0">
                  <p className="truncate text-sm font-black text-gray-900">
                    {title.trim().length > 0 ? title : `#${formatPreviewNumber(groupIndex + 1, locale)}`}
                  </p>
                  <p className="text-[11px] text-gray-500">
                    {previewFormat(messages.groupPlaceCount, {
                      count: formatPreviewNumber(group.places.length, locale),
                    })}
                    {" · "}
                    {`${messages.rowCount} ${formatPreviewNumber(group.rows.length, locale)}`}
                  </p>
                </div>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  className="inline-flex min-h-8 items-center rounded-lg border border-border px-2.5 text-xs font-bold text-gray-700"
                  data-testid={`shipping-preview-edit-regions-${group.id}`}
                  onClick={() => {
                    setPickerFor((current) => (current === group.id ? null : group.id));
                    setOpenGroups((current) => (current.includes(group.id) ? current : [...current, group.id]));
                  }}
                >
                  {messages.editRegions}
                </button>
                {readOnly ? null : (
                  <button
                    type="button"
                    className="inline-flex min-h-8 items-center gap-1 rounded-lg px-2 text-xs font-bold text-red-700"
                    data-testid={`shipping-preview-remove-group-${group.id}`}
                    onClick={() => {
                      onChange({
                        ...draft,
                        regionGroups: draft.regionGroups.filter((current) => current.id !== group.id),
                      });
                    }}
                  >
                    <Trash2 className="size-3.5" aria-hidden />
                    {messages.removeRegionGroup}
                  </button>
                )}
              </div>
            </div>

            {open ? (
              <div className="space-y-3 px-3 py-3">
                <div className="flex flex-wrap items-end gap-2">
                  <div className="min-w-[180px] flex-1">
                    <Field
                      id={`group-title-${group.id}`}
                      label={messages.regionGroupTitleLabel}
                      hint={messages.contentLanguageHint}
                    >
                      <Input
                        value={title}
                        maxLength={96}
                        disabled={readOnly}
                        data-testid={`shipping-preview-group-title-${group.id}`}
                        onChange={(event) =>
                          patchGroup(group.id, (current) => ({
                            ...current,
                            titles: { ...current.titles, [contentLanguageId]: event.target.value },
                          }))
                        }
                      />
                    </Field>
                  </div>
                  <ul className="flex flex-wrap gap-1 pb-1 text-[11px]">
                    {titleStates.map((state) => (
                      <li
                        key={state.languageId}
                        className={
                          state.translated
                            ? "rounded-full bg-emerald-50 px-2 py-0.5 font-bold text-emerald-700"
                            : "rounded-full bg-amber-50 px-2 py-0.5 font-bold text-amber-700"
                        }
                      >
                        {previewContentLanguageLabel(state.languageId, locale)}:{" "}
                        {state.translated ? messages.translated : messages.notTranslated}
                      </li>
                    ))}
                  </ul>
                </div>

                {pickerFor === group.id ? (
                  <PreviewRegionPicker
                    selected={group.places}
                    methodSelected={methodSelected}
                    locale={locale}
                    messages={messages}
                    readOnly={readOnly}
                    onAdd={(placeId) =>
                      patchGroup(group.id, (current) => ({
                        ...current,
                        places: [...current.places, placeId],
                      }))
                    }
                    onRemove={(placeId) =>
                      patchGroup(group.id, (current) => ({
                        ...current,
                        places: current.places.filter((id) => id !== placeId),
                      }))
                    }
                  />
                ) : null}

                <div>
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <h4 className="text-sm font-black text-gray-900">{messages.ratesTitle}</h4>
                    {readOnly ? null : (
                      <button
                        type="button"
                        className="inline-flex min-h-8 items-center gap-1 rounded-lg bg-primary px-2.5 text-xs font-bold text-primary-foreground"
                        data-testid={`shipping-preview-add-row-${group.id}`}
                        onClick={() => setEditingRowFor({ groupId: group.id, rowId: null })}
                      >
                        <Plus className="size-3.5" aria-hidden />
                        {messages.addRateRow}
                      </button>
                    )}
                  </div>
                  <p className="mt-1 text-[11px] text-gray-500">{messages.ratesAndNote}</p>
                  <p className="text-[11px] text-gray-500">{messages.ratesBoundsNote}</p>

                  {group.rows.length === 0 ? (
                    <p className="mt-2 rounded-lg border border-dashed border-border px-3 py-4 text-center text-[11px] text-gray-500">
                      {messages.noRows}
                    </p>
                  ) : (
                    <div className="mt-2 overflow-x-auto rounded-xl border border-border">
                      <table className="w-full min-w-[720px] border-collapse text-xs" data-testid={`shipping-preview-rows-${group.id}`}>
                        <thead className="bg-gray-50 text-[11px] text-gray-600">
                          <tr>
                            <th scope="col" className="px-2 py-2 text-start font-bold">{messages.colConditionWeight}</th>
                            <th scope="col" className="px-2 py-2 text-start font-bold">{messages.colConditionCount}</th>
                            <th scope="col" className="px-2 py-2 text-start font-bold">{messages.colConditionAmount}</th>
                            <th scope="col" className="px-2 py-2 text-start font-bold">{messages.colFreight}</th>
                            <th scope="col" className="px-2 py-2 text-start font-bold">{messages.colPackaging}</th>
                            <th scope="col" className="px-2 py-2 text-start font-bold">{messages.colDelivery}</th>
                            <th scope="col" className="px-2 py-2 text-start font-bold">{messages.colRowActions}</th>
                          </tr>
                        </thead>
                        <tbody>
                          {group.rows.map((row) => (
                            <tr key={row.id} className="border-t border-border align-top">
                              <td className="px-2 py-2">{describePreviewRange(row.weightGrams, locale, "weight")}</td>
                              <td className="px-2 py-2">{describePreviewRange(row.itemCount, locale, "count")}</td>
                              <td className="px-2 py-2">{describePreviewRange(row.merchandiseAmount, locale, "amount")}</td>
                              <td className="px-2 py-2 font-semibold text-gray-900">{formatPreviewMoney(row.freight, locale)}</td>
                              <td className="px-2 py-2">{formatPreviewMoney(row.packaging, locale)}</td>
                              <td className="px-2 py-2">{describePreviewDelivery(row.delivery, locale)}</td>
                              <td className="px-2 py-2">
                                <div className="flex items-center gap-1">
                                  <button
                                    type="button"
                                    className="inline-flex size-7 items-center justify-center rounded-lg border border-border text-gray-600"
                                    aria-label={`${messages.actionView}: ${messages.rowEditorTitle}`}
                                    data-testid={`shipping-preview-row-view-${row.id}`}
                                    onClick={() => setEditingRowFor({ groupId: group.id, rowId: row.id })}
                                  >
                                    <Pencil className="size-3.5" aria-hidden />
                                  </button>
                                  {readOnly ? null : (
                                    <button
                                      type="button"
                                      className="inline-flex size-7 items-center justify-center rounded-lg text-red-700"
                                      aria-label={`${messages.rowRemove}: ${messages.rowEditorTitle}`}
                                      data-testid={`shipping-preview-row-remove-${row.id}`}
                                      onClick={() => removeRow(group.id, row.id)}
                                    >
                                      <Trash2 className="size-3.5" aria-hidden />
                                    </button>
                                  )}
                                </div>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}

                  {editingRowFor && editingRowFor.groupId === group.id ? (
                    <PreviewRateRowEditor
                      row={group.rows.find((row) => row.id === editingRowFor.rowId) ?? previewNewRateRow()}
                      readOnly={readOnly}
                      locale={locale}
                      messages={messages}
                      onApply={(row) => saveRow(group.id, row)}
                      onCancel={() => setEditingRowFor(null)}
                    />
                  ) : null}
                </div>
              </div>
            ) : null}
          </section>
        );
      })}

      {readOnly ? null : (
        <div>
          <button
            type="button"
            className="inline-flex min-h-9 items-center gap-1 rounded-xl border border-dashed border-border px-3 text-xs font-bold text-gray-700"
            data-testid="shipping-preview-add-group"
            onClick={() => {
              // گروه تازه فقط با geography خالی؛ هیچ منطقه‌ای خودکار انتخاب نمی‌شود.
              const id = previewNextId("group");
              onChange({
                ...draft,
                regionGroups: [...draft.regionGroups, { id, titles: { fa: "", en: "" }, places: [], rows: [] }],
              });
              setOpenGroups((current) => [...current, id]);
              setPickerFor(id);
            }}
          >
            <Plus className="size-3.5" aria-hidden />
            {messages.addRegionGroup}
          </button>
        </div>
      )}
    </div>
  );
}
