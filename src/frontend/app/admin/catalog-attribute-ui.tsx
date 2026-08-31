"use client";

import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import {
  addAttributeOption,
  bindCategoryAttribute,
  createAttributeDefinition,
  listAttributeDefinitions,
  loadEffectiveCategorySchema,
  previewVariantAxisCapabilityDisable,
  setSellerProductAttribute,
  setSellerProductVariantAxes,
  setVariantAxisCapability,
  unbindCategoryAttribute,
  updateAttributeDefinition,
  valueKindLabel,
  type AttributeDefinition,
  type CatalogAttributeValueKind,
  type EffectiveSchemaEntry,
  type UpdateAttributeDefinitionInput,
  type VariantAxisCapabilityDisableImpact,
} from "./catalog-attribute-api.ts";
import { mapAdminErrorMessage } from "./admin-error-map.ts";
import {
  valueKindBlocksVariantAxis,
  VARIANT_AXIS_CAPABILITY_HELPER,
  VARIANT_AXIS_CAPABILITY_LABEL,
  VARIANT_AXIS_DISABLE_IN_USE_TITLE,
  VARIANT_AXIS_DISABLE_ZERO_USAGE_CONFIRM,
  VARIANT_AXIS_DISABLED_BY_KIND,
} from "./variant-axis-messages.ts";

const VALUE_KINDS: CatalogAttributeValueKind[] = [
  "Text",
  "Number",
  "Boolean",
  "Enumeration",
  "Instant",
];

const inputClass =
  "mt-1 min-h-10 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500";
const btnPrimary =
  "inline-flex min-h-10 items-center justify-center rounded-xl bg-blue-600 px-4 text-sm font-semibold text-white hover:bg-blue-700 disabled:opacity-50";
const btnSecondary =
  "inline-flex min-h-10 items-center justify-center rounded-xl border border-gray-200 bg-white px-3 text-sm font-medium text-gray-800 hover:bg-gray-50 disabled:opacity-50";
const btnDanger =
  "inline-flex min-h-9 items-center justify-center rounded-lg border border-red-200 bg-white px-3 text-xs font-medium text-red-700 hover:bg-red-50 disabled:opacity-50";
const cardClass = "rounded-2xl border border-gray-200 bg-white p-5 shadow-sm";

function Feedback({ error, success }: { error?: string | null; success?: string | null }) {
  if (!error && !success) return null;
  return (
    <div className="space-y-1 text-sm">
      {error ? <p className="text-red-600">{error}</p> : null}
      {success ? <p className="text-emerald-700">{success}</p> : null}
    </div>
  );
}

function defaultMetadata(): UpdateAttributeDefinitionInput {
  return {
    unit: null,
    isRequired: false,
    isFilterable: false,
    isComparable: false,
    isMultivalue: false,
    displayOrder: 0,
    validationMin: null,
    validationMax: null,
    validationMaxLength: null,
    isActive: true,
  };
}

/**
 * فهرست و ایجاد/ویرایش تعاریف ویژگی Admin.
 */
export function AttributeDefinitionsScreen() {
  const [rows, setRows] = useState<AttributeDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [code, setCode] = useState("");
  const [valueKind, setValueKind] = useState<CatalogAttributeValueKind>("Text");
  const [axisAllowed, setAxisAllowed] = useState(false);
  const [faName, setFaName] = useState("");

  const [editId, setEditId] = useState<string | null>(null);
  const [editRow, setEditRow] = useState<AttributeDefinition | null>(null);
  const [editAxisAllowed, setEditAxisAllowed] = useState(false);
  const [meta, setMeta] = useState<UpdateAttributeDefinitionInput>(defaultMetadata());
  const [impactOpen, setImpactOpen] = useState(false);
  const [impactData, setImpactData] = useState<VariantAxisCapabilityDisableImpact | null>(null);
  const [confirmDisableOpen, setConfirmDisableOpen] = useState(false);

  const [optionDefId, setOptionDefId] = useState<string | null>(null);
  const [optionCode, setOptionCode] = useState("");
  const [optionName, setOptionName] = useState("");
  const [lastOptionId, setLastOptionId] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    const result = await listAttributeDefinitions();
    setLoading(false);
    if (result.state === "denied") {
      setError("دسترسی مجاز نیست");
      setRows([]);
      return;
    }
    if (result.state !== "ok" || !result.data) {
      setError(result.message ?? "بارگذاری ناموفق بود");
      setRows([]);
      return;
    }
    setRows(result.data);
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function onCreate() {
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await createAttributeDefinition({
      code: code.trim(),
      valueKind,
      isVariantAxisAllowed: axisAllowed,
      localizedNames: { "fa-IR": faName.trim() || code.trim() },
    });
    setBusy(false);
    if (result.state !== "ok" || !result.data) {
      setError(result.message ?? "ایجاد ناموفق بود");
      return;
    }
    setSuccess("ویژگی جدید در کتابخانه ایجاد شد.");
    setCode("");
    setFaName("");
    setAxisAllowed(false);
    setValueKind("Text");
    await reload();
  }

  function openEdit(row: AttributeDefinition) {
    setEditId(row.definitionId);
    setEditRow(row);
    setEditAxisAllowed(row.isVariantAxisAllowed);
    setMeta({
      unit: row.unit,
      isRequired: row.isRequired,
      isFilterable: row.isFilterable,
      isComparable: row.isComparable,
      isMultivalue: row.isMultivalue,
      displayOrder: row.displayOrder,
      validationMin: row.validationMin,
      validationMax: row.validationMax,
      validationMaxLength: row.validationMaxLength,
      isActive: row.isActive,
    });
    setSuccess(null);
    setError(null);
    setImpactOpen(false);
    setImpactData(null);
    setConfirmDisableOpen(false);
  }

  async function onAxisAllowedChange(next: boolean) {
    if (!editId || !editRow) return;
    if (valueKindBlocksVariantAxis(editRow.valueKind) && next) return;

    if (editRow.isVariantAxisAllowed && !next) {
      setBusy(true);
      setError(null);
      const preview = await previewVariantAxisCapabilityDisable(editId);
      setBusy(false);
      if (preview.state !== "ok" || !preview.data) {
        setError(mapAdminErrorMessage(preview.message, "fa"));
        return;
      }
      setImpactData(preview.data);
      if (!preview.data.canDisable) {
        setImpactOpen(true);
        return;
      }
      setConfirmDisableOpen(true);
      return;
    }

    await applyAxisCapability(next);
  }

  async function applyAxisCapability(next: boolean) {
    if (!editId) return;
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await setVariantAxisCapability(editId, next);
    setBusy(false);
    if (result.state !== "ok" || !result.data) {
      setError(mapAdminErrorMessage(result.message, "fa"));
      return;
    }
    setEditAxisAllowed(result.data.isVariantAxisAllowed);
    setEditRow(result.data);
    setImpactOpen(false);
    setConfirmDisableOpen(false);
    setImpactData(null);
    setSuccess(next ? "قابلیت محور تنوع فعال شد." : "قابلیت محور تنوع غیرفعال شد.");
    await reload();
  }

  async function onSaveMeta() {
    if (!editId) return;
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await updateAttributeDefinition(editId, {
      ...meta,
      unit: meta.unit?.trim() ? meta.unit.trim() : null,
    });
    setBusy(false);
    if (result.state !== "ok") {
      setError(result.message ?? "ذخیره فراداده ناموفق بود");
      return;
    }
    setSuccess("فراداده ذخیره شد");
    setEditId(null);
    await reload();
  }

  async function onAddOption() {
    if (!optionDefId) return;
    setBusy(true);
    setError(null);
    setSuccess(null);
    setLastOptionId(null);
    const result = await addAttributeOption(optionDefId, optionCode.trim(), {
      "fa-IR": optionName.trim() || optionCode.trim(),
    });
    setBusy(false);
    if (result.state !== "ok" || !result.data) {
      setError(result.message ?? "افزودن گزینه ناموفق بود");
      return;
    }
    setLastOptionId(result.data.optionId);
    setSuccess(`گزینه افزوده شد: ${result.data.optionId}`);
    setOptionCode("");
    setOptionName("");
  }

  return (
    <div className="space-y-6" dir="rtl" data-testid="admin-attribute-definitions">
      <div>
        <h1 className="text-2xl font-semibold text-gray-900">تعاریف ویژگی</h1>
        <p className="mt-1 text-sm text-gray-500">تعریف schema ویژگی‌های Catalog؛ ماتریس کامل ترکیبی اینجا نیست.</p>
      </div>

      <Feedback error={error} success={success} />

      <section className={cardClass}>
        <h2 className="text-base font-semibold text-gray-900">ایجاد تعریف جدید</h2>
        <div className="mt-4 grid gap-4 sm:grid-cols-2">
          <label className="text-sm font-medium text-gray-700">
            کد (Code)
            <input className={inputClass} dir="ltr" value={code} onChange={(e) => setCode(e.target.value)} />
          </label>
          <label className="text-sm font-medium text-gray-700">
            نام فارسی (fa-IR)
            <input className={inputClass} value={faName} onChange={(e) => setFaName(e.target.value)} />
          </label>
          <label className="text-sm font-medium text-gray-700">
            گونه مقدار
            <select
              className={inputClass}
              value={valueKind}
              onChange={(e) => setValueKind(e.target.value as CatalogAttributeValueKind)}
            >
              {VALUE_KINDS.map((k) => (
                <option key={k} value={k}>
                  {valueKindLabel(k)} ({k})
                </option>
              ))}
            </select>
          </label>
          <label className={`flex flex-col gap-1 pt-2 text-sm font-medium text-gray-700 ${valueKindBlocksVariantAxis(valueKind) ? "opacity-60" : ""}`}>
            <span className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={axisAllowed}
                disabled={valueKindBlocksVariantAxis(valueKind)}
                onChange={(e) => setAxisAllowed(e.target.checked)}
                className="size-4 rounded border-gray-300"
              />
              {VARIANT_AXIS_CAPABILITY_LABEL.fa}
            </span>
            <span className="text-xs font-normal text-gray-500">{VARIANT_AXIS_CAPABILITY_HELPER.fa}</span>
          </label>
        </div>
        <button type="button" className={`${btnPrimary} mt-4`} disabled={busy || !code.trim()} onClick={() => void onCreate()}>
          ایجاد تعریف
        </button>
      </section>

      <section className={cardClass}>
        <div className="flex items-center justify-between gap-3">
          <h2 className="text-base font-semibold text-gray-900">فهرست تعاریف</h2>
          <button type="button" className={btnSecondary} disabled={loading || busy} onClick={() => void reload()}>
            بازخوانی
          </button>
        </div>
        {loading ? (
          <p className="mt-4 text-sm text-gray-500">در حال بارگذاری…</p>
        ) : rows.length === 0 ? (
          <p className="mt-4 text-sm text-gray-500">تعریفی ثبت نشده است.</p>
        ) : (
          <div className="mt-4 overflow-x-auto">
            <table className="w-full min-w-[640px] text-right text-sm">
              <thead className="border-b border-gray-200 text-gray-500">
                <tr>
                  <th className="py-2 font-medium">کد</th>
                  <th className="font-medium">گونه</th>
                  <th className="font-medium">محور</th>
                  <th className="font-medium">الزامی</th>
                  <th className="font-medium">فعال</th>
                  <th className="font-medium">عملیات</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.definitionId} className="border-b border-gray-100">
                    <td className="py-3 font-medium" dir="ltr">
                      {row.code}
                    </td>
                    <td>{valueKindLabel(row.valueKind)}</td>
                    <td>{row.isVariantAxisAllowed ? "بله" : "خیر"}</td>
                    <td>{row.isRequired ? "بله" : "خیر"}</td>
                    <td>{row.isActive ? "فعال" : "غیرفعال"}</td>
                    <td className="space-x-2 space-x-reverse py-3">
                      <button type="button" className={btnSecondary} onClick={() => openEdit(row)}>
                        فراداده
                      </button>
                      {row.valueKind === "Enumeration" ? (
                        <button
                          type="button"
                          className={btnSecondary}
                          onClick={() => {
                            setOptionDefId(row.definitionId);
                            setLastOptionId(null);
                            setSuccess(null);
                          }}
                        >
                          گزینه
                        </button>
                      ) : null}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {editId && editRow ? (
        <section className={cardClass}>
          <h2 className="text-base font-semibold text-gray-900">ویرایش تعریف</h2>
          <p className="mt-1 text-xs text-gray-500" dir="ltr">
            {editId} · {editRow.code}
          </p>
          <div
            className={`mt-4 rounded-lg border border-gray-100 bg-slate-50 p-4 ${
              valueKindBlocksVariantAxis(editRow.valueKind) ? "opacity-60" : ""
            }`}
          >
            <label className="flex flex-col gap-1 text-sm font-medium text-gray-700">
              <span className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={editAxisAllowed}
                  disabled={busy || valueKindBlocksVariantAxis(editRow.valueKind)}
                  data-testid="attr-def-variant-capability"
                  onChange={(e) => void onAxisAllowedChange(e.target.checked)}
                  className="size-4 rounded border-gray-300"
                />
                {VARIANT_AXIS_CAPABILITY_LABEL.fa}
              </span>
              <span className="text-xs font-normal text-gray-500">{VARIANT_AXIS_CAPABILITY_HELPER.fa}</span>
            </label>
            {valueKindBlocksVariantAxis(editRow.valueKind) ? (
              <p className="mt-2 text-xs text-gray-500">{VARIANT_AXIS_DISABLED_BY_KIND.fa.detail}</p>
            ) : null}
          </div>
          <h3 className="mt-6 text-sm font-semibold text-gray-800">فراداده</h3>
          <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            <label className="text-sm font-medium text-gray-700">
              واحد
              <input
                className={inputClass}
                dir="ltr"
                value={meta.unit ?? ""}
                onChange={(e) => setMeta({ ...meta, unit: e.target.value })}
              />
            </label>
            <label className="text-sm font-medium text-gray-700">
              ترتیب نمایش
              <input
                className={inputClass}
                type="number"
                dir="ltr"
                value={meta.displayOrder}
                onChange={(e) => setMeta({ ...meta, displayOrder: Number(e.target.value) || 0 })}
              />
            </label>
            <label className="text-sm font-medium text-gray-700">
              حداقل اعتبارسنجی
              <input
                className={inputClass}
                type="number"
                dir="ltr"
                value={meta.validationMin ?? ""}
                onChange={(e) =>
                  setMeta({
                    ...meta,
                    validationMin: e.target.value === "" ? null : Number(e.target.value),
                  })
                }
              />
            </label>
            <label className="text-sm font-medium text-gray-700">
              حداکثر اعتبارسنجی
              <input
                className={inputClass}
                type="number"
                dir="ltr"
                value={meta.validationMax ?? ""}
                onChange={(e) =>
                  setMeta({
                    ...meta,
                    validationMax: e.target.value === "" ? null : Number(e.target.value),
                  })
                }
              />
            </label>
            <label className="text-sm font-medium text-gray-700">
              حداکثر طول متن
              <input
                className={inputClass}
                type="number"
                dir="ltr"
                value={meta.validationMaxLength ?? ""}
                onChange={(e) =>
                  setMeta({
                    ...meta,
                    validationMaxLength: e.target.value === "" ? null : Number(e.target.value),
                  })
                }
              />
            </label>
          </div>
          <div className="mt-4 flex flex-wrap gap-4 text-sm">
            {(
              [
                ["isRequired", "الزامی"],
                ["isFilterable", "قابل فیلتر"],
                ["isComparable", "قابل مقایسه"],
                ["isMultivalue", "چندمقداری"],
                ["isActive", "فعال"],
              ] as const
            ).map(([key, label]) => (
              <label key={key} className="flex items-center gap-2">
                <input
                  type="checkbox"
                  className="size-4 rounded border-gray-300"
                  checked={Boolean(meta[key])}
                  onChange={(e) => setMeta({ ...meta, [key]: e.target.checked })}
                />
                {label}
              </label>
            ))}
          </div>
          <div className="mt-4 flex gap-2">
            <button type="button" className={btnPrimary} disabled={busy} onClick={() => void onSaveMeta()}>
              ذخیره فراداده
            </button>
            <button type="button" className={btnSecondary} onClick={() => { setEditId(null); setEditRow(null); }}>
              انصراف
            </button>
          </div>
        </section>
      ) : null}

      {impactOpen && impactData ? (
        <section className={`${cardClass} border-amber-200 bg-amber-50`} data-testid="variant-capability-impact-dialog">
          <h2 className="text-base font-semibold text-amber-900">{VARIANT_AXIS_DISABLE_IN_USE_TITLE.fa}</h2>
          <p className="mt-2 text-sm text-amber-800">
            {impactData.categoryBindingCount} دسته با binding تنوع فعال · {impactData.productCount} محصول ·{" "}
            {impactData.variantCombinationCount} محور محصول
          </p>
          {impactData.affectedCategories.length > 0 ? (
            <ul className="mt-3 max-h-40 space-y-1 overflow-y-auto text-sm text-amber-900">
              {impactData.affectedCategories.map((row) => (
                <li key={row.categoryId}>
                  {row.name} ({row.variantBindingCount})
                </li>
              ))}
            </ul>
          ) : null}
          <p className="mt-3 text-xs text-amber-700">
            تا زمانی که استفادهٔ فعال باقی است، امکان حذف capability وجود ندارد. ابتدا binding تنوع را از دسته‌های
            تحت تأثیر بردارید.
          </p>
          <div className="mt-4 flex gap-2">
            <button type="button" className={btnSecondary} onClick={() => { setImpactOpen(false); setEditAxisAllowed(true); }}>
              انصراف
            </button>
          </div>
        </section>
      ) : null}

      {confirmDisableOpen ? (
        <section className={cardClass} data-testid="variant-capability-disable-confirm">
          <h2 className="text-base font-semibold text-gray-900">تأیید غیرفعال‌سازی</h2>
          <p className="mt-2 text-sm text-gray-600">{VARIANT_AXIS_DISABLE_ZERO_USAGE_CONFIRM.fa}</p>
          <div className="mt-4 flex gap-2">
            <button type="button" className={btnPrimary} disabled={busy} onClick={() => void applyAxisCapability(false)}>
              غیرفعال‌سازی
            </button>
            <button
              type="button"
              className={btnSecondary}
              onClick={() => { setConfirmDisableOpen(false); setEditAxisAllowed(true); }}
            >
              انصراف
            </button>
          </div>
        </section>
      ) : null}

      {optionDefId ? (
        <section className={cardClass}>
          <h2 className="text-base font-semibold text-gray-900">افزودن گزینهٔ شمارشی</h2>
          <p className="mt-1 text-xs text-gray-500" dir="ltr">
            definitionId: {optionDefId}
          </p>
          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <label className="text-sm font-medium text-gray-700">
              کد گزینه
              <input className={inputClass} dir="ltr" value={optionCode} onChange={(e) => setOptionCode(e.target.value)} />
            </label>
            <label className="text-sm font-medium text-gray-700">
              نام فارسی
              <input className={inputClass} value={optionName} onChange={(e) => setOptionName(e.target.value)} />
            </label>
          </div>
          {lastOptionId ? (
            <p className="mt-2 text-xs text-emerald-700" dir="ltr">
              optionId: {lastOptionId}
            </p>
          ) : null}
          <p className="mt-2 text-xs text-gray-500">
            Host فهرست گزینه‌ها را در GET تعریف برنمی‌گرداند؛ شناسهٔ گزینه پس از ایجاد اینجا نمایش داده می‌شود.
          </p>
          <div className="mt-4 flex gap-2">
            <button
              type="button"
              className={btnPrimary}
              disabled={busy || !optionCode.trim()}
              onClick={() => void onAddOption()}
            >
              افزودن گزینه
            </button>
            <button type="button" className={btnSecondary} onClick={() => setOptionDefId(null)}>
              بستن
            </button>
          </div>
        </section>
      ) : null}
    </div>
  );
}

/**
 * مدیریت schema مؤثر رده و پیوند تعاریف.
 */
export function CategorySchemaScreen() {
  const searchParams = useSearchParams();
  const initialCategoryId = searchParams.get("categoryId") ?? "";
  const [categoryId, setCategoryId] = useState(initialCategoryId);
  const [rows, setRows] = useState<EffectiveSchemaEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [bindDefId, setBindDefId] = useState("");
  const [bindOrder, setBindOrder] = useState("0");

  useEffect(() => {
    if (initialCategoryId) {
      setCategoryId(initialCategoryId);
    }
  }, [initialCategoryId]);

  const load = useCallback(async (id: string) => {
    if (!id.trim()) {
      setRows([]);
      return;
    }
    setLoading(true);
    setError(null);
    const result = await loadEffectiveCategorySchema(id.trim());
    setLoading(false);
    if (result.state === "denied") {
      setError("دسترسی مجاز نیست");
      setRows([]);
      return;
    }
    if (result.state !== "ok" || !result.data) {
      setError(result.message ?? "بارگذاری schema ناموفق بود");
      setRows([]);
      return;
    }
    setRows(result.data);
  }, []);

  useEffect(() => {
    if (initialCategoryId) {
      void load(initialCategoryId);
    }
  }, [initialCategoryId, load]);

  async function onBind() {
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await bindCategoryAttribute(categoryId.trim(), {
      definitionId: bindDefId.trim(),
      displayOrder: Number(bindOrder) || 0,
      isRequired: false,
      isFilterable: false,
      isVariantAxis: false,
      isComparable: false,
    });
    setBusy(false);
    if (result.state !== "ok") {
      setError(result.message ?? "پیوند ناموفق بود");
      return;
    }
    setSuccess("پیوند ثبت شد");
    setBindDefId("");
    await load(categoryId);
  }

  async function onUnbind(definitionId: string) {
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await unbindCategoryAttribute(categoryId.trim(), definitionId);
    setBusy(false);
    if (result.state !== "ok") {
      setError(result.message ?? "حذف پیوند ناموفق بود");
      return;
    }
    setSuccess("پیوند حذف شد");
    await load(categoryId);
  }

  return (
    <div className="space-y-6" dir="rtl" data-testid="admin-category-schema">
      <div>
        <h1 className="text-2xl font-semibold text-gray-900">Schema رده</h1>
        <p className="mt-1 text-sm text-gray-500">schema مؤثر پس از ارث والدین؛ بدون تولید ماتریس کامل.</p>
      </div>

      <Feedback error={error} success={success} />

      <section className={cardClass}>
        <div className="flex flex-wrap items-end gap-3">
          <label className="min-w-[240px] flex-1 text-sm font-medium text-gray-700">
            شناسه رده (categoryId)
            <input
              className={inputClass}
              dir="ltr"
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
              placeholder="guid"
            />
          </label>
          <button
            type="button"
            className={btnPrimary}
            disabled={loading || !categoryId.trim()}
            onClick={() => void load(categoryId)}
          >
            بارگذاری schema
          </button>
        </div>
      </section>

      <section className={cardClass}>
        <h2 className="text-base font-semibold text-gray-900">schema مؤثر</h2>
        {loading ? (
          <p className="mt-4 text-sm text-gray-500">در حال بارگذاری…</p>
        ) : rows.length === 0 ? (
          <p className="mt-4 text-sm text-gray-500">ردیفی نیست یا رده بارگذاری نشده است.</p>
        ) : (
          <div className="mt-4 overflow-x-auto">
            <table className="w-full min-w-[720px] text-right text-sm">
              <thead className="border-b border-gray-200 text-gray-500">
                <tr>
                  <th className="py-2 font-medium">کد</th>
                  <th className="font-medium">الزامی</th>
                  <th className="font-medium">ارث از</th>
                  <th className="font-medium">محور مجاز</th>
                  <th className="font-medium">ترتیب</th>
                  <th className="font-medium">عملیات</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.definitionId} className="border-b border-gray-100">
                    <td className="py-3 font-medium" dir="ltr">
                      {row.code}
                    </td>
                    <td>{row.isRequired ? "بله" : "خیر"}</td>
                    <td className="max-w-[180px] truncate font-mono text-xs" dir="ltr" title={row.inheritedFromCategoryId}>
                      {row.inheritedFromCategoryId || "—"}
                    </td>
                    <td>{row.isVariantAxisAllowed ? "بله" : "خیر"}</td>
                    <td className="tabular-nums">{row.displayOrder}</td>
                    <td className="py-3">
                      <button
                        type="button"
                        className={btnDanger}
                        disabled={busy}
                        onClick={() => void onUnbind(row.definitionId)}
                      >
                        حذف پیوند
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className={cardClass}>
        <h2 className="text-base font-semibold text-gray-900">پیوند تعریف موجود</h2>
        <div className="mt-4 grid gap-4 sm:grid-cols-2">
          <label className="text-sm font-medium text-gray-700">
            شناسه تعریف (definitionId)
            <input className={inputClass} dir="ltr" value={bindDefId} onChange={(e) => setBindDefId(e.target.value)} />
          </label>
          <label className="text-sm font-medium text-gray-700">
            ترتیب نمایش
            <input className={inputClass} dir="ltr" type="number" value={bindOrder} onChange={(e) => setBindOrder(e.target.value)} />
          </label>
        </div>
        <button
          type="button"
          className={`${btnPrimary} mt-4`}
          disabled={busy || !categoryId.trim() || !bindDefId.trim()}
          onClick={() => void onBind()}
        >
          پیوند به رده
        </button>
      </section>
    </div>
  );
}

/**
 * پنل مقدار ویژگی محصول — به product-attributes-panel منتقل شد (TB-P07-T012).
 * @deprecated از `./product-attributes-panel` استفاده کنید.
 */
export { ProductAttributesPanel } from "./product-attributes-panel.tsx";

/**
 * ویرایشگر فشرده ویژگی/محور برای Seller؛ به productId کاتالوگ نیاز دارد.
 */
export function SellerProductAttributesPanel({
  productId: initialProductId,
}: {
  productId: string | null;
}) {
  const [productId, setProductId] = useState(initialProductId ?? "");
  const [definitionId, setDefinitionId] = useState("");
  const [rawValue, setRawValue] = useState("");
  const [enumOptionId, setEnumOptionId] = useState("");
  const [axisCsv, setAxisCsv] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    if (initialProductId) {
      setProductId(initialProductId);
    }
  }, [initialProductId]);

  async function onSaveAttribute() {
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await setSellerProductAttribute(productId.trim(), definitionId.trim(), {
      rawValue: rawValue.trim() || (enumOptionId.trim() ? "ignored" : ""),
      enumOptionId: enumOptionId.trim() || null,
    });
    setBusy(false);
    if (result.state !== "ok") {
      setError(result.message ?? "ذخیره ویژگی ناموفق بود");
      return;
    }
    setSuccess("مقدار ویژگی ذخیره شد");
  }

  async function onSaveAxes() {
    setBusy(true);
    setError(null);
    setSuccess(null);
    const ordered = axisCsv
      .split(/[\s,]+/)
      .map((s) => s.trim())
      .filter(Boolean);
    const result = await setSellerProductVariantAxes(productId.trim(), ordered);
    setBusy(false);
    if (result.state !== "ok") {
      setError(result.message ?? "ذخیره محورها ناموفق بود");
      return;
    }
    setSuccess("محورهای Variant ذخیره شدند");
  }

  return (
    <section
      className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm"
      data-testid="seller-product-attributes"
      dir="rtl"
    >
      <h2 className="text-base font-semibold">ویژگی‌ها و محورهای Variant</h2>
      <p className="mt-1 text-sm text-muted">
        فروشنده فقط مقدار محصول را می‌نویسد؛ تعریف schema را بازتعریف نمی‌کند.
      </p>

      {!initialProductId ? (
        <p className="mt-3 rounded-xl bg-amber-50 px-3 py-2 text-sm text-amber-900">
          شناسهٔ محصول Catalog در جزئیات Offer نبود. در صورت دانستن، آن را در فیلد زیر وارد کنید.
        </p>
      ) : null}

      <Feedback error={error} success={success} />

      <div className="mt-4 grid gap-3 sm:grid-cols-2">
        <label className="flex flex-col gap-1 text-sm">
          شناسه محصول (productId)
          <input
            className="min-h-11 rounded-ds border border-border bg-surface px-3 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
            dir="ltr"
            value={productId}
            onChange={(e) => setProductId(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          شناسه تعریف (definitionId)
          <input
            className="min-h-11 rounded-ds border border-border bg-surface px-3 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
            dir="ltr"
            value={definitionId}
            onChange={(e) => setDefinitionId(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          مقدار (rawValue)
          <input
            className="min-h-11 rounded-ds border border-border bg-surface px-3 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
            dir="ltr"
            value={rawValue}
            onChange={(e) => setRawValue(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          شناسه گزینه (enumOptionId)
          <input
            className="min-h-11 rounded-ds border border-border bg-surface px-3 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
            dir="ltr"
            value={enumOptionId}
            onChange={(e) => setEnumOptionId(e.target.value)}
          />
        </label>
      </div>
      <button
        type="button"
        disabled={busy || !productId.trim() || !definitionId.trim()}
        onClick={() => void onSaveAttribute()}
        className="mt-4 inline-flex min-h-11 items-center rounded-xl bg-[#E53935] px-5 text-sm font-bold text-white shadow-lg shadow-[#E53935]/30 hover:bg-[#c62828] disabled:opacity-50"
      >
        ذخیره ویژگی
      </button>

      <label className="mt-6 flex flex-col gap-1 text-sm">
        محورهای Variant (شناسه‌ها با کاما)
        <input
          className="min-h-11 rounded-ds border border-border bg-surface px-3 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
          dir="ltr"
          value={axisCsv}
          onChange={(e) => setAxisCsv(e.target.value)}
          placeholder="guid,guid,…"
        />
      </label>
      <button
        type="button"
        disabled={busy || !productId.trim()}
        onClick={() => void onSaveAxes()}
        className="mt-3 inline-flex min-h-11 items-center rounded-xl border border-border bg-white px-5 text-sm font-semibold text-gray-800 hover:bg-gray-50 disabled:opacity-50"
      >
        ذخیره محورها
      </button>
    </section>
  );
}
