"use client";

import { Truck } from "lucide-react";
import type { OfferWorkspaceMessages } from "./messages";
import type {
  OfferFulfillmentResponsibility,
  OfferRole,
  OfferShippingTypeOption,
  OfferWorkspaceDetail,
} from "./types";
import styles from "./offer-workspace.module.css";
import { Note, SectionTitle } from "./tab-shared";

export interface ShippingTabProps {
  t: OfferWorkspaceMessages;
  locale: "fa" | "en";
  role: OfferRole;
  detail: OfferWorkspaceDetail;
  shippingTypes: OfferShippingTypeOption[];
  typesError: string | null;
  responsibility: OfferFulfillmentResponsibility | null;
  shippingTypeId: string;
  saving: boolean;
  notice: string | null;
  error: string | null;
  onResponsibilityChange: (value: OfferFulfillmentResponsibility | null) => void;
  onShippingTypeChange: (value: string) => void;
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

  if (role === "seller") {
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
            <div style={{ marginTop: 12 }}>
              <Note tone="info">{t.shippingAdminOnly}</Note>
            </div>
            <p className={styles.resultMeta} style={{ marginTop: 8 }}>{t.shippingServiceUnavailable}</p>
          </div>
        </section>
      </div>
    );
  }

  return (
    <div className={styles.stack} data-testid="offer-tab-shipping">
      <section className={styles.card}>
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
              <span className={styles.fieldHint}>{responsibilityLabel}</span>
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
                      {type.label} ({type.code})
                    </option>
                  ))}
              </select>
              {props.typesError ? <span className={styles.fieldErr}>{props.typesError}</span> : null}
              <span className={styles.fieldHint}>{detail.fulfillmentShippingTypeId ? t.shippingSaved : t.shippingUnassigned}</span>
            </label>
          </div>

          <div className={styles.stack} style={{ marginTop: 12 }}>
            <Note tone="info">
              <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
                <Truck size={13} aria-hidden /> {t.shippingService}
              </span>
              <span style={{ display: "block" }}>{t.shippingServiceUnavailable}</span>
            </Note>
            {props.notice ? <div data-testid="offer-shipping-notice"><Note tone="ok">{props.notice}</Note></div> : null}
            {props.error ? <div data-testid="offer-shipping-error"><Note tone="danger">{props.error}</Note></div> : null}
            {props.saving ? <p className={styles.resultMeta}>{t.saving}</p> : null}
          </div>
        </div>
      </section>
    </div>
  );
}
