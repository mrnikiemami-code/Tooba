"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { AlertTriangle, Check, ImageOff, Lock } from "lucide-react";
import { toast } from "react-toastify";
import { MessageBox } from "../../design-system";
import { useLocale } from "../i18n/locale-context";
import {
  allowedStatuses,
  applySavedInventoryDraft,
  changedInventoryRows,
  commercialDirty,
  deriveBuyability,
  detailVersionChanged,
  draftFromDetail,
  inventoryDirty,
  offeredQuantityDirty,
  parseDecimal,
  priceDirty,
  statusLabel,
  validateDraft,
  type OfferWorkspaceDraft,
} from "./model";
import { offerErrorText, offerWorkspaceMessages, type OfferWorkspaceMessages } from "./messages";
import { createOfferFormatters, offerLocaleFromAppLocale, type OfferFormatters, type OfferLocaleDefinition } from "./i18n";
import { useOfferHistory } from "./use-offer-history.ts";
import { createOfferActions } from "./offer-actions.ts";
import { buildLocalDetail } from "./local-detail.ts";
import { ConditionsTab } from "./conditions-tab";
import { DiscountsTab, HistoryTab, NeedsCreateTab } from "./gated-tabs";
import { IdentityTab } from "./identity-tab";
import { InventoryTab } from "./inventory-tab";
import { PricingTab } from "./pricing-tab";
import { ShippingTab } from "./shipping-tab";
import { DirtyDot, StatusPill } from "./tab-shared";
import styles from "./offer-workspace.module.css";
import type {
  OfferCommercialPatch,
  OfferFulfillmentResponsibility,
  OfferHistoryPage,
  OfferInventoryLocationOption,
  OfferLocalInventoryDraft,
  OfferProductOption,
  OfferSellerOption,
  OfferShippingTypeOption,
  OfferTabId,
  OfferVariantOption,
  OfferWorkspaceAdapter,
  OfferWorkspaceDetail,
} from "./types";
const TAB_ORDER: OfferTabId[] = ["identity", "pricing", "discounts", "inventory", "shipping", "conditions", "history"];
export interface OfferWorkspaceProps {
  adapter: OfferWorkspaceAdapter;
  renderExtras?: (tab: OfferTabId, detail: OfferWorkspaceDetail) => ReactNode;
  localeDefinition?: OfferLocaleDefinition;
  messages?: Partial<OfferWorkspaceMessages>;
}
type PersistedOfferWorkspace = {
  draft?: Partial<OfferWorkspaceDraft>;
  selectedSeller?: OfferSellerOption | null;
  selectedProduct?: OfferProductOption | null;
  selectedVariant?: OfferVariantOption | null;
  responsibility?: OfferFulfillmentResponsibility | null;
  shippingTypeId?: string;
  selectedChannel?: string;
  localInventory?: OfferLocalInventoryDraft[];
  baseLastUpdatedAt?: string | null;
};
export function OfferWorkspace({ adapter, renderExtras, localeDefinition, messages }: OfferWorkspaceProps) {
  const appLocale = useLocale();
  const offerLocale = localeDefinition ?? offerLocaleFromAppLocale(appLocale);
  const format: OfferFormatters = useMemo(() => createOfferFormatters(offerLocale), [offerLocale]);
  const baseMessages = useMemo(
    () => offerWorkspaceMessages[offerLocale.language as "fa" | "en"] ?? offerWorkspaceMessages.en,
    [offerLocale.language],
  );
  const t: OfferWorkspaceMessages = useMemo(
    () => (messages ? { ...baseMessages, ...messages, tabs: { ...baseMessages.tabs, ...(messages.tabs ?? {}) } } : baseMessages),
    [baseMessages, messages],
  );
  const isEdit = adapter.mode === "edit";
  const [detail, setDetail] = useState<OfferWorkspaceDetail | null>(null);
  const [loading, setLoading] = useState(isEdit);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [draft, setDraft] = useState<OfferWorkspaceDraft>(emptyDraft());
  const [activeTab, setActiveTab] = useState<OfferTabId>("identity");
  const [selectedSeller, setSelectedSeller] = useState<OfferSellerOption | null>(null);
  const [selectedProduct, setSelectedProduct] = useState<OfferProductOption | null>(null);
  const [selectedChannel, setSelectedChannel] = useState("");
  const [channels, setChannels] = useState<string[]>([]);
  const [inventoryLocations, setInventoryLocations] = useState<OfferInventoryLocationOption[]>([]);
  const [localInventory, setLocalInventory] = useState<OfferLocalInventoryDraft[]>([]);
  const [selectedVariant, setSelectedVariant] = useState<OfferVariantOption | null>(null);
  const history = useOfferHistory(adapter, isEdit, activeTab, detail);
  const [activeConflict, setActiveConflict] = useState<{ offerId: string } | null>(null);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveNotice, setSaveNotice] = useState<string | null>(null);
  const [messageBox, setMessageBox] = useState<{ kind: "duplicate" | "reload" | "error" | "confirm"; title: string; text: string; offerId?: string } | null>(null);
  useEffect(() => {
    if (!saveNotice) return;
    const options = { autoClose: 3000 };
    if (saveNotice === t.partialSaved) toast.warning(saveNotice, options);
    else if (saveNotice === t.noChanges || saveNotice === t.reloaded || saveNotice === t.draftRestored) toast.info(saveNotice, options);
    else toast.success(saveNotice, options);
    setSaveNotice(null);
  }, [saveNotice, t.draftRestored, t.noChanges, t.partialSaved, t.reloaded]);
  function showBlocking(kind: "duplicate" | "reload" | "error" | "confirm", title: string, text: string, offerId?: string) {
    setMessageBox({ kind, title, text, offerId });
  }
  function handleCancel() {
    if (anyDirty) {
      showBlocking("confirm", t.dirtyExitTitle, t.dirtyExitText);
      return;
    }
    clearPersistedDraft();
    adapter.onCancel();
  }
  const [saveNeedsReload, setSaveNeedsReload] = useState(false);
  const persistKey = useMemo(() => {
    const actor = typeof window === "undefined"
      ? "ssr"
      : adapter.role === "admin"
        ? window.localStorage.getItem("tooba.adminActorUserId") ?? "anonymous"
        : window.localStorage.getItem("tooba.sellerActorUserId") ?? "anonymous";
    const tenant = typeof window === "undefined" ? "ssr" : window.localStorage.getItem("tooba.tenantId") ?? "default";
    const owner = detail?.sellerPartyId ?? selectedSeller?.sellerPartyId ?? adapter.fixedSeller?.sellerPartyId ?? "owner";
    const variant = detail?.catalogVariantId ?? selectedVariant?.catalogVariantId ?? "variant";
    return `tooba:offer-workspace:${tenant}:${actor}:${adapter.role}:${adapter.mode}:${owner}:${variant}:${detail?.channel ?? selectedChannel}:${adapter.offerId ?? "new"}`;
  }, [adapter.role, adapter.mode, adapter.offerId, adapter.fixedSeller?.sellerPartyId, detail?.sellerPartyId, detail?.catalogVariantId, selectedSeller?.sellerPartyId, selectedVariant?.catalogVariantId, selectedChannel]);
  const restoredRef = useRef(false);
  const [saveRetryable, setSaveRetryable] = useState(true);
  const [saveReloading, setSaveReloading] = useState(false);
  const [tabError, setTabError] = useState<{ tab: OfferTabId; code: string } | null>(null);
  const [showCreateErrors, setShowCreateErrors] = useState(false);
  const [shippingTypes, setShippingTypes] = useState<OfferShippingTypeOption[]>([]);
  const [typesError, setTypesError] = useState<string | null>(null);
  const [responsibility, setResponsibility] = useState<OfferFulfillmentResponsibility | null>(null);
  const [shippingTypeId, setShippingTypeId] = useState("");
  const [shippingNotice, setShippingNotice] = useState<string | null>(null);
  const [shippingError, setShippingError] = useState<string | null>(null);
  const [shippingSaving, setShippingSaving] = useState(false);
  const saveRef = useRef(false);
  const tabListRef = useRef<HTMLDivElement | null>(null);
  const tabLabel = useMemo<Record<OfferTabId, string>>(
    () => ({
      identity: t.tabs.identity,
      pricing: t.tabs.pricing,
      discounts: t.tabs.discounts,
      inventory: t.tabs.inventory,
      shipping: t.tabs.shipping,
      conditions: t.tabs.conditions,
      history: t.tabs.history,
    }),
    [t],
  );
  const applyLoadedDetail = useCallback((next: OfferWorkspaceDetail) => {
    setDetail(next);
    setDraft(draftFromDetail(next));
    setResponsibility(next.fulfillmentResponsibility);
    setShippingTypeId(next.fulfillmentShippingTypeId ?? "");
    setShippingNotice(null);
    setShippingError(null);
  }, []);
  const load = useCallback(async () => {
    if (!isEdit || !adapter.loadDetail) return;
    setLoading(true);
    setLoadError(null);
    setDenied(false);
    const result = await adapter.loadDetail();
    setLoading(false);
    if (!result.ok) {
      if (result.denied) setDenied(true);
      setLoadError(result.errorCode);
      return;
    }
    applyLoadedDetail(result.detail);
  }, [adapter, isEdit, applyLoadedDetail]);
  const identityReady = adapter.role === "seller"
    ? Boolean(adapter.fixedSeller) && Boolean(selectedProduct) && Boolean(selectedVariant) && Boolean(selectedChannel)
    : Boolean(selectedSeller) && Boolean(selectedProduct) && Boolean(selectedVariant) && Boolean(selectedChannel);
  const localDetail = useMemo(
    () => detail ?? (
      identityReady && selectedProduct && selectedVariant
        ? buildLocalDetail(adapter, selectedSeller, selectedProduct, selectedVariant, draft.sellerSku, selectedChannel)
        : null
    ),
    [adapter, detail, draft.sellerSku, identityReady, selectedProduct, selectedSeller, selectedVariant, selectedChannel],
  );
  const dirtyDetail = detail ?? localDetail;
  const commercialChanged = dirtyDetail ? commercialDirty(dirtyDetail, draft) : false;
  const priceChanged = dirtyDetail ? priceDirty(dirtyDetail, draft) : false;
  const stockChanged = detail ? inventoryDirty(detail, draft) : false;
  const offeredChanged = detail ? offeredQuantityDirty(detail, draft) : false;
  const shippingChanged = Boolean(
    dirtyDetail &&
      adapter.saveFulfillment &&
      (responsibility !== dirtyDetail.fulfillmentResponsibility ||
        (shippingTypeId || null) !== (dirtyDetail.fulfillmentShippingTypeId ?? null)),
  );
  const createReady = identityReady;
  const preCreateTabs = adapter.role === "admin"
    ? new Set<OfferTabId>(["pricing", "conditions", "shipping", ...(inventoryLocations.length ? ["inventory" as OfferTabId] : [])])
    : new Set<OfferTabId>(["pricing", "conditions", ...(inventoryLocations.length ? ["inventory" as OfferTabId] : [])]);
  const createDirty = !isEdit && (
    Boolean(selectedSeller)
    || Boolean(selectedProduct)
    || Boolean(selectedVariant)
    || draft.sellerSku.trim().length > 0
    || localInventory.some((row) => row.onHand.trim().length > 0)
    || (localDetail ? priceDirty(localDetail, draft) || commercialDirty(localDetail, draft) || (adapter.role === "admin" && (responsibility !== localDetail.fulfillmentResponsibility || shippingTypeId !== "")) : false)
  );
  const anyDirty = isEdit
    ? commercialChanged || priceChanged || stockChanged || offeredChanged || shippingChanged
    : createDirty;
  useEffect(() => {
    if (!anyDirty) return;
    const handler = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", handler);
    return () => window.removeEventListener("beforeunload", handler);
  }, [anyDirty]);
  useEffect(() => {
    const variantId = selectedVariant?.catalogVariantId;
    if (isEdit || !adapter.findActiveOffer || !variantId) {
      setActiveConflict(null);
      return;
    }
    const sellerId = adapter.role === "seller"
      ? adapter.fixedSeller?.sellerPartyId
      : selectedSeller?.sellerPartyId;
    if (!sellerId) {
      setActiveConflict(null);
      return;
    }
    let active = true;
    void adapter.findActiveOffer(sellerId, variantId, selectedChannel)
      .then((result) => {
        if (!active) return;
        setActiveConflict(result);
        })
      .catch(() => {
        if (!active) return;
        setActiveConflict(null);
        });
    return () => {
      active = false;
    };
  }, [adapter, isEdit, selectedChannel, selectedSeller?.sellerPartyId, selectedVariant?.catalogVariantId]);
  useEffect(() => {
    if (!isEdit && activeConflict) {
      showBlocking("duplicate", t.duplicateActiveTitle, t.duplicateActive);
    }
  }, [activeConflict, isEdit, t.duplicateActive, t.duplicateActiveTitle]);
  useEffect(() => {
    void load();
  }, [load]);
  useEffect(() => {
    if (isEdit || !adapter.loadChannels) return;
    let active = true;
    void adapter.loadChannels().then((rows) => {
      if (!active) return;
      setChannels(rows);
      setSelectedChannel((current) => current || rows[0] || "");
    });
    return () => { active = false; };
  }, [adapter, isEdit]);
  useEffect(() => {
    if (isEdit || !adapter.loadInventoryLocations) return;
    let active = true;
    setInventoryLocations([]);
    void adapter.loadInventoryLocations(selectedSeller?.sellerPartyId ?? adapter.fixedSeller?.sellerPartyId, responsibility).then((rows) => {
      if (!active) return;
      setInventoryLocations(rows);
      setLocalInventory((current) => rows.map((row) => current.find((item) => item.locationId === row.locationId) ?? { locationId: row.locationId, onHand: "", reason: "" }));
    });
    return () => { active = false; };
  }, [adapter, isEdit, selectedSeller?.sellerPartyId, responsibility]);
  useEffect(() => {
    if (restoredRef.current) return;
    if (isEdit && (!detail || loading)) return;
    let stored: PersistedOfferWorkspace | null = null;
    try {
      const raw = window.sessionStorage.getItem(persistKey);
      stored = raw ? JSON.parse(raw) as PersistedOfferWorkspace : null;
    } catch {
      stored = null;
    }
    if (stored && isEdit && detail?.lastUpdatedAt && stored.baseLastUpdatedAt && stored.baseLastUpdatedAt !== detail.lastUpdatedAt) {
      try { window.sessionStorage.removeItem(persistKey); } catch { /* ignore */ }
      stored = null;
      setSaveNotice(t.reloaded);
    }
    if (stored) {
      const base = detail ? draftFromDetail(detail) : emptyDraft();
      setDraft({ ...base, ...stored.draft });
      if (stored.selectedSeller) setSelectedSeller(stored.selectedSeller);
      if (stored.selectedProduct) setSelectedProduct(stored.selectedProduct);
      if (stored.selectedVariant) setSelectedVariant(stored.selectedVariant);
      if (stored.responsibility !== undefined) setResponsibility(stored.responsibility);
      if (stored.shippingTypeId !== undefined) setShippingTypeId(stored.shippingTypeId);
      if (stored.selectedChannel !== undefined) setSelectedChannel(stored.selectedChannel);
      if (stored.localInventory !== undefined) setLocalInventory(stored.localInventory);
      setSaveNotice(t.draftRestored);
    }
    restoredRef.current = true;
  }, [detail, isEdit, loading, persistKey, t.draftRestored, t.reloaded]);
  useEffect(() => {
    if (!restoredRef.current) return;
    if (isEdit && !anyDirty) return;
    try {
      window.sessionStorage.setItem(persistKey, JSON.stringify({
        draft,
        selectedSeller,
        selectedProduct,
        selectedVariant,
        responsibility,
        shippingTypeId,
        selectedChannel,
        localInventory,
        baseLastUpdatedAt: detail?.lastUpdatedAt ?? null,
      }));
    } catch {
      // Session storage is an optional recovery aid, never a domain write.
    }
  }, [anyDirty, detail?.lastUpdatedAt, draft, isEdit, localInventory, persistKey, responsibility, selectedChannel, selectedProduct, selectedSeller, selectedVariant, shippingTypeId]);
  function clearPersistedDraft() {
    try {
      window.sessionStorage.removeItem(persistKey);
    } catch {
      // ignore
    }
  }
  useEffect(() => {
    if (!adapter.loadShippingTypes) return;
    void adapter
      .loadShippingTypes()
      .then((rows) => {
        setShippingTypes(rows);
        setTypesError(null);
      })
      .catch(() => setTypesError(t.pickerError));
  }, [adapter, t.pickerError]);
  useEffect(() => {
    const tab = new URLSearchParams(window.location.search).get("tab");
    if (tab && (TAB_ORDER as string[]).includes(tab)) setActiveTab(tab as OfferTabId);
  }, []);
  useEffect(() => {
    const bar = tabListRef.current;
    const el = document.getElementById(`offer-tab-${activeTab}`);
    if (!bar || !el) return;
    const barRect = bar.getBoundingClientRect();
    const tabRect = el.getBoundingClientRect();
    if (tabRect.left < barRect.left + 8) bar.scrollLeft -= barRect.left + 8 - tabRect.left;
    else if (tabRect.right > barRect.right - 8) bar.scrollLeft += tabRect.right - (barRect.right - 8);
  }, [activeTab]);
  const tabDirty: Record<OfferTabId, boolean> = {
    identity: commercialChanged,
    pricing: priceChanged,
    discounts: false,
    inventory: stockChanged || offeredChanged,
    shipping: shippingChanged,
    conditions: commercialChanged,
    history: false,
  };
  function patchDraft(patch: Partial<OfferWorkspaceDraft>) {
    setDraft((current) => ({ ...current, ...patch }));
    setSaveNotice(null);
    setTabError(null);
    setSaveError(null);
  }
  const actions = createOfferActions({
    adapter, t, isEdit, detail, draft, selectedSeller, selectedProduct, selectedVariant, selectedChannel, localInventory,
    createReady, identityReady, activeConflict, saving, saveRef, saveNeedsReload, saveReloading,
    priceChanged, commercialChanged, stockChanged, offeredChanged, shippingChanged, responsibility, shippingTypeId,
    setDraft, setDetail, setSaving, setSaveError, setSaveNotice, setSaveNeedsReload, setSaveRetryable,
    setSaveReloading, setTabError, setActiveTab, setActiveConflict, setShippingSaving, setShippingError,
    setShippingNotice, setShowCreateErrors, showBlocking, applyLoadedDetail,
  });
  if (denied) {
    return (
      <div className={styles.root}>
        <div className={styles.page}>
          <div className={styles.card}>
            <div className={styles.empty}>
              <span className={styles.emptyIc}><AlertTriangle size={18} aria-hidden /></span>
              <span className={styles.emptyT}>{t.denied}</span>
              <span className={styles.emptyD}>{t.deniedDetail}</span>
              <button type="button" className={styles.btn} onClick={() => void load()}>{t.retry}</button>
            </div>
          </div>
        </div>
      </div>
    );
  }
  if (loading) {
    return (
      <div className={styles.root}>
        <div className={styles.page}>
          <div className={styles.card} aria-busy="true">
            <div className={styles.cardBd}>
              <div className={styles.row} style={{ gap: 12 }}>
                <span className={styles.thumb} />
                <div style={{ flex: 1 }}>
                  <div className={styles.emptyD}>{t.loading}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }
  if (loadError) {
    return (
      <div className={styles.root}>
        <div className={styles.page}>
          <div className={styles.card}>
            <div className={styles.empty}>
              <span className={styles.emptyIc}><AlertTriangle size={18} aria-hidden /></span>
              <span className={styles.emptyT}>{t.loadFailed}</span>
              <span className={styles.emptyD}>{offerErrorText(t, loadError)}</span>
              <button type="button" className={styles.btn} onClick={() => void load()}>{t.retry}</button>
            </div>
          </div>
        </div>
      </div>
    );
  }
  const buyability = detail ? deriveBuyability(detail) : null;
  const title = detail?.productTitle || selectedProduct?.title || t.titleCreate;
  const sellerName = detail?.sellerDisplayName || selectedSeller?.displayName || adapter.fixedSeller?.displayName || (adapter.role === "seller" ? t.sellerSession : t.notSelected);
  const mediaId = detail?.primaryMediaAssetId ?? selectedProduct?.primaryMediaAssetId ?? selectedVariant?.primaryMediaAssetId ?? null;
  const currentStatus = detail ? draft.status : "Draft";
  const existingOfferHref = activeConflict ? (adapter.role === "admin" ? `/admin/offers/${activeConflict.offerId}` : `/vendor-panel/products/${activeConflict.offerId}`) : messageBox?.offerId ? (adapter.role === "admin" ? `/admin/offers/${messageBox.offerId}` : `/vendor-panel/products/${messageBox.offerId}`) : null;
  const saveLabel = isEdit ? t.save : t.saveDraft;
  const saveTestId = isEdit
    ? adapter.role === "admin" ? "admin-offer-save" : "offer-save"
    : adapter.role === "admin" ? "admin-offer-submit" : "seller-create-submit";
  return (
    <div className={styles.root} data-testid="offer-workspace" dir={offerLocale.direction} lang={offerLocale.code}>
      <header className={styles.stickyHead} data-testid="offer-workspace-header">
        <div className={styles.headInner}>
          <div className={styles.headGrid}>
            <div className={styles.headId}>
              <span className={styles.thumb}>
                {mediaId ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={`/v1/media/${mediaId}`} alt={t.imageAlt} />
                ) : (
                  <ImageOff size={16} aria-hidden />
                )}
              </span>
              <div className={styles.headTxt}>
                <div className={styles.eyebrow}>
                  <span>{t.breadcrumb}</span>
                  <span className={styles.dotSep} />
                  <strong>{isEdit ? t.titleEdit : t.titleCreate}</strong>
                  {detail?.lastUpdatedAt ? <><span className={styles.dotSep} /><span>{t.lastSaved} {format.dateTime(detail.lastUpdatedAt)}</span></> : null}
                  {anyDirty ? <span className={`${styles.pill} ${styles.pillWarn}`} data-testid="offer-workspace-dirty">{t.unsaved}</span> : null}
                </div>
                <h1 className={styles.headName} data-testid="offer-workspace-title">{title}</h1>
                <div className={styles.headSub}>
                  {detail?.brandName ? <span>{detail.brandName}</span> : null}
                  {detail?.brandName && (detail?.variantLabel || detail?.productUnitName) ? <span className={styles.dotSep} /> : null}
                  {detail?.variantLabel ? <span>{detail.variantLabel}</span> : null}
                  {detail?.variantLabel && detail?.productUnitName ? <span className={styles.dotSep} /> : null}
                  {detail?.productUnitName ? <span>{detail.productUnitName}</span> : null}
                  {!detail ? (
                    <span>
                      {[selectedProduct?.brandName, selectedVariant?.displayLabel].filter(Boolean).join(" · ") || selectedProduct?.title || t.notSelected}
                    </span>
                  ) : null}
                </div>
              </div>
            </div>

            <div className={styles.meta} data-testid="offer-workspace-meta">
              <div className={styles.metaItem}>
                <span className={styles.metaK}>{t.seller}</span>
                <span className={styles.metaV} data-testid="offer-workspace-seller">{sellerName}</span>
              </div>
              <div className={styles.metaItem}>
                <span className={styles.metaK}>{t.status}</span>
                <span className={styles.metaV}>
                  <StatusPill status={currentStatus} label={statusLabel(t, currentStatus)} />
                </span>
              </div>
              <div className={styles.metaItem}>
                <span className={styles.metaK}>{t.buyDisplay}</span>
                <span className={styles.metaV}>
                  <button
                    type="button"
                    className={`${styles.pill} ${buyability?.buyable ? styles.pillOk : styles.pillWarn}`}
                    data-testid="offer-workspace-buyability"
                    title={buyability?.reason ? `${offerErrorText(t, buyability.reason)} — ${t.buyabilityHint}` : t.buyabilityHint}
                  >
                    {buyability?.buyable ? <Check size={11} aria-hidden /> : <AlertTriangle size={11} aria-hidden />}
                    {buyability?.buyable ? t.buyable : t.notBuyable}
                  </button>
                </span>
              </div>
            </div>

            <div className={styles.price} data-testid="offer-workspace-price">
              <span className={styles.priceCap}>{t.priceSummary}</span>
              {detail?.amount != null ? (
                <span className={styles.priceRow}>
                  <span className={styles.priceMkt}>{format.regionName("IR")}</span>
                  <span className={styles.priceAmt}>{format.money(detail.amount, detail.currency)}</span>
                  <span className={styles.priceCur} dir="ltr">{detail.currency || "IRR"}</span>
                </span>
              ) : (
                <span className={styles.priceEmpty}>{t.priceEmpty}</span>
              )}
            </div>

            <div className={styles.actions}>
              <button
                type="button"
                className={`${styles.btn} ${styles.btnPrimary} ${styles.btnLg}`}
                onClick={() => void (isEdit ? actions.runSave() : actions.runCreate())}
                disabled={saving || saveNeedsReload || (!isEdit && (!createReady || Boolean(activeConflict)))}
                data-offer-save="true"
                data-testid={saveTestId}
              >
                <Check size={14} aria-hidden /> {saving ? t.saving : saveLabel}
              </button>
              <button
                type="button"
                className={`${styles.btn} ${styles.btnGhost} ${styles.btnLg}`}
                onClick={handleCancel}
                data-offer-cancel="true"
                data-testid={isEdit ? "offer-cancel" : adapter.role === "admin" ? "admin-offer-cancel" : "seller-create-cancel"}
              >
                {t.cancel}
              </button>
            </div>
          </div>
        </div>

        <div className={styles.tabWrap}>
          <div className={styles.tabbar} role="tablist" aria-label={t.titleEdit} ref={tabListRef}>
            {TAB_ORDER.map((tab, index) => {
              const disabled = !isEdit && tab !== "identity" && !(identityReady && preCreateTabs.has(tab));
              const selected = activeTab === tab;
              return (
                <button
                  key={tab}
                  id={`offer-tab-${tab}`}
                  type="button"
                  role="tab"
                  aria-selected={selected}
                  aria-controls={`offer-pane-${tab}`}
                  aria-disabled={disabled}
                  disabled={disabled}
                  title={disabled ? (identityReady ? t.tabNeedsCreate : t.tabLocked) : undefined}
                  onClick={() => setActiveTab(tab)}
                  className={`${styles.tab} ${selected ? styles.tabActive : ""}`}
                  data-testid={`offer-tab-button-${tab}`}
                >
                  <span className={styles.tabN}>{format.integer(index + 1)}</span>
                  <span>{tabLabel[tab]}</span>
                  {tabDirty[tab] && isEdit ? <DirtyDot show label={t.dirtyTab} /> : null}
                  {disabled ? <span className={styles.tabN}><Lock size={9} aria-hidden /></span> : null}
                </button>
              );
            })}
          </div>
        </div>
      </header>

      <main className={styles.page} data-testid={`offer-tab-panel-${activeTab}`} id={`offer-pane-${activeTab}`} role="tabpanel" aria-labelledby={`offer-tab-${activeTab}`}>
        {tabError ? (
          <div className={styles.fieldErr} role="alert" data-testid="offer-tab-error" style={{ marginBottom: 10 }}>
            {t.perTabError} {offerErrorText(t, tabError.code)}
          </div>
        ) : null}
        {saveError ? (
          <div className={`${styles.banner} ${styles.bannerDanger}`} role="alert" data-testid="offer-save-error" style={{ marginBottom: 12 }}>
            <AlertTriangle size={15} aria-hidden />
            <div style={{ flex: 1 }}>
              <span className={styles.bannerTitle}>{t.saveErrorTitle}</span>
              {saveError}
              <div style={{ marginTop: 6 }}>
                {isEdit && saveNeedsReload ? (
                  <button type="button" className={`${styles.btn} ${styles.btnSm} ${styles.btnQuiet}`} onClick={() => void actions.reloadAfterUnknown()} disabled={saveReloading} data-testid="offer-save-reload">
                    {saveReloading ? t.loading : t.reloadState}
                  </button>
                ) : saveRetryable ? (
                  <button type="button" className={`${styles.btn} ${styles.btnSm} ${styles.btnQuiet}`} onClick={() => void (isEdit ? actions.runSave() : actions.runCreate())} disabled={saving} data-testid="offer-save-retry">
                    {t.retry}
                  </button>
                ) : null}
              </div>
            </div>
          </div>
        ) : null}
        {renderTab()}
      </main>

      <div className={styles.mobileActions}>
        <button
          type="button"
          className={`${styles.btn} ${styles.btnPrimary}`}
          onClick={() => void (isEdit ? actions.runSave() : actions.runCreate())}
          disabled={saving || saveNeedsReload || (!isEdit && (!createReady || Boolean(activeConflict)))}
          data-offer-save="true"
        >
          <Check size={14} aria-hidden /> {saving ? t.saving : saveLabel}
        </button>
        <button type="button" className={`${styles.btn} ${styles.btnGhost}`} onClick={handleCancel} data-offer-cancel="true">
          {t.cancel}
        </button>
      </div>
      <MessageBox
        open={Boolean(messageBox)}
        title={messageBox?.title ?? ""}
        text={messageBox?.text ?? ""}
        onClose={() => setMessageBox(null)}
        testId={messageBox ? `offer-message-${messageBox.kind}` : undefined}
        actions={
          messageBox ? (
            <>
              {(messageBox.kind === "duplicate" || messageBox.offerId) && existingOfferHref ? (
                <a className={`${styles.btn} ${styles.btnPrimary}`} href={existingOfferHref} data-testid="offer-message-open-existing">{t.viewExistingOffer}</a>
              ) : null}
              {messageBox.kind === "reload" ? (
                <button type="button" className={`${styles.btn} ${styles.btnPrimary}`} onClick={() => { setMessageBox(null); void actions.reloadAfterUnknown(); }} disabled={saveReloading} data-testid="offer-message-reload">
                  {saveReloading ? t.loading : t.reloadState}
                </button>
              ) : null}
              {messageBox.kind === "confirm" ? (
                <>
                  <button type="button" className={`${styles.btn} ${styles.btnPrimary}`} onClick={() => { setMessageBox(null); clearPersistedDraft(); adapter.onCancel(); }} data-testid="offer-message-confirm">
                    {t.dirtyExitConfirm}
                  </button>
                  <button type="button" className={`${styles.btn} ${styles.btnGhost}`} onClick={() => setMessageBox(null)}>
                    {t.dirtyExitCancel}
                  </button>
                </>
              ) : (
                <button type="button" className={`${styles.btn} ${styles.btnGhost}`} onClick={() => setMessageBox(null)}>
                  {t.close}
                </button>
              )}
            </>
          ) : null
        }
      />    </div>
  );
  function renderTab(): ReactNode {
    if (activeTab === "identity") {
      return (
        <IdentityTab
          t={t}
          format={format}
          mode={adapter.mode}
          role={adapter.role}
          detail={localDetail}
          sellerSku={draft.sellerSku}
          onSellerSkuChange={(value) => patchDraft({ sellerSku: value })}
          fixedSeller={adapter.fixedSeller}
          selectedSeller={selectedSeller}
          onSelectSeller={setSelectedSeller}
          channels={channels}
          selectedChannel={selectedChannel}
          onSelectChannel={setSelectedChannel}
          inventoryLocations={inventoryLocations}
          localInventory={localInventory}
          onLocalInventoryChange={(locationId, patch) => setLocalInventory((current) => current.map((row) => row.locationId === locationId ? { ...row, ...patch } : row))}
          selectedProduct={selectedProduct}
          onSelectProduct={(product) => {
            setSelectedProduct(product);
            setSelectedVariant(null);
            setActiveConflict(null);
          }}
          searchProducts={adapter.searchProducts}
          selectedVariant={selectedVariant}
          onSelectVariant={(variant) => {
            setSelectedVariant(variant);
            setActiveConflict(null);
          }}
          loadProductVariants={adapter.loadProductVariants}
          searchSellers={adapter.searchSellers}
          showErrors={showCreateErrors}
          extra={detail && renderExtras ? renderExtras("identity", detail) : null}
        />
      );
    }
    if (!localDetail) return <NeedsCreateTab t={t} title={tabLabel[activeTab]} />;
    switch (activeTab) {
      case "pricing":
        return (
          <PricingTab
            t={t}
            format={format}
            detail={localDetail}
            amount={draft.amount}
            currency={draft.currency}
            dirty={priceChanged}
            onAmountChange={(value) => patchDraft({ amount: value })}
          />
        );
      case "discounts":
        return <DiscountsTab t={t} />;
      case "inventory":
        return (
          <InventoryTab
            t={t}
            format={format}
            detail={localDetail}
            onHand={draft.inventoryOnHand}
            reason={draft.inventoryReason}
            dirty={stockChanged || offeredChanged}
            offeredQuantity={draft.offeredQuantity}
            onOfferedQuantityChange={(value) => patchDraft({ offeredQuantity: value })}
            onOnHandChange={(stockItemId, value) => patchDraft({ inventoryOnHand: { ...draft.inventoryOnHand, [stockItemId]: value } })}
            onReasonChange={(value) => patchDraft({ inventoryReason: value })}
            localLocations={inventoryLocations}
            localInventory={localInventory}
            onLocalInventoryChange={(locationId, patch) => setLocalInventory((current) => current.map((row) => row.locationId === locationId ? { ...row, ...patch } : row))}
          />
        );
      case "shipping":
        return (
          <ShippingTab
            t={t}
            role={adapter.role}
            detail={localDetail}
            shippingTypes={shippingTypes}
            typesError={typesError}
            responsibility={responsibility}
            shippingTypeId={shippingTypeId}
            saving={shippingSaving}
            notice={shippingNotice}
            error={shippingError}
            onResponsibilityChange={(value) => {
              setResponsibility(value);
              setShippingNotice(null);
              setShippingError(null);
              if (value == null) setShippingTypeId("");
            }}
            onShippingTypeChange={(value) => {
              setShippingTypeId(value);
              setShippingNotice(null);
              setShippingError(null);
            }}
          />
        );
      case "conditions":
        return (
          <ConditionsTab
            t={t}
            format={format}
            detail={localDetail}
            returnPolicyChoice={draft.returnPolicyChoice}
            customReturnWindowDays={draft.customReturnWindowDays}
            minimumOrderQuantity={draft.minimumOrderQuantity}
            maximumOrderQuantity={draft.maximumOrderQuantity}
            status={draft.status}
            dirty={commercialChanged}
            onReturnPolicyChange={(value) => patchDraft({ returnPolicyChoice: value })}
            onCustomReturnWindowChange={(value) => patchDraft({ customReturnWindowDays: value })}
            onMinQtyChange={(value) => patchDraft({ minimumOrderQuantity: value })}
            onMaxQtyChange={(value) => patchDraft({ maximumOrderQuantity: value })}
            onStatusChange={(value) => {
              if (allowedStatuses(localDetail.status).includes(value)) patchDraft({ status: value });
            }}
            extra={renderExtras ? renderExtras("conditions", localDetail) : null}
          />
        );
      case "history":
        return <HistoryTab t={t} format={format} data={history.data} loading={history.loading} error={history.error} onPrev={() => history.setPage((page) => Math.max(1, page - 1))} onNext={() => history.setPage((page) => page + 1)} />;
      default:
        return null;
    }
  }
}

function emptyDraft(): OfferWorkspaceDraft {
  return {
    sellerSku: "",
    amount: "",
    currency: "",
    inventoryOnHand: {},
    inventoryReason: "",
    offeredQuantity: "",
    returnPolicyChoice: "Default",
    customReturnWindowDays: "",
    minimumOrderQuantity: "",
    maximumOrderQuantity: "",
    status: "Draft",
  };
}
