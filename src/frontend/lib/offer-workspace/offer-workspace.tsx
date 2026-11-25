"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { AlertTriangle, Check, ImageOff, Lock } from "lucide-react";
import { useLocale } from "../i18n/locale-context";
import { formatJalaliDateTime } from "../../design-system";
import {
  allowedStatuses,
  commercialDirty,
  deriveBuyability,
  digitsFa,
  draftFromDetail,
  errorLabel,
  faMoney,
  inventoryDirty,
  marketLabel,
  parseDecimal,
  priceDirty,
  statusLabel,
  validateDraft,
  type OfferWorkspaceDraft,
} from "./model";
import { offerWorkspaceMessages } from "./messages";
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
  OfferSellerOption,
  OfferShippingTypeOption,
  OfferTabId,
  OfferVariantOption,
  OfferWorkspaceAdapter,
  OfferWorkspaceDetail,
} from "./types";

const TAB_ORDER: OfferTabId[] = [
  "identity",
  "pricing",
  "discounts",
  "inventory",
  "shipping",
  "conditions",
  "history",
];

export interface OfferWorkspaceProps {
  adapter: OfferWorkspaceAdapter;
  renderExtras?: (tab: OfferTabId, detail: OfferWorkspaceDetail) => ReactNode;
}

export function OfferWorkspace({ adapter, renderExtras }: OfferWorkspaceProps) {
  const locale = useLocale() === "en" ? "en" : "fa";
  const t = offerWorkspaceMessages[locale];
  const isEdit = adapter.mode === "edit";

  const [detail, setDetail] = useState<OfferWorkspaceDetail | null>(null);
  const [loading, setLoading] = useState(isEdit);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [draft, setDraft] = useState<OfferWorkspaceDraft>(emptyDraft());
  const [activeTab, setActiveTab] = useState<OfferTabId>("identity");
  const [selectedSeller, setSelectedSeller] = useState<OfferSellerOption | null>(null);
  const [selectedVariant, setSelectedVariant] = useState<OfferVariantOption | null>(null);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveNotice, setSaveNotice] = useState<string | null>(null);
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

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (!isEdit || !adapter.loadShippingTypes) return;
    void adapter
      .loadShippingTypes()
      .then((rows) => {
        setShippingTypes(rows);
        setTypesError(null);
      })
      .catch(() => setTypesError(t.pickerError));
  }, [adapter, isEdit, t.pickerError]);

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

  const commercialChanged = detail ? commercialDirty(detail, draft) : false;
  const priceChanged = detail ? priceDirty(detail, draft) : false;
  const stockChanged = detail ? inventoryDirty(detail, draft) : false;
  const shippingChanged = Boolean(
    detail &&
      adapter.saveFulfillment &&
      (responsibility !== detail.fulfillmentResponsibility ||
        (shippingTypeId || null) !== (detail.fulfillmentShippingTypeId ?? null)),
  );
  const createReady = adapter.role === "seller"
    ? Boolean(adapter.fixedSeller) && Boolean(selectedVariant)
    : Boolean(selectedSeller) && Boolean(selectedVariant);
  const createDirty = !isEdit && (Boolean(selectedSeller) || Boolean(selectedVariant) || draft.sellerSku.trim().length > 0);
  const anyDirty = isEdit
    ? commercialChanged || priceChanged || stockChanged || shippingChanged
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

  const tabDirty: Record<OfferTabId, boolean> = {
    identity: commercialChanged,
    pricing: priceChanged,
    discounts: false,
    inventory: stockChanged,
    shipping: shippingChanged,
    conditions: commercialChanged,
    history: false,
  };

  function patchDraft(patch: Partial<OfferWorkspaceDraft>) {
    setDraft((current) => ({ ...current, ...patch }));
    setSaveNotice(null);
  }

  function applySavedDetail(next: OfferWorkspaceDetail, domain: "commercial" | "price" | "inventory") {
    setDetail(next);
    setDraft((current) => {
      if (domain === "commercial") {
        return {
          ...current,
          sellerSku: next.sellerSku ?? "",
          status: next.status,
          returnPolicyChoice: next.returnPolicyChoice,
          customReturnWindowDays: next.customReturnWindowDays != null ? String(next.customReturnWindowDays) : "",
          minimumOrderQuantity: next.minimumOrderQuantity != null ? String(next.minimumOrderQuantity) : "",
          maximumOrderQuantity: next.maximumOrderQuantity != null ? String(next.maximumOrderQuantity) : "",
        };
      }
      if (domain === "price") {
        return { ...current, amount: next.amount != null ? String(next.amount) : "", currency: next.currency };
      }
      return {
        ...current,
        inventoryOnHand: Object.fromEntries(next.inventoryLocations.map((row) => [row.stockItemId, String(row.onHand)])),
        inventoryReason: "",
      };
    });
  }

  async function runSave() {
    if (!detail || saving || saveRef.current) return;
    const errors = validateDraft(detail, draft);
    if (errors.length > 0) {
      const first = errors[0]!;
      setTabError({ tab: first.tab, code: first.code });
      setActiveTab(first.tab);
      setSaveError(errorLabel(first.code, locale));
      return;
    }
    if (!commercialChanged && !priceChanged && !stockChanged && !shippingChanged) {
      setSaveNotice(t.noChanges);
      setSaveError(null);
      setTabError(null);
      return;
    }
    saveRef.current = true;
    setSaving(true);
    setSaveError(null);
    setSaveNotice(null);
    setTabError(null);
    setShippingError(null);

    if (commercialChanged) {
      const result = await adapter.saveCommercial(buildCommercialPatch(detail, draft));
      if (!result.ok) return finishSaveFailure("conditions", result.denied);
      applySavedDetail(result.detail, "commercial");
    }

    if (priceChanged) {
      const result = await adapter.savePrice({ amount: parseDecimal(draft.amount) ?? 0, currency: draft.currency || "IRR" });
      if (!result.ok) return finishSaveFailure("pricing", result.denied);
      applySavedDetail(result.detail, "price");
    }

    if (stockChanged) {
      const changedRows = detail.inventoryLocations.filter(
        (row) => parseDecimal(draft.inventoryOnHand[row.stockItemId]) !== row.onHand,
      );
      for (const row of changedRows) {
        const result = await adapter.saveInventory({
          stockItemId: row.stockItemId,
          onHand: parseDecimal(draft.inventoryOnHand[row.stockItemId]) ?? row.onHand,
          reason: draft.inventoryReason.trim(),
        });
        if (!result.ok) return finishSaveFailure("inventory", result.denied);
        applySavedDetail(result.detail, "inventory");
      }
    }

    if (shippingChanged && adapter.saveFulfillment) {
      setShippingSaving(true);
      const result = await adapter.saveFulfillment({
        responsibility,
        shippingTypeId: shippingTypeId || null,
      });
      setShippingSaving(false);
      if (!result.ok) return finishSaveFailure("shipping", result.denied);
      const verification = adapter.loadDetail ? await adapter.loadDetail() : null;
      if (
        !verification ||
        !verification.ok ||
        verification.detail.fulfillmentResponsibility !== responsibility ||
        (verification.detail.fulfillmentShippingTypeId ?? null) !== (shippingTypeId || null)
      ) {
        setShippingError(t.perTabError);
        return finishSaveFailure("shipping", false);
      }
      applyLoadedDetail(verification.detail);
      setShippingNotice(t.shippingSaved);
    }

    saveRef.current = false;
    setSaving(false);
    setSaveNotice(t.saved);
  }

  function finishSaveFailure(tab: OfferTabId, denied?: boolean) {
    saveRef.current = false;
    setSaving(false);
    setActiveTab(tab);
    setSaveError(denied ? t.denied : t.saveError);
    setTabError({ tab, code: denied ? "denied" : "save" });
  }

  async function runCreate() {
    if (saving) return;
    if (!createReady) {
      setShowCreateErrors(true);
      setSaveError(t.createNeedsIdentity);
      return;
    }
    if (adapter.role === "admin" && !selectedSeller) {
      setShowCreateErrors(true);
      setSaveError(t.createNeedsIdentity);
      return;
    }
    if (!selectedVariant) {
      setShowCreateErrors(true);
      setSaveError(t.createNeedsIdentity);
      return;
    }
    setSaving(true);
    setSaveError(null);
    const result = await adapter.createDraft({
      sellerPartyId: selectedSeller?.sellerPartyId,
      catalogVariantId: selectedVariant.catalogVariantId,
      sellerSku: draft.sellerSku.trim() || null,
    });
    setSaving(false);
    if (!result.ok) {
      setSaveError(result.denied ? t.denied : errorLabel(result.errorCode, locale));
      return;
    }
    adapter.onCreated(result.offerId);
  }

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
              <span className={styles.emptyD}>{errorLabel(loadError, locale)}</span>
              <button type="button" className={styles.btn} onClick={() => void load()}>{t.retry}</button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  const buyability = detail ? deriveBuyability(detail) : null;
  const title = detail?.productTitle || selectedVariant?.productTitle || t.titleCreate;
  const sellerName = detail?.sellerDisplayName || selectedSeller?.displayName || adapter.fixedSeller?.displayName || (adapter.role === "seller" ? t.sellerSession : t.notSelected);
  const mediaId = detail?.primaryMediaAssetId ?? selectedVariant?.primaryMediaAssetId ?? null;
  const currentStatus = detail ? draft.status : "Draft";
  const saveLabel = isEdit ? t.save : t.saveDraft;
  const saveTestId = isEdit
    ? adapter.role === "admin" ? "admin-offer-save" : "offer-save"
    : adapter.role === "admin" ? "admin-offer-submit" : "seller-create-submit";

  return (
    <div className={styles.root} data-testid="offer-workspace">
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
                  {detail?.lastUpdatedAt ? <><span className={styles.dotSep} /><span>{t.lastSaved} {digitsFa(formatJalaliDateTime(detail.lastUpdatedAt, locale))}</span></> : null}
                  {anyDirty ? <span className={`${styles.pill} ${styles.pillWarn}`} data-testid="offer-workspace-dirty">{t.unsaved}</span> : null}

                </div>
                <h1 className={styles.headName} data-testid="offer-workspace-title">{title}</h1>
                <div className={styles.headSub}>
                  {detail?.brandName ? <span>{detail.brandName}</span> : null}
                  {detail?.brandName && detail?.productUnitName ? <span className={styles.dotSep} /> : null}
                  {detail?.productUnitName ? <span>{detail.productUnitName}</span> : null}
                  {!detail?.brandName && !detail?.productUnitName ? <span>{selectedVariant ? selectedVariant.productTitle : t.notSelected}</span> : null}
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
                <span className={styles.metaK}>{t.buy}</span>
                <span className={styles.metaV}>
                  <button
                    type="button"
                    className={`${styles.pill} ${buyability?.buyable ? styles.pillOk : styles.pillWarn}`}
                    data-testid="offer-workspace-buyability"
                    title={buyability?.reason ? errorLabel(buyability.reason, locale) : undefined}
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
                  <span className={styles.priceMkt}>{marketLabel("IR", locale)}</span>
                  <span className={styles.priceAmt}>{faMoney(detail.amount, detail.currency, locale)}</span>
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
                onClick={() => void (isEdit ? runSave() : runCreate())}
                disabled={saving || (!isEdit && !createReady)}
                data-offer-save="true"
                data-testid={saveTestId}
              >
                <Check size={14} aria-hidden /> {saving ? t.saving : saveLabel}
              </button>
              <button
                type="button"
                className={`${styles.btn} ${styles.btnGhost} ${styles.btnLg}`}
                onClick={() => adapter.onCancel()}
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
              const disabled = !isEdit && tab !== "identity";
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
                  title={disabled ? t.tabNeedsCreate : undefined}
                  onClick={() => setActiveTab(tab)}
                  className={`${styles.tab} ${selected ? styles.tabActive : ""}`}
                  data-testid={`offer-tab-button-${tab}`}
                >
                  <span className={styles.tabN}>{digitsFa(index + 1)}</span>
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
        {saveError ? (
          <div className={`${styles.banner} ${styles.bannerDanger}`} role="alert" data-testid="offer-save-error" style={{ marginBottom: 12 }}>
            <AlertTriangle size={15} aria-hidden />
            <div style={{ flex: 1 }}>
              <span className={styles.bannerTitle}>{t.saveErrorTitle}</span>
              {saveError}
              <div style={{ marginTop: 6 }}>
                <button type="button" className={`${styles.btn} ${styles.btnSm} ${styles.btnQuiet}`} onClick={() => void (isEdit ? runSave() : runCreate())}>
                  {t.retry}
                </button>
              </div>
            </div>
          </div>
        ) : null}
        {saveNotice ? (
          <div className={`${styles.banner} ${styles.bannerOk}`} data-testid="offer-save-notice" style={{ marginBottom: 12 }}>
            <Check size={14} aria-hidden />
            <div>{saveNotice}</div>
          </div>
        ) : null}
        {tabError ? (
          <div className={`${styles.banner} ${styles.bannerDanger}`} data-testid="offer-tab-error" style={{ marginBottom: 12 }}>
            <AlertTriangle size={14} aria-hidden />
            <div>{t.perTabError} {errorLabel(tabError.code, locale)}</div>
          </div>
        ) : null}

        {renderTab()}
      </main>

      <div className={styles.mobileActions}>
        <button
          type="button"
          className={`${styles.btn} ${styles.btnPrimary}`}
          onClick={() => void (isEdit ? runSave() : runCreate())}
          disabled={saving || (!isEdit && !createReady)}
          data-offer-save="true"
        >
          <Check size={14} aria-hidden /> {saving ? t.saving : saveLabel}
        </button>
        <button type="button" className={`${styles.btn} ${styles.btnGhost}`} onClick={() => adapter.onCancel()} data-offer-cancel="true">
          {t.cancel}
        </button>
      </div>
    </div>
  );

  function renderTab(): ReactNode {
    if (activeTab === "identity") {
      return (
        <IdentityTab
          t={t}
          locale={locale}
          mode={adapter.mode}
          role={adapter.role}
          detail={detail}
          sellerSku={draft.sellerSku}
          onSellerSkuChange={(value) => patchDraft({ sellerSku: value })}
          fixedSeller={adapter.fixedSeller}
          selectedSeller={selectedSeller}
          selectedVariant={selectedVariant}
          onSelectSeller={setSelectedSeller}
          onSelectVariant={setSelectedVariant}
          searchSellers={adapter.searchSellers}
          searchVariants={adapter.searchVariants}
          showErrors={showCreateErrors}
          extra={detail && renderExtras ? renderExtras("identity", detail) : null}
        />
      );
    }
    if (!detail) return <NeedsCreateTab t={t} title={tabLabel[activeTab]} />;
    switch (activeTab) {
      case "pricing":
        return (
          <PricingTab
            t={t}
            locale={locale}
            detail={detail}
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
            locale={locale}
            detail={detail}
            onHand={draft.inventoryOnHand}
            reason={draft.inventoryReason}
            dirty={stockChanged}
            onOnHandChange={(stockItemId, value) => patchDraft({ inventoryOnHand: { ...draft.inventoryOnHand, [stockItemId]: value } })}
            onReasonChange={(value) => patchDraft({ inventoryReason: value })}
          />
        );
      case "shipping":
        return (
          <ShippingTab
            t={t}
            locale={locale}
            role={adapter.role}
            detail={detail}
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
            locale={locale}
            detail={detail}
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
              if (allowedStatuses(detail.status).includes(value)) patchDraft({ status: value });
            }}
            extra={renderExtras ? renderExtras("conditions", detail) : null}
          />
        );
      case "history":
        return <HistoryTab t={t} />;
      default:
        return null;
    }
  }
}

function emptyDraft(): OfferWorkspaceDraft {
  return {
    sellerSku: "",
    amount: "",
    currency: "IRR",
    inventoryOnHand: {},
    inventoryReason: "",
    returnPolicyChoice: "Default",
    customReturnWindowDays: "",
    minimumOrderQuantity: "",
    maximumOrderQuantity: "",
    status: "Draft",
  };
}

function buildCommercialPatch(detail: OfferWorkspaceDetail, draft: OfferWorkspaceDraft): OfferCommercialPatch {
  const min = parseDecimal(draft.minimumOrderQuantity);
  const max = parseDecimal(draft.maximumOrderQuantity);
  const customDays = draft.returnPolicyChoice === "Custom" ? parseDecimal(draft.customReturnWindowDays) : null;
  const returnsChanged =
    draft.returnPolicyChoice !== detail.returnPolicyChoice || customDays !== detail.customReturnWindowDays;
  const qtyChanged = min !== detail.minimumOrderQuantity || max !== detail.maximumOrderQuantity;
  return {
    sellerSku: draft.sellerSku.trim() || null,
    status: draft.status !== detail.status ? draft.status : undefined,
    returnPolicyChoice: returnsChanged ? draft.returnPolicyChoice : undefined,
    customReturnWindowDays: returnsChanged ? customDays : undefined,
    minimumOrderQuantity: qtyChanged ? min : undefined,
    maximumOrderQuantity: qtyChanged ? max : undefined,
    clearOrderQuantityLimits:
      qtyChanged && min == null && max == null &&
      (detail.minimumOrderQuantity != null || detail.maximumOrderQuantity != null),
  };
}
