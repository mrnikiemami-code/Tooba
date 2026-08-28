"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Badge, Card, ErrorState, WorkspaceShell, faWorkspaceMessages } from "../../design-system";
import { formatAdminStatus } from "./admin-api";
import { listAttributeDefinitions, type AttributeDefinition } from "./catalog-attribute-api";
import { ProductAttributesPanel } from "./catalog-attribute-ui";
import {
  attachAdminProductMedia,
  createAdminProductVariant,
  loadProductWorkspace,
  mutateAdminProductLifecycle,
  patchAdminProductVariant,
  patchAdminProductMediaAlt,
  patchCatalogTitle,
  removeAdminProductMedia,
  reorderAdminProductMedia,
  setAdminProductMediaPrimary,
  type HostReadSource,
} from "./host-client";
import { type ProductWorkspaceView } from "./workspace-model";
import { storefrontMediaUrl } from "../storefront/storefront-api";

const sections = [
  { id: "overview", label: "نمای کلی" },
  { id: "attributes", label: "ویژگی‌ها" },
  { id: "variants", label: "گونه‌ها" },
  { id: "media", label: "رسانه" },
  { id: "commercial", label: "فروش و قیمت" },
  { id: "inventory", label: "موجودی" },
  { id: "seo", label: "سئو و محتوا" },
  { id: "publication", label: "انتشار" },
  { id: "history", label: "تاریخچه" },
];

const AXIS_LABELS: Record<string, string> = {
  color: "رنگ",
  colour: "رنگ",
  size: "سایز",
  storage: "حافظه",
  memory: "حافظه",
  ram: "رم",
  pack: "بسته",
};

function money(amount: number | undefined, currency: string | undefined): string {
  if (amount == null) {
    return "—";
  }
  const digits = new Intl.NumberFormat("fa-IR").format(amount);
  return currency === "IRR" ? `${digits} ریال` : `${digits} ${currency ?? ""}`.trim();
}

/** اثرانگشت خام `color=sand|size=m` را برای اپراتور خوانا می‌کند. */
export function humanizeFingerprint(fingerprint: string): string {
  if (!fingerprint.trim()) {
    return "بدون ترکیب";
  }
  return fingerprint
    .split("|")
    .map((part) => {
      const [rawKey, ...rest] = part.split("=");
      const key = (rawKey ?? "").trim().toLowerCase();
      const value = rest.join("=").trim() || "—";
      const label = AXIS_LABELS[key] ?? (rawKey?.trim() || "محور");
      return `${label}: ${value}`;
    })
    .join(" · ");
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

/**
 * Workspace محصول Admin. Commercial چند فروشنده را جدا از Product.Price نشان می‌دهد.
 * SpiceDB در این کامپوننت صدا زده نمی‌شود؛ مجوز از Host می‌آید.
 */
export function ProductWorkspaceScreen({ productId, viewScope = false }: { productId: string; viewScope?: boolean }) {
  const [view, setView] = useState<ProductWorkspaceView | null>(null);
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [sectionId, setSectionId] = useState("overview");
  const [titleDraft, setTitleDraft] = useState("");
  const [conflict, setConflict] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [dirty, setDirty] = useState<Set<string>>(new Set());
  const [denied, setDenied] = useState(false);
  const [busy, setBusy] = useState(false);
  const [attachAssetId, setAttachAssetId] = useState("");
  const [attachAlt, setAttachAlt] = useState("");
  const [altDrafts, setAltDrafts] = useState<Record<string, string>>({});
  const [axisDefs, setAxisDefs] = useState<AttributeDefinition[]>([]);
  const [createAxes, setCreateAxes] = useState<Record<string, { rawValue: string; enumOptionId: string }>>({});
  const [createCatalogCode, setCreateCatalogCode] = useState("");
  const [variantStatusDraft, setVariantStatusDraft] = useState<Record<string, string>>({});

  const reload = useCallback(() => {
    void loadProductWorkspace(productId, viewScope).then((result) => {
      setSource(result.source);
      setView(result.view);
      setTitleDraft(result.view?.title ?? "");
      setConflict(null);
      setError(result.message ?? null);
      setDenied(Boolean(result.denied));
      if (result.view) {
        const alts: Record<string, string> = {};
        for (const item of result.view.media) {
          alts[item.mediaAssetId] = item.altText ?? "";
        }
        setAltDrafts(alts);
        const statuses: Record<string, string> = {};
        for (const variant of result.view.variants) {
          statuses[variant.variantId] = variant.status;
        }
        setVariantStatusDraft(statuses);
      }
    });
  }, [productId, viewScope]);

  useEffect(() => {
    reload();
  }, [reload]);

  useEffect(() => {
    void listAttributeDefinitions().then((result) => {
      if (result.state === "ok" && result.data) {
        setAxisDefs(result.data.filter((row) => row.isVariantAxisAllowed && row.isActive));
      }
    });
  }, []);

  const readOnly = !view?.permissions.canEditCatalog || viewScope;
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

  function applyMedia(media: ProductWorkspaceView["media"]) {
    setView((prev) => (prev ? { ...prev, media } : prev));
    const alts: Record<string, string> = {};
    for (const item of media) {
      alts[item.mediaAssetId] = item.altText ?? "";
    }
    setAltDrafts(alts);
  }

  async function onAction(actionId: string) {
    if (actionId === "save") {
      const result = await patchCatalogTitle(current.productId, "fa", titleDraft, current.catalogUpdatedAt, viewScope);
      if (!result.ok) {
        if (result.errorCode === "workspace.catalog.stale") {
          setConflict("این محصول را کاربر دیگری تغییر داده است. نسخهٔ تازه را بارگذاری کنید.");
          return;
        }
        setError(result.errorCode);
        return;
      }
      setView(result.view);
      setTitleDraft(result.view.title);
      setDirty(new Set());
      setConflict(null);
      return;
    }
    if (actionId === "publish") {
      setBusy(true);
      const result = await mutateAdminProductLifecycle(current.productId, "publish");
      setBusy(false);
      if (!result.ok) {
        setError(result.message);
        return;
      }
      if (result.view) {
        setView(result.view);
      } else {
        reload();
      }
    }
  }

  async function runLifecycle(action: "unpublish" | "archive" | "delete") {
    setBusy(true);
    setError(null);
    const result = await mutateAdminProductLifecycle(current.productId, action);
    setBusy(false);
    if (!result.ok) {
      setError(result.message);
      return;
    }
    if (action === "delete") {
      window.location.href = "/admin/products";
      return;
    }
    if (result.view) {
      setView(result.view);
    } else {
      reload();
    }
  }

  async function onAttachMedia() {
    const assetId = attachAssetId.trim();
    if (!assetId) {
      setError("شناسهٔ دارایی رسانه لازم است");
      return;
    }
    setBusy(true);
    setError(null);
    const result = await attachAdminProductMedia(current.productId, assetId, attachAlt.trim() || null);
    setBusy(false);
    if (!result.ok) {
      setError(result.message);
      return;
    }
    applyMedia(result.media);
    setAttachAssetId("");
    setAttachAlt("");
  }

  async function onReorder(mediaAssetId: string, direction: -1 | 1) {
    const ordered = sortedMedia(current.media).map((row) => row.mediaAssetId);
    const index = ordered.indexOf(mediaAssetId);
    const swapWith = index + direction;
    if (index < 0 || swapWith < 0 || swapWith >= ordered.length) {
      return;
    }
    const next = [...ordered];
    const tmp = next[index]!;
    next[index] = next[swapWith]!;
    next[swapWith] = tmp;
    setBusy(true);
    setError(null);
    const result = await reorderAdminProductMedia(current.productId, next);
    setBusy(false);
    if (!result.ok) {
      setError(result.message);
      return;
    }
    applyMedia(result.media);
  }

  async function onSetPrimary(mediaAssetId: string) {
    setBusy(true);
    setError(null);
    const result = await setAdminProductMediaPrimary(current.productId, mediaAssetId);
    setBusy(false);
    if (!result.ok) {
      setError(result.message);
      return;
    }
    applyMedia(result.media);
  }

  async function onSaveAlt(mediaAssetId: string) {
    setBusy(true);
    setError(null);
    const result = await patchAdminProductMediaAlt(current.productId, mediaAssetId, altDrafts[mediaAssetId]?.trim() || null);
    setBusy(false);
    if (!result.ok) {
      setError(result.message);
      return;
    }
    applyMedia(result.media);
  }

  async function onRemoveMedia(mediaAssetId: string) {
    setBusy(true);
    setError(null);
    const result = await removeAdminProductMedia(current.productId, mediaAssetId);
    setBusy(false);
    if (!result.ok) {
      setError(result.message);
      return;
    }
    applyMedia(result.media);
  }

  async function onCreateVariant() {
    const axes = Object.entries(createAxes)
      .filter(([, draft]) => draft.rawValue.trim() || draft.enumOptionId.trim())
      .map(([definitionId, draft]) => ({
        definitionId,
        rawValue: draft.rawValue.trim() || (draft.enumOptionId.trim() ? "ignored" : null),
        enumOptionId: draft.enumOptionId.trim() || null,
      }));
    if (axes.length === 0) {
      setError("حداقل یک محور با مقدار برای ایجاد گونه لازم است");
      return;
    }
    setBusy(true);
    setError(null);
    const result = await createAdminProductVariant(current.productId, {
      catalogCodeSeam: createCatalogCode.trim() || null,
      axes,
    });
    setBusy(false);
    if (!result.ok) {
      setError(result.message);
      return;
    }
    setView(result.view);
    setCreateAxes({});
    setCreateCatalogCode("");
  }

  async function onPatchVariantStatus(variantId: string) {
    const status = variantStatusDraft[variantId];
    if (!status) return;
    setBusy(true);
    setError(null);
    const result = await patchAdminProductVariant(current.productId, variantId, { status });
    setBusy(false);
    if (!result.ok) {
      setError(result.message);
      return;
    }
    setView(result.view);
  }

  return (
    <div className="w-full">
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
        subtitle={`${view.brandName ?? "بدون برند"} · ${view.categoryNames.join("، ") || "بدون دسته"}`}
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
        onSectionChange={setSectionId}
        actions={[
          { id: "save", label: "ذخیره", kind: "primary", permission: canMutateCatalog ? "allowed" : "denied" },
          { id: "publish", label: "انتشار", kind: "secondary", permission: canPublish && !busy ? "allowed" : "denied" },
        ]}
        onAction={(actionId) => void onAction(actionId)}
        readOnly={readOnly}
        conflict={conflict}
        onReloadConflict={reload}
        error={error}
        onRetry={reload}
        dirtySections={dirtySections}
        summary={
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <Summary label="آمادگی فروش" value={view.publication.purchasableHint ? "آماده" : "نیاز به بررسی"} />
            <Summary label="پیشنهاد فعال" value={String(view.offers.filter((row) => row.status === "Active").length)} />
            <Summary label="بازهٔ قیمت" value={priceRange} />
            <Summary label="قابل‌فروش" value={String(available)} />
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
        {sectionId === "overview" ? (
          <div className="grid gap-4 xl:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]">
            <Card>
              <p className="text-sm font-medium text-muted">مشخصات نمایشی</p>
              {canMutateCatalog ? (
                <label className="mt-3 block text-sm font-medium">
                  عنوان نمایشی
                  <input
                    className="mt-2 min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-base"
                    value={titleDraft}
                    onChange={(event) => {
                      setTitleDraft(event.target.value);
                      setDirty(new Set(["overview"]));
                    }}
                  />
                </label>
              ) : (
                <p className="mt-2 text-lg font-semibold">{view.title}</p>
              )}
              <p className="mt-4 text-sm text-muted">دسته: {view.categoryNames.join("، ") || "—"}</p>
            </Card>
            <Card>
              <p className="text-sm font-medium text-muted">هشدارهای قابل اقدام</p>
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
              <p className="mt-4 text-2xl font-semibold tabular-nums">{available} قابل‌فروش</p>
              <p className="mt-1 text-sm text-muted">
                {onHand} موجود · {reserved} رزرو · {view.stock.length} محل
              </p>
            </Card>
          </div>
        ) : null}
        {sectionId === "attributes" ? (
          <Card>
            <ProductAttributesPanel productId={current.productId} />
          </Card>
        ) : null}
        {sectionId === "variants" ? (
          <div className="space-y-4" data-testid="admin-product-variants">
            <Card>
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p className="font-semibold">تنوع‌های محصول</p>
                  <p className="mt-1 text-sm text-muted">ترکیب محورها · بدون قیمت یا موجودی روی گونه</p>
                </div>
                <button
                  type="button"
                  className="rounded-ds border border-border px-3 py-2 text-sm hover:bg-secondary"
                  onClick={() => setSectionId("attributes")}
                >
                  تنظیم محورها در ویژگی‌ها
                </button>
              </div>
            </Card>

            <div className="overflow-x-auto md:overflow-visible">
              <ul className="space-y-2 md:hidden">
                {view.variants.map((variant) => (
                  <li key={variant.variantId} className="rounded-ds border border-border p-3">
                    <p className="font-medium">{humanizeFingerprint(variant.fingerprint)}</p>
                    <p className="mt-1 text-sm text-muted" dir="ltr">
                      {variant.catalogCodeSeam ?? "بدون کد کاتالوگ"}
                    </p>
                    <div className="mt-2 flex flex-wrap items-center gap-2">
                      <Badge tone={statusTone(variant.status)}>{formatAdminStatus(variant.status)}</Badge>
                      <span className="text-sm text-muted">{variant.offerCount} پیشنهاد</span>
                    </div>
                    {canMutateCatalog ? (
                      <div className="mt-3 flex flex-wrap gap-2">
                        <select
                          className="min-h-10 rounded-ds border border-border bg-surface px-2 text-sm"
                          value={variantStatusDraft[variant.variantId] ?? variant.status}
                          onChange={(event) =>
                            setVariantStatusDraft((prev) => ({ ...prev, [variant.variantId]: event.target.value }))
                          }
                        >
                          <option value="Draft">پیش‌نویس</option>
                          <option value="Published">منتشرشده</option>
                          <option value="Archived">بایگانی</option>
                        </select>
                        <button
                          type="button"
                          disabled={busy}
                          className="rounded-ds bg-primary px-3 py-2 text-sm text-primary-foreground disabled:opacity-50"
                          onClick={() => void onPatchVariantStatus(variant.variantId)}
                        >
                          ذخیره وضعیت
                        </button>
                      </div>
                    ) : null}
                  </li>
                ))}
              </ul>
              <table className="hidden w-full text-right text-base md:table">
                <thead className="border-b border-border text-sm text-muted">
                  <tr>
                    <th className="py-2">ترکیب</th>
                    <th>کد کاتالوگ</th>
                    <th>وضعیت</th>
                    <th>پیشنهاد</th>
                    {canMutateCatalog ? <th>عملیات</th> : null}
                  </tr>
                </thead>
                <tbody>
                  {view.variants.map((variant) => (
                    <tr key={variant.variantId} className="border-b border-border/70">
                      <td className="py-3 font-medium">{humanizeFingerprint(variant.fingerprint)}</td>
                      <td dir="ltr">{variant.catalogCodeSeam ?? "—"}</td>
                      <td>
                        <Badge tone={statusTone(variant.status)}>{formatAdminStatus(variant.status)}</Badge>
                      </td>
                      <td>{variant.offerCount}</td>
                      {canMutateCatalog ? (
                        <td>
                          <div className="flex flex-wrap items-center gap-2">
                            <select
                              className="min-h-10 rounded-ds border border-border bg-surface px-2 text-sm"
                              value={variantStatusDraft[variant.variantId] ?? variant.status}
                              onChange={(event) =>
                                setVariantStatusDraft((prev) => ({ ...prev, [variant.variantId]: event.target.value }))
                              }
                            >
                              <option value="Draft">پیش‌نویس</option>
                              <option value="Published">منتشرشده</option>
                              <option value="Archived">بایگانی</option>
                            </select>
                            <button
                              type="button"
                              disabled={busy}
                              className="rounded-ds border border-border px-3 py-1.5 text-sm hover:bg-secondary disabled:opacity-50"
                              onClick={() => void onPatchVariantStatus(variant.variantId)}
                            >
                              ذخیره
                            </button>
                          </div>
                        </td>
                      ) : null}
                    </tr>
                  ))}
                </tbody>
              </table>
              {view.variants.length === 0 ? <p className="text-sm text-muted">هنوز گونه‌ای ثبت نشده است.</p> : null}
            </div>

            {canMutateCatalog ? (
              <Card data-testid="admin-variant-create">
                <p className="font-semibold">ایجاد گونه</p>
                <p className="mt-1 text-sm text-muted">
                  محورها را از تعاریف مجاز انتخاب کنید. ماتریس کامل ترکیبی در این Task نیست.
                </p>
                {axisDefs.length === 0 ? (
                  <p className="mt-3 text-sm text-muted">تعریف محور مجازی نیست — ابتدا در تب ویژگی‌ها محور ذخیره کنید.</p>
                ) : (
                  <ul className="mt-3 space-y-3">
                    {axisDefs.map((def) => {
                      const draft = createAxes[def.definitionId] ?? { rawValue: "", enumOptionId: "" };
                      return (
                        <li key={def.definitionId} className="rounded-ds border border-border p-3">
                          <p className="text-sm font-medium">{AXIS_LABELS[def.code.toLowerCase()] ?? def.code}</p>
                          <div className="mt-2 grid gap-2 sm:grid-cols-2">
                            <label className="text-sm">
                              مقدار
                              <input
                                className="mt-1 min-h-10 w-full rounded-ds border border-border bg-surface px-3"
                                dir="ltr"
                                value={draft.rawValue}
                                onChange={(event) =>
                                  setCreateAxes((prev) => ({
                                    ...prev,
                                    [def.definitionId]: { ...draft, rawValue: event.target.value },
                                  }))
                                }
                              />
                            </label>
                            <label className="text-sm">
                              شناسه گزینه (اختیاری)
                              <input
                                className="mt-1 min-h-10 w-full rounded-ds border border-border bg-surface px-3"
                                dir="ltr"
                                value={draft.enumOptionId}
                                onChange={(event) =>
                                  setCreateAxes((prev) => ({
                                    ...prev,
                                    [def.definitionId]: { ...draft, enumOptionId: event.target.value },
                                  }))
                                }
                              />
                            </label>
                          </div>
                        </li>
                      );
                    })}
                  </ul>
                )}
                <label className="mt-3 block text-sm">
                  کد کاتالوگ (اختیاری)
                  <input
                    className="mt-1 min-h-10 w-full max-w-md rounded-ds border border-border bg-surface px-3"
                    dir="ltr"
                    value={createCatalogCode}
                    onChange={(event) => setCreateCatalogCode(event.target.value)}
                  />
                </label>
                <button
                  type="button"
                  disabled={busy || axisDefs.length === 0}
                  className="mt-4 min-h-11 rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground disabled:opacity-50"
                  onClick={() => void onCreateVariant()}
                >
                  ایجاد گونه
                </button>
              </Card>
            ) : null}
          </div>
        ) : null}
        {sectionId === "media" ? (
          <div className="space-y-4" data-testid="admin-product-media">
            <Card>
              <p className="font-semibold">گالری تصویر</p>
              <p className="mt-1 text-sm text-muted">
                پیش‌نمایش تصویر از مسیر امن رسانهٔ ویترین (قالب SVG). بارگذاری فایل باینری هنوز فعال نیست —
                دارایی را با شناسه پیوست کنید.
              </p>
              <p className="mt-2 hidden text-sm text-muted" data-testid="product-video-control">
                کنترل ویدئو محصول مخفی است.
              </p>
            </Card>

            {mediaRows.length === 0 ? (
              <p className="text-sm text-muted">هنوز تصویری پیوست نشده است.</p>
            ) : (
              <ul className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                {mediaRows.map((item, index) => (
                  <li key={item.mediaAssetId} className="rounded-ds border border-border bg-surface p-3">
                    <div className="relative aspect-square overflow-hidden rounded-ds bg-secondary">
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img
                        src={storefrontMediaUrl(item.mediaAssetId)}
                        alt={item.altText ?? `رسانه ${index + 1}`}
                        className="h-full w-full object-contain p-3"
                      />
                      {item.primary ? (
                        <span className="absolute start-2 top-2 rounded-ds bg-success/90 px-2 py-0.5 text-xs text-white">اصلی</span>
                      ) : null}
                    </div>
                    <p className="mt-2 truncate text-xs text-muted">
                      شناسه کوتاه: <span dir="ltr">{item.mediaAssetId.slice(0, 8)}</span>
                    </p>
                    <label className="mt-2 block text-sm">
                      متن جایگزین
                      <input
                        className="mt-1 min-h-10 w-full rounded-ds border border-border bg-surface px-3"
                        value={altDrafts[item.mediaAssetId] ?? ""}
                        disabled={!canMutateCatalog || busy}
                        onChange={(event) =>
                          setAltDrafts((prev) => ({ ...prev, [item.mediaAssetId]: event.target.value }))
                        }
                      />
                    </label>
                    {canMutateCatalog ? (
                      <div className="mt-3 flex flex-wrap gap-2">
                        <button
                          type="button"
                          disabled={busy || item.primary}
                          className="rounded-ds border border-border px-2 py-1.5 text-xs hover:bg-secondary disabled:opacity-50"
                          onClick={() => void onSetPrimary(item.mediaAssetId)}
                        >
                          اصلی
                        </button>
                        <button
                          type="button"
                          disabled={busy || index === 0}
                          className="rounded-ds border border-border px-2 py-1.5 text-xs hover:bg-secondary disabled:opacity-50"
                          onClick={() => void onReorder(item.mediaAssetId, -1)}
                        >
                          بالا
                        </button>
                        <button
                          type="button"
                          disabled={busy || index >= mediaRows.length - 1}
                          className="rounded-ds border border-border px-2 py-1.5 text-xs hover:bg-secondary disabled:opacity-50"
                          onClick={() => void onReorder(item.mediaAssetId, 1)}
                        >
                          پایین
                        </button>
                        <button
                          type="button"
                          disabled={busy}
                          className="rounded-ds border border-border px-2 py-1.5 text-xs hover:bg-secondary disabled:opacity-50"
                          onClick={() => void onSaveAlt(item.mediaAssetId)}
                        >
                          ذخیره متن جایگزین
                        </button>
                        <button
                          type="button"
                          disabled={busy}
                          className="rounded-ds border border-danger/40 px-2 py-1.5 text-xs text-danger hover:bg-danger/10 disabled:opacity-50"
                          onClick={() => void onRemoveMedia(item.mediaAssetId)}
                        >
                          حذف
                        </button>
                      </div>
                    ) : null}
                  </li>
                ))}
              </ul>
            )}

            {canMutateCatalog ? (
              <Card>
                <p className="font-semibold">پیوست تصویر با شناسه دارایی</p>
                <p className="mt-1 text-sm text-muted">آپلود فایل تصویری در این Task پیاده‌سازی نشده است.</p>
                <div className="mt-3 grid gap-3 sm:grid-cols-[1fr_1fr_auto]">
                  <label className="text-sm">
                    شناسه دارایی رسانه
                    <input
                      className="mt-1 min-h-11 w-full rounded-ds border border-border bg-surface px-3"
                      dir="ltr"
                      value={attachAssetId}
                      onChange={(event) => setAttachAssetId(event.target.value)}
                      placeholder="aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"
                    />
                  </label>
                  <label className="text-sm">
                    متن جایگزین (اختیاری)
                    <input
                      className="mt-1 min-h-11 w-full rounded-ds border border-border bg-surface px-3"
                      value={attachAlt}
                      onChange={(event) => setAttachAlt(event.target.value)}
                    />
                  </label>
                  <button
                    type="button"
                    disabled={busy}
                    className="mt-6 min-h-11 rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground disabled:opacity-50"
                    onClick={() => void onAttachMedia()}
                  >
                    پیوست
                  </button>
                </div>
              </Card>
            ) : null}
          </div>
        ) : null}
        {sectionId === "commercial" ? (
          <div className="grid gap-4 lg:grid-cols-2">
            {view.offers.map((offer) => {
              const price = view.prices.find((row) => row.offerId === offer.offerId);
              const tax = view.taxClassifications.find((row) => row.offerId === offer.offerId);
              const stockRows = view.stock.filter((row) => row.offerId === offer.offerId);
              const sellable = stockRows.reduce((sum, row) => sum + row.available, 0);
              return (
                <Card key={offer.offerId}>
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-center gap-3">
                      <span className="flex size-12 items-center justify-center rounded-full bg-secondary text-sm font-semibold">
                        {offer.sellerDisplayName.slice(0, 1)}
                      </span>
                      <div>
                        <p className="text-lg font-semibold">{offer.sellerDisplayName}</p>
                        <p className="text-sm text-muted" dir="ltr">
                          {offer.sellerSku ?? "—"}
                        </p>
                      </div>
                    </div>
                    <Badge tone={offer.status === "Active" ? "success" : "neutral"}>{offer.status === "Active" ? "فعال" : offer.status}</Badge>
                  </div>
                  <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
                    <div>
                      <dt className="text-muted">کانال</dt>
                      <dd>{offer.channel}</dd>
                    </div>
                    <div>
                      <dt className="text-muted">مالیات</dt>
                      <dd>{tax?.displayName ?? "—"}</dd>
                    </div>
                    <div>
                      <dt className="text-muted">قیمت</dt>
                      <dd className="text-lg font-semibold tabular-nums">{money(price?.amountExclusiveOfTax, price?.currency)}</dd>
                    </div>
                    <div>
                      <dt className="text-muted">قابل‌فروش</dt>
                      <dd className="text-lg font-semibold tabular-nums">{sellable}</dd>
                    </div>
                  </dl>
                  <p className="mt-3 text-sm text-muted">{stockRows.length} محل موجودی</p>
                </Card>
              );
            })}
          </div>
        ) : null}
        {sectionId === "inventory" ? (
          <div className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
              <Summary label="موجود" value={String(onHand)} />
              <Summary label="رزرو" value={String(reserved)} />
              <Summary label="قابل‌فروش" value={String(available)} />
              <Summary label="محل‌ها" value={String(view.stock.length)} />
              <Summary label="سلامت" value={available > 0 ? "سالم" : "کم‌موجود"} />
            </div>
            <ul className="space-y-2 md:hidden">
              {view.stock.map((row) => {
                const offer = view.offers.find((item) => item.offerId === row.offerId);
                const healthy = row.available > 3;
                const warning = row.available > 0 && row.available <= 3;
                return (
                  <li key={`${row.offerId}:${row.locationId}`} className="rounded-ds border border-border p-3">
                    <p className="font-semibold">{row.locationName}</p>
                    <p className="text-sm text-muted">{offer?.sellerDisplayName}</p>
                    <p className="mt-2 text-lg font-semibold tabular-nums">{row.available} قابل‌فروش</p>
                    <p className="text-sm text-muted">
                      موجود {row.onHand} · رزرو {row.reserved} · {healthy ? "سالم" : warning ? "کم" : "ناموجود"}
                    </p>
                  </li>
                );
              })}
            </ul>
            <div className="hidden overflow-x-auto md:block">
              <table className="w-full text-right text-base">
                <thead className="border-b border-border text-sm text-muted">
                  <tr>
                    <th className="py-3">محل</th>
                    <th>فروشنده / پیشنهاد</th>
                    <th>موجود</th>
                    <th>رزرو</th>
                    <th>قابل‌فروش</th>
                    <th>سلامت</th>
                  </tr>
                </thead>
                <tbody>
                  {view.stock.map((row) => {
                    const offer = view.offers.find((item) => item.offerId === row.offerId);
                    const healthy = row.available > 3;
                    const warning = row.available > 0 && row.available <= 3;
                    return (
                      <tr key={`${row.offerId}:${row.locationId}`} className="border-b border-border/70">
                        <td className="py-3">
                          <p className="text-base font-semibold">{row.locationName}</p>
                        </td>
                        <td>{offer?.sellerDisplayName}</td>
                        <td className="text-end tabular-nums">{row.onHand}</td>
                        <td className="text-end tabular-nums">{row.reserved}</td>
                        <td className="text-end text-lg font-semibold tabular-nums">{row.available}</td>
                        <td>
                          <Badge tone={healthy ? "success" : warning ? "warning" : "danger"}>{healthy ? "سالم" : warning ? "کم" : "ناموجود"}</Badge>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        ) : null}
        {sectionId === "seo" ? (
          <div className="grid gap-4 lg:grid-cols-3">
            <Card>
              <p className="text-sm text-muted">آمادگی سئو</p>
              <p className="mt-2 text-xl font-semibold">{view.seo.slugSeam && view.seo.seoTitleSeam ? "آماده" : "ناقص"}</p>
              <p className="mt-2 text-sm text-muted">نشانی صفحه و عنوان جستجو باید هر دو پر باشند.</p>
            </Card>
            <Card>
              <p className="text-sm text-muted">نشانی و عنوان</p>
              <p className="mt-3 text-sm text-muted">نشانی صفحه</p>
              <p className="text-lg font-medium" dir="ltr">
                {view.seo.slugSeam ?? "—"}
              </p>
              <p className="mt-3 text-sm text-muted">عنوان جستجو</p>
              <p className="text-lg font-medium">{view.seo.seoTitleSeam ?? "—"}</p>
            </Card>
            <Card>
              <p className="text-sm text-muted">یادداشت محتوا</p>
              <p className="mt-3 text-base">پیش‌نمایش ویترین فروشگاه در این صفحه ساخته نمی‌شود.</p>
            </Card>
          </div>
        ) : null}
        {sectionId === "publication" ? (
          <div className="space-y-4" data-testid="admin-product-publication">
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <p className="mb-2 font-semibold">آمادگی محتوا</p>
                <ul className="space-y-2">
                  <Check ok={Boolean(view.title)} label="عنوان" />
                  <Check ok={view.media.length > 0} label="رسانه" />
                </ul>
              </div>
              <div>
                <p className="mb-2 font-semibold">آمادگی فروش</p>
                <ul className="space-y-2">
                  <Check ok={view.offers.some((row) => row.status === "Active")} label="پیشنهاد فعال" />
                  <Check ok={view.prices.length > 0} label="قیمت" />
                </ul>
              </div>
              <div>
                <p className="mb-2 font-semibold">آمادگی موجودی و سئو</p>
                <ul className="space-y-2">
                  <Check ok={available > 0} label="موجودی" />
                  <Check ok={Boolean(view.seo.slugSeam)} label="سئو" />
                </ul>
              </div>
              <div>
                <p className="mb-2 font-semibold">وضعیت انتشار</p>
                <p className="text-lg font-semibold">{formatAdminStatus(view.status)}</p>
                <p className="mt-2 text-sm text-muted">برای فروش باید پیشنهاد، قیمت و موجودی آماده باشند.</p>
              </div>
            </div>
            <Card>
              <p className="font-semibold">عملیات چرخهٔ عمر</p>
              <p className="mt-1 text-sm text-muted">انتشار از نوار فضای کار؛ لغو انتشار / بایگانی / حذف امن اینجا.</p>
              <div className="mt-4 flex flex-wrap gap-2">
                <button
                  type="button"
                  disabled={!canPublish || busy || view.status !== "Published"}
                  className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
                  onClick={() => void runLifecycle("unpublish")}
                >
                  لغو انتشار
                </button>
                <button
                  type="button"
                  disabled={!canPublish || busy || view.status === "Archived"}
                  className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
                  onClick={() => void runLifecycle("archive")}
                >
                  بایگانی
                </button>
                <button
                  type="button"
                  disabled={!canMutateCatalog || busy}
                  className="min-h-11 rounded-ds border border-danger/40 px-4 text-sm text-danger hover:bg-danger/10 disabled:opacity-50"
                  onClick={() => void runLifecycle("delete")}
                >
                  حذف امن
                </button>
              </div>
            </Card>
          </div>
        ) : null}
        {sectionId === "history" ? (
          <ol className="space-y-3 border-s-2 border-border ps-4">
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

function Check({ ok, label }: { ok: boolean; label: string }) {
  return (
    <li className="flex items-center justify-between rounded-ds border border-border px-3 py-2">
      <span>{label}</span>
      <Badge tone={ok ? "success" : "warning"}>{ok ? "آماده" : "ناقص"}</Badge>
    </li>
  );
}
