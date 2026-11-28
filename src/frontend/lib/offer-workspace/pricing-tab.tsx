"use client";

import { useLayoutEffect, useRef } from "react";
import type { OfferWorkspaceMessages } from "./messages";
import type { OfferCurrencyOption, OfferPriceView, OfferWorkspaceDetail } from "./types";
import { currencyDecimalPlaces, validatePrice } from "./model";
import type { OfferFormatters } from "./i18n";
import styles from "./offer-workspace.module.css";
import { DirtyDot, SectionTitle } from "./tab-shared";

export interface PricingTabProps {
  t: OfferWorkspaceMessages;
  format: OfferFormatters;
  detail: OfferWorkspaceDetail;
  prices: OfferPriceView[];
  currencies: OfferCurrencyOption[];
  currencyStatus?: "legacy" | "loading" | "ready" | "unavailable";
  onRetryCurrencies?: () => void;
  amount: string;
  currency: string;
  dirty: boolean;
  onAmountChange: (value: string) => void;
  onCurrencyChange: (code: string) => void;
  onRemove?: () => void;
  onAddCurrency?: () => void;
}

function normalizeAmountRaw(value: string, culture: string): string {
  const parts = new Intl.NumberFormat(culture).formatToParts(12345.6);
  const group = parts.find(part => part.type === "group")?.value;
  const decimal = parts.find(part => part.type === "decimal")?.value;
  const ungrouped = group ? value.split(group).join("") : value;
  return (decimal && decimal !== "." ? ungrouped.replace(decimal, ".") : ungrouped)
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

function AmountInput({ value, onChange, format, invalid, scale, disabled }: { value: string; onChange: (value: string) => void; format: OfferFormatters; invalid: boolean; scale: number; disabled?: boolean }) {
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
        onChange(normalizeAmountRaw(node.value, format.locale.culture));
      }}
      disabled={disabled}
      inputMode={scale === 0 ? "numeric" : "decimal"}
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
  const scale = currencyDecimalPlaces(selected, currencies);
  const unavailable = props.currencyStatus === "loading" || props.currencyStatus === "unavailable";
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
                        <span className={`${styles.pill} ${row.status === "Active" ? styles.pillOk : styles.pillNeutral}`}>{row.status === "Active" ? t.lifecycleActive : row.status === "Draft" ? t.lifecycleDraft : row.status === "Retired" ? t.pricingRetired : "—"}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {props.currencyStatus === "unavailable" ? (
            <div role="alert" className={styles.resultMeta}>{t.pricingUnavailable}{" "}<button type="button" className={`${styles.btn} ${styles.btnSm}`} onClick={props.onRetryCurrencies}>{t.retry}</button></div>
          ) : props.currencyStatus === "loading" ? <p className={styles.resultMeta} role="status">{t.loading}</p> : null}
          <div className={`${styles.formGrid}`} style={{ marginTop: 14, maxWidth: 560 }}>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>{t.pricingCurrency}</span>
              <select
                className={styles.ctl}
                value={selected}
                disabled={unavailable}
                onChange={(event) => props.onCurrencyChange(event.target.value)}
                dir="auto"
                data-testid="offer-price-currency-select"
                aria-label={t.pricingSelectCurrency}
              >
                {currencies.length === 0 || !currencies.some(row => row.code === selected) ? <option value={selected}>{selected || t.pricingSelectCurrency}</option> : null}
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
                invalid={validatePrice(props.amount, selected, currencies) != null}
                scale={scale}
                disabled={unavailable}
              />
              {props.onRemove && props.amount.trim() !== "" ? <button type="button" className={`${styles.btn} ${styles.btnSm} ${styles.btnQuiet}`} disabled={unavailable} onClick={props.onRemove} data-testid="offer-price-remove">{t.pricingRemove}</button> : null}
              {scale === 0 ? <span className={styles.fieldHint}>{t.pricingNoDecimals}</span> : null}
            </label>
          </div>

          {props.onAddCurrency ? <button type="button" className={`${styles.btn} ${styles.btnSm} ${styles.btnPrimary}`} disabled={unavailable} onClick={props.onAddCurrency} data-testid="offer-price-add-currency">{t.pricingAddCurrency}</button> : null}

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
