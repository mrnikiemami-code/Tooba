"use client";

import { useLayoutEffect, useRef } from "react";
import type { OfferWorkspaceMessages } from "./messages";
import type { OfferCurrencyOption, OfferPriceView, OfferWorkspaceDetail } from "./types";
import { parseDecimal } from "./model";
import type { OfferFormatters } from "./i18n";
import styles from "./offer-workspace.module.css";
import { DirtyDot, SectionTitle } from "./tab-shared";

export interface PricingTabProps {
  t: OfferWorkspaceMessages;
  format: OfferFormatters;
  detail: OfferWorkspaceDetail;
  prices: OfferPriceView[];
  currencies: OfferCurrencyOption[];
  amount: string;
  currency: string;
  dirty: boolean;
  onAmountChange: (value: string) => void;
  onCurrencyChange: (code: string) => void;
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
  const { t, format, detail, prices, currencies } = props;
  const selected = (props.currency || detail.currency || "").toUpperCase();
  const parsed = parseDecimal(props.amount);
  const selectedCurrency = currencies.find((row) => row.code.toUpperCase() === selected);
  const scale = selectedCurrency?.decimalPlaces ?? (selected === "IRR" ? 0 : 2);
  return (
    <div className={styles.stack} data-testid="offer-tab-pricing">
      <section className={styles.card}>
        <header className={styles.cardHd}>
          <SectionTitle title={t.pricingTitle} />
        </header>
        <div className={styles.cardBd}>
          {prices.length === 0 ? (
            <p className={styles.resultMeta} data-testid="offer-price-empty">{t.pricingNoPrices}</p>
          ) : (
            <div className={styles.tableWrap}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>{t.pricingCurrency}</th>
                    <th>{t.pricingAmount}</th>
                    <th>{t.pricingStatus}</th>
                  </tr>
                </thead>
                <tbody>
                  {prices.map((row) => (
                    <tr key={row.currency} data-testid="offer-price-row" data-currency={row.currency}>
                      <td data-label={t.pricingCurrency}>
                        <span className={styles.chip} dir="ltr">{row.currency}</span>{" "}
                        <span className={styles.resultMeta}>{format.currencyName(row.currency)}</span>
                      </td>
                      <td data-label={t.pricingAmount} className={`${styles.numCell} ${styles.strongCell}`}>
                        {format.money(row.amount, row.currency)}
                      </td>
                      <td data-label={t.pricingStatus}>
                        <span className={`${styles.pill} ${styles.pillOk}`}>{t.lifecycleActive}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <div className={`${styles.formGrid}`} style={{ marginTop: 14, maxWidth: 560 }}>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>{t.pricingCurrency}</span>
              <select
                className={styles.ctl}
                value={selected}
                onChange={(event) => props.onCurrencyChange(event.target.value)}
                dir="auto"
                data-testid="offer-price-currency-select"
                aria-label={t.pricingSelectCurrency}
              >
                {currencies.length === 0 ? <option value={selected}>{selected || t.pricingStoreCurrency}</option> : null}
                {currencies.map((row) => (
                  <option key={row.code} value={row.code}>{row.code} — {format.currencyName(row.code) || row.englishName}</option>
                ))}
              </select>
            </label>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>
                {t.pricingAmount}
                <DirtyDot show={props.dirty} label={t.dirtyTab} />
              </span>
              <AmountInput
                value={props.amount}
                onChange={props.onAmountChange}
                format={format}
                invalid={parsed != null && (parsed < 0 || (scale === 0 && !Number.isInteger(parsed)))}
              />
              {scale === 0 ? <span className={styles.fieldHint}>{t.pricingNoDecimals}</span> : null}
            </label>
          </div>

          {detail.amount != null && detail.currency && selected !== detail.currency.toUpperCase() ? (
            <p className={styles.resultMeta} style={{ marginTop: 8 }} data-testid="offer-price-primary">
              {t.pricingPrimaryPrefix}: {format.money(detail.amount, detail.currency)}
            </p>
          ) : null}
        </div>
      </section>
    </div>
  );
}
