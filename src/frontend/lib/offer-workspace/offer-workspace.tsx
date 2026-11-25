"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { Button, ErrorState, Skeleton } from "../../design-system";
import { useLocale } from "../i18n/locale-context";
import {
  allowedStatuses,
  commercialDirty,
  deriveBuyability,
  draftFromDetail,
  errorLabel,
  faMoney,
  inventoryDirty,
  parseDecimal,
  priceDirty,
  validateDraft,
  type OfferWorkspaceDraft,
} from "./model";
import { offerWorkspaceMessages, type OfferWorkspaceMessages } from "./messages";
import { ConditionsTab } from "./conditions-tab";
import { DiscountsTab, HistoryTab, NeedsCreateTab } from "./gated-tabs";
import { IdentityTab } from "./identity-tab";
import { InventoryTab } from "./inventory-tab";
import { PricingTab } from "./pricing-tab";
import { ShippingTab } from "./shipping-tab";
import { DirtyDot, StatusPill } from "./tab-shared";
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

  const [shippingTypes, setShippingTypes] = useState<OfferShippingTypeOption[]>([]);
  const [typesError, setTypesError] = useState<string | null>(null);
  const [responsibility, setResponsibility] = useState<OfferFulfillmentResponsibility | null>(null);
  const [shippingTypeId, setShippingTypeId] = useState("");
  const [serviceId, setServiceId] = useState("");
  const [shippingSaving, setShippingSaving] = useState(false);
  const [shippingNotice, setShippingNotice] = useState<string | null>(null);
  const [shippingError, setShippingError] = useState<string | null>(null);
  const saveRef = useRef(false);

  const tabLabel = useMemo<Record<OfferTabId, string>>(
    () => ({
      identity: t.identityTab,
      pricing: t.pricingTab,
      discounts: t.discountsTab,
      inventory: t.inventoryTab,
      shipping: t.shippingTab,
      conditions: t.conditionsTab,
      history: t.historyTab,
    }),
    [t],
  );

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
    setDetail(result.detail);
    setDraft(draftFromDetail(result.detail));
  }, [adapter, isEdit]);

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
    if (tab && (TAB_ORDER as string[]).includes(tab)) {
      setActiveTab(tab as OfferTabId);
    }
  }, []);

  const commercialChanged = detail ? commercialDirty(detail, draft) : false;
  const priceChanged = detail ? priceDirty(detail, draft) : false;
  const stockChanged = detail ? inventoryDirty(detail, draft) : false;
  const createReady = adapter.role === "seller"
    ? Boolean(adapter.fixedSeller) && Boolean(selectedVariant)
    : Boolean(selectedSeller) && Boolean(selectedVariant);
  const createDirty = !isEdit && (Boolean(selectedSeller) || Boolean(selectedVariant) || draft.sellerSku.trim().length > 0);
  const anyDirty = isEdit
    ? commercialChanged || priceChanged || stockChanged
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
    shipping: false,
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
          customReturnWindowDays:
            next.customReturnWindowDays != null ? String(next.customReturnWindowDays) : "",
          minimumOrderQuantity:
            next.minimumOrderQuantity != null ? String(next.minimumOrderQuantity) : "",
          maximumOrderQuantity:
            next.maximumOrderQuantity != null ? String(next.maximumOrderQuantity) : "",
        };
      }
      if (domain === "price") {
        return { ...current, amount: next.amount != null ? String(next.amount) : "", currency: next.currency };
      }
      return {
        ...current,
        inventoryOnHand: Object.fromEntries(
          next.inventoryLocations.map((row) => [row.stockItemId, String(row.onHand)]),
        ),
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
    if (!commercialChanged && !priceChanged && !stockChanged) {
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

    if (commercialChanged) {
      const patch = buildCommercialPatch(detail, draft);
      const result = await adapter.saveCommercial(patch);
      if (!result.ok) {
        finishSaveFailure("conditions", result.denied);
        return;
      }
      applySavedDetail(result.detail, "commercial");
    }

    if (priceChanged) {
      const result = await adapter.savePrice({
        amount: parseDecimal(draft.amount) ?? 0,
        currency: draft.currency || "IRR",
      });
      if (!result.ok) {
        finishSaveFailure("pricing", result.denied);
        return;
      }
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
        if (!result.ok) {
          finishSaveFailure("inventory", result.denied);
          return;
        }
        applySavedDetail(result.detail, "inventory");
      }
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
    if (!createReady || saving) return;
    if (adapter.role === "admin" && !selectedSeller) {
      setSaveError(t.createNeedsIdentity);
      return;
    }
    if (!selectedVariant) {
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

  async function runShippingSave() {
    if (!adapter.saveFulfillment || shippingSaving) return;
    setShippingSaving(true);
    setShippingNotice(null);
    setShippingError(null);
    const result = await adapter.saveFulfillment({
      responsibility,
      shippingTypeId: shippingTypeId || null,
    });
    setShippingSaving(false);
    if (!result.ok) {
      setShippingError(result.denied ? t.denied : errorLabel(result.errorCode, locale));
      return;
    }
    setShippingNotice(t.shippingSaved);
  }

  if (denied) {
    return (
      <ErrorState title={t.denied} detail={t.deniedDetail} onRetry={() => void load()} retryLabel={t.retry} />
    );
  }
  if (loading) {
    return (
      <div className="grid gap-3" aria-busy="true" data-testid="offer-workspace-loading">
        <Skeleton className="h-16 w-full" />
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }
  if (loadError) {
    return (
      <ErrorState
        title={t.loadFailed}
        detail={errorLabel(loadError, locale)}
        onRetry={() => void load()}
        retryLabel={t.retry}
      />
    );
  }

  const buyability = detail ? deriveBuyability(detail) : null;
  const createTitle = t.titleCreate;

  return (
    <div className="grid gap-4" data-testid="offer-workspace">
      <header
        className="sticky top-0 z-20 rounded-ds border border-border bg-surface/95 p-3 shadow-sm backdrop-blur"
        data-testid="offer-workspace-header"
      >
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <p className="text-[11px] text-muted">{t.breadcrumb}</p>
            <h1 className="truncate text-lg font-bold" data-testid="offer-workspace-title">
              {detail ? t.titleEdit : createTitle}
            </h1>
            <div className="mt-1 flex flex-wrap items-center gap-2 text-xs text-muted">
              <span className="max-w-[18rem] truncate font-medium text-foreground" data-testid="offer-workspace-product">
                {detail?.productTitle ?? selectedVariant?.productTitle ?? t.selectProduct}
              </span>
              <span aria-hidden="true">·</span>
              <span data-testid="offer-workspace-seller">
                {detail?.sellerDisplayName || selectedSeller?.displayName || adapter.fixedSeller?.displayName || "—"}
              </span>
              <span aria-hidden="true">·</span>
              <span dir="ltr" className="font-mono" data-testid="offer-workspace-offer-id">
                {detail?.offerId ?? "—"}
              </span>
            </div>
            <div className="mt-1 flex flex-wrap items-center gap-2 text-xs">
              {detail ? <StatusPill status={draft.status} label={statusLabel(t, draft.status)} /> : null}
              {detail ? (
                <span
                  className={`rounded-full px-2 py-0.5 ${buyability?.buyable ? "bg-success/15 text-success" : "bg-warning/15 text-warning"}`}
                  data-testid="offer-workspace-buyability"
                >
                  {buyability?.buyable ? t.buyable : t.notBuyable}
                </span>
              ) : null}
              <span className="tabular-nums" data-testid="offer-workspace-price">
                {detail ? faMoney(detail.amount, detail.currency, locale) : "—"} · {t.market} IR
              </span>
              {anyDirty ? (
                <span className="font-medium text-warning" data-testid="offer-workspace-dirty">
                  {t.unsaved}
                </span>
              ) : null}
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Button
              type="button"
              onClick={() => void (isEdit ? runSave() : runCreate())}
              disabled={saving || (!isEdit && !createReady)}
              data-offer-save="true"
              data-testid={saveTestId(adapter)}
            >
              {saving ? t.saving : isEdit ? t.save : t.saveDraft}
            </Button>
            <Button
              type="button"
              tone="secondary"
              onClick={() => adapter.onCancel()}
              data-offer-cancel="true"
              data-testid={isEdit ? "offer-cancel" : adapter.role === "admin" ? "admin-offer-cancel" : "seller-create-cancel"}
            >
              {t.cancel}
            </Button>
          </div>
        </div>
      </header>

      <div className="flex flex-nowrap gap-2 overflow-x-auto pb-1" role="tablist" aria-label={t.titleEdit}>
        {TAB_ORDER.map((tab) => {
          const disabled = !isEdit && tab !== "identity";
          return (
            <button
              key={tab}
              type="button"
              role="tab"
              aria-selected={activeTab === tab}
              disabled={disabled}
              title={disabled ? t.tabNeedsCreate : undefined}
              onClick={() => setActiveTab(tab)}
              className={`inline-flex min-h-11 shrink-0 items-center gap-2 rounded-ds px-3 text-sm ${
                activeTab === tab
                  ? "bg-primary text-primary-foreground"
                  : disabled
                    ? "bg-secondary/40 text-muted opacity-60"
                    : "bg-secondary"
              }`}
              data-testid={`offer-tab-button-${tab}`}
            >
              {tabLabel[tab]}
              {tabDirty[tab] && isEdit ? <DirtyDot show label="" /> : null}
            </button>
          );
        })}
      </div>

      {saveError ? (
        <p className="rounded-ds border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger" data-testid="offer-save-error">
          {saveError}
        </p>
      ) : null}
      {saveNotice ? (
        <p className="rounded-ds border border-success/40 bg-success/10 px-3 py-2 text-sm text-success" data-testid="offer-save-notice">
          {saveNotice}
        </p>
      ) : null}
      {tabError ? (
        <p className="text-xs text-danger" data-testid="offer-tab-error">
          {t.perTabError} {errorLabel(tabError.code, locale)}
        </p>
      ) : null}

      <section className="rounded-ds border border-border bg-surface p-4" data-testid={`offer-tab-panel-${activeTab}`}>
        {renderTab()}
      </section>

      {detail ? (
        <p className="text-[11px] text-muted" data-testid="offer-workspace-note">
          {t.readonlySeam}
        </p>
      ) : null}
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
          extra={detail && renderExtras ? renderExtras("identity", detail) : null}
        />
      );
    }
    if (!detail) {
      return <NeedsCreateTab t={t} title={tabLabel[activeTab]} />;
    }
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
            onOnHandChange={(stockItemId, value) =>
              patchDraft({ inventoryOnHand: { ...draft.inventoryOnHand, [stockItemId]: value } })
            }
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
            serviceId={serviceId}
            saving={shippingSaving}
            notice={shippingNotice}
            error={shippingError}
            onResponsibilityChange={(value) => {
              setResponsibility(value);
              if (value == null) {
                setShippingTypeId("");
                setServiceId("");
              }
            }}
            onShippingTypeChange={setShippingTypeId}
            onServiceChange={setServiceId}
            onSave={() => void runShippingSave()}
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

function saveTestId(adapter: OfferWorkspaceAdapter): string {
  if (adapter.mode === "edit") {
    return adapter.role === "admin" ? "admin-offer-save" : "offer-save";
  }
  return adapter.role === "admin" ? "admin-offer-submit" : "seller-create-submit";
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

function statusLabel(t: OfferWorkspaceMessages, status: string): string {
  switch (status) {
    case "Draft":
      return t.lifecycleDraft;
    case "Active":
      return t.lifecycleActive;
    case "Suspended":
      return t.lifecycleSuspended;
    case "Archived":
      return t.lifecycleArchived;
    default:
      return status;
  }
}

function buildCommercialPatch(
  detail: OfferWorkspaceDetail,
  draft: OfferWorkspaceDraft,
): OfferCommercialPatch {
  const min = parseDecimal(draft.minimumOrderQuantity);
  const max = parseDecimal(draft.maximumOrderQuantity);
  const customDays =
    draft.returnPolicyChoice === "Custom" ? parseDecimal(draft.customReturnWindowDays) : null;
  const returnsChanged =
    draft.returnPolicyChoice !== detail.returnPolicyChoice ||
    customDays !== detail.customReturnWindowDays;
  const qtyChanged =
    min !== detail.minimumOrderQuantity || max !== detail.maximumOrderQuantity;
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
