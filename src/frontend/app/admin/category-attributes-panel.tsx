"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import { buildCategoryPath, type AppCategoryTreeNode } from "../../design-system/app-category-tree";
import {
  addAttributeOption,
  bindCategoryAttribute,
  createAttributeDefinition,
  listAttributeDefinitions,
  loadEffectiveCategorySchema,
  reorderCategoryBindings,
  unbindCategoryAttribute,
  updateCategoryAttributeBinding,
  valueKindLabel,
  type AttributeDefinition,
  type CatalogAttributeValueKind,
  type EffectiveSchemaEntry,
} from "./catalog-attribute-api.ts";
import { slugifyCategoryName } from "./catalog-category-api.ts";
import { mapAdminErrorMessage } from "./admin-error-map.ts";
import { resolveAdminChromeLocale } from "./admin-chrome-messages.ts";
import {
  valueKindBlocksVariantAxis,
  VARIANT_AXIS_DISABLED_BY_CAPABILITY,
  VARIANT_AXIS_DISABLED_BY_KIND,
} from "./variant-axis-messages.ts";

const VALUE_KINDS: CatalogAttributeValueKind[] = [
  "Text",
  "Number",
  "Boolean",
  "Enumeration",
  "Instant",
];

/** برچسب‌های کاربرپسند برای پرچم‌های schema — بدون اصطلاحات فنی. */
export const ATTRIBUTE_FLAG_LABELS = {
  required: {
    fa: "برای محصولات این دسته باید مقدار داشته باشد.",
    en: "Products in this category must have a value.",
  },
  filterable: {
    fa: "مشتری می‌تواند در صفحه دسته بر اساس این ویژگی فیلتر کند.",
    en: "Customers can filter products by this attribute on the category page.",
  },
  variant: {
    fa: "می‌تواند برای ساخت تنوع‌های محصول مثل رنگ یا سایز استفاده شود.",
    en: "Can be used to build product variants such as color or size.",
  },
  comparable: {
    fa: "در جدول مقایسه محصولات نمایش داده می‌شود.",
    en: "Shown in the product comparison table.",
  },
} as const;

/** برچسب کوتاه چیپ رفتار — قابل انتخاب/حذف مستقل. */
export const ATTRIBUTE_FLAG_CHIP_LABELS = {
  required: { fa: "الزامی", en: "Required" },
  filterable: { fa: "فیلتر", en: "Filter" },
  variant: { fa: "تنوع", en: "Variant" },
  comparable: { fa: "مقایسه", en: "Compare" },
} as const;

/** تبدیل کد داخلی به عنوان قابل‌خواندن (بدون نمایش GUID). */
export function humanizeAttributeCode(code: string): string {
  const trimmed = code.trim();
  if (!trimmed) return "—";
  return trimmed
    .replace(/[-_.]+/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

/** کد پایدار از نام فارسی/لاتین برای ایجاد تعریف. */
export function attributeCodeFromLabel(label: string): string {
  const slug = slugifyCategoryName(label.trim());
  if (slug) return slug.slice(0, 64);
  return `attr-${Date.now().toString(36)}`;
}

/** آیا assignment در همین رده تعریف شده (نه ارثی). */
export function isLocalSchemaEntry(entry: EffectiveSchemaEntry, categoryId: string): boolean {
  return entry.inheritedFromCategoryId === categoryId;
}

export function partitionEffectiveSchema(
  entries: EffectiveSchemaEntry[],
  categoryId: string,
): {
  inherited: EffectiveSchemaEntry[];
  local: EffectiveSchemaEntry[];
  localOverrides: EffectiveSchemaEntry[];
  localOnly: EffectiveSchemaEntry[];
} {
  const inherited: EffectiveSchemaEntry[] = [];
  const local: EffectiveSchemaEntry[] = [];
  const localOverrides: EffectiveSchemaEntry[] = [];
  const localOnly: EffectiveSchemaEntry[] = [];
  for (const row of entries) {
    if (isLocalSchemaEntry(row, categoryId)) {
      local.push(row);
      if (row.isLocalOverride) localOverrides.push(row);
      else localOnly.push(row);
    } else {
      inherited.push(row);
    }
  }
  return { inherited, local, localOverrides, localOnly };
}

/** پیام جلوگیری از افزودن دوبارهٔ ویژگی ارثی. */
export const DUPLICATE_INHERITED_ATTRIBUTE_MESSAGE =
  "این ویژگی از قبل از دسته والد به ارث رسیده است. در صورت نیاز تنظیمات استفاده آن را برای این دسته تغییر دهید.";


function badgeClass(tone: "blue" | "slate" | "amber" | "violet" | "emerald"): string {
  switch (tone) {
    case "blue":
      return "rounded-full bg-blue-50 px-2 py-0.5 text-[11px] font-medium text-blue-800";
    case "amber":
      return "rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-900";
    case "violet":
      return "rounded-full bg-violet-50 px-2 py-0.5 text-[11px] font-medium text-violet-900";
    case "emerald":
      return "rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-900";
    default:
      return "rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-medium text-slate-700";
  }
}

function AttributeBadges({ row }: { row: EffectiveSchemaEntry }) {
  return (
    <div className="flex flex-wrap gap-1">
      {row.isLocalOverride ? (
        <span className={badgeClass("amber")} data-testid="attr-badge-local-override">
          تنظیم اختصاصی
        </span>
      ) : null}
      {row.isRequired ? (
        <span className={badgeClass("amber")} data-testid="attr-badge-required">
          الزامی
        </span>
      ) : null}
      {row.isFilterable ? (
        <span className={badgeClass("blue")} data-testid="attr-badge-filter">
          فیلتر
        </span>
      ) : null}
      {row.isVariantAxis ? (
        <span className={badgeClass("violet")} data-testid="attr-badge-variant">
          تنوع
        </span>
      ) : null}
      {row.isComparable ? (
        <span className={badgeClass("emerald")} data-testid="attr-badge-compare">
          مقایسه
        </span>
      ) : null}
      {!row.definitionIsActive ? (
        <span className={badgeClass("slate")} data-testid="attr-badge-inactive">
          غیرفعال
        </span>
      ) : null}
    </div>
  );
}

function AttributeRowView({
  row,
  sourceLabel,
  showSource,
}: {
  row: EffectiveSchemaEntry;
  sourceLabel?: string;
  showSource?: boolean;
}) {
  return (
    <li
      className="flex flex-col gap-2 rounded-2xl border border-gray-100 bg-white px-4 py-3 sm:flex-row sm:items-center sm:justify-between"
      data-testid={`category-attribute-row-${row.code}`}
    >
      <div className="min-w-0">
        <div className="font-medium text-slate-900">{humanizeAttributeCode(row.code)}</div>
        <div className="mt-0.5 text-xs text-slate-500">
          نوع: {valueKindLabel(row.valueKind)}
          {row.unit ? ` · واحد: ${row.unit}` : ""}
        </div>
        {showSource && sourceLabel ? (
          <div className="mt-1 text-xs text-slate-500" data-testid="attr-source-category">
            به ارث رسیده از {sourceLabel}
          </div>
        ) : null}
      </div>
      <AttributeBadges row={row} />
    </li>
  );
}

function AttributeRowEdit({
  row,
  index,
  total,
  busy,
  onMoveUp,
  onMoveDown,
  onRemove,
  onConfigure,
}: {
  row: EffectiveSchemaEntry;
  index: number;
  total: number;
  busy: boolean;
  onMoveUp: () => void;
  onMoveDown: () => void;
  onRemove: () => void;
  onConfigure: () => void;
}) {
  const isOverride = row.isLocalOverride;
  return (
    <li
      className="flex flex-col gap-2 rounded-2xl border border-gray-100 bg-white px-4 py-3 sm:flex-row sm:items-center sm:justify-between"
      data-testid={`category-attribute-edit-row-${row.code}`}
    >
      <div className="min-w-0">
        <div className="font-medium text-slate-900">{humanizeAttributeCode(row.code)}</div>
        <div className="mt-0.5 text-xs text-slate-500">نوع: {valueKindLabel(row.valueKind)}</div>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <AttributeBadges row={row} />
        <div className="flex gap-1">
          <button
            type="button"
            className="inline-flex min-h-9 items-center justify-center rounded-lg border border-gray-200 px-3 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-40"
            disabled={busy}
            onClick={onConfigure}
            data-testid={`attr-configure-${row.code}`}
          >
            تنظیم رفتار
          </button>
          <button
            type="button"
            className="inline-flex min-h-9 min-w-9 items-center justify-center rounded-lg border border-gray-200 text-sm disabled:opacity-40"
            disabled={busy || index === 0}
            onClick={onMoveUp}
            aria-label="بالا"
            data-testid={`attr-move-up-${row.code}`}
          >
            ↑
          </button>
          <button
            type="button"
            className="inline-flex min-h-9 min-w-9 items-center justify-center rounded-lg border border-gray-200 text-sm disabled:opacity-40"
            disabled={busy || index >= total - 1}
            onClick={onMoveDown}
            aria-label="پایین"
            data-testid={`attr-move-down-${row.code}`}
          >
            ↓
          </button>
          <button
            type="button"
            className="inline-flex min-h-9 items-center justify-center rounded-lg border border-red-200 px-3 text-xs font-medium text-red-700 hover:bg-red-50 disabled:opacity-40"
            disabled={busy}
            onClick={onRemove}
            data-testid={isOverride ? `attr-reset-override-${row.code}` : `attr-remove-${row.code}`}
          >
            {isOverride ? "بازگشت به تنظیمات والد" : "حذف از این دسته"}
          </button>
        </div>
      </div>
    </li>
  );
}

interface BindFlags {
  isRequired: boolean;
  isFilterable: boolean;
  isVariantAxis: boolean;
  isComparable: boolean;
}

const defaultBindFlags = (): BindFlags => ({
  isRequired: false,
  isFilterable: false,
  isVariantAxis: false,
  isComparable: false,
});

function flagsFromEntry(row: EffectiveSchemaEntry): BindFlags {
  return {
    isRequired: row.isRequired,
    isFilterable: row.isFilterable,
    isVariantAxis: row.isVariantAxis,
    isComparable: row.isComparable,
  };
}

function BindFlagsEditor({
  flags,
  valueKind,
  variantCapabilityAllowed,
  onChange,
  locale = "fa",
}: {
  flags: BindFlags;
  valueKind: CatalogAttributeValueKind;
  variantCapabilityAllowed: boolean;
  onChange: (next: BindFlags) => void;
  locale?: "fa" | "en";
}) {
  const variantDisabledByKind = valueKindBlocksVariantAxis(valueKind);
  const variantDisabled =
    variantDisabledByKind || !variantCapabilityAllowed;

  const chips: Array<{
    key: keyof BindFlags;
    chip: string;
    detail: string;
    checked: boolean;
    disabled?: boolean;
    testId: string;
  }> = [
    {
      key: "isRequired",
      chip: ATTRIBUTE_FLAG_CHIP_LABELS.required[locale],
      detail: ATTRIBUTE_FLAG_LABELS.required[locale],
      checked: flags.isRequired,
      testId: "attr-flag-required",
    },
    {
      key: "isFilterable",
      chip: ATTRIBUTE_FLAG_CHIP_LABELS.filterable[locale],
      detail: ATTRIBUTE_FLAG_LABELS.filterable[locale],
      checked: flags.isFilterable,
      testId: "attr-flag-filterable",
    },
    {
      key: "isVariantAxis",
      chip: ATTRIBUTE_FLAG_CHIP_LABELS.variant[locale],
      detail: ATTRIBUTE_FLAG_LABELS.variant[locale],
      checked: flags.isVariantAxis,
      disabled: variantDisabled,
      testId: "attr-flag-variant",
    },
    {
      key: "isComparable",
      chip: ATTRIBUTE_FLAG_CHIP_LABELS.comparable[locale],
      detail: ATTRIBUTE_FLAG_LABELS.comparable[locale],
      checked: flags.isComparable,
      testId: "attr-flag-comparable",
    },
  ];

  return (
    <div
      className="space-y-2 rounded-xl border border-gray-100 bg-slate-50 p-3 text-sm"
      role="group"
      aria-label={locale === "en" ? "Attribute behavior" : "رفتار ویژگی"}
      data-testid="attr-behavior-chips"
    >
      <div className="flex flex-wrap gap-2" dir={locale === "en" ? "ltr" : "rtl"}>
        {chips.map((chip) => {
          const selected = chip.checked;
          return (
            <button
              key={chip.key}
              type="button"
              role="switch"
              aria-checked={selected}
              aria-label={chip.detail}
              title={chip.detail}
              disabled={chip.disabled}
              data-testid={chip.testId}
              onClick={() => {
                if (chip.disabled) return;
                onChange({ ...flags, [chip.key]: !selected });
              }}
              onKeyDown={(e) => {
                if (chip.disabled) return;
                if (e.key === " " || e.key === "Enter") {
                  e.preventDefault();
                  onChange({ ...flags, [chip.key]: !selected });
                }
              }}
              className={
                chip.disabled
                  ? "inline-flex min-h-9 cursor-not-allowed items-center rounded-full border border-slate-200 bg-slate-100 px-3 text-xs font-medium text-slate-400"
                  : selected
                    ? "inline-flex min-h-9 items-center rounded-full border border-[#2563EB] bg-[#2563EB] px-3 text-xs font-semibold text-white shadow-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500"
                    : "inline-flex min-h-9 items-center rounded-full border border-slate-300 bg-white px-3 text-xs font-medium text-slate-700 hover:bg-slate-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500"
              }
            >
              {chip.chip}
            </button>
          );
        })}
      </div>
      <ul className="space-y-1 text-xs text-slate-600" data-testid="attr-behavior-explanations">
        {chips.map((chip) => (
          <li key={`help-${chip.key}`}>
            <span className="font-medium text-slate-800">{chip.chip}:</span> {chip.detail}
          </li>
        ))}
      </ul>
      {variantDisabled ? (
        <p className="text-xs text-slate-500" data-testid="attr-variant-disabled-reason">
          {variantDisabledByKind
            ? VARIANT_AXIS_DISABLED_BY_KIND[locale].title
            : VARIANT_AXIS_DISABLED_BY_CAPABILITY[locale].title}
          <span className="mt-1 block text-slate-400">
            {variantDisabledByKind
              ? VARIANT_AXIS_DISABLED_BY_KIND[locale].detail
              : VARIANT_AXIS_DISABLED_BY_CAPABILITY[locale].detail}
          </span>
        </p>
      ) : null}
    </div>
  );
}

/**
 * تب ویژگی‌های workspace — schema مؤثر با ارث‌بری و assignment محلی.
 */
export function CategoryAttributesPanel({
  categoryId,
  treeNodes,
  isEdit,
  canEdit: _canEdit,
  busy: externalBusy,
}: {
  categoryId: string;
  treeNodes: AppCategoryTreeNode[];
  isEdit: boolean;
  canEdit: boolean;
  busy?: boolean;
}) {
  void _canEdit;
  const [schema, setSchema] = useState<EffectiveSchemaEntry[]>([]);
  const [definitions, setDefinitions] = useState<AttributeDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [addOpen, setAddOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [addSearch, setAddSearch] = useState("");
  const [selectedDefId, setSelectedDefId] = useState<string | null>(null);
  const [bindFlags, setBindFlags] = useState<BindFlags>(defaultBindFlags());
  const [addDuplicateMessage, setAddDuplicateMessage] = useState<string | null>(null);

  const [createName, setCreateName] = useState("");
  const [createKind, setCreateKind] = useState<CatalogAttributeValueKind>("Text");
  const [createMultivalue, setCreateMultivalue] = useState(false);
  const [createFlags, setCreateFlags] = useState<BindFlags>(defaultBindFlags());
  const [createAdvanced, setCreateAdvanced] = useState(false);
  const [createCode, setCreateCode] = useState("");
  const [createOptions, setCreateOptions] = useState<{ code: string; name: string }[]>([]);
  const [newOptionName, setNewOptionName] = useState("");

  const [configureOpen, setConfigureOpen] = useState(false);
  const [configureTarget, setConfigureTarget] = useState<EffectiveSchemaEntry | null>(null);
  const [configureFlags, setConfigureFlags] = useState<BindFlags>(defaultBindFlags());
  const [configureMode, setConfigureMode] = useState<"local-update" | "inherited-override">("local-update");

  const locale = resolveAdminChromeLocale();
  const toUiError = useCallback(
    (raw: string | null | undefined, fallback?: string) =>
      mapAdminErrorMessage(raw || fallback || "host-unreachable", locale),
    [locale],
  );
  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const n of treeNodes) map.set(n.id, n.name);
    return map;
  }, [treeNodes]);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    const [schemaResult, defsResult] = await Promise.all([
      loadEffectiveCategorySchema(categoryId),
      listAttributeDefinitions(),
    ]);
    setLoading(false);
    if (schemaResult.state === "denied" || defsResult.state === "denied") {
      setError(toUiError("admin.authorization.denied"));
      setSchema([]);
      setDefinitions([]);
      return;
    }
    if (schemaResult.state !== "ok" || !schemaResult.data) {
      setError(toUiError(schemaResult.message, "بارگذاری schema ناموفق بود"));
      setSchema([]);
    } else {
      setSchema(schemaResult.data);
    }
    if (defsResult.state === "ok" && defsResult.data) {
      setDefinitions(defsResult.data);
    } else {
      setDefinitions([]);
    }
  }, [categoryId, toUiError]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const { inherited, local } = useMemo(
    () => partitionEffectiveSchema(schema, categoryId),
    [schema, categoryId],
  );

  const effectiveIds = useMemo(() => new Set(schema.map((r) => r.definitionId)), [schema]);

  const addCandidates = useMemo(() => {
    const q = addSearch.trim().toLowerCase();
    return definitions
      .filter((d) => d.isActive && !effectiveIds.has(d.definitionId))
      .filter((d) => {
        if (!q) return true;
        const label = humanizeAttributeCode(d.code).toLowerCase();
        return label.includes(q) || d.code.toLowerCase().includes(q);
      })
      .sort((a, b) => a.code.localeCompare(b.code, "fa"));
  }, [definitions, effectiveIds, addSearch]);

  const selectedDefinition = selectedDefId
    ? definitions.find((d) => d.definitionId === selectedDefId) ?? null
    : null;

  const resolveSourceName = (sourceCategoryId: string): string => {
    if (sourceCategoryId === categoryId) return "این دسته";
    const direct = categoryNameById.get(sourceCategoryId);
    if (direct) return direct;
    const path = buildCategoryPath(treeNodes, sourceCategoryId);
    return path.length ? path.join(" / ") : "دستهٔ والد";
  };

  const runMutation = async (fn: () => Promise<void>) => {
    if (externalBusy || busy) return;
    setBusy(true);
    setError(null);
    try {
      await fn();
      await reload();
    } finally {
      setBusy(false);
    }
  };

  const toBindInput = (flags: BindFlags) => ({
    isRequired: flags.isRequired,
    isFilterable: flags.isFilterable,
    isVariantAxis: flags.isVariantAxis,
    isComparable: flags.isComparable,
  });

  const openConfigureLocal = (row: EffectiveSchemaEntry) => {
    setConfigureTarget(row);
    setConfigureFlags(flagsFromEntry(row));
    setConfigureMode("local-update");
    setConfigureOpen(true);
  };

  const openConfigureInherited = (row: EffectiveSchemaEntry) => {
    setConfigureTarget(row);
    setConfigureFlags(flagsFromEntry(row));
    setConfigureMode("inherited-override");
    setConfigureOpen(true);
  };

  const handleBindExisting = async () => {
    if (!selectedDefinition) return;
    if (effectiveIds.has(selectedDefinition.definitionId)) {
      setAddDuplicateMessage(DUPLICATE_INHERITED_ATTRIBUTE_MESSAGE);
      toast.error(DUPLICATE_INHERITED_ATTRIBUTE_MESSAGE);
      return;
    }
    await runMutation(async () => {
      const result = await bindCategoryAttribute(categoryId, {
        definitionId: selectedDefinition.definitionId,
        displayOrder: local.length,
        ...toBindInput(bindFlags),
      });
      if (result.state !== "ok") {
        const msg = result.message ?? "افزودن ویژگی ناموفق بود";
        if (/ارث|inherited|پیوند شده|duplicate/i.test(msg)) {
          setAddDuplicateMessage(DUPLICATE_INHERITED_ATTRIBUTE_MESSAGE);
        }
        throw new Error(msg);
      }
      toast.success("ویژگی به این دسته اضافه شد");
      setAddOpen(false);
      setAddSearch("");
      setSelectedDefId(null);
      setBindFlags(defaultBindFlags());
      setAddDuplicateMessage(null);
    });
  };

  const handleCreateAndBind = async () => {
    const name = createName.trim();
    if (!name) return;
    const code = (createAdvanced && createCode.trim()) || attributeCodeFromLabel(name);
    const variantAllowed =
      createFlags.isVariantAxis
      && !valueKindBlocksVariantAxis(createKind);
    const safeFlags: BindFlags = {
      ...createFlags,
      isVariantAxis: variantAllowed,
    };
    await runMutation(async () => {
      const createResult = await createAttributeDefinition({
        code,
        valueKind: createKind,
        isVariantAxisAllowed: variantAllowed,
        localizedNames: { "fa-IR": name, "en-US": name },
        metadata: {
          isRequired: false,
          isFilterable: false,
          isComparable: false,
          isMultivalue: createMultivalue,
          displayOrder: 0,
          isActive: true,
        },
      });
      if (createResult.state !== "ok" || !createResult.data) {
        throw new Error(createResult.message ?? "ایجاد ویژگی ناموفق بود");
      }
      const definitionId = createResult.data.definitionId;
      if (createKind === "Enumeration") {
        for (const opt of createOptions) {
          const optCode = attributeCodeFromLabel(opt.code || opt.name);
          const optResult = await addAttributeOption(definitionId, optCode, {
            "fa-IR": opt.name.trim() || optCode,
            "en-US": opt.name.trim() || optCode,
          });
          if (optResult.state !== "ok") {
            throw new Error(optResult.message ?? "افزودن گزینه ناموفق بود");
          }
        }
      }
      const bindResult = await bindCategoryAttribute(categoryId, {
        definitionId,
        displayOrder: local.length,
        ...toBindInput(safeFlags),
      });
      if (bindResult.state !== "ok") {
        throw new Error(bindResult.message ?? "پیوند به دسته ناموفق بود");
      }
      toast.success("ویژگی جدید ایجاد و به این دسته اضافه شد");
      setCreateOpen(false);
      setCreateName("");
      setCreateKind("Text");
      setCreateMultivalue(false);
      setCreateFlags(defaultBindFlags());
      setCreateAdvanced(false);
      setCreateCode("");
      setCreateOptions([]);
      setNewOptionName("");
    });
  };

  const handleSaveConfigure = async () => {
    if (!configureTarget) return;
    await runMutation(async () => {
      if (configureMode === "local-update") {
        const result = await updateCategoryAttributeBinding(
          categoryId,
          configureTarget.definitionId,
          toBindInput(configureFlags),
        );
        if (result.state !== "ok") {
          throw new Error(result.message ?? "ذخیره تنظیمات ناموفق بود");
        }
        toast.success("رفتار ویژگی برای این دسته به‌روز شد");
      } else {
        const result = await bindCategoryAttribute(categoryId, {
          definitionId: configureTarget.definitionId,
          displayOrder: local.length,
          ...toBindInput(configureFlags),
        });
        if (result.state !== "ok") {
          throw new Error(result.message ?? "تنظیم برای این دسته ناموفق بود");
        }
        toast.success("تنظیمات فقط برای این دسته اعمال شد");
      }
      setConfigureOpen(false);
      setConfigureTarget(null);
      setConfigureFlags(defaultBindFlags());
    });
  };

  const handleReorderLocal = async (fromIndex: number, toIndex: number) => {
    if (toIndex < 0 || toIndex >= local.length) return;
    const ids = local.map((r) => r.definitionId);
    const [moved] = ids.splice(fromIndex, 1);
    ids.splice(toIndex, 0, moved!);
    await runMutation(async () => {
      const result = await reorderCategoryBindings(categoryId, ids);
      if (result.state !== "ok") {
        throw new Error(result.message ?? "تغییر ترتیب ناموفق بود");
      }
      toast.success("ترتیب ویژگی‌ها به‌روز شد");
    });
  };

  const handleRemoveLocal = async (row: EffectiveSchemaEntry) => {
    const isOverride = row.isLocalOverride;
    const confirmMsg = isOverride
      ? "تنظیم اختصاصی این دسته حذف شود و رفتار از والد بازگردد؟ تعریف سراسری ویژگی حذف نمی‌شود."
      : "این ویژگی فقط از این دسته حذف می‌شود؛ تعریف سراسری باقی می‌ماند.";
    if (!window.confirm(confirmMsg)) {
      return;
    }
    await runMutation(async () => {
      const result = await unbindCategoryAttribute(categoryId, row.definitionId);
      if (result.state !== "ok") {
        throw new Error(result.message ?? (isOverride ? "بازگشت به والد ناموفق بود" : "حذف از دسته ناموفق بود"));
      }
      toast.success(isOverride ? "به تنظیمات والد بازگشت" : "ویژگی از این دسته حذف شد");
    });
  };

  const combinedBusy = busy || Boolean(externalBusy);

  if (loading) {
    return (
      <p className="text-sm text-slate-500" data-testid="category-attributes-loading">
        در حال بارگذاری ویژگی‌ها…
      </p>
    );
  }

  return (
    <div className="space-y-5" data-testid="category-attributes-panel">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-sm text-slate-600">
            ویژگی‌هایی که محصولات این دسته باید داشته باشند — شامل موارد ارث‌برده از والد و موارد
            اختصاصی این دسته.
          </p>
        </div>
      </div>

      {error ? (
        <p className="text-sm text-red-600" role="alert" data-testid="category-attributes-error">
          {error}
        </p>
      ) : null}

      {!isEdit ? (
        <div className="space-y-6" data-testid="category-attributes-view" data-form-mode="view">
          <section data-testid="category-attributes-inherited-section">
            <h2 className="text-sm font-semibold text-slate-800">ویژگی‌های به‌ارث‌رسیده</h2>
            <p className="mt-1 text-xs text-slate-500" data-testid="category-attributes-inherited-help">
              این ویژگی‌ها از دسته‌های والد به ارث رسیده‌اند و برای محصولات این دسته نیز قابل استفاده‌اند.
            </p>
            {inherited.length === 0 ? (
              <p className="mt-2 text-sm text-slate-500">ویژگی ارثی ثبت نشده است.</p>
            ) : (
              <ul className="mt-3 space-y-2">
                {inherited.map((row) => (
                  <AttributeRowView
                    key={row.definitionId}
                    row={row}
                    showSource
                    sourceLabel={resolveSourceName(row.inheritedFromCategoryId)}
                  />
                ))}
              </ul>
            )}
          </section>

          <section data-testid="category-attributes-local-section">
            <h2 className="text-sm font-semibold text-slate-800">ویژگی‌های مخصوص این دسته</h2>
            {local.length === 0 ? (
              <p className="mt-2 text-sm text-slate-500">هنوز ویژگی اختصاصی برای این دسته تعریف نشده است.</p>
            ) : (
              <ul className="mt-3 space-y-2">
                {local.map((row) => (
                  <AttributeRowView key={row.definitionId} row={row} />
                ))}
              </ul>
            )}
          </section>
        </div>
      ) : (
        <div className="space-y-5" data-testid="category-attributes-edit" data-form-mode="edit">
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95 disabled:opacity-50"
              disabled={combinedBusy}
              onClick={() => {
                setAddOpen(true);
                setBindFlags(defaultBindFlags());
                setSelectedDefId(null);
                setAddSearch("");
                setAddDuplicateMessage(null);
              }}
              data-testid="category-attributes-add-existing"
            >
              افزودن ویژگی
            </button>
            <button
              type="button"
              className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-semibold text-slate-800 hover:bg-slate-50 disabled:opacity-50"
              disabled={combinedBusy}
              onClick={() => {
                setCreateOpen(true);
                setCreateFlags(defaultBindFlags());
              }}
              data-testid="category-attributes-create-new"
            >
              ایجاد ویژگی جدید
            </button>
          </div>

          {inherited.length > 0 ? (
            <section data-testid="category-attributes-inherited-edit-section">
              <h2 className="text-sm font-semibold text-slate-800">ارث‌برده‌شده</h2>
              <ul className="mt-3 space-y-2">
                {inherited.map((row) => (
                  <li
                    key={row.definitionId}
                    className="flex flex-col gap-2 rounded-2xl border border-gray-100 bg-slate-50 px-4 py-3 sm:flex-row sm:items-center sm:justify-between"
                    data-testid={`category-attribute-inherited-edit-${row.code}`}
                  >
                    <div className="min-w-0 flex-1">
                      <div className="font-medium text-slate-900">{humanizeAttributeCode(row.code)}</div>
                      <div className="mt-0.5 text-xs text-slate-500">
                        نوع: {valueKindLabel(row.valueKind)}
                      </div>
                      <div className="mt-1 text-xs text-slate-500" data-testid="attr-source-category">
                        به ارث رسیده از {resolveSourceName(row.inheritedFromCategoryId)}
                      </div>
                    </div>
                    <div className="flex flex-wrap items-center gap-2">
                      <AttributeBadges row={row} />
                      <button
                        type="button"
                        className="inline-flex min-h-9 shrink-0 items-center justify-center rounded-lg border border-gray-200 bg-white px-3 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-40"
                        disabled={combinedBusy}
                        onClick={() => openConfigureInherited(row)}
                        data-testid={`attr-customize-inherited-${row.code}`}
                      >
                        تنظیم اختصاصی برای این دسته
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            </section>
          ) : null}

          <section data-testid="category-attributes-local-edit-section">
            <h2 className="text-sm font-semibold text-slate-800">ویژگی‌های مخصوص این دسته</h2>
            {local.length === 0 ? (
              <p className="mt-2 text-sm text-slate-500">با دکمه‌های بالا ویژگی اضافه کنید.</p>
            ) : (
              <ul className="mt-3 space-y-2">
                {local.map((row, index) => (
                  <AttributeRowEdit
                    key={row.definitionId}
                    row={row}
                    index={index}
                    total={local.length}
                    busy={combinedBusy}
                    onMoveUp={() => void handleReorderLocal(index, index - 1)}
                    onMoveDown={() => void handleReorderLocal(index, index + 1)}
                    onRemove={() => void handleRemoveLocal(row)}
                    onConfigure={() => openConfigureLocal(row)}
                  />
                ))}
              </ul>
            )}
          </section>
        </div>
      )}

      {addOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="add-attr-title"
          data-testid="category-attributes-add-dialog"
        >
          <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-5 shadow-xl">
            <h3 id="add-attr-title" className="text-lg font-semibold text-slate-900">
              افزودن ویژگی موجود
            </h3>
            <p className="mt-2 text-xs text-slate-500" data-testid="attr-add-inherited-hint">
              ویژگی‌هایی که از والد به ارث رسیده‌اند در این فهرست نیستند؛ برای تغییر رفتار آن‌ها از
              «تنظیم اختصاصی برای این دسته» استفاده کنید.
            </p>
            <label className="mt-4 block text-sm font-medium text-slate-700">
              جستجو
              <input
                className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={addSearch}
                onChange={(e) => {
                  setAddSearch(e.target.value);
                  setAddDuplicateMessage(null);
                }}
                placeholder="نام یا نوع ویژگی"
                data-testid="attr-add-search"
              />
            </label>
            {addDuplicateMessage ? (
              <p
                className="mt-3 text-sm text-amber-800"
                role="alert"
                data-testid="attr-add-duplicate-inherited"
              >
                {addDuplicateMessage}
              </p>
            ) : null}
            <ul className="mt-3 max-h-48 space-y-1 overflow-y-auto" data-testid="attr-add-candidates">
              {addCandidates.length === 0 ? (
                <li className="text-sm text-slate-500">ویژگی قابل افزودن یافت نشد.</li>
              ) : (
                addCandidates.map((d) => {
                  const selected = selectedDefId === d.definitionId;
                  return (
                    <li key={d.definitionId}>
                      <button
                        type="button"
                        className={
                          selected
                            ? "w-full rounded-xl border border-[#2563EB] bg-blue-50 px-3 py-2 text-start text-sm"
                            : "w-full rounded-xl border border-gray-100 px-3 py-2 text-start text-sm hover:bg-slate-50"
                        }
                        onClick={() => {
                          setSelectedDefId(d.definitionId);
                          setBindFlags(defaultBindFlags());
                          setAddDuplicateMessage(null);
                        }}
                        data-testid={`attr-add-option-${d.code}`}
                      >
                        <span className="font-medium">{humanizeAttributeCode(d.code)}</span>
                        <span className="mt-0.5 block text-xs text-slate-500">
                          {valueKindLabel(d.valueKind)}
                        </span>
                      </button>
                    </li>
                  );
                })
              )}
            </ul>
            {selectedDefinition ? (
              <div className="mt-4">
                <BindFlagsEditor
                  flags={bindFlags}
                  valueKind={selectedDefinition.valueKind}
                  variantCapabilityAllowed={selectedDefinition.isVariantAxisAllowed}
                  onChange={setBindFlags}
                  locale={locale}
                />
              </div>
            ) : null}
            <div className="mt-5 flex flex-wrap justify-end gap-2">
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-4 text-sm"
                onClick={() => setAddOpen(false)}
                disabled={combinedBusy}
                data-testid="attr-add-cancel"
              >
                انصراف
              </button>
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white disabled:opacity-50"
                disabled={combinedBusy || !selectedDefinition}
                onClick={() => void handleBindExisting().catch((e) => {
                  const msg = toUiError(e instanceof Error ? e.message : null);
                  setError(msg);
                  toast.error(msg);
                })}
                data-testid="attr-add-confirm"
              >
                افزودن به این دسته
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {createOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="create-attr-title"
          data-testid="category-attributes-create-dialog"
        >
          <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-5 shadow-xl">
            <h3 id="create-attr-title" className="text-lg font-semibold text-slate-900">
              ایجاد ویژگی جدید
            </h3>
            <div className="mt-4 space-y-4">
              <label className="block text-sm font-medium text-slate-700">
                نام
                <input
                  className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={createName}
                  onChange={(e) => setCreateName(e.target.value)}
                  data-testid="attr-create-name"
                />
              </label>
              <label className="block text-sm font-medium text-slate-700">
                نوع مقدار
                <select
                  className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 px-3 text-sm"
                  value={createKind}
                  onChange={(e) => setCreateKind(e.target.value as CatalogAttributeValueKind)}
                  data-testid="attr-create-kind"
                >
                  {VALUE_KINDS.map((k) => (
                    <option key={k} value={k}>
                      {valueKindLabel(k)}
                    </option>
                  ))}
                </select>
              </label>
              <label className="flex items-center gap-2 text-sm font-medium text-slate-700">
                <input
                  type="checkbox"
                  checked={createMultivalue}
                  onChange={(e) => setCreateMultivalue(e.target.checked)}
                  data-testid="attr-create-multivalue"
                />
                چندمقداری (چند مقدار برای یک محصول)
              </label>
              <BindFlagsEditor
                flags={createFlags}
                valueKind={createKind}
                variantCapabilityAllowed
                onChange={setCreateFlags}
                locale={locale}
              />
              {createKind === "Enumeration" ? (
                <div className="rounded-xl border border-gray-100 p-3">
                  <div className="text-sm font-medium text-slate-700">گزینه‌ها</div>
                  <ul className="mt-2 space-y-1">
                    {createOptions.map((opt, i) => (
                      <li key={`${opt.code}-${i}`} className="text-sm text-slate-700">
                        {opt.name}
                      </li>
                    ))}
                  </ul>
                  <div className="mt-2 flex gap-2">
                    <input
                      className="min-h-10 flex-1 rounded-xl border border-gray-200 px-3 text-sm"
                      value={newOptionName}
                      onChange={(e) => setNewOptionName(e.target.value)}
                      placeholder="نام گزینه"
                      data-testid="attr-create-option-name"
                    />
                    <button
                      type="button"
                      className="rounded-xl border border-gray-200 px-3 text-sm"
                      onClick={() => {
                        const name = newOptionName.trim();
                        if (!name) return;
                        setCreateOptions((prev) => [...prev, { code: attributeCodeFromLabel(name), name }]);
                        setNewOptionName("");
                      }}
                      data-testid="attr-create-option-add"
                    >
                      افزودن
                    </button>
                  </div>
                </div>
              ) : null}
              <button
                type="button"
                className="text-sm font-medium text-[#2563EB]"
                onClick={() => setCreateAdvanced((v) => !v)}
                data-testid="attr-create-advanced-toggle"
              >
                {createAdvanced ? "بستن تنظیمات پیشرفته" : "تنظیمات پیشرفته"}
              </button>
              {createAdvanced ? (
                <label className="block text-sm font-medium text-slate-700">
                  کد فنی (اختیاری)
                  <input
                    className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 px-3 text-sm"
                    dir="ltr"
                    value={createCode}
                    onChange={(e) => setCreateCode(e.target.value)}
                    placeholder={attributeCodeFromLabel(createName || "ویژگی")}
                    data-testid="attr-create-code"
                  />
                </label>
              ) : null}
            </div>
            <div className="mt-5 flex flex-wrap justify-end gap-2">
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-4 text-sm"
                onClick={() => setCreateOpen(false)}
                disabled={combinedBusy}
                data-testid="attr-create-cancel"
              >
                انصراف
              </button>
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white disabled:opacity-50"
                disabled={combinedBusy || !createName.trim()}
                onClick={() => void handleCreateAndBind().catch((e) => {
                  const msg = toUiError(e instanceof Error ? e.message : null);
                  setError(msg);
                  toast.error(msg);
                })}
                data-testid="attr-create-confirm"
              >
                ایجاد و افزودن
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {configureOpen && configureTarget ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="configure-attr-title"
          data-testid="category-attributes-configure-dialog"
        >
          <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-5 shadow-xl">
            <h3 id="configure-attr-title" className="text-lg font-semibold text-slate-900">
              {configureMode === "inherited-override"
                ? "تنظیم اختصاصی برای این دسته"
                : "تنظیم رفتار ویژگی"}
            </h3>
            <p className="mt-2 text-sm text-slate-600">
              {configureMode === "inherited-override"
                ? "این تغییرات فقط برای دستهٔ فعلی اعمال می‌شود و دسته‌های دیگر را تحت تأثیر قرار نمی‌دهد."
                : "رفتار این ویژگی در همین دسته را مشخص کنید."}
            </p>
            <div className="mt-4">
              <BindFlagsEditor
                flags={configureFlags}
                valueKind={configureTarget.valueKind}
                variantCapabilityAllowed={configureTarget.isVariantAxisAllowed}
                onChange={setConfigureFlags}
                locale={locale}
              />
            </div>
            <div className="mt-5 flex flex-wrap justify-end gap-2">
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-4 text-sm"
                onClick={() => {
                  setConfigureOpen(false);
                  setConfigureTarget(null);
                }}
                disabled={combinedBusy}
                data-testid="attr-configure-cancel"
              >
                انصراف
              </button>
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white disabled:opacity-50"
                disabled={combinedBusy}
                onClick={() => void handleSaveConfigure().catch((e) => {
                  const msg = toUiError(e instanceof Error ? e.message : null);
                  setError(msg);
                  toast.error(msg);
                })}
                data-testid="attr-configure-save"
              >
                ذخیره
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
