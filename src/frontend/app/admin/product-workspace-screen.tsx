"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Card, ErrorState, WorkspaceShell, faWorkspaceMessages, useAdminFormMode } from "../../design-system";
import { formatAdminStatus } from "./admin-api";
import { previewProductCategoryChange } from "./catalog-attribute-api";
import { slugifyCategoryName } from "./catalog-category-api";
import { ProductAttributesPanel } from "./product-attributes-panel";
import { ProductMediaPanel } from "./product-media-panel";
import { ProductSeoPanel } from "./product-seo-panel";
import { ProductPublishingPanel } from "./product-publishing-panel";
import { ProductVariantsPanel } from "./product-variants-panel";
import {
  assignAdminProductCategory,
  loadProductWorkspace,
  mutateAdminProductLifecycle,
  updateAdminProductCore,
  type HostReadSource,
} from "./host-client";
import { ProductCategoryPicker } from "./product-category-picker";
import { PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA } from "./product-category-level";
import { type ProductTranslationView, type ProductWorkspaceView } from "./workspace-model";
import { storefrontMediaUrl } from "../storefront/storefront-api";

const sections = [
  { id: "general", label: "عمومی" },
  { id: "translations", label: "ترجمه‌ها" },
  { id: "attributes", label: "ویژگی‌ها" },
  { id: "variants", label: "تنوع‌ها" },
  { id: "media", label: "رسانه" },
  { id: "seo", label: "SEO" },
  { id: "publication", label: "انتشار" },
  { id: "history", label: "تاریخچه" },
];

const TRANSLATION_LOCALES = ["fa-IR", "en"] as const;

const LOCALE_DISPLAY: Record<string, string> = {
  "fa-IR": "فارسی",
  en: "English",
  "en-US": "English",
};

function money(amount: number | undefined, currency: string | undefined): string {
  if (amount == null) {
    return "—";
  }
  const digits = new Intl.NumberFormat("fa-IR").format(amount);
  return currency === "IRR" ? `${digits} ریال` : `${digits} ${currency ?? ""}`.trim();
}

function statusTone(status: string): "success" | "warning" | "neutral" | "danger" {
  if (status === "Published" || status === "Active") return "success";
  if (status === "Draft") return "warning";
  if (status === "Archived") return "neutral";
  return "neutral";
}

function sortedMedia(media: ProductWorkspaceView["media"]) {
  return [...media].sort((a, b) => {
    if (a.primary !== b.primary) return a.primary ? -1 : 1;
    return (a.displayOrder ?? 0) - (b.displayOrder ?? 0);
  });
}

function categoryLabel(view: ProductWorkspaceView): string {
  return view.categoryPath || view.categoryNames.join(" › ") || "بدون دسته";
}

function resolveTranslation(
  view: ProductWorkspaceView,
  locale: string,
): ProductTranslationView | undefined {
  const list = view.translations ?? [];
  return (
    list.find((t) => t.locale === locale) ??
    list.find((t) => t.locale.startsWith(locale.split("-")[0] ?? locale))
  );
}

interface GeneralDraft {
  title: string;
  slug: string;
  shortDescription: string;
  categoryId: string | null;
  slugTouched: boolean;
}

function draftFromView(view: ProductWorkspaceView): GeneralDraft {
  return {
    title: view.title,
    slug: view.slug ?? view.seo.slugSeam ?? "",
    shortDescription: view.shortDescription ?? "",
    categoryId: view.primaryCategoryId ?? null,
    slugTouched: true,
  };
}

function SummaryCard({ label, value, ltr }: { label: string; value: string; ltr?: boolean }) {
  return (
    <div className="rounded-ds border border-border bg-surface p-3">
      <p className="text-sm text-muted">{label}</p>
      <p className="mt-1 text-base font-semibold" dir={ltr ? "ltr" : undefined}>
        {value}
      </p>
    </div>
  );
}

/**
 * Workspace محصول Admin. Commercial چند فروشنده را جدا از Product.Price نشان می‌دهد.
 * SpiceDB در این کامپوننت صدا زده نمی‌شود؛ مجوز از Host می‌آید.
 */
export function ProductWorkspaceScreen({
  productId,
  viewScope = false,
  initialEdit = false,
}: {
  productId: string;
  viewScope?: boolean;
  initialEdit?: boolean;
}) {
  const [view, setView] = useState<ProductWorkspaceView | null>(null);
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [sectionId, setSectionId] = useState("general");
  const [draft, setDraft] = useState<GeneralDraft | null>(null);
  const [conflict, setConflict] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [dirty, setDirty] = useState<Set<string>>(new Set());
  const [denied, setDenied] = useState(false);
  const [busy, setBusy] = useState(false);
  const [translationLocale, setTranslationLocale] = useState<string>("fa-IR");
  const [enteredInitialEdit, setEnteredInitialEdit] = useState(false);

  const canView = Boolean(view?.permissions.canView ?? true);
  const canEdit = Boolean(view?.permissions.canEditCatalog) && !viewScope;
  const formMode = useAdminFormMode({ canView, canEdit });

  const reload = useCallback(() => {
    void loadProductWorkspace(productId, viewScope).then((result) => {
      setSource(result.source);
      setView(result.view);
      setDraft(result.view ? draftFromView(result.view) : null);
      setConflict(null);
      setError(result.message ?? null);
      setDenied(Boolean(result.denied));
    });
  }, [productId, viewScope]);

  useEffect(() => {
    reload();
  }, [reload]);

  useEffect(() => {
    if (!view || enteredInitialEdit || viewScope) return;
    if (initialEdit && canEdit) {
      formMode.onEdit();
      setEnteredInitialEdit(true);
    }
    // onEdit is stable; avoid formMode object identity loops
    // eslint-disable-next-line react-hooks/exhaustive-deps -- enter edit once after load
  }, [view, initialEdit, viewScope, enteredInitialEdit, canEdit]);

  const dirtySections = useMemo(() => dirty, [dirty]);
  const mediaRows = useMemo(() => (view ? sortedMedia(view.media) : []), [view]);
  const primaryMedia = mediaRows.find((row) => row.primary) ?? mediaRows[0] ?? null;

  if (!view) {
    if (denied) {
      return (
        <div className="p-6" data-testid="admin-auth-denied">
          <ErrorState title="دسترسی مجاز نیست" detail="سامانه هویت فعلی را مدیر تشخیص نداد." onRetry={reload} retryLabel={faWorkspaceMessages.retry} />
        </div>
      );
    }
    if (source === "error") {
      return (
        <div className="p-6">
          <ErrorState title="فضای کار محصول از فروشگاه خوانده نشد" detail={error ?? undefined} onRetry={reload} retryLabel={faWorkspaceMessages.retry} />
        </div>
      );
    }
    return <p className="p-6 text-base">در حال بارگذاری فضای کار محصول…</p>;
  }

  const current = view;
  const onHand = view.stock.reduce((sum, row) => sum + row.onHand, 0);
  const reserved = view.stock.reduce((sum, row) => sum + row.reserved, 0);
  const available = view.stock.reduce((sum, row) => sum + row.available, 0);
  const amounts = view.prices.map((row) => row.amountExclusiveOfTax);
  const priceRange = amounts.length
    ? `${money(Math.min(...amounts), view.prices[0]?.currency)} — ${money(Math.max(...amounts), view.prices[0]?.currency)}`
    : "بدون قیمت";
  const canMutateCatalog = view.permissions.canEditCatalog && !viewScope;
  const canPublish = view.permissions.canPublish && !viewScope;
  const isGeneralEdit = formMode.mode === "edit" && sectionId === "general";
  const activeDraft = draft ?? draftFromView(view);

  function markGeneralDirty() {
    formMode.markDirty();
    setDirty(new Set(["general"]));
  }

  function handleEnterEdit() {
    if (!formMode.canEdit) return;
    setDraft(draftFromView(current));
    formMode.onEdit();
    setSectionId("general");
  }

  function handleCancelEdit() {
    if (!formMode.confirmDiscardIfDirty()) return;
    setDraft(draftFromView(current));
    formMode.onCancel();
    setDirty(new Set());
    setConflict(null);
  }

  async function handleSaveGeneral() {
    if (!activeDraft.title.trim()) {
      setError("عنوان لازم است");
      return;
    }
    if (!activeDraft.categoryId) {
      setError("انتخاب دسته لازم است");
      return;
    }
    const categoryChanged = activeDraft.categoryId !== (current.primaryCategoryId ?? null);
    if (!categoryChanged && current.isPrimaryCategoryAssignable === false && current.primaryCategoryId) {
      setError(PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA);
      return;
    }
    setBusy(true);
    setError(null);
    let expectedUpdatedAt = current.catalogUpdatedAt;
    if (categoryChanged && activeDraft.categoryId) {
      const needsConfirm = Boolean(current.primaryCategoryId);
      if (needsConfirm) {
        const preview = await previewProductCategoryChange(
          current.productId,
          activeDraft.categoryId,
          "fa-IR",
        );
        const message =
          preview.state === "ok" && preview.data?.messageFa
            ? `${preview.data.messageFa}\n\nتغییر دسته را تأیید می‌کنید؟`
            : "تغییر دسته ممکن است ویژگی‌ها و تنوع‌های وابسته به schema را تحت تأثیر قرار دهد. ادامه می‌دهید؟";
        if (!window.confirm(message)) {
          setBusy(false);
          return;
        }
      }
      const catResult = await assignAdminProductCategory(
        current.productId,
        {
          categoryId: activeDraft.categoryId,
          confirmSchemaImpact: needsConfirm,
          expectedUpdatedAt,
        },
        viewScope,
      );
      if (!catResult.ok) {
        setBusy(false);
        if (catResult.errorCode === "workspace.catalog.stale") {
          setConflict("این محصول را کاربر دیگری تغییر داده است. نسخهٔ تازه را بارگذاری کنید.");
          return;
        }
        setError(catResult.errorCode);
        return;
      }
      setView(catResult.view);
      expectedUpdatedAt = catResult.view.catalogUpdatedAt;
    }

    const coreResult = await updateAdminProductCore(
      current.productId,
      {
        locale: "fa-IR",
        title: activeDraft.title.trim(),
        slug: activeDraft.slug.trim() || null,
        shortDescription: activeDraft.shortDescription.trim() || null,
        expectedUpdatedAt,
      },
      viewScope,
    );
    setBusy(false);
    if (!coreResult.ok) {
      if (coreResult.errorCode === "workspace.catalog.stale") {
        setConflict("این محصول را کاربر دیگری تغییر داده است. نسخهٔ تازه را بارگذاری کنید.");
        return;
      }
      setError(coreResult.errorCode);
      return;
    }
    setView(coreResult.view);
    setDraft(draftFromView(coreResult.view));
    formMode.onSaved();
    setDirty(new Set());
    setConflict(null);
  }

  async function onAction(actionId: string) {
    if (actionId === "edit") {
      handleEnterEdit();
      return;
    }
    if (actionId === "cancel") {
      handleCancelEdit();
      return;
    }
    if (actionId === "save") {
      await handleSaveGeneral();
      return;
    }
    if (actionId === "publish") {
      if (current.status === "Archived") {
        setError("برای انتشار دوباره، ابتدا محصول را از بایگانی خارج کنید.");
        return;
      }
      if (current.isPrimaryCategoryAssignable === false) {
        setError(PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA);
        return;
      }
      setBusy(true);
      const result = await mutateAdminProductLifecycle(current.productId, "publish");
      setBusy(false);
      if (!result.ok) {
        setError(result.message);
        return;
      }
      if (result.view) {
        setView(result.view);
        setDraft(draftFromView(result.view));
      } else {
        reload();
      }
      return;
    }
    if (actionId === "restore") {
      if (!window.confirm("محصول از بایگانی به پیش‌نویس بازگردد؟")) return;
      setBusy(true);
      setError(null);
      const result = await mutateAdminProductLifecycle(current.productId, "restore");
      setBusy(false);
      if (!result.ok) {
        setError(result.message);
        return;
      }
      if (result.view) {
        setView(result.view);
        setDraft(draftFromView(result.view));
      } else {
        reload();
      }
    }
  }

  const shellActions = isGeneralEdit
    ? [
        { id: "save", label: "ذخیره", kind: "primary" as const, permission: canMutateCatalog && !busy ? ("allowed" as const) : ("denied" as const) },
        { id: "cancel", label: "انصراف", kind: "secondary" as const, permission: "allowed" as const },
      ]
    : [
        ...(formMode.canEdit && formMode.mode === "view"
          ? [{ id: "edit", label: "ویرایش", kind: "secondary" as const, permission: "allowed" as const }]
          : []),
        ...(current.status === "Archived"
          ? [{ id: "restore", label: "خروج از بایگانی", kind: "secondary" as const, permission: canPublish && !busy ? ("allowed" as const) : ("denied" as const) }]
          : [{ id: "publish", label: "انتشار", kind: "secondary" as const, permission: canPublish && !busy ? ("allowed" as const) : ("denied" as const) }]),
      ];

  const translationRows = TRANSLATION_LOCALES.map((locale) => {
    const existing = resolveTranslation(view, locale);
    return { locale, existing };
  });
  const activeTranslation = resolveTranslation(view, translationLocale);

  return (
    <div className="w-full" data-form-mode={formMode.mode} data-testid="product-workspace-screen">
      <WorkspaceShell
        flush
        leading={
          primaryMedia ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={storefrontMediaUrl(primaryMedia.mediaAssetId)}
              alt={primaryMedia.altText ?? view.title}
              className="size-16 shrink-0 rounded-ds bg-secondary object-contain p-1 md:size-20"
            />
          ) : (
            <div className="flex size-16 shrink-0 items-center justify-center rounded-ds bg-secondary text-sm text-muted md:size-20">بدون تصویر</div>
          )
        }
        title={view.title}
        subtitle={`${view.brandName ?? "بدون برند"} · ${categoryLabel(view)}`}
        breadcrumbs={["عملیات", "محصولات", view.title]}
        statusItems={[
          { id: "pub", label: formatAdminStatus(view.status), tone: statusTone(view.status) },
          {
            id: "ready",
            label: view.publication.purchasableHint ? "آمادهٔ فروش" : "غیرقابل‌خرید",
            tone: view.publication.purchasableHint ? "success" : "warning",
          },
          {
            id: "warn",
            label: view.readinessWarnings.length ? `${view.readinessWarnings.length} هشدار` : "بدون مسدودکننده",
            tone: view.readinessWarnings.length ? "warning" : "neutral",
          },
        ]}
        sections={sections}
        activeSectionId={sectionId}
        onSectionChange={(next) => {
          if (formMode.mode === "edit" && next !== "general" && !formMode.confirmDiscardIfDirty()) {
            return;
          }
          if (formMode.mode === "edit" && next !== "general") {
            setDraft(draftFromView(current));
            formMode.onCancel();
            setDirty(new Set());
          }
          setSectionId(next);
        }}
        actions={shellActions}
        onAction={(actionId) => void onAction(actionId)}
        readOnly={formMode.mode === "view" || viewScope}
        conflict={conflict}
        onReloadConflict={reload}
        error={error}
        onRetry={reload}
        dirtySections={dirtySections}
        summary={
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <Summary label="آمادگی فروش" value={view.publication.purchasableHint ? "آماده" : "نیاز به بررسی"} />
            <Summary label="تنوع‌ها" value={String(view.variants.length)} />
            <Summary label="رسانه" value={String(view.media.length)} />
            <Summary label="وضعیت" value={formatAdminStatus(view.status)} />
          </div>
        }
        inspector={
          <div className="space-y-2 text-sm">
            <p className="font-medium">وضعیت عملیات</p>
            <p data-testid="workspace-source">{source === "host" ? "آخرین همگام‌سازی با فروشگاه انجام شد" : "اتصال فروشگاه برقرار نیست"}</p>
          </div>
        }
        activity={view.activity.map((item) => ({ id: item.summary, at: item.at, actor: "ops", summary: item.summary }))}
        audit={view.audit.map((item) => ({ id: item.summary, at: item.at, actor: "system", event: item.summary }))}
      >
        {sectionId === "general" ? (
          <div className="space-y-4" data-testid="product-general-panel">
            {view.primaryCategoryId && view.isPrimaryCategoryAssignable === false ? (
              <div
                className="rounded-ds border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-950"
                data-testid="product-category-level-warning"
                role="status"
              >
                <p className="font-medium">دستهٔ فعلی قابل استفاده نیست</p>
                <p className="mt-1">{PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA}</p>
                <p className="mt-1 text-xs text-amber-900/80">
                  مشاهده مجاز است؛ برای ذخیره یا انتشار، یک دستهٔ سطح سوم انتخاب کنید. جابه‌جایی خودکار انجام نمی‌شود.
                </p>
              </div>
            ) : null}
            {isGeneralEdit ? (
              <Card data-testid="product-general-edit">
                <p className="text-sm font-medium text-muted">ویرایش مشخصات عمومی</p>
                <div className="mt-4 grid gap-4">
                  <ProductCategoryPicker
                    value={activeDraft.categoryId}
                    onChange={(next) => {
                      setDraft({ ...activeDraft, categoryId: next });
                      markGeneralDirty();
                    }}
                    required
                    invalidSelectionHint
                  />
                  <label className="block text-sm font-medium">
                    عنوان
                    <input
                      className="mt-2 min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-base"
                      value={activeDraft.title}
                      data-testid="product-edit-title"
                      onChange={(event) => {
                        const title = event.target.value;
                        setDraft({
                          ...activeDraft,
                          title,
                          slug: activeDraft.slugTouched ? activeDraft.slug : slugifyCategoryName(title),
                        });
                        markGeneralDirty();
                      }}
                    />
                  </label>
                  <label className="block text-sm font-medium">
                    نامک (slug)
                    <input
                      className="mt-2 min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-base"
                      value={activeDraft.slug}
                      dir="ltr"
                      data-testid="product-edit-slug"
                      onChange={(event) => {
                        setDraft({ ...activeDraft, slug: event.target.value, slugTouched: true });
                        markGeneralDirty();
                      }}
                    />
                  </label>
                  <label className="block text-sm font-medium">
                    خلاصه کوتاه
                    <textarea
                      className="mt-2 min-h-24 w-full rounded-ds border border-border bg-surface px-3 py-2 text-base"
                      value={activeDraft.shortDescription}
                      data-testid="product-edit-short-description"
                      onChange={(event) => {
                        setDraft({ ...activeDraft, shortDescription: event.target.value });
                        markGeneralDirty();
                      }}
                    />
                  </label>
                </div>
                <div className="mt-4 flex flex-wrap gap-2">
                  <button
                    type="button"
                    disabled={busy}
                    className="min-h-11 rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground disabled:opacity-50"
                    data-testid="product-edit-save"
                    onClick={() => void handleSaveGeneral()}
                  >
                    ذخیره
                  </button>
                  <button
                    type="button"
                    disabled={busy}
                    className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
                    data-testid="product-edit-cancel"
                    onClick={handleCancelEdit}
                  >
                    انصراف
                  </button>
                </div>
              </Card>
            ) : (
              <div className="grid gap-4 xl:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]" data-testid="product-general-summary">
                <Card>
                  <p className="text-sm font-medium text-muted">خلاصه محصول</p>
                  <div className="mt-3 grid gap-3 sm:grid-cols-2">
                    <SummaryCard label="نام" value={view.title} />
                    <SummaryCard label="مسیر دسته" value={categoryLabel(view)} />
                    <SummaryCard label="وضعیت" value={formatAdminStatus(view.status)} />
                    <SummaryCard label="نامک" value={view.slug ?? view.seo.slugSeam ?? "—"} ltr />
                    <SummaryCard label="خلاصه کوتاه" value={view.shortDescription || "—"} />
                    <SummaryCard label="برند" value={view.brandName ?? "بدون برند"} />
                  </div>
                </Card>
                <Card>
                  <p className="text-sm font-medium text-muted">آمادگی انتشار</p>
                  {view.readinessWarnings.length === 0 ? (
                    <p className="mt-3 text-base">مورد معلقی برای فروش دیده نمی‌شود.</p>
                  ) : (
                    <ul className="mt-3 space-y-2 text-base">
                      {view.readinessWarnings.map((item) => (
                        <li key={item} className="rounded-ds bg-warning/15 px-3 py-2">
                          {item === "seo-incomplete"
                            ? "عنوان جستجو یا نشانی صفحه ناقص است"
                            : item === "no-price"
                              ? "قیمت فروشنده ثبت نشده است"
                              : item === "no-inventory"
                                ? "موجودی قابل‌فروش وجود ندارد"
                                : item}
                        </li>
                      ))}
                    </ul>
                  )}
                  <p className="mt-4 text-sm text-muted">
                    {view.variants.length} تنوع · {view.media.length} رسانه
                  </p>
                </Card>
              </div>
            )}
          </div>
        ) : null}

        {sectionId === "translations" ? (
          <div className="space-y-4" data-testid="product-translations-panel">
            <Card>
              <p className="font-semibold">ترجمه‌ها</p>
              <p className="mt-1 text-sm text-muted">
                مدل locale-based بدون فیلدهای locale-suffixed. ویرایش کامل ترجمه در تسک بعدی تکمیل می‌شود.
              </p>
              <div className="mt-4 flex flex-wrap gap-2" data-testid="product-locale-switcher">
                {translationRows.map(({ locale, existing }) => (
                  <button
                    key={locale}
                    type="button"
                    className={
                      translationLocale === locale
                        ? "min-h-10 rounded-ds bg-primary px-3 text-sm text-primary-foreground"
                        : "min-h-10 rounded-ds border border-border px-3 text-sm hover:bg-secondary"
                    }
                    data-testid={`translation-locale-${locale}`}
                    onClick={() => setTranslationLocale(locale)}
                  >
                    {LOCALE_DISPLAY[locale] ?? locale}
                    <span className="ms-2 text-xs opacity-80">{existing ? "آماده" : "ایجاد نشده"}</span>
                  </button>
                ))}
              </div>
            </Card>
            <Card data-testid="product-translation-view">
              <p className="text-sm font-medium text-muted">{LOCALE_DISPLAY[translationLocale] ?? translationLocale}</p>
              <div className="mt-3 grid gap-3 sm:grid-cols-2">
                <SummaryCard label="نام" value={activeTranslation?.name || (translationLocale === "fa-IR" ? view.title : "—")} />
                <SummaryCard label="نامک" value={activeTranslation?.slug || (translationLocale === "fa-IR" ? view.slug ?? "—" : "—")} ltr />
                <SummaryCard
                  label="خلاصه کوتاه"
                  value={activeTranslation?.shortDescription || (translationLocale === "fa-IR" ? view.shortDescription || "—" : "—")}
                />
                <SummaryCard label="توضیح" value={activeTranslation?.description || "—"} />
                <SummaryCard label="عنوان SEO" value={activeTranslation?.seoTitle || view.seo.seoTitleSeam || "—"} />
                <SummaryCard label="توضیح SEO" value={activeTranslation?.seoDescription || "—"} />
              </div>
            </Card>
          </div>
        ) : null}

        {sectionId === "attributes" ? (
          <Card data-testid="product-attributes-panel">
            {!view.primaryCategoryId ? (
              <div data-testid="product-attributes-category-required">
                <p className="font-semibold">دسته لازم است</p>
                <p className="mt-2 text-sm text-muted">
                  برای بارگذاری schema ویژگی‌های وابسته به دسته، ابتدا در تب عمومی یک دسته انتخاب و ذخیره کنید.
                </p>
              </div>
            ) : (
              <ProductAttributesPanel
                productId={current.productId}
                categoryId={view.primaryCategoryId}
                categoryPath={view.categoryPath}
                canEdit={canMutateCatalog}
                mode={formMode.mode === "edit" ? "edit" : "view"}
              />
            )}
          </Card>
        ) : null}

        {sectionId === "variants" ? (
          <Card data-testid="admin-product-variants">
            <ProductVariantsPanel
              productId={current.productId}
              canEdit={canMutateCatalog}
              mode={formMode.mode === "edit" ? "edit" : "view"}
            />
          </Card>
        ) : null}
        {sectionId === "media" ? (
          <Card data-testid="admin-product-media">
            <ProductMediaPanel
              productId={current.productId}
              canEdit={canMutateCatalog}
              mode={formMode.mode === "edit" ? "edit" : "view"}
            />
          </Card>
        ) : null}

        {sectionId === "seo" ? (
          <Card data-testid="admin-product-seo">
            <ProductSeoPanel
              productId={current.productId}
              canEdit={canMutateCatalog}
              mode={formMode.mode === "edit" ? "edit" : "view"}
            />
          </Card>
        ) : null}

        {sectionId === "publication" ? (
          <div className="space-y-4" data-testid="admin-product-publication">
            <Card>
              <ProductPublishingPanel
                productId={current.productId}
                status={view.status}
                statusUpdatedAt={view.publication.statusUpdatedAt ?? view.catalogUpdatedAt}
                canPublish={canPublish}
                mode={formMode.mode === "edit" ? "edit" : "view"}
                purchasableHint={view.publication.purchasableHint}
                onNavigateTab={(tabId) => setSectionId(tabId)}
                onStatusChanged={(hint) => {
                  if (hint?.status === "__deleted__") {
                    window.location.href = "/admin/products";
                    return;
                  }
                  if (hint?.status) {
                    setView((prev) => (prev ? { ...prev, status: hint.status } : prev));
                  }
                  reload();
                }}
              />
            </Card>
            <Card data-testid="product-commercial-readonly-detail">
              <p className="font-semibold">جزئیات تجاری ترکیبی (فقط‌خواندنی)</p>
              <div className="mt-3 grid gap-3 sm:grid-cols-3">
                <SummaryCard label="پیشنهاد فعال" value={String(view.offers.filter((row) => row.status === "Active").length)} />
                <SummaryCard label="بازهٔ قیمت" value={priceRange} />
                <SummaryCard label="قابل‌فروش" value={String(available)} />
              </div>
              <p className="mt-2 text-sm text-muted">
                موجود {onHand} · رزرو {reserved} · {view.stock.length} محل
              </p>
            </Card>
          </div>
        ) : null}

        {sectionId === "history" ? (
          <ol className="space-y-3 border-s-2 border-border ps-4" data-testid="product-history-placeholder">
            {view.activity.length === 0 && view.audit.length === 0 ? (
              <li className="text-sm text-muted">تاریخچه هنوز خالی است.</li>
            ) : null}
            {view.activity.map((item) => (
              <li key={`${item.summary}:${item.at}`}>
                <p className="font-medium">{item.summary}</p>
                <p className="text-sm text-muted">عملیات · {item.at}</p>
              </li>
            ))}
            {view.audit.map((item) => (
              <li key={`${item.summary}-audit`}>
                <p className="font-medium">{item.summary}</p>
                <p className="text-sm text-muted">حسابرسی · {item.at}</p>
              </li>
            ))}
          </ol>
        ) : null}
      </WorkspaceShell>
    </div>
  );
}

function Summary({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-ds border border-border bg-surface p-3">
      <p className="text-sm text-muted">{label}</p>
      <p className="mt-1 text-lg font-semibold">{value}</p>
    </div>
  );
}