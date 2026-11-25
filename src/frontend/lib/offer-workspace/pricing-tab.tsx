"use client";

import { useLayoutEffect, useRef } from "react";
import { BadgeCheck, Info } from "lucide-react";
import type { OfferWorkspaceMessages } from "./messages";
import type { OfferWorkspaceDetail } from "./types";
import { parseDecimal } from "./model";
import type { OfferFormatters } from "./i18n";
import styles from "./offer-workspace.module.css";
import { DirtyDot, SectionTitle } from "./tab-shared";

export interface PricingTabProps {
  t: OfferWorkspaceMessages;
  format: OfferFormatters;
  detail: OfferWorkspaceDetail;
  amount: string;
  currency: string;
  dirty: boolean;
  onAmountChange: (value: string) => void;
}

function normalizeAmountRaw(value: string): string {
  return value
    .replace(/[۰-۹]/g, (digit) => String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit)))
    .replace(/[٠-٩]/g, (digit) => String("٠١٢٣٤٥٦٧٨٩".indexOf(digit)))
    .replace(/[٬,\s]/g, "")
    .replace(/٫/g, ".");
}

function numericCount(value: string): number {
  return (value.match(/[0-9۰-۹٠-٩.,٫]/g) ?? []).length;
}

function positionAfterNumeric(display: string, count: number): number {
  if (count <= 0) return 0;
  let seen = 0;
  for (let index = 0; index < display.length; index += 1) {
    if (/[0-9۰-۹٠-٩.,٫]/.test(display[index]!)) {
      seen += 1;
      if (seen === count) return index + 1;
    }
  }
  return display.length;
}

function AmountInput({ value, onChange, format, invalid }: { value: string; onChange: (value: string) => void; format: OfferFormatters; invalid: boolean }) {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const pendingCaret = useRef<number | null>(null);
  const display = format.amountInput(value);

  useLayoutEffect(() => {
    if (pendingCaret.current == null) return;
    const node = inputRef.current;
    if (!node) return;
    const position = positionAfterNumeric(display, pendingCaret.current);
    node.setSelectionRange(position, position);
    pendingCaret.current = null;
  }, [display]);

  return (
    <input
      ref={inputRef}
      className={`${styles.ctl} ${styles.ctlNum} ${styles.ctlLtr}`}
      value={display}
      onChange={(event) => {
        const node = event.currentTarget;
        const caret = node.selectionStart ?? node.value.length;
        pendingCaret.current = numericCount(node.value.slice(0, caret));
        onChange(normalizeAmountRaw(node.value));
      }}
      inputMode="decimal"
      autoComplete="off"
      dir="ltr"
      aria-invalid={invalid}
      data-testid="offer-price-input"
    />
  );
}

export function PricingTab(props: PricingTabProps) {
  const { t, format, detail } = props;
  const currency = detail.currency || "IRR";
  const amount = detail.amount;
  const legacyDecimals = amount != null && (currency || "IRR").toUpperCase() === "IRR" && !Number.isInteger(amount);
  const parsed = parseDecimal(props.amount);
  const preview = parsed != null && parsed >= 0 ? format.money(parsed, currency) : null;
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
                    <span className={styles.resultMeta}>{format.currencyName(currency)}</span>
                  </td>
                  <td data-label={t.pricingAmount} className={`${styles.numCell} ${styles.strongCell}`}>
                    {amount == null ? "—" : format.money(amount, currency)}
                  </td>
                  <td data-label={t.market}>{format.regionName("IR")}</td>
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
              <AmountInput
                value={props.amount}
                onChange={props.onAmountChange}
                format={format}
                invalid={parsed != null && parsed < 0}
              />
              <span className={styles.fieldHint}>{t.pricingNoDecimals}</span>
            </label>
            <div className={styles.field}>
              <span className={styles.fieldLabel}>{t.pricingCurrency}</span>
              <div className={`${styles.ctl} ${styles.ctlReadonly}`} style={{ display: "flex", alignItems: "center", gap: 6 }}>
                <span dir="ltr">{currency}</span>
                <span className={styles.resultMeta}>· {format.currencyName(currency)}</span>
              </div>
              <span className={styles.fieldHint}>{t.pricingCurrent}</span>
            </div>
          </div>

          {preview ? (
            <p className={styles.resultMeta} style={{ marginTop: 8 }} data-testid="offer-price-preview">
              <BadgeCheck size={12} aria-hidden /> {preview}
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
