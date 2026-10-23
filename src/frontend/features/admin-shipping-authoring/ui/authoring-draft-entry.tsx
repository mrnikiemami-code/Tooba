"use client";

/**
 * Read-only entry from a legacy flat method into the connected atomic editor.
 *
 * The binding is the SERVER-resolved `(serviceCode, optionCode)` handed in by the caller — never a
 * split `methodCode`, never a browser-selected owner. Nothing is created: the authorized draft list is
 * read once on deliberate activation and the EXACT binding must match one draft.
 */
import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { loadAuthoringDraftRows, resolveDraftEntry } from "../api/authoring-lookup-api.ts";
import { authoringMessages, type AuthoringLocale } from "../messages/authoring-messages.ts";

type EntryState =
  | { readonly state: "idle" }
  | { readonly state: "loading" }
  | { readonly state: "none" }
  | { readonly state: "ambiguous" }
  | { readonly state: "failed" };

export function ShippingAuthoringDraftEntry({
  serviceCode,
  optionCode,
  locale,
}: {
  serviceCode: string;
  optionCode: string | null;
  locale: AuthoringLocale;
}) {
  const router = useRouter();
  const messages = authoringMessages(locale);
  const [state, setState] = useState<EntryState>({ state: "idle" });
  const [pending, setPending] = useState(false);

  const open = useCallback(async () => {
    if (pending) return;
    setPending(true);
    setState({ state: "loading" });
    try {
      const outcome = await loadAuthoringDraftRows();
      if (outcome.state === "failure") {
        setState({ state: "failed" });
        return;
      }
      const resolution = resolveDraftEntry(outcome.value, serviceCode, optionCode);
      if (resolution.state === "matched") {
        router.push(`/admin/shipping-services/authoring/${resolution.draftId}?mode=view`);
        return;
      }
      setState({ state: resolution.state });
    } finally {
      setPending(false);
    }
  }, [optionCode, pending, router, serviceCode]);

  return (
    <div className="flex flex-wrap items-center gap-2" data-testid="shipping-authoring-entry">
      <button
        type="button"
        className="inline-flex min-h-9 items-center rounded-lg border border-border px-3 text-xs font-bold text-gray-700 disabled:opacity-50"
        disabled={state.state === "loading"}
        data-testid="shipping-authoring-entry-open"
        onClick={() => void open()}
      >
        {state.state === "loading" ? messages.entryLoading : messages.entryOpen}
      </button>
      <span className="text-[11px] text-gray-500">{messages.entryHint}</span>
      {state.state === "none" ? (
        <p role="status" className="text-[11px] font-semibold text-amber-700">{messages.entryNoDraft}</p>
      ) : null}
      {state.state === "ambiguous" ? (
        <p role="alert" className="text-[11px] font-semibold text-danger">{messages.entryAmbiguous}</p>
      ) : null}
      {state.state === "failed" ? (
        <p role="alert" className="text-[11px] font-semibold text-danger">{messages.entryFailed}</p>
      ) : null}
    </div>
  );
}
