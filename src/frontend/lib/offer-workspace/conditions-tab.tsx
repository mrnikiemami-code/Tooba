"use client";

import type { ReactNode } from "react";
import { ShieldCheck } from "lucide-react";
import type { OfferWorkspaceMessages } from "./messages";
import type { OfferWorkspaceDetail } from "./types";
import { allowedStatuses, deriveBuyability, statusLabel } from "./model";
import type { OfferFormatters } from "./i18n";
import styles from "./offer-workspace.module.css";
import { DirtyDot, SectionTitle } from "./tab-shared";

export interface ConditionsTabProps {
  t: OfferWorkspaceMessages;
  format: OfferFormatters;
  detail: OfferWorkspaceDetail;
  returnPolicyChoice: string;
  customReturnWindowDays: string;
  minimumOrderQuantity: string;
  maximumOrderQuantity: string;
  status: string;
  dirty: boolean;
  onReturnPolicyChange: (value: string) => void;
  onCustomReturnWindowChange: (value: string) => void;
  onMinQtyChange: (value: string) => void;
  onMaxQtyChange: (value: string) => void;
  onStatusChange: (value: string) => void;
  extra?: ReactNode;
}

export function ConditionsTab(props: ConditionsTabProps) {
  const { t, format, detail } = props;
  const buyability = deriveBuyability({ status: props.status, amount: detail.amount, availableUnits: detail.availableUnits });
  const statuses = allowedStatuses(detail.status);
  const archived = detail.status === "Archived";
  const returnOptions: Array<{ value: string; label: string }> = [{ value: "Default", label: t.returnDefault }];
  if (detail.sellerCanOverrideReturnPolicy) {
    returnOptions.push({ value: "Custom", label: t.returnCustom });
    if (detail.allowNonReturnableOffers) returnOptions.push({ value: "NonReturnable", label: t.returnNonReturnable });
  }

  return (
    <div className={styles.stack} data-testid="offer-tab-conditions">
      <section className={styles.card}>
        <header className={styles.cardHd}>
          <SectionTitle title={t.quantityTitle} />
        </header>
        <div className={styles.cardBd}>
          <div className={styles.tilesFour}>
            <Tile label={t.unit} value={detail.productUnitName ?? detail.productUnitCode ?? "—"} />
            <Tile label={t.precision} value={format.integer(detail.unitQuantityDecimalPlaces)} />
            <Tile label={t.step} value={detail.unitQuantityStep != null ? format.number(detail.unitQuantityStep, detail.unitQuantityDecimalPlaces) : "—"} />
            <Tile label={t.rounding} value={detail.unitRoundingMode === "Nearest" ? t.roundingNearest : detail.unitRoundingMode ?? "—"} />
          </div>
          <div className={styles.formGrid} style={{ marginTop: 14 }}>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>{t.minQty}</span>
              <input
                className={`${styles.ctl} ${styles.ctlNum} ${styles.ctlLtr}`}
                value={props.minimumOrderQuantity}
                onChange={(event) => props.onMinQtyChange(event.target.value)}
                inputMode="decimal"
                dir="ltr"
                data-testid="offer-min-qty"
              />
            </label>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>{t.maxQty}</span>
              <input
                className={`${styles.ctl} ${styles.ctlNum} ${styles.ctlLtr}`}
                value={props.maximumOrderQuantity}
                onChange={(event) => props.onMaxQtyChange(event.target.value)}
                inputMode="decimal"
                dir="ltr"
                data-testid="offer-max-qty"
              />
            </label>
          </div>
        </div>
      </section>

      <section className={styles.card}>
        <header className={styles.cardHd}>
          <SectionTitle title={t.returnPolicy} />
        </header>
        <div className={styles.cardBd}>
          <label className={styles.field} style={{ maxWidth: 460 }}>
            <span className={styles.fieldLabel}>
              {t.returnPolicy}
              <DirtyDot show={props.dirty} label={t.dirtyTab} />
            </span>
            <select
              className={`${styles.ctl} ${styles.select}`}
              value={props.returnPolicyChoice}
              onChange={(event) => props.onReturnPolicyChange(event.target.value)}
              disabled={!detail.sellerCanOverrideReturnPolicy && props.returnPolicyChoice === "Default"}
              data-testid="offer-return-policy"
            >
              {returnOptions.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
            <span className={styles.fieldHint}>
              {detail.sellerCanOverrideReturnPolicy
                ? props.returnPolicyChoice === "Custom" ? `${format.integer(detail.minReturnWindowDays)}–${format.integer(detail.maxReturnWindowDays)}` : props.returnPolicyChoice === "Default" ? `${t.returnWindowHint}: ${format.integer(detail.defaultReturnWindowDays)}` : ""
                : t.returnSellerCannotOverride}
            </span>
          </label>
          {props.returnPolicyChoice === "Custom" ? (
            <label className={styles.field} style={{ marginTop: 10, maxWidth: 460 }}>
              <span className={styles.fieldLabel}>{t.returnCustomDays}</span>
              <input
                className={`${styles.ctl} ${styles.ctlNum} ${styles.ctlLtr}`}
                value={props.customReturnWindowDays}
                onChange={(event) => props.onCustomReturnWindowChange(event.target.value)}
                inputMode="numeric"
                dir="ltr"
                data-testid="offer-custom-return-days"
              />
            </label>
          ) : null}
        </div>
      </section>

      <section className={styles.card}>
        <header className={styles.cardHd}>
          <SectionTitle title={t.lifecycle} />
        </header>
        <div className={styles.cardBd}>
          <div className={styles.formGrid}>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>{t.lifecycle}</span>
              <select
                className={`${styles.ctl} ${styles.select}`}
                value={props.status}
                onChange={(event) => props.onStatusChange(event.target.value)}
                disabled={archived}
                data-testid="offer-lifecycle"
              >
                {statuses.map((status) => (
                  <option key={status} value={status}>{statusLabel(t, status)}</option>
                ))}
              </select>
              {archived ? <span className={styles.fieldHint}>{t.archivedLocked}</span> : null}
            </label>
            <div className={styles.field}>
              <span className={styles.fieldLabel}>{t.buy}</span>
              <div className={styles.row}>
                <span className={`${styles.pill} ${buyability.buyable ? styles.pillOk : styles.pillWarn}`} data-testid="offer-buyability">
                  <ShieldCheck size={12} aria-hidden />
                  {buyability.buyable ? `${t.buyable} · ${t.buyDisplay}` : t.notBuyable}
                </span>
                <span className={styles.resultMeta}>
                  {t.inventoryAvailable}: {format.number(detail.availableUnits)}
                </span>
              </div>
              {buyability.reason ? <span className={styles.fieldHint}>{buyabilityReason(t, buyability.reason)}</span> : <span className={styles.fieldHint}>{t.buyabilityHint}</span>}
            </div>
          </div>
        </div>
      </section>
      {props.extra ? <div>{props.extra}</div> : null}
    </div>
  );
}

function Tile({ label, value }: { label: string; value: string }) {
  return (
    <div className={styles.tile}>
      <div className={styles.tileK}>{label}</div>
      <div className={styles.tileV} style={{ fontSize: 13 }}>{value}</div>
    </div>
  );
}

function buyabilityReason(t: OfferWorkspaceMessages, reason: string): string {
  switch (reason) {
    case "offer.buyable.no_price":
      return t.buyableReasonNoPrice;
    case "offer.buyable.no_stock":
      return t.buyableReasonNoStock;
    case "offer.buyable.inactive":
      return t.buyableReasonInactive;
    default:
      return reason;
  }
}
