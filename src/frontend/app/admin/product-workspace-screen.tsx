"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Badge, Card, ErrorState, WorkspaceShell, faWorkspaceMessages } from "../../design-system";
import { loadProductWorkspace, patchCatalogTitle, type HostReadSource } from "./host-client";
import { type ProductWorkspaceView } from "./workspace-model";

const sections = [
  { id: "overview", label: "نمای کلی" },
  { id: "variants", label: "گونه‌ها" },
  { id: "media", label: "رسانه" },
  { id: "commercial", label: "فروش و قیمت" },
  { id: "inventory", label: "موجودی" },
  { id: "seo", label: "سئو و محتوا" },
  { id: "publication", label: "انتشار" },
  { id: "history", label: "تاریخچه" },
];

function money(amount: number | undefined, currency: string | undefined): string {
  if (amount == null) {
    return "—";
  }
  return `${new Intl.NumberFormat("fa-IR").format(amount)} ${currency ?? ""}`.trim();
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

  const reload = useCallback(() => {
    void loadProductWorkspace(productId, viewScope).then((result) => {
      setSource(result.source);
      setView(result.view);
      setTitleDraft(result.view?.title ?? "");
      setConflict(null);
      setError(result.message ?? null);
    });
  }, [productId, viewScope]);

  useEffect(() => {
    reload();
  }, [reload]);

  const readOnly = !view?.permissions.canEditCatalog || viewScope;
  const dirtySections = useMemo(() => dirty, [dirty]);

  if (!view) {
    if (source === "error") {
      return (
        <div className="p-6">
          <ErrorState title="Workspace از Host خوانده نشد" detail={error ?? undefined} onRetry={reload} retryLabel={faWorkspaceMessages.retry} />
        </div>
      );
    }
    return <p className="p-6 text-base">در حال بارگذاری Workspace…</p>;
  }

  const current = view;
  const onHand = view.stock.reduce((sum, row) => sum + row.onHand, 0);
  const reserved = view.stock.reduce((sum, row) => sum + row.reserved, 0);
  const available = view.stock.reduce((sum, row) => sum + row.available, 0);
  const amounts = view.prices.map((row) => row.amountExclusiveOfTax);
  const priceRange = amounts.length
    ? `${money(Math.min(...amounts), view.prices[0]?.currency)} — ${money(Math.max(...amounts), view.prices[0]?.currency)}`
    : "بدون قیمت";

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
    }
  }

  return (
    <div className="w-full">
      <WorkspaceShell
        flush
        leading={
          <div className="flex size-16 shrink-0 items-center justify-center rounded-ds bg-secondary text-sm text-muted md:size-20">تصویر</div>
        }
        title={view.title}
        subtitle={`${view.brandName ?? "بدون برند"} · ${view.categoryNames.join("، ") || "بدون دسته"}`}
        breadcrumbs={["عملیات", "محصولات", view.title]}
        statusItems={[
          { id: "pub", label: view.status === "Published" ? "منتشرشده" : view.status, tone: "success" },
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
          { id: "save", label: "ذخیره", kind: "primary", permission: view.permissions.canEditCatalog && !viewScope ? "allowed" : "denied" },
          { id: "publish", label: "انتشار", kind: "secondary", permission: view.permissions.canPublish && !viewScope ? "allowed" : "denied" },
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
              {view.permissions.canEditCatalog && !viewScope ? (
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
        {sectionId === "variants" ? (
          <div className="overflow-x-auto md:overflow-visible">
            <ul className="space-y-2 md:hidden">
              {view.variants.map((variant, index) => (
                <li key={variant.variantId} className="rounded-ds border border-border p-3">
                  <p className="font-medium">گونه {index + 1}</p>
                  <p className="mt-1 text-sm text-muted">
                    {variant.status} · {variant.offerCount} پیشنهاد
                  </p>
                </li>
              ))}
            </ul>
            <table className="hidden w-full text-right text-base md:table">
              <thead className="border-b border-border text-sm text-muted">
                <tr>
                  <th className="py-2">گونه</th>
                  <th>وضعیت</th>
                  <th>پیشنهاد</th>
                </tr>
              </thead>
              <tbody>
                {view.variants.map((variant, index) => (
                  <tr key={variant.variantId} className="border-b border-border/70">
                    <td className="py-3 font-medium">گونه {index + 1}</td>
                    <td>
                      <Badge tone="success">{variant.status}</Badge>
                    </td>
                    <td>{variant.offerCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
        {sectionId === "media" ? (
          <div className="flex gap-4">
            <div className="flex h-32 w-32 items-center justify-center rounded-ds bg-secondary text-sm text-muted">تصویر اصلی</div>
            <p className="text-sm text-muted">رسانهٔ باینری در این Task بارگذاری نمی‌شود.</p>
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
              <p className="text-sm text-muted">آمادگی SEO</p>
              <p className="mt-2 text-xl font-semibold">{view.seo.slugSeam && view.seo.seoTitleSeam ? "آماده" : "ناقص"}</p>
              <p className="mt-2 text-sm text-muted">slug و عنوان جستجو باید هر دو پر باشند.</p>
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
              <p className="text-lg font-semibold">{view.status === "Published" ? "منتشرشده" : view.status}</p>
              <p className="mt-2 text-sm text-muted">برای فروش باید پیشنهاد، قیمت و موجودی آماده باشند.</p>
            </div>
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
