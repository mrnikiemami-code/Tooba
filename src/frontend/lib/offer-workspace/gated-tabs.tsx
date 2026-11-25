"use client";

import { ChevronLeft, ChevronRight, History, Percent } from "lucide-react";
import type { OfferHistoryPage } from "./types";
import type { OfferFormatters } from "./i18n";
import type { OfferWorkspaceMessages } from "./messages";
import styles from "./offer-workspace.module.css";
import { Note, SectionTitle } from "./tab-shared";

export function DiscountsTab({ t }: { t: OfferWorkspaceMessages }) {
  return (
    <div className={styles.stack} data-testid="offer-tab-discounts">
      <section className={styles.card}>
        <header className={styles.cardHd}>
          <SectionTitle title={t.discountsTitle} desc={t.discountsDesc} />
        </header>
        <div className={styles.cardBd}>
          <div className={styles.empty}>
            <span className={styles.emptyIc}><Percent size={16} aria-hidden /></span>
            <span className={styles.emptyT}>{t.discountsEmpty}</span>
            <span className={styles.emptyD}>{t.discountsUnavailable}</span>
          </div>
          <Note tone="info">{t.discountsUnavailable}</Note>
        </div>
      </section>
    </div>
  );
}

export function HistoryTab({ t, format, data, loading, error, onPrev, onNext }: { t: OfferWorkspaceMessages; format: OfferFormatters; data: OfferHistoryPage | null; loading: boolean; error: string | null; onPrev: () => void; onNext: () => void }) {
  const items = data?.items ?? [];
  return (
    <div className={styles.stack} data-testid="offer-tab-history">
      <section className={styles.card}>
        <header className={styles.cardHd}>
          <SectionTitle title={t.historyTitle} desc={t.historyDesc} />
        </header>
        <div className={styles.cardBd}>
          {loading ? <div className={styles.resultMeta}>{t.loading}</div> : null}
          {error ? <Note tone="warning">{t.perTabError}</Note> : null}
          {!loading && !error && items.length === 0 ? (
            <div className={styles.empty} data-testid="offer-history-empty">
              <span className={styles.emptyIc}><History size={16} aria-hidden /></span>
              <span className={styles.emptyT}>{t.historyEmpty}</span>
              <span className={styles.emptyD}>{t.historyOnlyEditDesc}</span>
            </div>
          ) : null}
          {items.length > 0 ? (
            <div className={styles.stack} data-testid="offer-history-list">
              {items.map((item) => (
                <div key={`${item.offerId}-${item.occurredAt}-${item.kind}`} className={styles.historyRow}>
                  <div className={styles.historyKind}>{item.kind}</div>
                  <div className={styles.historySummary}>{item.summary}</div>
                  <div className={styles.historyMeta}>{format.dateTime(item.occurredAt)}</div>
                  {(item.oldValue || item.newValue) ? <div className={styles.historyDelta} dir="ltr">{item.oldValue ?? "—"} → {item.newValue ?? "—"}</div> : null}
                </div>
              ))}
              <div className={styles.row} style={{ justifyContent: "space-between" }}>
                <span className={styles.resultMeta}>{format.integer(data?.page ?? 1)} / {format.integer(Math.max(1, Math.ceil((data?.totalCount ?? 0) / (data?.pageSize ?? 20))))}</span>
                <span style={{ display: "flex", gap: 6 }}>
                  <button type="button" className={`${styles.btn} ${styles.btnSm}`} onClick={onPrev} disabled={(data?.page ?? 1) <= 1}><ChevronRight size={12} aria-hidden /></button>
                  <button type="button" className={`${styles.btn} ${styles.btnSm}`} onClick={onNext} disabled={((data?.page ?? 1) * (data?.pageSize ?? 20)) >= (data?.totalCount ?? 0)}><ChevronLeft size={12} aria-hidden /></button>
                </span>
              </div>
            </div>
          ) : null}
        </div>
      </section>
    </div>
  );
}
export function NeedsCreateTab({ t, title }: { t: OfferWorkspaceMessages; title: string }) {
  return (
    <div className={styles.stack} data-testid="offer-tab-needs-create">
      <section className={styles.card}>
        <header className={styles.cardHd}>
          <SectionTitle title={title} />
        </header>
        <div className={styles.cardBd}>
          <Note tone="info">{t.tabNeedsCreate}</Note>
        </div>
      </section>
    </div>
  );
}
