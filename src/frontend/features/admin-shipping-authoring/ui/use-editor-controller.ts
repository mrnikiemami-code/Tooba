"use client";

/**
 * React binding of the feature controller (thin): creates it ONCE per draft, exposes its snapshot and
 * disposes it on unmount. All the real orchestration lives in `session/editor-controller.ts`.
 */
import { useCallback, useEffect, useRef, useSyncExternalStore } from "react";
import { loadEditorDocument, saveEditorDocument } from "../api/editor-document-api.ts";
import { createEditorController, type EditorController, type EditorControllerState } from "../session/editor-controller.ts";

const TRANSPORT = { load: loadEditorDocument, save: saveEditorDocument };

/** Stable transport identity so the controller is not recreated per render. */
export function useEditorController(draftId: string): {
  readonly controller: EditorController;
  readonly state: EditorControllerState;
} {
  const controllerRef = useRef<EditorController | null>(null);
  if (controllerRef.current === null) controllerRef.current = createEditorController(draftId, TRANSPORT);

  const subscribe = useCallback(
    (listener: () => void) => controllerRef.current!.subscribe(listener),
    [],
  );
  const snapshot = useCallback(() => controllerRef.current!.getState(), []);
  const state = useSyncExternalStore(subscribe, snapshot, snapshot);

  useEffect(() => {
    controllerRef.current?.begin();
  }, []);

  return { controller: controllerRef.current, state };
}
