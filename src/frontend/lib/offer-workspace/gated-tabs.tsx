"use client";

import type { OfferWorkspaceMessages } from "./messages";
import { Note, TabHeading } from "./tab-shared";

export function DiscountsTab({ t }: { t: OfferWorkspaceMessages }) {
  return (
    <div data-testid="offer-tab-discounts">
      <TabHeading title={t.discountsTab} />
      <div className="grid gap-2">
        <Note tone="warning" >{t.discountsGated}</Note>
        <Note>{t.discountsOwner}</Note>
      </div>
    </div>
  );
}

export function HistoryTab({ t }: { t: OfferWorkspaceMessages }) {
  return (
    <div data-testid="offer-tab-history">
      <TabHeading title={t.historyTab} />
      <div className="grid gap-2">
        <Note tone="warning">{t.historyGated}</Note>
        <Note>{t.historyNoFake}</Note>
      </div>
    </div>
  );
}

export function NeedsCreateTab({ t, title }: { t: OfferWorkspaceMessages; title: string }) {
  return (
    <div data-testid="offer-tab-needs-create">
      <TabHeading title={title} />
      <Note tone="warning">{t.tabNeedsCreate}</Note>
    </div>
  );
}
