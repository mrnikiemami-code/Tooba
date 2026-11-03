"use client";

import { useState } from "react";
import { ProductPhysicalSpecificationFields } from "./product-physical-specification-fields";
import {
  PHYSICAL_SPEC_VALIDATION_FA,
  parsePhysicalSpecificationDraft,
  physicalDraftFromValues,
  type PhysicalSpecificationDraft,
  type PhysicalSpecificationValues,
} from "./product-physical-specification";

/**
 * Isolated save/read fixture around the real physical editor fields.
 * Used by mount tests; not a redesign of the workspace layout.
 */
export function ProductPhysicalSpecificationRoundtripFixture({
  productId,
  initial,
  expectedUpdatedAt = "2026-11-03T00:00:00.000Z",
}: {
  productId: string;
  initial: PhysicalSpecificationValues;
  expectedUpdatedAt?: string;
}) {
  const [draft, setDraft] = useState<PhysicalSpecificationDraft>(() => physicalDraftFromValues(initial));
  const [error, setError] = useState<string | null>(null);
  const [lastSaved, setLastSaved] = useState<PhysicalSpecificationValues | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSave() {
    setError(null);
    const parsed = parsePhysicalSpecificationDraft(draft);
    if (!parsed.ok) {
      setError(PHYSICAL_SPEC_VALIDATION_FA);
      return;
    }
    setBusy(true);
    try {
      const response = await fetch(`/v1/admin/products/${productId}/physical-specification`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          weightGrams: parsed.values.weightGrams,
          lengthCentimeters: parsed.values.lengthCentimeters,
          widthCentimeters: parsed.values.widthCentimeters,
          heightCentimeters: parsed.values.heightCentimeters,
          expectedUpdatedAt,
        }),
      });
      if (!response.ok) {
        const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
        setError(body?.errorCode ?? "workspace.physical_specification.rejected");
        return;
      }
      const body = (await response.json()) as Partial<PhysicalSpecificationValues>;
      const reloaded: PhysicalSpecificationValues = {
        weightGrams: body.weightGrams ?? null,
        lengthCentimeters: body.lengthCentimeters ?? null,
        widthCentimeters: body.widthCentimeters ?? null,
        heightCentimeters: body.heightCentimeters ?? null,
      };
      setLastSaved(reloaded);
      setDraft(physicalDraftFromValues(reloaded));
    } catch {
      setError("workspace.host.unreachable");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div data-testid="product-physical-roundtrip-fixture">
      <ProductPhysicalSpecificationFields draft={draft} onChange={setDraft} />
      <button
        type="button"
        className="mt-3 min-h-11 rounded-ds border border-border px-3 text-sm font-medium"
        disabled={busy}
        data-testid="product-edit-physical-save"
        onClick={() => void onSave()}
      >
        ذخیره مشخصات فیزیکی
      </button>
      {error ? (
        <p className="mt-2 text-sm text-danger" data-testid="product-edit-physical-error">
          {error}
        </p>
      ) : null}
      {lastSaved ? (
        <pre className="mt-2 text-xs" data-testid="product-edit-physical-saved-echo" dir="ltr">
          {JSON.stringify(lastSaved)}
        </pre>
      ) : null}
    </div>
  );
}
