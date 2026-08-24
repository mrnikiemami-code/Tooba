"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Badge, Card, ErrorState, WorkspaceShell, faWorkspaceMessages } from "../../design-system";
import { loadProductWorkspace, patchCatalogTitle, type HostReadSource } from "./host-client";
import { groupOffersBySeller, type ProductWorkspaceView } from "./workspace-model";

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

  const sellers = view ? groupOffersBySeller(view) : new Map();
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
    <div className="mx-auto max-w-6xl p-4 md:p-6">
      <WorkspaceShell
        title={view.title}
        subtitle={`${view.brandName ?? "بدون برند"} · ${view.categoryNames.join("، ") || "بدون دسته"}`}
        breadcrumbs={["Admin", "محصولات", view.title]}
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
            <Summary label="گونه‌ها" value={String(view.variants.length)} />
            <Summary label="فروشندگان" value={String(sellers.size)} />
            <Summary label="پیشنهادها" value={String(view.offers.length)} />
            <Summary label="بازهٔ قیمت" value={priceRange} />
          </div>
        }
        inspector={
          <div className="space-y-2 text-sm">
            <p className="font-medium">آمادگی عملیات</p>
            <p>طبقهٔ مالیات جدا از نرخ است.</p>
            <p data-testid="workspace-source">{source === "host" ? "همگام با Host" : "Host در دسترس نیست"}</p>
          </div>
        }
        activity={view.activity.map((item) => ({ id: item.summary, at: item.at, actor: "ops", summary: item.summary }))}
        audit={view.audit.map((item) => ({ id: item.summary, at: item.at, actor: "system", event: item.summary }))}
      >
        {sectionId === "overview" ? (
          <div className="grid gap-3 md:grid-cols-2">
            <Card>
              <p className="text-sm text-muted">هویت Catalog</p>
              {view.permissions.canEditCatalog && !viewScope ? (
                <label className="mt-3 block text-sm font-medium">
                  عنوان
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
              <p className="mt-3 text-sm text-muted">قیمت و موجودی روی Product نیستند.</p>
            </Card>
            <Card>
              <p className="text-sm text-muted">سلامت موجودی</p>
              <p className="mt-2 text-2xl font-semibold">{available} قابل‌فروش</p>
              <p className="mt-1 text-sm text-muted">
                {onHand} موجود · {reserved} رزرو · {view.stock.length} محل
              </p>
            </Card>
          </div>
        ) : null}
        {sectionId === "variants" ? (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[36rem] text-right text-base">
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
          <div className="space-y-4">
            <p className="text-sm text-muted">یک گونه می‌تواند چند پیشنهاد فروشنده با قیمت و موجودی جدا داشته باشد.</p>
            {view.offers.map((offer) => {
              const price = view.prices.find((row) => row.offerId === offer.offerId);
              const tax = view.taxClassifications.find((row) => row.offerId === offer.offerId);
              const stockRows = view.stock.filter((row) => row.offerId === offer.offerId);
              return (
                <Card key={offer.offerId}>
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <p className="text-lg font-semibold">{offer.sellerDisplayName}</p>
                      <p className="text-sm text-muted">SKU {offer.sellerSku} · {offer.channel}</p>
                    </div>
                    <Badge tone={offer.status === "Active" ? "success" : "neutral"}>{offer.status === "Active" ? "فعال" : offer.status}</Badge>
                  </div>
                  <dl className="mt-4 grid gap-3 sm:grid-cols-3">
                    <div>
                      <dt className="text-sm text-muted">قیمت بدون مالیات</dt>
                      <dd className="text-base font-medium">{money(price?.amountExclusiveOfTax, price?.currency)}</dd>
                    </div>
                    <div>
                      <dt className="text-sm text-muted">طبقهٔ مالیات</dt>
                      <dd>{tax?.displayName ?? tax?.categoryCode ?? "—"}</dd>
                    </div>
                    <div>
                      <dt className="text-sm text-muted">موجودی پیشنهاد</dt>
                      <dd>{stockRows.reduce((sum, row) => sum + row.available, 0)} قابل‌فروش</dd>
                    </div>
                  </dl>
                </Card>
              );
            })}
          </div>
        ) : null}
        {sectionId === "inventory" ? (
          <div className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-3">
              <Summary label="موجود" value={String(onHand)} />
              <Summary label="رزرو" value={String(reserved)} />
              <Summary label="قابل‌فروش" value={String(available)} />
            </div>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[40rem] text-right text-base">
                <thead className="border-b border-border text-sm text-muted">
                  <tr>
                    <th className="py-2">محل</th>
                    <th>فروشنده / SKU</th>
                    <th>موجود</th>
                    <th>رزرو</th>
                    <th>قابل‌فروش</th>
                  </tr>
                </thead>
                <tbody>
                  {view.stock.map((row) => {
                    const offer = view.offers.find((item) => item.offerId === row.offerId);
                    return (
                      <tr key={`${row.offerId}:${row.locationId}`} className="border-b border-border/70">
                        <td className="py-3">
                          <p className="font-medium">{row.locationName}</p>
                          <p className="text-sm text-muted">{row.locationCode}</p>
                        </td>
                        <td>
                          {offer?.sellerDisplayName} · {offer?.sellerSku ?? "—"}
                        </td>
                        <td>{row.onHand}</td>
                        <td>{row.reserved}</td>
                        <td className="font-medium">{row.available}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        ) : null}
        {sectionId === "seo" ? (
          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <p className="font-medium">محتوای معنایی</p>
              <p className="mt-2 text-sm text-muted">Slug</p>
              <p className="font-medium">{view.seo.slugSeam ?? "—"}</p>
              <p className="mt-3 text-sm text-muted">عنوان SEO</p>
              <p>{view.seo.seoTitleSeam ?? "—"}</p>
            </Card>
            <Card>
              <p className="font-medium">ترکیب صفحه</p>
              <p className="mt-2 text-sm text-muted">{view.seo.semanticNote}</p>
            </Card>
          </div>
        ) : null}
        {sectionId === "publication" ? (
          <ul className="space-y-3">
            <Check ok={Boolean(view.title)} label="عنوان" />
            <Check ok={view.media.length > 0} label="رسانه" />
            <Check ok={view.offers.some((row) => row.status === "Active")} label="پیشنهاد فعال" />
            <Check ok={view.prices.length > 0} label="قیمت" />
            <Check ok={available > 0} label="موجودی" />
            <Check ok={Boolean(view.seo.slugSeam)} label="سئو" />
            <p className="text-sm text-muted">منتشرشده با قابل‌خرید یکی نیست.</p>
          </ul>
        ) : null}
        {sectionId === "history" ? <p className="text-sm text-muted">رویدادها در ستون کناری آمده‌اند.</p> : null}
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
