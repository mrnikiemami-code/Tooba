"use client";

import { History, Percent } from "lucide-react";
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

export function HistoryTab({ t }: { t: OfferWorkspaceMessages }) {
  return (
    <div className={styles.stack} data-testid="offer-tab-history">
      <section className={styles.card}>
        <header className={styles.cardHd}>
          <SectionTitle title={t.historyTitle} desc={t.historyDesc} />
        </header>
        <div className={styles.cardBd}>
          <div className={styles.empty} data-testid="offer-history-empty">
            <span className={styles.emptyIc}><History size={16} aria-hidden /></span>
            <span className={styles.emptyT}>{t.historyEmpty}</span>
            <span className={styles.emptyD}>{t.historyOnlyEditDesc}</span>
          </div>
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
