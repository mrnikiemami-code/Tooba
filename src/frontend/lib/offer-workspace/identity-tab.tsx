"use client";

import { useEffect, useState, type ReactNode } from "react";
import { Check, ChevronLeft, ChevronRight, ImageOff, Lock, Search } from "lucide-react";
import type { OfferWorkspaceMessages } from "./messages";
import type {
  OfferMode,
  OfferRole,
  OfferSellerOption,
  OfferVariantOption,
  OfferWorkspaceDetail,
  OfferPickerPage,
} from "./types";
import styles from "./offer-workspace.module.css";
import { Note, ReadOnlyChip, SectionTitle } from "./tab-shared";

interface PickerState<T> {
  rows: T[];
  page: number;
  total: number;
  hasMore: boolean;
  loading: boolean;
  error: string | null;
}

function emptyPicker<T>(): PickerState<T> {
  return { rows: [], page: 1, total: 0, hasMore: false, loading: false, error: null };
}

export interface IdentityTabProps {
  t: OfferWorkspaceMessages;
  locale: "fa" | "en";
  mode: OfferMode;
  role: OfferRole;
  detail: OfferWorkspaceDetail | null;
  sellerSku: string;
  onSellerSkuChange: (value: string) => void;
  fixedSeller?: OfferSellerOption;
  selectedSeller: OfferSellerOption | null;
  selectedVariant: OfferVariantOption | null;
  onSelectSeller: (seller: OfferSellerOption) => void;
  onSelectVariant: (variant: OfferVariantOption) => void;
  searchSellers?: (search: string, page: number) => Promise<OfferPickerPage<OfferSellerOption>>;
  searchVariants: (search: string, page: number) => Promise<OfferPickerPage<OfferVariantOption>>;
  showErrors?: boolean;
  extra?: ReactNode;
}

export function IdentityTab(props: IdentityTabProps) {
  const { mode, detail } = props;
  if (mode === "edit" && detail) {
    return <EditIdentity {...props} detail={detail} />;
  }
  return <CreateIdentity {...props} />;
}

function EditIdentity(props: IdentityTabProps & { detail: OfferWorkspaceDetail }) {
  const { t, detail } = props;
  const channel = detail.channel === "Marketplace" ? t.channelMarketplace : detail.channel;
  return (
    <div className={styles.split} data-testid="offer-tab-identity">
      <div className={styles.stack}>
        <section className={styles.card}>
          <header className={styles.cardHd}>
            <SectionTitle title={t.identityTitle} desc={t.identityDesc} />
            <div className={styles.cardHdActions}>
              <ReadOnlyChip>
                <Lock size={11} aria-hidden /> {t.sellerLocked}
              </ReadOnlyChip>
            </div>
          </header>
          <div className={styles.cardBd}>
            <Note tone="info">{t.identityLocked}</Note>
            <div className={`${styles.formGrid} mt-4`} style={{ marginTop: 14 }}>
              <ReadBox label={t.seller} value={detail.sellerDisplayName || detail.sellerPartyId} />
              <ReadBox label={t.channel} value={channel} hint={t.channelHint} />
              <div className={`${styles.field} ${styles.span2}`}>
                <span className={styles.fieldLabel}>{t.product}</span>
                <ProductSummary detail={detail} t={t} />
              </div>
              <ReadBox
                label={t.variantCodeLabel}
                value={
                  <span className={styles.sku} dir="ltr" style={{ whiteSpace: "normal", overflowWrap: "anywhere" }}>
                    {detail.variantCode || t.skuNone} <ReadOnlyChip><Lock size={10} aria-hidden /> {t.variantLocked}</ReadOnlyChip>
                  </span>
                }
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
        <ReadinessCard t={t} sellerName={detail.sellerDisplayName || detail.sellerPartyId} productName={detail.productTitle} variantCode={detail.variantCode} />
        <HistoryHint t={t} />
      </aside>
    </div>
  );
}

function ReadBox({ label, value, hint }: { label: string; value: ReactNode; hint?: string }) {
  return (
    <div className={styles.field}>
      <span className={styles.fieldLabel}>{label}</span>
      <div className={`${styles.ctl} ${styles.ctlReadonly}`} style={{ display: "flex", alignItems: "center", minHeight: "var(--ow-control)" }}>
        {value}
      </div>
      {hint ? <span className={styles.fieldHint}>{hint}</span> : null}
    </div>
  );
}

function CreateIdentity(props: IdentityTabProps) {
  const { t, showErrors } = props;
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
                <span className={styles.fieldLabel}>
                  {t.seller} <span className={styles.req}>*</span>
                </span>
                {props.fixedSeller ? (
                  <div className={styles.ctlReadonly} data-testid="offer-fixed-seller" style={{ display: "flex", alignItems: "center", minHeight: "var(--ow-control)", borderRadius: 6, border: "1px solid var(--ow-border)", padding: "0 10px" }}>
                    {props.fixedSeller.displayName || t.sellerSession}
                  </div>
                ) : (
                  <SellerPicker {...props} />
                )}
                <span className={styles.fieldHint}>{props.fixedSeller ? t.sellerSessionHint : t.sellerHint}</span>
                {showErrors && !props.fixedSeller && !props.selectedSeller ? (
                  <span className={styles.fieldErr}>{t.sellerRequired}</span>
                ) : null}
              </div>

              <div className={styles.field}>
                <span className={styles.fieldLabel}>{t.channel}</span>
                <select className={`${styles.ctl} ${styles.select}`} value="Marketplace" disabled aria-label={t.channel}>
                  <option value="Marketplace">{t.channelMarketplace}</option>
                </select>
                <span className={styles.fieldHint}>{t.channelHint}</span>
              </div>

              <div className={`${styles.field} ${styles.span2}`}>
                <span className={styles.fieldLabel}>
                  {t.product} <span className={styles.req}>*</span>
                </span>
                <VariantPicker {...props} />
                <span className={styles.fieldHint}>{t.productPickerHint}</span>
                {showErrors && !props.selectedVariant ? <span className={styles.fieldErr}>{t.productRequired}</span> : null}
              </div>

              <div className={styles.field}>
                <span className={styles.fieldLabel}>{t.variant}</span>
                <div className={styles.ctlReadonly} style={{ display: "flex", alignItems: "center", minHeight: "var(--ow-control)", borderRadius: 6, border: "1px solid var(--ow-border)", padding: "0 10px" }}>
                  {props.selectedVariant ? (
                    <span className={styles.metaV}>{props.selectedVariant.catalogCode || props.selectedVariant.catalogVariantId}</span>
                  ) : (
                    <span className={styles.fieldHint}>{t.select}</span>
                  )}
                </div>
                <span className={styles.fieldHint}>{t.variantHint}</span>
                {showErrors && !props.selectedVariant ? <span className={styles.fieldErr}>{t.variantRequired}</span> : null}
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
        <ReadinessCard t={t} sellerName={props.fixedSeller?.displayName || props.selectedSeller?.displayName} productName={props.selectedVariant?.productTitle} variantCode={props.selectedVariant?.catalogCode} />
        <HistoryHint t={t} />
      </aside>
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
function ReadinessCard({
  t,
  sellerName,
  productName,
  variantCode,
}: {
  t: OfferWorkspaceMessages;
  sellerName?: string | null;
  productName?: string | null;
  variantCode?: string | null;
}) {
  const items = [
    { label: t.seller, value: sellerName || "—", ok: Boolean(sellerName) },
    { label: t.product, value: productName || "—", ok: Boolean(productName) },
    { label: t.variant, value: variantCode ? t.variantSelected : "—", ok: Boolean(variantCode) },
  ];
  const ready = items.filter((item) => item.ok).length;
  return (
    <section className={styles.card} data-testid="offer-readiness">
      <header className={styles.cardHd}>
        <SectionTitle title={t.readiness} desc={`${ready} ${t.readinessOf}`} />
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
      <div className={styles.cardFt}>{t.readinessHint}</div>
    </section>
  );
}

function HistoryHint({ t }: { t: OfferWorkspaceMessages }) {
  return (
    <section className={styles.card}>
      <div className={styles.cardBd}>
        <div className={styles.secHd}>
          <Lock size={14} aria-hidden />
          <span className={styles.secTitle}>{t.historyOnlyEdit}</span>
        </div>
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

function SellerPicker(props: IdentityTabProps) {
  const { t } = props;
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [state, setState] = useState<PickerState<OfferSellerOption>>(emptyPicker);

  async function load(search: string, page: number) {
    if (!props.searchSellers) return;
    setState((current) => ({ ...current, loading: true, error: null }));
    try {
      const result = await props.searchSellers(search, page);
      setState({ rows: result.rows, page: result.page, total: result.totalCount, hasMore: result.hasMore, loading: false, error: null });
    } catch {
      setState((current) => ({ ...current, loading: false, error: t.pickerError }));
    }
  }

  useEffect(() => {
    if (!open || !props.searchSellers) return;
    void load("", 1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  return (
    <div>
      <div style={{ display: "flex", gap: 8 }}>
        <div className={styles.ctlReadonly} style={{ flex: 1, display: "flex", alignItems: "center", minHeight: "var(--ow-control)", borderRadius: 6, border: "1px solid var(--ow-border)", padding: "0 10px" }}>
          {props.selectedSeller ? <span className={styles.metaV}>{props.selectedSeller.displayName}</span> : <span className={styles.fieldHint}>{t.select}</span>}
        </div>
        <button type="button" className={styles.btn} onClick={() => setOpen((value) => !value)} data-testid="offer-seller-picker-toggle">
          <Search size={13} aria-hidden /> {t.search}
        </button>
      </div>
      {open ? (
        <div className={styles.card} style={{ marginTop: 8 }} data-testid="offer-seller-picker">
          <div className={styles.cardBd}>
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
                data-testid="offer-seller-search"
              />
              <button type="button" className={styles.btn} onClick={() => void load(query, 1)}>{t.search}</button>
            </div>
            <div className={styles.tableWrap} style={{ marginTop: 8 }}>
              {state.loading ? <p className={styles.resultMeta} style={{ padding: 10 }}>{t.loading}</p> : null}
              {state.error ? <p className={styles.fieldErr} style={{ padding: 10 }}>{state.error}</p> : null}
              {!state.loading && !state.error && state.rows.length === 0 ? <p className={styles.resultMeta} style={{ padding: 10 }}>{t.noResults}</p> : null}
              {state.rows.map((seller) => {
                const active = props.selectedSeller?.sellerPartyId === seller.sellerPartyId;
                return (
                  <button
                    key={seller.sellerPartyId}
                    type="button"
                    aria-pressed={active}
                    onClick={() => {
                      props.onSelectSeller(seller);
                      setOpen(false);
                    }}
                    className={styles.btn}
                    style={{ width: "100%", justifyContent: "space-between", borderRadius: 0, borderInline: 0, borderTop: 0, background: active ? "var(--ow-accent-soft)" : undefined }}
                    data-testid="offer-seller-option"
                  >
                    <span>{seller.displayName || seller.sellerPartyId}</span>
                    {active ? <Check size={13} aria-hidden /> : null}
                  </button>
                );
              })}
            </div>
            <Pager t={t} state={state} onPage={(page) => void load(query, page)} />
          </div>
        </div>
      ) : null}
    </div>
  );
}

function VariantPicker(props: IdentityTabProps) {
  const { t } = props;
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [state, setState] = useState<PickerState<OfferVariantOption>>(emptyPicker);

  async function load(search: string, page: number) {
    setState((current) => ({ ...current, loading: true, error: null }));
    try {
      const result = await props.searchVariants(search, page);
      setState({ rows: result.rows, page: result.page, total: result.totalCount, hasMore: result.hasMore, loading: false, error: null });
    } catch {
      setState((current) => ({ ...current, loading: false, error: t.pickerError }));
    }
  }

  useEffect(() => {
    if (!open) return;
    void load("", 1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  return (
    <div data-testid="offer-variant-picker">
      <div style={{ display: "flex", gap: 8 }}>
        <div className={styles.ctlReadonly} style={{ flex: 1, display: "flex", alignItems: "center", minHeight: "var(--ow-control)", borderRadius: 6, border: "1px solid var(--ow-border)", padding: "0 10px", gap: 9 }}>
          {props.selectedVariant ? (
            <>
              <Thumb mediaAssetId={props.selectedVariant.primaryMediaAssetId} alt={t.imageAlt} size={28} />
              <span style={{ minWidth: 0 }}>
                <span style={{ display: "block", fontSize: 12.5, fontWeight: 600 }}>{props.selectedVariant.productTitle}</span>
                <span className={styles.fieldHint} dir="ltr">{props.selectedVariant.catalogCode || props.selectedVariant.catalogVariantId}</span>
              </span>
            </>
          ) : (
            <span className={styles.fieldHint}>{t.select}</span>
          )}
        </div>
        <button type="button" className={styles.btn} onClick={() => setOpen((value) => !value)} data-testid="offer-variant-picker-toggle">
          <Search size={13} aria-hidden /> {t.search}
        </button>
      </div>
      {open ? (
        <div className={styles.card} style={{ marginTop: 8 }}>
          <div className={styles.cardBd}>
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
                data-testid="offer-variant-search"
              />
              <button type="button" className={styles.btn} onClick={() => void load(query, 1)}>{t.search}</button>
            </div>
            <div className={styles.tableWrap} style={{ marginTop: 8 }}>
              {state.loading ? <p className={styles.resultMeta} style={{ padding: 10 }}>{t.loading}</p> : null}
              {state.error ? <p className={styles.fieldErr} style={{ padding: 10 }}>{state.error}</p> : null}
              {!state.loading && !state.error && state.rows.length === 0 ? <p className={styles.resultMeta} style={{ padding: 10 }}>{t.noResults}</p> : null}
              {state.rows.map((variant) => {
                const active = props.selectedVariant?.catalogVariantId === variant.catalogVariantId;
                return (
                  <button
                    key={variant.catalogVariantId}
                    type="button"
                    aria-pressed={active}
                    onClick={() => {
                      props.onSelectVariant(variant);
                      setOpen(false);
                    }}
                    className={styles.btn}
                    style={{ width: "100%", justifyContent: "flex-start", borderRadius: 0, borderInline: 0, borderTop: 0, background: active ? "var(--ow-accent-soft)" : undefined, height: "auto", padding: "8px 10px" }}
                    data-testid="offer-variant-option"
                  >
                    <span style={{ display: "flex", flexDirection: "column", alignItems: "flex-start", minWidth: 0 }}>
                      <span style={{ fontSize: 12.5, fontWeight: 600 }}>{variant.productTitle}</span>
                      <span className={styles.fieldHint} dir="ltr">{variant.catalogCode ?? variant.catalogVariantId} · {variant.productStatus}</span>
                    </span>
                  </button>
                );
              })}
            </div>
            <Pager t={t} state={state} onPage={(page) => void load(query, page)} />
          </div>
        </div>
      ) : null}
    </div>
  );
}

function Pager<T>({ t, state, onPage }: { t: OfferWorkspaceMessages; state: PickerState<T>; onPage: (page: number) => void }) {
  return (
    <div className={styles.row} style={{ justifyContent: "space-between", marginTop: 8 }}>
      <span className={styles.resultMeta} data-testid="offer-picker-page">
        {t.pageInfo} {state.page} · {state.total.toLocaleString("fa-IR")}
      </span>
      <span style={{ display: "flex", gap: 6 }}>
        <button type="button" className={`${styles.btn} ${styles.btnSm}`} disabled={state.page <= 1 || state.loading} onClick={() => onPage(state.page - 1)} data-testid="offer-picker-prev">
          <ChevronRight size={12} aria-hidden /> {t.prevPage}
        </button>
        <button type="button" className={`${styles.btn} ${styles.btnSm}`} disabled={!state.hasMore || state.loading} onClick={() => onPage(state.page + 1)} data-testid="offer-picker-next">
          {t.nextPage} <ChevronLeft size={12} aria-hidden />
        </button>
      </span>
    </div>
  );
}
