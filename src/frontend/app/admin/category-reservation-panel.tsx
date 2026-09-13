"use client";

import { useEffect, useState } from "react";
import {
  loadReservationPolicy,
  saveReservationPolicy,
} from "./reservation-policy-api";
import {
  ReservationPolicyEditor,
  draftFromView,
  readReservationWrite,
  type ReservationPolicyDraft,
} from "./reservation-policy-editor.tsx";
import type { ReservationPolicyEditorView } from "./reservation-policy-api";

export function CategoryReservationPanel({
  categoryId,
  canEdit,
}: {
  categoryId: string;
  canEdit: boolean;
}) {
  const [view, setView] = useState<ReservationPolicyEditorView | null>(null);
  const [draft, setDraft] = useState<ReservationPolicyDraft>({
    initial: "",
    retry: "",
    max: "",
    inheritInitial: true,
    inheritRetry: true,
    inheritMax: true,
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function refresh() {
    const result = await loadReservationPolicy(`/v1/admin/settings/reservation-policy/categories/${categoryId}`);
    if (result.ok) {
      setView(result.data);
      setDraft(draftFromView(result.data));
    }
  }

  useEffect(() => {
    void refresh();
  }, [categoryId]);

  async function onSave() {
    const parsed = readReservationWrite(draft);
    if (!parsed.ok) {
      setError(parsed.message);
      return;
    }
    setBusy(true);
    setError(null);
    const result = await saveReservationPolicy(
      `/v1/admin/settings/reservation-policy/categories/${categoryId}`,
      parsed.body,
    );
    setBusy(false);
    if (!result.ok) {
      setError(result.message ?? "ذخیره سیاست رزرو انجام نشد.");
      return;
    }
    setView(result.data);
    setDraft(draftFromView(result.data));
  }

  if (!view) {
    return <p className="text-sm text-gray-500">در حال دریافت سیاست رزرو...</p>;
  }

  return (
    <div data-testid="category-reservation-panel">
      <ReservationPolicyEditor
        view={view}
        draft={draft}
        onDraftChange={setDraft}
        canEdit={canEdit}
        busy={busy}
        locale="fa"
        onSave={canEdit ? () => void onSave() : undefined}
        onCancel={canEdit ? () => { setDraft(draftFromView(view)); setError(null); } : undefined}
      />
      {error ? <p className="mt-3 text-xs font-bold text-red-600">{error}</p> : null}
    </div>
  );
}
