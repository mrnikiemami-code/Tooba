"use client";

import { useEffect, useState } from "react";
import { Save } from "lucide-react";
import {
  parseReservationInteger,
  type ReservationPolicyEditorView,
  type ReservationPolicyFieldView,
} from "./reservation-policy-api";

export interface ReservationPolicyDraft {
  initial: string;
  retry: string;
  max: string;
  inheritInitial: boolean;
  inheritRetry: boolean;
  inheritMax: boolean;
}

export function draftFromView(view: ReservationPolicyEditorView): ReservationPolicyDraft {
  return {
    initial: view.initialHold.overrideValue == null ? "" : String(view.initialHold.overrideValue),
    retry: view.retryHold.overrideValue == null ? "" : String(view.retryHold.overrideValue),
    max: view.maxCycles.overrideValue == null ? "" : String(view.maxCycles.overrideValue),
    inheritInitial: view.initialHold.overrideValue == null,
    inheritRetry: view.retryHold.overrideValue == null,
    inheritMax: view.maxCycles.overrideValue == null,
  };
}

export function ReservationPolicyEditor({
  view,
  draft,
  onDraftChange,
  canEdit,
  busy,
  locale,
  onSave,
  onCancel,
  testId,
}: {
  view: ReservationPolicyEditorView;
  draft: ReservationPolicyDraft;
  onDraftChange: (next: ReservationPolicyDraft) => void;
  canEdit: boolean;
  busy?: boolean;
  locale: "fa" | "en";
  onSave?: () => void;
  onCancel?: () => void;
  testId?: string;
}) {
  const dir = locale === "en" ? "ltr" : "rtl";
  const fields: Array<{ key: keyof ReservationPolicyDraft; field: ReservationPolicyFieldView; unitFa: string; unitEn: string }> = [
    { key: "initial", field: view.initialHold, unitFa: "دقیقه", unitEn: "minutes" },
    { key: "retry", field: view.retryHold, unitFa: "دقیقه", unitEn: "minutes" },
    { key: "max", field: view.maxCycles, unitFa: "دفعه", unitEn: "cycles" },
  ];

  return (
    <section className="space-y-4" dir={dir} data-testid={testId ?? `reservation-policy-${view.scope}`}>
      <div>
        <h3 className="text-sm font-bold text-gray-900">سیاست رزرو موجودی</h3>
        <p className="text-xs text-gray-500 mt-1 leading-6">{locale === "en" ? "Inventory reservation policy" : "سیاست رزرو موجودی"}</p>
        <p className="text-xs text-gray-500 mt-2 leading-6">{view.multiLineHelpFa}</p>
        <p className="text-[11px] text-gray-400 leading-5" dir="ltr">{view.multiLineHelpEn}</p>
        {view.scope === "offer" ? (
          <>
            <p className="text-xs text-gray-500 mt-2 leading-6">{view.flashSaleHelpFa}</p>
            <p className="text-[11px] text-gray-400 leading-5" dir="ltr">{view.flashSaleHelpEn}</p>
          </>
        ) : null}
        {view.flashSaleStricter ? (
          <p className="mt-2 rounded-xl bg-sky-50 border border-sky-100 text-sky-800 text-xs px-3 py-2" data-testid="reservation-policy-stricter">
            {view.stricterNoteFa}
            <span className="block mt-1" dir="ltr">{view.stricterNoteEn}</span>
          </p>
        ) : null}
        {view.longHoldWarning ? (
          <p className="mt-2 rounded-xl bg-amber-50 border border-amber-100 text-amber-800 text-xs px-3 py-2" data-testid="reservation-policy-long-hold">
            {view.longHoldNoteFa}
            <span className="block mt-1" dir="ltr">{view.longHoldNoteEn}</span>
          </p>
        ) : null}
      </div>
      {fields.map(({ key, field, unitFa, unitEn }) => {
        const inheritKey = key === "initial" ? "inheritInitial" : key === "retry" ? "inheritRetry" : "inheritMax";
        const inherit = draft[inheritKey];
        return (
          <div key={key} className="rounded-xl border border-gray-100 p-4" data-testid={`reservation-policy-field-${key}`}>
            <label className="text-sm font-medium text-gray-800">{field.labelFa}</label>
            <p className="text-xs text-gray-500 mt-1">{field.helperFa}</p>
            <p className="text-[11px] text-gray-400 mt-1" dir="ltr">{field.labelEn} — {field.helperEn}</p>
            <label className="mt-3 flex items-center gap-2 text-xs text-gray-600">
              <input
                type="checkbox"
                checked={inherit}
                disabled={!canEdit || busy}
                onChange={(event) => onDraftChange({
                  ...draft,
                  [inheritKey]: event.target.checked,
                  [key]: event.target.checked ? "" : draft[key],
                })}
                data-testid={`reservation-policy-inherit-${key}`}
              />
              {view.inheritLabelFa}
              <span dir="ltr">{view.inheritLabelEn}</span>
            </label>
            <div className="mt-3 flex items-center gap-2">
              <input
                type="text"
                inputMode="numeric"
                value={draft[key]}
                disabled={!canEdit || busy || inherit}
                onChange={(event) => onDraftChange({ ...draft, [key]: event.target.value })}
                className="w-32 px-3 py-2 bg-gray-50 rounded-xl text-sm border border-gray-200"
                data-testid={`reservation-policy-input-${key}`}
              />
              <span className="text-xs text-gray-500">{locale === "en" ? unitEn : unitFa}</span>
            </div>
            <p className="mt-2 text-xs text-gray-500" data-testid={`reservation-policy-effective-${key}`}>
              {locale === "en"
                ? `Effective: ${field.effectiveValue} ${unitEn} — ${field.overridden ? "overridden" : `inherited from ${field.sourceLabelEn}`}`
                : `مؤثر: ${field.effectiveValue} ${unitFa} — ${field.overridden ? "overridden" : `inherited from ${field.sourceLabelEn}`}`}
            </p>
          </div>
        );
      })}
      {canEdit && onSave ? (
        <div className="flex gap-3">
          <button
            type="button"
            disabled={busy}
            onClick={onSave}
            className="flex-1 py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold disabled:opacity-70"
            data-testid="reservation-policy-save"
          >
            <Save className="w-4 h-4 inline-block ml-1" />
            ذخیره
          </button>
          {onCancel ? (
            <button
              type="button"
              disabled={busy}
              onClick={onCancel}
              className="px-4 py-2.5 rounded-xl border border-gray-200 text-sm font-bold"
              data-testid="reservation-policy-cancel"
            >
              انصراف
            </button>
          ) : null}
        </div>
      ) : null}
    </section>
  );
}

export function useReservationDraft(view: ReservationPolicyEditorView | null) {
  const [draft, setDraft] = useState<ReservationPolicyDraft>({
    initial: "",
    retry: "",
    max: "",
    inheritInitial: true,
    inheritRetry: true,
    inheritMax: true,
  });
  useEffect(() => {
    if (view) setDraft(draftFromView(view));
  }, [view]);
  return { draft, setDraft };
}

export function readReservationWrite(draft: ReservationPolicyDraft):
  | { ok: true; body: { initialReservationHoldMinutes: number | null; retryReservationHoldMinutes: number | null; maxReservationCycles: number | null } }
  | { ok: false; message: string } {
  const initial = draft.inheritInitial ? { ok: true as const, value: null } : parseReservationInteger(draft.initial);
  const retry = draft.inheritRetry ? { ok: true as const, value: null } : parseReservationInteger(draft.retry);
  const max = draft.inheritMax ? { ok: true as const, value: null } : parseReservationInteger(draft.max);
  if (!initial.ok || !retry.ok || !max.ok || initial.value == null && !draft.inheritInitial || retry.value == null && !draft.inheritRetry || max.value == null && !draft.inheritMax) {
    return { ok: false, message: "برای لغو ارث‌بری باید عدد صحیح وارد شود." };
  }
  return {
    ok: true,
    body: {
      initialReservationHoldMinutes: initial.value,
      retryReservationHoldMinutes: retry.value,
      maxReservationCycles: max.value,
    },
  };
}
