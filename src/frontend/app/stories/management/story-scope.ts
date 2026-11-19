/**
 * Same-tab Story scope — subscribe to VendorShell / admin actor+shop changes without polling.
 */
import {
  ACTOR_STORAGE_KEY,
  SELLER_PARTY_STORAGE_KEY,
  readActorUserId,
  readSellerPartyId,
} from "../../vendor-panel/seller-api";
import { ADMIN_ACTOR_STORAGE_KEY, DEFAULT_ADMIN_ACTOR_ID } from "../../../lib/admin/admin-result";

export const STORY_SCOPE_EVENT = "tooba.story.scope.changed";

export type StoryScopeSnapshot = {
  role: "admin" | "seller";
  actorUserId: string;
  shopId: string | null;
};

export function readStoryScope(role: "admin" | "seller"): StoryScopeSnapshot {
  if (typeof window === "undefined") {
    return { role, actorUserId: "", shopId: null };
  }
  if (role === "seller") {
    return {
      role,
      actorUserId: readActorUserId() ?? "",
      shopId: readSellerPartyId(window.location.search),
    };
  }
  return {
    role,
    actorUserId: window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY) ?? DEFAULT_ADMIN_ACTOR_ID,
    shopId: null,
  };
}

export function emitStoryScopeChanged(): void {
  if (typeof window === "undefined") return;
  window.dispatchEvent(new CustomEvent(STORY_SCOPE_EVENT));
}

export function subscribeStoryScope(
  role: "admin" | "seller",
  onChange: (snapshot: StoryScopeSnapshot) => void,
): () => void {
  if (typeof window === "undefined") return () => undefined;

  const notify = () => onChange(readStoryScope(role));

  const onStorage = (event: StorageEvent) => {
    if (role === "seller") {
      if (event.key === SELLER_PARTY_STORAGE_KEY || event.key === ACTOR_STORAGE_KEY || event.key === null) {
        notify();
      }
      return;
    }
    if (event.key === ADMIN_ACTOR_STORAGE_KEY || event.key === null) notify();
  };

  window.addEventListener("storage", onStorage);
  window.addEventListener(STORY_SCOPE_EVENT, notify);
  return () => {
    window.removeEventListener("storage", onStorage);
    window.removeEventListener(STORY_SCOPE_EVENT, notify);
  };
}
