"use client";

import { useEffect, useState, type ReactNode } from "react";
import { Check, ChevronLeft, ChevronRight, ImageOff, Lock, Search } from "lucide-react";
import { Dialog } from "../../design-system";
import type { OfferWorkspaceMessages } from "./messages";
import type { OfferFormatters } from "./i18n";
import type {
  OfferMode,
  OfferProductOption,
  OfferRole,
  OfferSellerOption,
  OfferVariantOption,
  OfferWorkspaceDetail,
  OfferPickerPage,
} from "./types";
import styles from "./offer-workspace.module.css";
import { Note, ReadOnlyChip, SectionTitle } from "./tab-shared";

export interface IdentityTabProps {
  t: OfferWorkspaceMessages;
  format: OfferFormatters;
  mode: OfferMode;
  role: OfferRole;
  detail: OfferWorkspaceDetail | null;
  sellerSku: string;
  onSellerSkuChange: (value: string) => void;
  fixedSeller?: OfferSellerOption;
  selectedSeller: OfferSellerOption | null;
  onSelectSeller: (seller: OfferSellerOption) => void;
  channels: string[];
  selectedChannel: string;
  onSelectChannel: (value: string) => void;
  inventoryLocations: import("./types.ts").OfferInventoryLocationOption[];
  localInventory: import("./types.ts").OfferLocalInventoryDraft[];
  onLocalInventoryChange: (locationId: string, patch: { onHand?: string; reason?: string }) => void;
  selectedProduct: OfferProductOption | null;
  onSelectProduct: (product: OfferProductOption) => void;
  searchProducts: (search: string, page: number) => Promise<OfferPickerPage<OfferProductOption>>;
  selectedVariant: OfferVariantOption | null;
  onSelectVariant: (variant: OfferVariantOption | null) => void;
  loadProductVariants: (productId: string) => Promise<OfferVariantOption[]>;
  searchSellers?: (search: string, page: number) => Promise<OfferPickerPage<OfferSellerOption>>;
  showErrors?: boolean;
  extra?: ReactNode;
}

export function IdentityTab(props: IdentityTabProps) {
  if (props.mode === "edit" && props.detail) {
    return <EditIdentity {...props} detail={props.detail} />;
  }
  return <CreateIdentity {...props} />;
}

function EditIdentity(props: IdentityTabProps & { detail: OfferWorkspaceDetail }) {
  const { t, format, detail } = props;
  const channel = detail.channel === "Marketplace" ? t.channelMarketplace : detail.channel;
  return (
    <div className={styles.split} data-testid="offer-tab-identity">
      <div className={styles.stack}>
        <section className={styles.card}>
          <header className={styles.cardHd}>
            <SectionTitle title={t.identityTitle} desc={t.identityDesc} />
            <div className={styles.cardHdActions}>
              <ReadOnlyChip><Lock size={11} aria-hidden /> {t.sellerLocked}</ReadOnlyChip>
            </div>
          </header>
          <div className={styles.cardBd}>
            <Note tone="info">{t.identityLocked}</Note>
            <div className={styles.formGrid} style={{ marginTop: 14 }}>
              <ReadBox label={t.seller} value={detail.sellerDisplayName || detail.sellerPartyId} />
              <ReadBox label={t.channel} value={channel} hint={t.channelHint} />
              <div className={`${styles.field} ${styles.span2}`}>
                <span className={styles.fieldLabel}>{t.product}</span>
                <ProductSummary detail={detail} t={t} />
              </div>
              <ReadBox
                label={t.variant}
                value={
                  <span className={styles.metaV}>
                    {detail.variantLabel || t.variantUnnamed}{" "}
                    <ReadOnlyChip><Lock size={10} aria-hidden /> {t.variantLocked}</ReadOnlyChip>
                  </span>
                }
                hint={detail.variantCode ? `${t.variantCodeLabel}: ${detail.variantCode}` : undefined}
              />
              <label className={styles.field}>
                <span className={styles.fieldLabel}>{t.sellerSku}</span>
                <input
                  className={`${styles.ctl} ${styles.ctlLtr}`}
                  value={props.sellerSku}
                  onChange={(event) => props.onSellerSkuChange(event.target.value)}
                  dir="ltr"
                  data-testid="offer-seller-sku"
                />
                <span className={styles.fieldHint}>{t.skuHint}</span>
              </label>
            </div>
          </div>
        </section>
        {props.extra ? <div>{props.extra}</div> : null}
      </div>
      <aside className={styles.stack}>
        <ReadinessCard
          t={t}
          format={format}
          sellerName={detail.sellerDisplayName || detail.sellerPartyId}
          productName={detail.productTitle}
          variantLabel={detail.variantLabel || detail.variantCode}
          recordCreated
        />
        <HistoryHint t={t} />
      </aside>
    </div>
  );
}

function CreateIdentity(props: IdentityTabProps) {
  const { t, format, selectedProduct, selectedVariant, showErrors, selectedChannel, channels } = props;
  const [sellerOpen, setSellerOpen] = useState(false);
  const [productOpen, setProductOpen] = useState(false);
  const [variantOpen, setVariantOpen] = useState(false);
  const [variants, setVariants] = useState<OfferVariantOption[]>([]);
  const [variantLoading, setVariantLoading] = useState(false);
  const [variantError, setVariantError] = useState<string | null>(null);
  const [autoSelected, setAutoSelected] = useState(false);

  useEffect(() => {
    setVariants([]);
    setAutoSelected(false);
    setVariantError(null);
    if (!selectedProduct) return;
    let active = true;
    setVariantLoading(true);
    void props.loadProductVariants(selectedProduct.productId).then((rows) => {
      if (!active) return;
      setVariantLoading(false);
      setVariants(rows);
      if (rows.length === 1) {
        setAutoSelected(true);
        props.onSelectVariant(rows[0]!);
      } else if (rows.length === 0) {
        props.onSelectVariant(null);
      }
    }).catch(() => {
      if (!active) return;
      setVariantLoading(false);
      setVariantError(t.variantPickerError);
    });
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedProduct?.productId]);

  const variantDisabled = !selectedProduct || variantLoading;
  const variantButtonLabel = selectedVariant
    ? (variantTitle(selectedVariant, t))
    : !selectedProduct
      ? t.chooseProductFirst
      : variantLoading
        ? t.loading
        : variantError
          ? t.variantPickerError
          : variants.length === 0
            ? t.variantsEmpty
            : t.selectVariantTitle;

  return (
    <div className={styles.split} data-testid="offer-tab-identity">
      <div className={styles.stack}>
        <section className={styles.card}>
          <header className={styles.cardHd}>
            <SectionTitle title={t.identityTitle} desc={t.identityDesc} />
          </header>
          <div className={styles.cardBd}>
            <div className={styles.formGrid}>
              <div className={styles.field}>
                <span className={styles.fieldLabel}>{t.seller} <span className={styles.req}>*</span></span>
                {props.fixedSeller ? (
                  <div className={styles.ctlReadonly} data-testid="offer-fixed-seller" style={{ display: "flex", alignItems: "center", minHeight: "var(--ow-control)", borderRadius: 6, border: "1px solid var(--ow-border)", padding: "0 10px" }}>
                    {props.fixedSeller.displayName || t.sellerSession}
                  </div>
                ) : (
                  <ChoiceField
                    value={props.selectedSeller?.displayName || t.select}
                    onOpen={() => setSellerOpen(true)}
                    testId="offer-seller-picker-toggle"
                  />
                )}
                <span className={styles.fieldHint}>{props.fixedSeller ? t.sellerSessionHint : t.sellerHint}</span>
                {showErrors && !props.fixedSeller && !props.selectedSeller ? <span className={styles.fieldErr}>{t.sellerRequired}</span> : null}
              </div>

              <label className={styles.field}>
                <span className={styles.fieldLabel}>{t.channel}</span>
                <select
                  className={`${styles.ctl} ${styles.select}`}
                  value={selectedChannel}
                  onChange={(event) => props.onSelectChannel(event.target.value)}
                  aria-label={t.channel}
                  data-testid="offer-channel-select"
                >
                  {channels.map((channel) => <option key={channel} value={channel}>{channel === "Direct" ? t.channelDirect : channel === "Agency" ? t.channelAgency : channel === "Corporate" ? t.channelCorporate : channel === "Affiliate" ? t.channelAffiliate : channel === "Api" ? t.channelApi : t.channelMarketplace}</option>)}
                </select>
                <span className={styles.fieldHint}>{t.channelHint}</span>
              </label>

              <div className={`${styles.field} ${styles.span2}`}>
                <span className={styles.fieldLabel}>{t.product} <span className={styles.req}>*</span></span>
                <ChoiceField
                  value={selectedProduct ? selectedProduct.title : t.select}
                  onOpen={() => setProductOpen(true)}
                  testId="offer-product-picker-toggle"
                  detail={selectedProduct ? `${selectedProduct.brandName ?? ""} · ${format.integer(selectedProduct.publishedVariantCount)}` : undefined}
                />
                <span className={styles.fieldHint}>{t.productPickerHint}</span>
                {showErrors && !selectedProduct ? <span className={styles.fieldErr}>{t.productRequired}</span> : null}
              </div>

              <div className={`${styles.field} ${styles.span2}`}>
                <span className={styles.fieldLabel}>{t.variant} <span className={styles.req}>*</span></span>
                <ChoiceField
                  value={variantButtonLabel}
                  onOpen={() => setVariantOpen(true)}
                  disabled={variantDisabled}
                  testId="offer-variant-picker-toggle"
                  detail={selectedVariant ? distinctVariantAxes(selectedVariant) || selectedVariant.catalogCode || undefined : undefined}
                />
                {autoSelected ? <span className={styles.fieldHint}>{t.singleVariantAuto}</span> : <span className={styles.fieldHint}>{t.variantHint}</span>}
                {variantError ? <span className={styles.fieldErr}>{variantError}</span> : null}
                {showErrors && !selectedVariant ? <span className={styles.fieldErr}>{t.variantRequired}</span> : null}
              </div>

              <label className={styles.field}>
                <span className={styles.fieldLabel}>{t.sellerSku}</span>
                <input
                  className={`${styles.ctl} ${styles.ctlLtr}`}
                  value={props.sellerSku}
                  onChange={(event) => props.onSellerSkuChange(event.target.value)}
                  dir="ltr"
                  data-testid="offer-seller-sku"
                />
                <span className={styles.fieldHint}>{t.skuHint}</span>
              </label>
            </div>
          </div>
        </section>
        {props.extra ? <div>{props.extra}</div> : null}
      </div>
      <aside className={styles.stack}>
        <ReadinessCard
          t={t}
          format={format}
          sellerName={props.fixedSeller?.displayName || props.selectedSeller?.displayName}
          productName={selectedProduct?.title}
          variantLabel={selectedVariant ? (variantTitle(selectedVariant, t)) : undefined}
          recordCreated={false}
        />
        <HistoryHint t={t} />
      </aside>

      {props.searchSellers ? (
        <SellerDialog
          t={t}
          format={format}
          open={sellerOpen}
          onClose={() => setSellerOpen(false)}
          selected={props.selectedSeller}
          onSelect={(seller) => {
            props.onSelectSeller(seller);
            setSellerOpen(false);
          }}
          searchSellers={props.searchSellers}
        />
      ) : null}
      <ProductDialog
        t={t}
        format={format}
        open={productOpen}
        onClose={() => setProductOpen(false)}
        selected={selectedProduct}
        onSelect={(product) => {
          props.onSelectProduct(product);
          setProductOpen(false);
        }}
        searchProducts={props.searchProducts}
      />
      <VariantDialog
        t={t}
        open={variantOpen}
        onClose={() => setVariantOpen(false)}
        selected={selectedVariant}
        variants={variants}
        loading={variantLoading}
        error={variantError}
        onSelect={(variant) => {
          if (!selectedProduct || variant.productId !== selectedProduct.productId) return;
          props.onSelectVariant(variant);
          setVariantOpen(false);
        }}
        onRetry={() => selectedProduct && props.loadProductVariants(selectedProduct.productId).then(setVariants)}
      />
    </div>
  );
}

function ChoiceField({ value, onOpen, disabled, testId, detail }: { value: string; onOpen: () => void; disabled?: boolean; testId: string; detail?: string }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
      <button type="button" className={`${styles.ctl} ${styles.ctlReadonly}`} style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 8, textAlign: "start" }} onClick={onOpen} disabled={disabled} data-testid={testId}>
        <span style={{ minWidth: 0, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{value}</span>
        <Search size={14} aria-hidden />
      </button>
      {detail ? <span className={styles.fieldHint} style={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{detail}</span> : null}
    </div>
  );
}

function ReadBox({ label, value, hint }: { label: string; value: ReactNode; hint?: string }) {
  return (
    <div className={styles.field}>
      <span className={styles.fieldLabel}>{label}</span>
      <div className={`${styles.ctl} ${styles.ctlReadonly}`} style={{ display: "flex", alignItems: "center", minHeight: "var(--ow-control)" }}>{value}</div>
      {hint ? <span className={styles.fieldHint}>{hint}</span> : null}
    </div>
  );
}

function ProductSummary({ detail, t }: { detail: OfferWorkspaceDetail; t: OfferWorkspaceMessages }) {
  return (
    <div className={styles.productBox}>
      <Thumb mediaAssetId={detail.primaryMediaAssetId} alt={t.imageAlt} size={42} />
      <div className={styles.productBoxText}>
        <div className={styles.productBoxTitle}>{detail.productTitle}</div>
        <div className={styles.productBoxMeta}>
          {detail.brandName ? <span>{detail.brandName}</span> : null}
          {detail.brandName && detail.productUnitName ? <span className={styles.dotSep} /> : null}
          {detail.productUnitName ? <span>{detail.productUnitName}</span> : null}
        </div>
      </div>
    </div>
  );
}

function ReadinessCard({ t, format, sellerName, productName, variantLabel, recordCreated }: { t: OfferWorkspaceMessages; format: OfferFormatters; sellerName?: string | null; productName?: string | null; variantLabel?: string | null; recordCreated: boolean }) {
  const items = [
    { label: t.seller, value: sellerName || "—", ok: Boolean(sellerName) },
    { label: t.product, value: productName || "—", ok: Boolean(productName) },
    { label: t.variant, value: variantLabel || "—", ok: Boolean(variantLabel) },
  ];
  const ready = items.filter((item) => item.ok).length;
  return (
    <section className={styles.card} data-testid="offer-readiness">
      <header className={styles.cardHd}>
        <SectionTitle title={t.readiness} desc={`${format.integer(ready)} ${t.readinessOf}`} />
      </header>
      <div className={styles.cardBd}>
        <ol className={styles.readinessList}>
          {items.map((item) => (
            <li key={item.label} className={`${styles.readinessItem} ${item.ok ? styles.readyDone : ""}`}>
              <span className={styles.readyIc}>{item.ok ? <Check size={11} aria-hidden /> : "•"}</span>
              <span className={styles.readyTxt}>
                <span className={styles.readyT}>{item.label}</span>
                <span className={styles.readyV} title={item.value}>{item.value}</span>
              </span>
            </li>
          ))}
        </ol>
      </div>
      <div className={styles.cardFt}>
        {ready < 3 ? t.tabLocked : recordCreated ? t.readinessHint : t.tabNeedsCreate}
      </div>
    </section>
  );
}

function HistoryHint({ t }: { t: OfferWorkspaceMessages }) {
  return (
    <section className={styles.card}>
      <div className={styles.cardBd}>
        <div className={styles.secHd}><Lock size={14} aria-hidden /><span className={styles.secTitle}>{t.historyOnlyEdit}</span></div>
        <p className={styles.cardDesc}>{t.historyOnlyEditDesc}</p>
      </div>
    </section>
  );
}

function Thumb({ mediaAssetId, alt, size = 46 }: { mediaAssetId: string | null | undefined; alt: string; size?: number }) {
  return (
    <span className={styles.thumb} style={{ width: size, height: size, flexBasis: size }}>
      {mediaAssetId ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={`/v1/media/${mediaAssetId}`} alt={alt} />
      ) : (
        <ImageOff size={Math.max(14, Math.round(size / 3))} aria-hidden />
      )}
    </span>
  );
}

interface Pager<T> {
  rows: T[];
  page: number;
  totalCount: number;
  hasMore: boolean;
}

function PagedDialog<T>({
  title,
  testId,
  open,
  onClose,
  loadPage,
  selectedId,
  getId,
  renderRow,
  onSelect,
  t,
  format,
  emptyLabel,
  errorLabel,
}: {
  title: string;
  testId: string;
  open: boolean;
  onClose: () => void;
  loadPage: (search: string, page: number) => Promise<OfferPickerPage<T>>;
  selectedId?: string | null;
  getId: (row: T) => string;
  renderRow: (row: T) => ReactNode;
  onSelect: (row: T) => void;
  t: OfferWorkspaceMessages;
  format: OfferFormatters;
  emptyLabel: string;
  errorLabel: string;
}) {
  const [query, setQuery] = useState("");
  const [state, setState] = useState<Pager<T>>({ rows: [], page: 1, totalCount: 0, hasMore: false });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load(search: string, target: number) {
    setLoading(true);
    setError(null);
    try {
      const result = await loadPage(search, target);
      setState({ rows: result.rows, page: result.page, totalCount: result.totalCount, hasMore: result.hasMore });
    } catch {
      setError(errorLabel);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (!open) return;
    setQuery("");
    void load("", 1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  return (
    <Dialog title={title} open={open} onClose={onClose} size="lg">
      <div style={{ display: "grid", gap: 10 }} data-testid={testId}>
        <div style={{ display: "flex", gap: 8 }}>
          <input
            className={styles.ctl}
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
                void load(query, 1);
              }
            }}
            placeholder={t.search}
            data-testid={`${testId}-search`}
          />
          <button type="button" className={styles.btn} onClick={() => void load(query, 1)}>{t.search}</button>
        </div>
        <div className={styles.tableWrap} style={{ maxHeight: "22rem", overflowY: "auto" }}>
          {loading ? <p className={styles.resultMeta} style={{ padding: 12 }}>{t.loading}</p> : null}
          {error ? <p className={styles.fieldErr} style={{ padding: 12 }}>{error}</p> : null}
          {!loading && !error && state.rows.length === 0 ? <p className={styles.resultMeta} style={{ padding: 12 }}>{emptyLabel}</p> : null}
          {state.rows.map((row) => {
            const id = getId(row);
            const active = selectedId === id;
            return (
              <button
                key={id}
                type="button"
                aria-pressed={active}
                onClick={() => onSelect(row)}
                className={styles.btn}
                style={{ width: "100%", justifyContent: "space-between", borderRadius: 0, borderInline: 0, borderTop: 0, background: active ? "var(--ow-accent-soft)" : undefined, height: "auto", padding: "10px 12px" }}
                data-testid={`${testId}-option`}
              >
                <span style={{ minWidth: 0, textAlign: "start", flex: 1, display: "flex", flexDirection: "column", gap: 2 }}>{renderRow(row)}</span>
                {active ? <Check size={14} aria-hidden /> : null}
              </button>
            );
          })}
        </div>
        <div className={styles.row} style={{ justifyContent: "space-between" }}>
          <span className={styles.resultMeta} data-testid={`${testId}-page`}>
            {t.pageInfo} {format.integer(state.page)} · {format.integer(state.totalCount)}
          </span>
          <span style={{ display: "flex", gap: 6 }}>
            <button type="button" className={`${styles.btn} ${styles.btnSm}`} disabled={state.page <= 1 || loading} onClick={() => void load(query, state.page - 1)} data-testid={`${testId}-prev`}>
              <ChevronRight size={12} aria-hidden /> {t.prevPage}
            </button>
            <button type="button" className={`${styles.btn} ${styles.btnSm}`} disabled={!state.hasMore || loading} onClick={() => void load(query, state.page + 1)} data-testid={`${testId}-next`}>
              {t.nextPage} <ChevronLeft size={12} aria-hidden />
            </button>
          </span>
        </div>
      </div>
    </Dialog>
  );
}

function SellerDialog({ t, format, open, onClose, selected, onSelect, searchSellers }: {
  t: OfferWorkspaceMessages;
  format: OfferFormatters;
  open: boolean;
  onClose: () => void;
  selected: OfferSellerOption | null;
  onSelect: (seller: OfferSellerOption) => void;
  searchSellers: (search: string, page: number) => Promise<OfferPickerPage<OfferSellerOption>>;
}) {
  return (
    <PagedDialog<OfferSellerOption>
      title={t.selectSellerTitle}
      testId="offer-seller-dialog"
      open={open}
      onClose={onClose}
      loadPage={searchSellers}
      selectedId={selected?.sellerPartyId}
      getId={(row) => row.sellerPartyId}
      renderRow={(row) => <span style={{ fontWeight: 600 }}>{row.displayName || row.sellerPartyId}</span>}
      onSelect={onSelect}
      t={t}
      format={format}
      emptyLabel={t.noResults}
      errorLabel={t.pickerError}
    />
  );
}

function ProductDialog({ t, format, open, onClose, selected, onSelect, searchProducts }: {
  t: OfferWorkspaceMessages;
  format: OfferFormatters;
  open: boolean;
  onClose: () => void;
  selected: OfferProductOption | null;
  onSelect: (product: OfferProductOption) => void;
  searchProducts: (search: string, page: number) => Promise<OfferPickerPage<OfferProductOption>>;
}) {
  return (
    <PagedDialog<OfferProductOption>
      title={t.selectProductTitle}
      testId="offer-product-dialog"
      open={open}
      onClose={onClose}
      loadPage={searchProducts}
      selectedId={selected?.productId}
      getId={(row) => row.productId}
      renderRow={(row) => (
        <span style={{ display: "flex", alignItems: "center", gap: 9, minWidth: 0 }}>
          <Thumb mediaAssetId={row.primaryMediaAssetId} alt={t.imageAlt} size={34} />
          <span style={{ display: "flex", flexDirection: "column", minWidth: 0 }}>
            <span style={{ fontWeight: 600, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{row.title}</span>
            <span className={styles.fieldHint}>
              {row.brandName ? `${row.brandName} · ` : ""}{format.integer(row.publishedVariantCount)} {t.variantCount}
            </span>
          </span>
        </span>
      )}
      onSelect={onSelect}
      t={t}
      format={format}
      emptyLabel={t.productsEmpty}
      errorLabel={t.productPickerError}
    />
  );
}

function variantAxes(variant: OfferVariantOption) {
  return variant.axisLabels.map((axis) => `${axis.name}: ${axis.value}`).join(" · ");
}

function variantTitle(variant: OfferVariantOption, t: OfferWorkspaceMessages) {
  return variant.displayLabel.trim() || variantAxes(variant) || t.variantUnnamed;
}

function distinctVariantAxes(variant: OfferVariantOption) {
  const axes = variantAxes(variant);
  return variant.displayLabel.trim() && axes.trim() !== variant.displayLabel.trim() ? axes : "";
}

function VariantDialog({ t, open, onClose, selected, variants, loading, error, onSelect, onRetry }: {
  t: OfferWorkspaceMessages;
  open: boolean;
  onClose: () => void;
  selected: OfferVariantOption | null;
  variants: OfferVariantOption[];
  loading: boolean;
  error: string | null;
  onSelect: (variant: OfferVariantOption) => void;
  onRetry: () => void;
}) {
  return (
    <Dialog title={t.selectVariantTitle} open={open} onClose={onClose} size="lg">
      <div style={{ display: "grid", gap: 10 }} data-testid="offer-variant-dialog">
        <div className={styles.tableWrap} style={{ maxHeight: "22rem", overflowY: "auto" }}>
          {loading ? <p className={styles.resultMeta} style={{ padding: 12 }}>{t.loading}</p> : null}
          {error ? (
            <div style={{ padding: 12 }}>
              <p className={styles.fieldErr}>{error}</p>
              <button type="button" className={`${styles.btn} ${styles.btnSm}`} onClick={onRetry}>{t.retry}</button>
            </div>
          ) : null}
          {!loading && !error && variants.length === 0 ? <p className={styles.resultMeta} style={{ padding: 12 }}>{t.variantsEmpty}</p> : null}
          {variants.map((variant) => {
            const active = selected?.catalogVariantId === variant.catalogVariantId;
            return (
              <button
                key={variant.catalogVariantId}
                type="button"
                aria-pressed={active}
                onClick={() => onSelect(variant)}
                className={styles.btn}
                style={{ width: "100%", justifyContent: "space-between", borderRadius: 0, borderInline: 0, borderTop: 0, background: active ? "var(--ow-accent-soft)" : undefined, height: "auto", padding: "10px 12px", whiteSpace: "normal", overflowWrap: "anywhere" }}
                data-testid="offer-variant-option"
              >
                <span style={{ minWidth: 0, display: "flex", flexDirection: "column", gap: 2, textAlign: "start" }}>
                  <span style={{ fontWeight: 600 }}>{variantTitle(variant, t)}</span>
                  {distinctVariantAxes(variant) ? <span className={styles.fieldHint}>{distinctVariantAxes(variant)}</span> : null}
                  {variant.catalogCode ? <span className={styles.fieldHint} dir="ltr">{variant.catalogCode}</span> : null}
                </span>
                {active ? <Check size={14} aria-hidden /> : null}
              </button>
            );
          })}
        </div>
      </div>
    </Dialog>
  );
}
