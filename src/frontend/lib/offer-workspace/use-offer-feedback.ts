"use client";

import { useState } from "react";
import type { OfferWorkspaceMessages } from "./messages.ts";

export type OfferMessageBox = {
  kind: "duplicate" | "reload" | "error" | "confirm";
  title: string;
  text: string;
  offerId?: string;
};

export function useOfferFeedback(
  t: OfferWorkspaceMessages,
  anyDirty: boolean,
  clearPersistedDraft: () => void,
  onCancel: () => void,
) {
  const [messageBox, setMessageBox] = useState<OfferMessageBox | null>(null);
  function showBlocking(kind: OfferMessageBox["kind"], title: string, text: string, offerId?: string) {
    setMessageBox({ kind, title, text, offerId });
  }
  function handleCancel() {
    if (anyDirty) {
      showBlocking("confirm", t.dirtyExitTitle, t.dirtyExitText);
      return;
    }
    clearPersistedDraft();
    onCancel();
  }
  return { messageBox, setMessageBox, showBlocking, handleCancel };
}
