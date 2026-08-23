"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { ErrorState, WorkspaceShell, faWorkspaceMessages, useTheme } from "../../design-system";
import { loadProductWorkspace, patchCatalogTitle, type HostReadSource } from "./host-client";
import { groupOffersBySeller, type ProductWorkspaceView } from "./workspace-model";

const sections = [
  { id: "overview", label: "Overview" },
  { id: "variants", label: "Variants" },
  { id: "media", label: "Media" },
  { id: "commercial", label: "Commercial" },
  { id: "inventory", label: "Inventory" },
  { id: "seo", label: "SEO & Content" },
  { id: "publication", label: "Publication" },
  { id: "history", label: "History" },
];

/**
 * Workspace محصول Admin. Commercial چند فروشنده را جدا از Product.Price نشان می‌دهد.
 * SpiceDB در این کامپوننت صدا زده نمی‌شود؛ مجوز از Host می‌آید.
 */
export function ProductWorkspaceScreen({ productId, viewScope = false }: { productId: string; viewScope?: boolean }) {
  const { theme, setColorScheme, setDirection } = useTheme();
  const [view, setView] = useState<ProductWorkspaceView | null>(null);
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [sectionId, setSectionId] = useState("overview");
  const [mode, setMode] = useState<"view" | "edit">("view");
  const [narrow, setNarrow] = useState(false);
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
  const readOnly = !view?.permissions.canEditCatalog;

  const dirtySections = useMemo(() => dirty, [dirty]);

  if (!view) {
    if (source === "error") {
      return (
        <div className="p-4">
          <ErrorState title="Workspace از Host خوانده نشد" detail={error ?? undefined} onRetry={reload} retryLabel={faWorkspaceMessages.retry} />
        </div>
      );
    }
    return <p className="p-4">در حال بارگذاری Workspace…</p>;
  }

  const current = view;

  async function onAction(actionId: string) {
    if (actionId === "save") {
      const expected = current.catalogUpdatedAt;
      const result = await patchCatalogTitle(current.productId, "fa", titleDraft, expected, viewScope);
      if (!result.ok) {
        if (result.errorCode === "workspace.catalog.stale") {
          setConflict("workspace.catalog.stale");
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
    if (actionId === "conflict-demo") {
      if (source !== "host") {
        setConflict("workspace.catalog.stale — UI demonstration; Host did not return HTTP 409");
        return;
      }
      const result = await patchCatalogTitle(current.productId, "fa", titleDraft, "2000-01-01T00:00:00Z", viewScope);
      if (!result.ok) {
        setConflict(result.errorCode);
        return;
      }
      setConflict("workspace.catalog.stale");
    }
  }

  return (
    <div className="p-4" dir={theme.direction}>
      <p className="mb-2 text-sm" data-testid="workspace-source">
        منبع: {source === "host" ? "Host" : "خطای Host — فیکسچر فعال نشد"} · scope={viewScope ? "view" : "edit"}
      </p>
      <div className="mb-3 flex flex-wrap gap-2">
        <button type="button" className="rounded-ds border px-3 py-2" onClick={() => setNarrow((value) => !value)}>
          {narrow ? "desktop" : "mobile"}
        </button>
        <button
          type="button"
          className="rounded-ds border px-3 py-2"
          onClick={() => setDirection(theme.direction === "rtl" ? "ltr" : "rtl")}
        >
          {theme.direction}
        </button>
        <button
          type="button"
          className="rounded-ds border px-3 py-2"
          onClick={() => setColorScheme(theme.colorScheme === "dark" ? "light" : "dark")}
        >
          {theme.colorScheme}
        </button>
        <button type="button" className="rounded-ds border px-3 py-2" onClick={() => setMode((value) => (value === "view" ? "edit" : "view"))}>
          {mode}
        </button>
      </div>
      <WorkspaceShell
        title={view.title}
        subtitle={`Catalog ${view.status} · purchasable hint ${view.publication.purchasableHint ? "yes" : "no"}`}
        breadcrumbs={["Admin", "Products", view.title]}
        statusItems={[
          { id: "pub", label: view.status, tone: "success" },
          {
            id: "warn",
            label: `${view.readinessWarnings.length} warnings`,
            tone: view.readinessWarnings.length ? "warning" : "neutral",
          },
        ]}
        sections={sections}
        activeSectionId={sectionId}
        onSectionChange={setSectionId}
        actions={[
          { id: "save", label: "Save", kind: "primary", permission: view.permissions.canEditCatalog ? "allowed" : "denied" },
          { id: "publish", label: "Publish", kind: "secondary", permission: view.permissions.canPublish ? "allowed" : "denied" },
          { id: "conflict-demo", label: "Stale save", kind: "secondary", permission: "allowed" },
        ]}
        onAction={(actionId) => void onAction(actionId)}
        forceNarrow={narrow}
        readOnly={readOnly}
        conflict={conflict}
        onReloadConflict={reload}
        error={error}
        onRetry={reload}
        dirtySections={dirtySections}
        summary={
          <p>
            Variants {view.variants.length} · Offers {view.offers.length} · Sellers {sellers.size}
          </p>
        }
        inspector={<p>بازرس: طبقهٔ مالیات جدا از نرخ؛ Related seller north</p>}
        activity={view.activity.map((item) => ({ id: item.summary, at: item.at, actor: "ops", summary: item.summary }))}
        audit={view.audit.map((item) => ({ id: item.summary, at: item.at, actor: "system", event: item.summary }))}
      >
        {sectionId === "overview" ? (
          <div>
            <p>Brand: {view.brandName}</p>
            <p>Categories: {view.categoryNames.join("، ")}</p>
            <p>Mode: {mode}</p>
            {mode === "edit" && view.permissions.canEditCatalog ? (
              <label className="mt-2 block">
                عنوان Catalog
                <input
                  className="mt-1 min-h-11 w-full rounded-ds border px-3"
                  value={titleDraft}
                  onChange={(event) => {
                    setTitleDraft(event.target.value);
                    setDirty(new Set(["overview"]));
                  }}
                />
              </label>
            ) : (
              <p>نمای خواندنی هویت Catalog — Price/Stock روی Product نیست.</p>
            )}
          </div>
        ) : null}
        {sectionId === "variants" ? (
          <ul>
            {view.variants.map((variant) => (
              <li key={variant.variantId}>
                {variant.fingerprint} · offers {variant.offerCount}
              </li>
            ))}
          </ul>
        ) : null}
        {sectionId === "media" ? <p>Primary {view.media[0]?.mediaAssetId}</p> : null}
        {sectionId === "commercial" ? (
          <table className="w-full text-sm">
            <thead>
              <tr>
                <th>Seller</th>
                <th>Offer</th>
                <th>SKU</th>
                <th>Price ex-tax</th>
                <th>Tax class</th>
              </tr>
            </thead>
            <tbody>
              {view.offers.map((offer) => (
                <tr key={offer.offerId}>
                  <td>{offer.sellerPartyId}</td>
                  <td>{offer.status}</td>
                  <td>{offer.sellerSku}</td>
                  <td>{view.prices.find((price) => price.offerId === offer.offerId)?.amountExclusiveOfTax}</td>
                  <td>{view.taxClassifications.find((row) => row.offerId === offer.offerId)?.categoryCode ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
        {sectionId === "inventory" ? (
          <table className="w-full text-sm">
            <thead>
              <tr>
                <th>Offer</th>
                <th>Location</th>
                <th>OnHand</th>
                <th>Reserved</th>
                <th>Available</th>
              </tr>
            </thead>
            <tbody>
              {view.stock.map((row) => (
                <tr key={`${row.offerId}:${row.locationId}`}>
                  <td>{row.offerId}</td>
                  <td>{row.locationCode}</td>
                  <td>{row.onHand}</td>
                  <td>{row.reserved}</td>
                  <td>{row.available}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
        {sectionId === "seo" ? (
          <div>
            <p>Slug: {view.seo.slugSeam}</p>
            <p>{view.seo.semanticNote}</p>
          </div>
        ) : null}
        {sectionId === "publication" ? (
          <div>
            <p>Catalog {view.publication.catalogStatus}</p>
            <p>Purchasable hint {String(view.publication.purchasableHint)} سیگنال UI است نه Product.Stock.</p>
          </div>
        ) : null}
        {sectionId === "history" ? <p>Activity و Audit در ستون بازرس جدا هستند.</p> : null}
      </WorkspaceShell>
    </div>
  );
}
