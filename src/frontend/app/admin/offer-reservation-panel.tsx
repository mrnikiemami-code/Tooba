"use client";

import { useEffect, useState } from "react";
import {
  loadOfferReservationPolicies,
  saveReservationPolicy,
  type ReservationPolicyEditorView,
} from "./reservation-policy-api";
import {
  ReservationPolicyEditor,
  draftFromView,
  readReservationWrite,
  type ReservationPolicyDraft,
} from "./reservation-policy-editor.tsx";

export function OfferReservationPanel({
  offers,
  canEdit,
}: {
  offers: { offerId: string; sellerDisplayName?: string }[];
  canEdit: boolean;
}) {
  const [items, setItems] = useState<ReservationPolicyEditorView[]>([]);
  const [drafts, setDrafts] = useState<Record<string, ReservationPolicyDraft>>({});
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function refresh() {
    const result = await loadOfferReservationPolicies(offers.map((row) => row.offerId));
    if (!result.ok) return;
    setItems(result.items);
    const next: Record<string, ReservationPolicyDraft> = {};
    for (const item of result.items) {
      if (item.scopeId) next[item.scopeId] = draftFromView(item);
    }
    setDrafts(next);
  }

  useEffect(() => {
    void refresh();
  }, [offers.map((row) => row.offerId).join(",")]);

  async function onSave(offerId: string) {
    const draft = drafts[offerId];
    if (!draft) return;
    const parsed = readReservationWrite(draft);
    if (!parsed.ok) {
      setError(parsed.message);
      return;
    }
    setBusyId(offerId);
    setError(null);
    const result = await saveReservationPolicy(
      `/v1/admin/settings/reservation-policy/offers/${offerId}`,
      parsed.body,
    );
    setBusyId(null);
    if (!result.ok) {
      setError(result.message ?? "ذخیره سیاست رزرو پیشنهاد انجام نشد.");
      return;
    }
    setItems((current) => current.map((row) => (row.scopeId === offerId ? result.data : row)));
    setDrafts((current) => ({ ...current, [offerId]: draftFromView(result.data) }));
  }

  return (
    <div className="space-y-6" data-testid="offer-reservation-panel">
      <p className="text-sm font-semibold">سیاست رزرو موجودی پیشنهادها</p>
      {items.map((item) => {
        const offerId = item.scopeId ?? "";
        const label = offers.find((row) => row.offerId === offerId)?.sellerDisplayName;
        return (
          <div key={offerId} className="rounded-2xl border border-gray-100 p-4">
            {label ? <p className="mb-3 text-xs text-gray-500">{label}</p> : null}
            {drafts[offerId] ? (
              <ReservationPolicyEditor
                view={item}
                draft={drafts[offerId]}
                onDraftChange={(next) => setDrafts((current) => ({ ...current, [offerId]: next }))}
                canEdit={canEdit}
                busy={busyId === offerId}
                locale="fa"
                onSave={canEdit ? () => void onSave(offerId) : undefined}
                onCancel={canEdit ? () => setDrafts((current) => ({ ...current, [offerId]: draftFromView(item) })) : undefined}
                testId={`offer-reservation-${offerId}`}
              />
            ) : null}
          </div>
        );
      })}
      {error ? <p className="text-xs font-bold text-red-600">{error}</p> : null}
    </div>
  );
}
