"use client";

/**
 * Stored-destination breadcrumb of the CURRENT document, resolved through the existing Geography reader.
 *
 * Kept as a hook so the screen stays one composition point: the document's own facts prove identity, and
 * this only adds the ancestor LABELS the snapshot does not carry. A superseded read can never win, the
 * map resets with the document, and a failure leaves the chip on its proven terminal name.
 */
import { useEffect, useMemo, useState } from "react";
import { loadGeographyOptions } from "../api/geography-lookup-api.ts";
import type { EditorDocument } from "../model/editor-document.ts";
import { readStoredTrailSegments, storedTrailChains } from "../model/destination-trail-resolver.ts";

export function useStoredTrails(document: EditorDocument | null): ReadonlyMap<string, readonly string[]> {
  const [trails, setTrails] = useState<ReadonlyMap<string, readonly string[]>>(new Map());
  const identity = document === null ? null : `${document.draftId}:${document.revision}`;
  const languageId = document?.presentation.defaultContentLanguageId ?? "";
  // ONE chain list per document read: the effect must not re-run because a render produced a new array.
  const chains = useMemo(
    () => (document === null ? null : storedTrailChains(document.destinationFacts ?? [])),
    [document],
  );

  useEffect(() => {
    const abort = new AbortController();
    setTrails(new Map());
    if (chains === null || chains.length === 0 || languageId.trim().length === 0) {
      return () => abort.abort();
    }
    void (async () => {
      const resolved = await readStoredTrailSegments(
        chains,
        languageId,
        (query) => loadGeographyOptions(query, abort.signal),
        abort.signal,
      );
      if (!abort.signal.aborted) setTrails(resolved);
    })();
    return () => abort.abort();
    // `identity` covers the same draft at a NEW saved revision; `chains`/`languageId` describe that read.
  }, [identity, languageId, chains]);

  return trails;
}
