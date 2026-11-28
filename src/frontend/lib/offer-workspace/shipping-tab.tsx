"use client";

import { Check, X } from "lucide-react";
import { SearchableSelect, type SearchableSelectOption } from "../../design-system/primitives/searchable-select";
import type { OfferWorkspaceMessages } from "./messages";
import type {
  OfferFulfillmentResponsibility,
  OfferRole,
  OfferShippingMethodOption,
  OfferShippingSelectionMode,
  OfferShippingTypeOption,
  OfferWorkspaceDetail,
} from "./types";
import styles from "./offer-workspace.module.css";
import tab from "./shipping-tab.module.css";
import { Note, SectionTitle } from "./tab-shared";

export interface ShippingTabProps {
  t: OfferWorkspaceMessages;
  role: OfferRole;
  detail: OfferWorkspaceDetail;
  shippingTypes: OfferShippingTypeOption[];
  typesError: string | null;
  responsibility: OfferFulfillmentResponsibility | null;
  shippingTypeId: string;
  mode: OfferShippingSelectionMode;
  selectedMethodIds: string[];
  methodOptions: OfferShippingMethodOption[];
  methodsLoading: boolean;
  methodsError: string | null;
  saving: boolean;
  notice: string | null;
  error: string | null;
  onResponsibilityChange: (value: OfferFulfillmentResponsibility | null) => void;
  onShippingTypeChange: (value: string) => void;
  onModeChange: (value: OfferShippingSelectionMode) => void;
  onSelectedMethodsChange: (value: string[]) => void;
}

export function ShippingTab(props: ShippingTabProps) {
  const { t, role, detail } = props;
  const responsibilityLabel =
    props.responsibility === "Seller"
      ? t.shippingSeller
      : props.responsibility === "Platform"
        ? t.shippingPlatform
        : t.shippingUnassigned;
  const typeLabel = props.shippingTypes.find((type) => type.shippingTypeId === props.shippingTypeId)?.label;
  const selectedLabels = props.selectedMethodIds.map((id) => {
    const option = props.methodOptions.find((row) => row.methodId === id);
    return { id, option };
  });

  if (role === "seller" || (detail.offerId && detail.capabilities?.canAssignFulfillment !== true)) {
    return (
      <div className={styles.stack} data-testid="offer-tab-shipping">
        <section className={styles.card}>
          <header className={styles.cardHd}>
            <SectionTitle title={t.shippingTitle} desc={t.shippingAdminOnly} />
          </header>
          <div className={styles.cardBd}>
            <div className={styles.formGrid}>
              <div className={styles.field}>
                <span className={styles.fieldLabel}>{t.shippingResponsibility}</span>
                <div className={`${styles.ctl} ${styles.ctlReadonly}`} style={{ display: "flex", alignItems: "center" }} data-testid="offer-shipping-current">
                  {responsibilityLabel}
                </div>
              </div>
              <div className={styles.field}>
                <span className={styles.fieldLabel}>{t.shippingType}</span>
                <div className={`${styles.ctl} ${styles.ctlReadonly}`} style={{ display: "flex", alignItems: "center" }}>
                  {typeLabel || t.shippingUnassigned}
                </div>
              </div>
            </div>
            <div className={styles.field} style={{ marginTop: 16 }}>
              <span className={styles.fieldLabel}>{t.shippingMode}</span>
              <div className={`${styles.ctl} ${styles.ctlReadonly}`} data-testid="offer-shipping-mode">
                {props.mode === "Specific" ? t.shippingModeSpecific : t.shippingModeAuto}
              </div>
            </div>
            {props.mode === "Specific" ? (
              <div className={styles.field} style={{ marginTop: 12 }}>
                <span className={styles.fieldLabel}>{t.shippingMethods}</span>
                <div className={tab.chips} data-testid="offer-shipping-selected-methods">
                  {selectedLabels.length === 0 ? <span className={styles.resultMeta}>{t.shippingNoSelection}</span> : null}
                  {selectedLabels.map(({ id, option }) => {
                    const stale = !option && props.methodOptions.length > 0;
                    return (
                      <span key={id} className={`${styles.chip} ${stale ? tab.chipStale : ""}`}>
                        <span className={tab.readonlyChip}>
                          {option ? option.label : stale ? t.shippingMethodStale : `${id.slice(0, 8)}…`}
                          {option ? <span className={tab.chipCode} dir="ltr">{option.optionCode ? `${option.serviceCode}:${option.optionCode}` : option.serviceCode}</span> : null}
                        </span>
                      </span>
                    );
                  })}
                </div>
              </div>
            ) : null}
          </div>
        </section>
      </div>
    );
  }

  const ready = Boolean(props.responsibility && props.shippingTypeId);
  const addOptions: SearchableSelectOption[] = props.methodOptions
    .filter((method) => !props.selectedMethodIds.includes(method.methodId))
    .map((method) => ({
      value: method.methodId,
      label: method.label,
      searchText: `${method.label} ${method.serviceCode} ${method.optionCode ?? ""}`,
    }));
  const emptySelected = ready && props.mode === "Specific" && selectedLabels.length === 0;

  return (
    <div className={styles.stack} data-testid="offer-tab-shipping">
      <section className={`${styles.card} ${tab.shippingCard}`}>
        <header className={styles.cardHd}>
          <SectionTitle title={t.shippingTitle} desc={t.shippingDesc} />
        </header>
        <div className={styles.cardBd}>
          <div className={styles.formGrid}>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>{t.shippingResponsibility}</span>
              <select
                className={`${styles.ctl} ${styles.select}`}
                value={props.responsibility ?? ""}
                onChange={(event) => {
                  const value = event.target.value;
                  props.onResponsibilityChange(value === "" ? null : (value as OfferFulfillmentResponsibility));
                }}
                data-testid="offer-shipping-responsibility"
              >
                <option value="">{t.shippingUnassigned}</option>
                <option value="Seller">{t.shippingSeller}</option>
                <option value="Platform">{t.shippingPlatform}</option>
              </select>
            </label>

            <label className={styles.field}>
              <span className={styles.fieldLabel}>{t.shippingType}</span>
              <select
                className={`${styles.ctl} ${styles.select}`}
                value={props.shippingTypeId}
                onChange={(event) => props.onShippingTypeChange(event.target.value)}
                disabled={props.responsibility == null}
                data-testid="offer-shipping-type"
              >
                <option value="">{t.shippingUnassigned}</option>
                {props.shippingTypes
                  .filter((type) => type.isActive)
                  .map((type) => (
                    <option key={type.shippingTypeId} value={type.shippingTypeId}>
                      {type.label || type.code}
                    </option>
                  ))}
              </select>
              {props.typesError ? <span className={styles.fieldErr}>{props.typesError}</span> : null}
            </label>
          </div>

          <div className={styles.field} style={{ marginTop: 16 }}>
            <span className={styles.fieldLabel}>{t.shippingMode}</span>
            <div className={tab.segmented} data-testid="offer-shipping-mode-picker" role="group" aria-label={t.shippingMode}>
              <button
                type="button"
                className={props.mode === "Automatic" ? tab.segActive : tab.seg}
                disabled={!ready && props.mode !== "Specific"}
                aria-pressed={props.mode === "Automatic"}
                data-testid="offer-shipping-mode-auto"
                onClick={() => props.onModeChange("Automatic")}
              >
                {props.mode === "Automatic" ? <Check size={14} aria-hidden /> : null}
                <span><strong>{t.shippingModeAuto}</strong><small>{t.shippingModeAutoHint}</small></span>
              </button>
              <button
                type="button"
                className={props.mode === "Specific" ? tab.segActive : tab.seg}
                disabled={!ready}
                aria-pressed={props.mode === "Specific"}
                data-testid="offer-shipping-mode-specific"
                onClick={() => props.onModeChange("Specific")}
              >
                {props.mode === "Specific" ? <Check size={14} aria-hidden /> : null}
                <span><strong>{t.shippingModeSpecific}</strong><small>{t.shippingModeSpecificHint}</small></span>
              </button>
            </div>
          </div>

          {!ready || props.mode === "Automatic" ? (
            ready ? <p className={styles.fieldHint} role="status">{t.shippingLockedAuto}</p> : null
          ) : (
            <div className={styles.field} style={{ marginTop: 14 }}>
              <span className={styles.fieldLabel}>{t.shippingMethods}</span>
              <div className={tab.chips} data-testid="offer-shipping-selected-methods">
                {selectedLabels.map(({ id, option }) => (
                  <span key={id} className={`${styles.chip} ${option ? "" : tab.chipStale}`}>
                    <span className={tab.readonlyChip}>
                      {option ? option.label : t.shippingMethodStale}
                      {option ? <span className={tab.chipCode} dir="ltr">{option.optionCode ? `${option.serviceCode}:${option.optionCode}` : option.serviceCode}</span> : null}
                    </span>
                    <button
                      type="button"
                      aria-label={`${t.shippingRemoveMethod} ${option ? option.label : id}`}
                      className={tab.chipRemove}
                      onClick={() => props.onSelectedMethodsChange(props.selectedMethodIds.filter((value) => value !== id))}
                      data-testid={`offer-shipping-remove-${id}`}
                    >
                      <X size={12} aria-hidden />
                    </button>
                  </span>
                ))}
              </div>
              <div className={tab.methodPicker}>
                <SearchableSelect
                  options={addOptions}
                  value=""
                  onChange={(value) => {
                    if (!props.methodOptions.some((row) => row.methodId === value) || props.selectedMethodIds.includes(value)) return;
                    props.onSelectedMethodsChange([...props.selectedMethodIds, value]);
                  }}
                  label={t.shippingAddMethod}
                  placeholder={t.shippingAddMethod}
                  searchLabel={t.shippingSearchMethod}
                  emptyLabel={t.shippingNoMethodResults}
                  disabled={!addOptions.length || props.methodsLoading}
                  testId="offer-shipping-method-picker"
                />
              </div>
              {props.methodsLoading ? <p className={styles.resultMeta} role="status">{t.loading}</p> : null}
              {props.methodsError ? <p className={styles.fieldErr} role="status" data-testid="offer-shipping-methods-error">{props.methodsError}</p> : null}
              {!props.methodsLoading && !props.methodsError && !props.methodOptions.length ? (
                <p className={styles.fieldHint} role="status">{t.shippingNoMethods}</p>
              ) : emptySelected ? (
                <p className={styles.fieldErr} role="status">{t.shippingSelectAtLeastOne}</p>
              ) : (
                <p className={styles.fieldHint}>{t.shippingMethodNote}</p>
              )}
            </div>
          )}

          <div className={styles.stack} style={{ marginTop: 12 }}>
            {props.notice ? <div data-testid="offer-shipping-notice"><Note tone="ok">{props.notice}</Note></div> : null}
            {props.error ? <div data-testid="offer-shipping-error"><Note tone="danger">{props.error}</Note></div> : null}
            {props.saving ? <p className={styles.resultMeta}>{t.saving}</p> : null}
          </div>
        </div>
      </section>
    </div>
  );
}
