"use client";

import { BadgeCheck, Info } from "lucide-react";
import type { OfferWorkspaceMessages } from "./messages";
import type { OfferWorkspaceDetail } from "./types";
import { currencyLabel, digitsFa, faMoney, marketLabel, parseDecimal } from "./model";
import styles from "./offer-workspace.module.css";
import { DirtyDot, SectionTitle } from "./tab-shared";

export interface PricingTabProps {
  t: OfferWorkspaceMessages;
  locale: "fa" | "en";
  detail: OfferWorkspaceDetail;
  amount: string;
  currency: string;
  dirty: boolean;
  onAmountChange: (value: string) => void;
}

export function PricingTab(props: PricingTabProps) {
  const { t, locale, detail } = props;
  const currency = detail.currency || "IRR";
  const amount = detail.amount;
  const legacyDecimals = amount != null && (currency || "IRR").toUpperCase() === "IRR" && !Number.isInteger(amount);
  const parsed = parseDecimal(props.amount);
  const preview = parsed != null && parsed >= 0 ? faMoney(parsed, currency, locale) : null;
  return (
    <div className={styles.stack} data-testid="offer-tab-pricing">
      <section className={styles.card}>
        <header className={styles.cardHd}>
          <SectionTitle title={t.pricingTitle} desc={t.pricingDesc} />
        </header>
        <div className={styles.cardBd}>
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>{t.pricingCurrency}</th>
                  <th>{t.pricingAmount}</th>
                  <th>{t.market}</th>
                  <th>{t.channel}</th>
                  <th>{t.pricingType}</th>
                  <th>{t.pricingStatus}</th>
                  <th>{t.pricingValidity}</th>
                </tr>
              </thead>
              <tbody>
                <tr data-testid="offer-price-row">
                  <td data-label={t.pricingCurrency}>
                    <span className={styles.chip} dir="ltr">{currency}</span>{" "}
                    <span className={styles.resultMeta}>{currencyLabel(currency, locale)}</span>
                  </td>
                  <td data-label={t.pricingAmount} className={`${styles.numCell} ${styles.strongCell}`}>
                    {amount == null ? "—" : faMoney(amount, currency, locale)}
                  </td>
                  <td data-label={t.market}>{marketLabel("IR", locale)}</td>
                  <td data-label={t.channel}>{t.channelMarketplace}</td>
                  <td data-label={t.pricingType}>{t.pricingTypeBase}</td>
                  <td data-label={t.pricingStatus}>
                    <span className={`${styles.pill} ${amount == null ? styles.pillNeutral : styles.pillOk}`}>
                      {amount == null ? "—" : t.lifecycleActive}
                    </span>
                  </td>
                  <td data-label={t.pricingValidity}>—</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div className={`${styles.formGrid}`} style={{ marginTop: 14, maxWidth: 560 }}>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>
                {t.pricingAmount}
                <DirtyDot show={props.dirty} label={t.dirtyTab} />
              </span>
              <input
                className={`${styles.ctl} ${styles.ctlNum} ${styles.ctlLtr}`}
                value={props.amount}
                onChange={(event) => props.onAmountChange(event.target.value)}
                inputMode="numeric"
                dir="ltr"
                aria-invalid={parsed != null && parsed < 0}
                data-testid="offer-price-input"
              />
              <span className={styles.fieldHint}>{t.pricingNoDecimals}</span>
            </label>
            <div className={styles.field}>
              <span className={styles.fieldLabel}>{t.pricingCurrency}</span>
              <div className={`${styles.ctl} ${styles.ctlReadonly}`} style={{ display: "flex", alignItems: "center", gap: 6 }}>
                <span dir="ltr">{currency}</span>
                <span className={styles.resultMeta}>· {currencyLabel(currency, locale)}</span>
              </div>
              <span className={styles.fieldHint}>{t.pricingCurrent}</span>
            </div>
          </div>

          {preview ? (
            <p className={styles.resultMeta} style={{ marginTop: 8 }} data-testid="offer-price-preview">
              <BadgeCheck size={12} aria-hidden /> {digitsFa(preview)}
            </p>
          ) : null}
          {legacyDecimals ? (
            <div className={`${styles.banner} ${styles.bannerWarn}`} style={{ marginTop: 12 }}>
              <Info size={14} aria-hidden />
              <div>{t.pricingLegacyDecimals}</div>
            </div>
          ) : null}
          <p className={styles.resultMeta} style={{ marginTop: 12 }}>{t.pricingUnavailable}</p>
        </div>
      </section>
    </div>
  );
}
