"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Button,
  Card,
  Dialog,
  ErrorState,
  WorkspaceShell,
  faWorkspaceMessages,
  useAdminFormMode,
} from "../../design-system";
import { formatAdminStatus } from "./admin-api";
import { previewProductCategoryChange } from "./catalog-attribute-api";
import { ProductAttributesPanel } from "./product-attributes-panel";
import { ProductMediaPanel } from "./product-media-panel";
import { ProductSeoPanel } from "./product-seo-panel";
import { ProductPublishingPanel } from "./product-publishing-panel";
import { buildPublishChecklist } from "./product-publishing-panel-model";
import { ProductHistoryPanel } from "./product-history-panel";
import { ProductTranslationsPanel, translationReadiness } from "./product-translations-panel";
import { ProductVariantsPanel } from "./product-variants-panel";
import {
  ProductWorkspaceDirtyProvider,
  useProductWorkspaceDirtyRegistration,
  useProductWorkspaceDirtyRegistry,
} from "./product-workspace-dirty-context";
import {
  assignAdminProductBrand,
  assignAdminProductCategory,
  listAdminBrandOptions,
  loadProductWorkspace,
  mutateAdminProductLifecycle,
  updateAdminProductCore,
  type AdminBrandOption,
  type HostReadSource,
} from "./host-client";
import { ProductCategoryPicker } from "./product-category-picker";
import { PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA } from "./product-category-level";
import { mapAdminErrorMessage } from "./admin-error-map";
import { type ProductTranslationView, type ProductWorkspaceView } from "./workspace-model";
import { storefrontMediaUrl } from "../storefront/storefront-api";

const UNSAVED_DIALOG_COPY =
  "تغییرات ذخیره‌نشده دارید. بدون ذخیره از این بخش خارج می‌شوید؟";

type PendingNav =
  | { type: "tab"; next: string }
  | { type: "exit-edit" }
  | { type: "route"; href: string };

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
  slug: string;
  categoryId: string | null;
  brandId: string | null;
  slugTouched: boolean;
}

function draftFromView(view: ProductWorkspaceView): GeneralDraft {
  return {
    slug: view.slug ?? view.seo.slugSeam ?? "",
    categoryId: view.primaryCategoryId ?? null,
    brandId: view.brandId ?? null,
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
export function ProductWorkspaceScreen(props: {
  productId: string;
  viewScope?: boolean;
  initialEdit?: boolean;
}) {
  return (
    <ProductWorkspaceDirtyProvider>
      <ProductWorkspaceScreenInner {...props} />
    </ProductWorkspaceDirtyProvider>
  );
}

function ProductWorkspaceScreenInner({
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
  const [enteredInitialEdit, setEnteredInitialEdit] = useState(false);
  const [pendingNav, setPendingNav] = useState<PendingNav | null>(null);
  const [brandOptions, setBrandOptions] = useState<AdminBrandOption[]>([]);
  const [brandQuery, setBrandQuery] = useState("");

  const canView = Boolean(view?.permissions.canView ?? true);
  const canEdit = Boolean(view?.permissions.canEditCatalog) && !viewScope;
  const formMode = useAdminFormMode({ canView, canEdit });
  const dirtyRegistry = useProductWorkspaceDirtyRegistry();

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
    let cancelled = false;
    void listAdminBrandOptions(brandQuery).then((result) => {
      if (cancelled || !result.ok) return;
      setBrandOptions(result.items);
    });
    return () => {
      cancelled = true;
    };
  }, [brandQuery]);

  useEffect(() => {
    if (!view || enteredInitialEdit || viewScope) return;
    if (initialEdit && canEdit) {
      formMode.onEdit();
      setEnteredInitialEdit(true);
    }
    // onEdit is stable; avoid formMode object identity loops
    // eslint-disable-next-line react-hooks/exhaustive-deps -- enter edit once after load
  }, [view, initialEdit, viewScope, enteredInitialEdit, canEdit]);

  const discardGeneral = useCallback(() => {
    if (view) setDraft(draftFromView(view));
    formMode.clearDirty();
    setDirty(new Set());
    setConflict(null);
    // clearDirty is stable; formMode object identity changes every render
    // eslint-disable-next-line react-hooks/exhaustive-deps -- clearDirty only
  }, [view, formMode.clearDirty]);

  useProductWorkspaceDirtyRegistration(
    "general",
    Boolean(view) && formMode.mode === "edit" && formMode.isDirty,
    discardGeneral,
  );

  const workspaceDirty =
    dirtyRegistry.isAnyDirty() || (formMode.mode === "edit" && formMode.isDirty);

  useEffect(() => {
    if (!workspaceDirty) return;
    const onBeforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", onBeforeUnload);
    return () => window.removeEventListener("beforeunload", onBeforeUnload);
  }, [workspaceDirty]);

  const dirtySections = useMemo(() => {
    const next = new Set(dirty);
    for (const id of dirtyRegistry.dirtySectionIds()) next.add(id);
    return next;
    // dirtyRegistry identity is stable; workspaceDirty tracks register/unregister content
    // eslint-disable-next-line react-hooks/exhaustive-deps -- bump via workspaceDirty
  }, [dirty, workspaceDirty]);

  const mediaRows = useMemo(() => (view ? sortedMedia(view.media) : []), [view]);
  const primaryMedia = mediaRows.find((row) => row.primary) ?? mediaRows[0] ?? null;

  function performPendingNav(nav: PendingNav) {
    if (nav.type === "tab") {
      setSectionId(nav.next);
      return;
    }
    if (nav.type === "exit-edit") {
      if (view) setDraft(draftFromView(view));
      formMode.onCancel();
      setDirty(new Set());
      setConflict(null);
      return;
    }
    window.location.href = nav.href;
  }

  function confirmDiscardPendingNav() {
    dirtyRegistry.discardAll();
    if (formMode.mode === "edit" && formMode.isDirty) {
      discardGeneral();
    }
    const nav = pendingNav;
    setPendingNav(null);
    if (nav) performPendingNav(nav);
  }

  function stayOnPendingNav() {
    setPendingNav(null);
  }

  function requestSectionChange(next: string) {
    if (next === sectionId) return;
    if (dirtyRegistry.isAnyDirty() || (formMode.mode === "edit" && formMode.isDirty)) {
      setPendingNav({ type: "tab", next });
      return;
    }
    setSectionId(next);
  }

  function requestLeaveWorkspace(href: string) {
    if (dirtyRegistry.isAnyDirty() || (formMode.mode === "edit" && formMode.isDirty)) {
      setPendingNav({ type: "route", href });
      return;
    }
    window.location.href = href;
  }

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
  }

  function handleCancelEdit() {
    if (dirtyRegistry.isAnyDirty() || formMode.isDirty) {
      setPendingNav({ type: "exit-edit" });
      return;
    }
    if (view) setDraft(draftFromView(view));
    formMode.onCancel();
    setDirty(new Set());
    setConflict(null);
  }

  async function handleSaveGeneral() {
    if (!activeDraft.categoryId) {
      setError("انتخاب دسته لازم است");
      return;
    }
    if (!activeDraft.slug.trim()) {
      setError("نامک سراسری لازم است");
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
        setError(mapAdminErrorMessage(catResult.errorCode));
        return;
      }
      setView(catResult.view);
      expectedUpdatedAt = catResult.view.catalogUpdatedAt;
    }

    const brandChanged = (activeDraft.brandId ?? null) !== (current.brandId ?? null);
    if (brandChanged) {
      const brandResult = await assignAdminProductBrand(
        current.productId,
        { brandId: activeDraft.brandId, expectedUpdatedAt },
        viewScope,
      );
      if (!brandResult.ok) {
        setBusy(false);
        if (brandResult.errorCode === "workspace.catalog.stale") {
          setConflict("این محصول را کاربر دیگری تغییر داده است. نسخهٔ تازه را بارگذاری کنید.");
          return;
        }
        setError(mapAdminErrorMessage(brandResult.errorCode));
        return;
      }
      setView(brandResult.view);
      expectedUpdatedAt = brandResult.view.catalogUpdatedAt;
    }

    const faName =
      resolveTranslation(current, "fa-IR")?.name?.trim() || current.title.trim() || "محصول";
    const coreResult = await updateAdminProductCore(
      current.productId,
      {
        locale: "fa-IR",
        title: faName,
        slug: activeDraft.slug.trim() || null,
        shortDescription: resolveTranslation(current, "fa-IR")?.shortDescription ?? current.shortDescription ?? null,
        description: resolveTranslation(current, "fa-IR")?.description ?? null,
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
      setError(mapAdminErrorMessage(coreResult.errorCode));
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

  const lifecycleAction =
    current.status === "Archived"
      ? {
          id: "restore",
          label: "خروج از بایگانی",
          kind: "secondary" as const,
          permission: canPublish && !busy ? ("allowed" as const) : ("denied" as const),
        }
      : {
          id: "publish",
          label: "انتشار",
          kind: "secondary" as const,
          permission: canPublish && !busy ? ("allowed" as const) : ("denied" as const),
        };

  const shellActions = isGeneralEdit
    ? [
        { id: "save", label: "ذخیره", kind: "primary" as const, permission: canMutateCatalog && !busy ? ("allowed" as const) : ("denied" as const) },
        { id: "cancel", label: "انصراف", kind: "secondary" as const, permission: "allowed" as const },
      ]
    : formMode.mode === "edit"
      ? [
          { id: "cancel", label: "پایان ویرایش", kind: "secondary" as const, permission: "allowed" as const },
          lifecycleAction,
        ]
      : [
          ...(formMode.canEdit
            ? [{ id: "edit", label: "ویرایش", kind: "secondary" as const, permission: "allowed" as const }]
            : []),
          lifecycleAction,
        ];

  const translationRows = TRANSLATION_LOCALES.map((locale) => {
    const existing = resolveTranslation(view, locale);
    return { locale, existing };
  });
  const translationCompleteCount = translationRows.filter(({ locale, existing }) => {
    const draftLike = {
      name: existing?.name || (locale === "fa-IR" ? view.title : "") || "",
      shortDescription:
        existing?.shortDescription || (locale === "fa-IR" ? view.shortDescription || "" : "") || "",
      description: existing?.description || "",
      seoTitle: "",
      seoDescription: "",
    };
    return translationReadiness(draftLike) === "complete";
  }).length;
  const publishChecklist = buildPublishChecklist(view.publication.aggregateReadiness ?? null);
  const publishTotalCount = publishChecklist.length || null;
  const publishReadyCount = publishTotalCount
    ? publishChecklist.filter((item) => item.ready).length
    : null;
  const seoReady =
    Boolean(view.publication.aggregateReadiness?.seoReady) ||
    (!view.readinessWarnings.includes("seo-incomplete") &&
      Boolean(view.seo.seoTitleSeam || view.slug));

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
              className="size-16 shrink-0 rounded-full border border-border bg-secondary object-cover md:size-20"
            />
          ) : (
            <div className="flex size-16 shrink-0 items-center justify-center rounded-full border border-border bg-secondary text-xs text-muted md:size-20">
              بدون تصویر
            </div>
          )
        }
        title={view.title}
        subtitle={categoryLabel(view)}
        breadcrumbs={["محصولات", "فهرست محصولات", view.title]}
        statusItems={[
          { id: "pub", label: formatAdminStatus(view.status), tone: statusTone(view.status) },
          {
            id: "mode",
            label: formMode.mode === "edit" ? "ویرایش" : "مشاهده",
            tone: formMode.mode === "edit" ? "warning" : "neutral",
          },
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
        onSectionChange={requestSectionChange}
        actions={shellActions}
        onAction={(actionId) => void onAction(actionId)}
        readOnly={viewScope}
        conflict={conflict}
        onReloadConflict={reload}
        error={error}
        onRetry={reload}
        dirtySections={dirtySections}
        summary={
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-6" data-testid="product-summary-cards">
            <Summary
              label="وضعیت محصول"
              value={formatAdminStatus(view.status)}
              hint={view.brandName ? `برند: ${view.brandName}` : "بدون برند"}
            />
            <Summary
              label="آمادگی انتشار"
              value={
                publishReadyCount != null && publishTotalCount != null
                  ? `${publishReadyCount} از ${publishTotalCount}`
                  : view.readinessWarnings.length === 0
                    ? "کامل"
                    : `${view.readinessWarnings.length} مورد ناقص`
              }
              hint={view.publication.purchasableHint ? "آمادهٔ فروش ترکیبی" : "نیاز به بررسی"}
            />
            <Summary
              label="ترجمه‌ها"
              value={`${translationCompleteCount} از ${TRANSLATION_LOCALES.length} زبان`}
              hint="فارسی و English"
            />
            <Summary
              label="SEO"
              value={seoReady ? "آماده" : "قابل بهبود"}
              hint={view.seo.seoTitleSeam || view.slug || "عنوان جستجو ناقص"}
            />
            <Summary label="تنوع‌ها" value={`${view.variants.length} مورد`} />
            <Summary
              label="رسانه"
              value={`${view.media.length} مورد`}
              hint={primaryMedia ? "تصویر اصلی دارد" : "بدون تصویر اصلی"}
            />
          </div>
        }
        inspector={
          <div className="space-y-4 text-sm" data-testid="product-workspace-inspector">
            <div>
              <p className="font-medium">وضعیت عملیات</p>
              <p className="mt-1 text-muted" data-testid="workspace-source">
                {source === "host" ? "آخرین همگام‌سازی با فروشگاه انجام شد" : "اتصال فروشگاه برقرار نیست"}
              </p>
            </div>
            <div>
              <p className="font-medium">ترجمه‌ها و زبان‌ها</p>
              <ul className="mt-2 space-y-1.5">
                {TRANSLATION_LOCALES.map((locale) => {
                  const existing = resolveTranslation(view, locale);
                  const state = translationReadiness({
                    name: existing?.name || (locale === "fa-IR" ? view.title : "") || "",
                    shortDescription:
                      existing?.shortDescription ||
                      (locale === "fa-IR" ? view.shortDescription || "" : "") ||
                      "",
                    description: existing?.description || "",
                    seoTitle: "",
                    seoDescription: "",
                  });
                  const label =
                    state === "complete" ? "کامل" : state === "partial" ? "ناقص" : "ایجاد نشده";
                  return (
                    <li key={locale} className="flex items-center justify-between gap-2 rounded-ds bg-secondary/40 px-2 py-1.5">
                      <span>{LOCALE_DISPLAY[locale] ?? locale}</span>
                      <span
                        className={
                          state === "complete"
                            ? "rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-800"
                            : state === "partial"
                              ? "rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-900"
                              : "rounded-full bg-secondary px-2 py-0.5 text-[11px] font-medium text-muted"
                        }
                      >
                        {label}
                      </span>
                    </li>
                  );
                })}
              </ul>
            </div>
            <div>
              <p className="font-medium">چک‌لیست آمادگی</p>
              <p className="mt-1 text-muted">
                {publishReadyCount != null && publishTotalCount != null
                  ? `${publishReadyCount} از ${publishTotalCount} مورد کامل`
                  : view.readinessWarnings.length === 0
                    ? "مورد معلقی دیده نمی‌شود"
                    : `${view.readinessWarnings.length} مورد نیاز به تکمیل`}
              </p>
              <button
                type="button"
                className="mt-2 text-sm font-medium text-primary underline-offset-2 hover:underline"
                onClick={() => requestSectionChange("publication")}
              >
                مشاهده چک‌لیست کامل
              </button>
            </div>
          </div>
        }
        activity={view.activity.map((item) => ({
          id: item.summary,
          at: item.at,
          actor: item.actor?.trim() || "سیستم",
          summary: item.summary,
        }))}
        audit={view.audit.map((item) => ({
          id: item.summary,
          at: item.at,
          actor: item.actor?.trim() || "سیستم",
          event: item.summary,
        }))}
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
              <div className="space-y-4" data-testid="product-general-edit">
                <Card>
                  <p className="text-sm font-medium text-muted">هویت غیرزبانی محصول</p>
                  <p className="mt-1 text-xs text-muted">
                    نام و توضیحات محصول در تب ترجمه‌ها به‌صورت locale-based ویرایش می‌شوند — نه به‌عنوان فیلد ثابت انگلیسی/فارسی در عمومی.
                  </p>
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
                      برند
                      <input
                        className="mt-2 min-h-10 w-full rounded-ds border border-border bg-surface px-3 text-sm"
                        placeholder="جستجوی برند…"
                        value={brandQuery}
                        onChange={(event) => setBrandQuery(event.target.value)}
                        data-testid="product-brand-search"
                      />
                      <select
                        className="mt-2 min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-base"
                        value={activeDraft.brandId ?? ""}
                        data-testid="product-edit-brand"
                        onChange={(event) => {
                          setDraft({
                            ...activeDraft,
                            brandId: event.target.value ? event.target.value : null,
                          });
                          markGeneralDirty();
                        }}
                      >
                        <option value="">بدون برند</option>
                        {brandOptions.map((brand) => (
                          <option key={brand.brandId} value={brand.brandId}>
                            {brand.name}
                          </option>
                        ))}
                      </select>
                    </label>
                    <label className="block text-sm font-medium">
                      نامک سراسری (slug)
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
                    <div className="rounded-ds border border-border bg-secondary/30 p-3 text-sm">
                      <p className="font-medium">کد کاتالوگ</p>
                      <p className="mt-1 text-muted">
                        کد کاتالوگ روی هر <strong>تنوع</strong> (CatalogCodeSeam) است؛ کد سطح Product و GTIN/بارکد هنوز در دامنه تعریف نشده‌اند.
                      </p>
                      <button
                        type="button"
                        className="mt-2 text-sm font-medium text-primary underline-offset-2 hover:underline"
                        onClick={() => requestSectionChange("variants")}
                      >
                        مدیریت کد در تب تنوع‌ها
                      </button>
                    </div>
                  </div>
                </Card>
                <Card>
                  <p className="text-sm font-medium text-muted">وضعیت / مالکیت Product</p>
                  <div className="mt-3 grid gap-3 sm:grid-cols-2">
                    <SummaryCard label="وضعیت" value={formatAdminStatus(view.status)} />
                    <SummaryCard label="آخرین به‌روزرسانی Catalog" value={view.catalogUpdatedAt} ltr />
                  </div>
                </Card>
              </div>
            ) : (
              <div className="grid gap-4 xl:grid-cols-[minmax(0,1.45fr)_minmax(16rem,0.9fr)]" data-testid="product-general-summary">
                <div className="space-y-4">
                  <Card>
                    <p className="text-sm font-medium text-muted">هویت غیرزبانی</p>
                    <div className="mt-3 grid gap-3 sm:grid-cols-2">
                      <SummaryCard label="مسیر دسته" value={categoryLabel(view)} />
                      <SummaryCard label="برند" value={view.brandName ?? "بدون برند"} />
                      <SummaryCard label="وضعیت" value={formatAdminStatus(view.status)} />
                      <SummaryCard label="نامک سراسری" value={view.slug ?? view.seo.slugSeam ?? "—"} ltr />
                      <SummaryCard label="آخرین به‌روزرسانی" value={view.catalogUpdatedAt} ltr />
                      <SummaryCard
                        label="کد کاتالوگ"
                        value={
                          view.variants.some((v) => v.catalogCodeSeam)
                            ? `${view.variants.filter((v) => v.catalogCodeSeam).length} تنوع دارای کد`
                            : "روی تنوع‌ها تعریف می‌شود"
                        }
                      />
                    </div>
                    <p className="mt-3 text-xs text-muted">
                      نام و توضیحات نمایشی از تب ترجمه‌ها می‌آید (locale-based). مدل/سری و GTIN ساخته نشده‌اند.
                    </p>
                    <button
                      type="button"
                      className="mt-2 text-sm font-medium text-primary underline-offset-2 hover:underline"
                      onClick={() => requestSectionChange("translations")}
                    >
                      ویرایش محتوای محلی در ترجمه‌ها
                    </button>
                  </Card>
                  <Card>
                    <p className="text-sm font-medium text-muted">پیش‌نمایش محتوای locale فعال (فقط‌خواندنی)</p>
                    <div className="mt-3 grid gap-3">
                      <SummaryCard label="نام (fa-IR)" value={view.title || "—"} />
                      <SummaryCard label="خلاصه کوتاه (fa-IR)" value={view.shortDescription || "—"} />
                      <SummaryCard
                        label="توضیح کامل (fa-IR)"
                        value={resolveTranslation(view, "fa-IR")?.description || "—"}
                      />
                    </div>
                  </Card>
                </div>
                <div className="space-y-4">
                  <Card data-testid="product-general-media-preview">
                    <div className="flex items-center justify-between gap-2">
                      <p className="text-sm font-medium text-muted">رسانه محصول</p>
                      <button
                        type="button"
                        className="text-xs font-medium text-primary underline-offset-2 hover:underline"
                        onClick={() => requestSectionChange("media")}
                      >
                        مدیریت همه رسانه‌ها
                      </button>
                    </div>
                    {primaryMedia ? (
                      <div className="mt-3">
                        <div className="relative aspect-square overflow-hidden rounded-ds bg-secondary">
                          {/* eslint-disable-next-line @next/next/no-img-element */}
                          <img
                            src={storefrontMediaUrl(primaryMedia.mediaAssetId)}
                            alt={primaryMedia.altText ?? view.title}
                            className="h-full w-full object-contain p-4"
                          />
                          <span className="absolute start-2 top-2 rounded-ds bg-success/90 px-2 py-0.5 text-xs text-white">
                            تصویر اصلی
                          </span>
                        </div>
                        {mediaRows.length > 1 ? (
                          <ul className="mt-3 flex flex-wrap gap-2">
                            {mediaRows.slice(0, 5).map((item) => (
                              <li key={item.mediaAssetId}>
                                {/* eslint-disable-next-line @next/next/no-img-element */}
                                <img
                                  src={storefrontMediaUrl(item.mediaAssetId)}
                                  alt={item.altText ?? "رسانه"}
                                  className="size-14 rounded-ds border border-border bg-secondary object-contain p-1"
                                />
                              </li>
                            ))}
                          </ul>
                        ) : null}
                      </div>
                    ) : (
                      <p className="mt-3 text-sm text-muted">هنوز تصویری متصل نشده است.</p>
                    )}
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
              </div>
            )}
          </div>
        ) : null}

        {sectionId === "translations" ? (
          <ProductTranslationsPanel
            view={view}
            canEdit={canMutateCatalog}
            mode={formMode.mode === "edit" ? "edit" : "view"}
            viewScope={viewScope}
            onSaved={(next) => {
              setView(next);
              setDraft(draftFromView(next));
            }}
          />
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
                onNavigateTab={(tabId) => requestSectionChange(tabId)}
                onStatusChanged={(hint) => {
                  if (hint?.status === "__deleted__") {
                    requestLeaveWorkspace("/admin/products");
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
          <Card data-testid="admin-product-history">
            <ProductHistoryPanel productId={view.productId} viewScope={viewScope} />
          </Card>
        ) : null}
      </WorkspaceShell>

      <Dialog
        title={UNSAVED_DIALOG_COPY}
        open={pendingNav != null}
        onClose={stayOnPendingNav}
        showCloseButton={false}
      >
        <div data-testid="product-workspace-unsaved-dialog">
          <p className="text-sm">{UNSAVED_DIALOG_COPY}</p>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button
              type="button"
              tone="secondary"
              data-testid="product-workspace-unsaved-stay"
              onClick={stayOnPendingNav}
            >
              بازگشت
            </Button>
            <Button
              type="button"
              tone="primary"
              data-testid="product-workspace-unsaved-discard"
              onClick={confirmDiscardPendingNav}
            >
              ادامه و لغو تغییرات
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  );
}

function Summary({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <div className="rounded-ds border border-border bg-surface p-3">
      <p className="text-sm text-muted">{label}</p>
      <p className="mt-1 text-lg font-semibold leading-snug">{value}</p>
      {hint ? <p className="mt-1 text-xs text-muted">{hint}</p> : null}
    </div>
  );
}