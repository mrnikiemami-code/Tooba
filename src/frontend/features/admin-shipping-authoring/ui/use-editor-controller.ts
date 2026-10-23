"use client";

/**
 * React binding of the feature controller (thin): ONE live controller per draft.
 *
 * The binding owns the real lifecycle the pure controller cannot:
 * - a different draft id replaces (and disposes) the previous controller, so a reused screen can
 *   never keep serving the old draft;
 * - unmount retires the controller, and every completion it reports afterwards is ignored by the
 *   controller's own epoch/request identity;
 * - React StrictMode's setup -> cleanup -> setup re-creates a live controller instead of reviving a
 *   permanently disposed one, so the initial load still happens (and is not lost forever).
 */
import { useCallback, useEffect, useRef, useState, useSyncExternalStore } from "react";
import { loadEditorDocument, saveEditorDocument } from "../api/editor-document-api.ts";
import { createEditorController, type EditorController, type EditorControllerState } from "../session/editor-controller.ts";

const TRANSPORT = { load: loadEditorDocument, save: saveEditorDocument };

interface ControllerSlot {
  readonly draftId: string;
  readonly controller: EditorController;
  disposed: boolean;
}

function createSlot(draftId: string): ControllerSlot {
  return { draftId, controller: createEditorController(draftId, TRANSPORT), disposed: false };
}

/**
 * Returns the controller for THIS draft plus its observable snapshot.
 *
 * @param draftId Draft id from the route.
 */
export function useEditorController(draftId: string): {
  readonly controller: EditorController;
  readonly state: EditorControllerState;
} {
  const slotRef = useRef<ControllerSlot | null>(null);
  if (slotRef.current === null || slotRef.current.draftId !== draftId) {
    // A different draft never inherits the previous session; retire it first.
    if (slotRef.current) {
      slotRef.current.disposed = true;
      slotRef.current.controller.dispose();
    }
    slotRef.current = createSlot(draftId);
  }

  const [generation, setGeneration] = useState(0);

  useEffect(() => {
    let slot = slotRef.current;
    if (slot === null || slot.draftId !== draftId) {
      slot = createSlot(draftId);
      slotRef.current = slot;
    }
    if (slot.disposed) {
      // StrictMode or a remounted effect: never reuse a permanently disposed controller.
      slot = createSlot(draftId);
      slotRef.current = slot;
      setGeneration((value) => value + 1);
    }
    const live = slot;
    live.controller.begin();
    return () => {
      live.disposed = true;
      live.controller.dispose();
    };
  }, [draftId]);

  const active = slotRef.current!;
  const subscribe = useCallback(
    (listener: () => void) => active.controller.subscribe(listener),
    [active, generation],
  );
  const snapshot = useCallback(() => active.controller.getState(), [active, generation]);
  const state = useSyncExternalStore(subscribe, snapshot, snapshot);

  return { controller: active.controller, state };
}
