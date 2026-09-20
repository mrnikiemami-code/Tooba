/**
 * Shared admin HTTP result/auth header primitives (technical lib — not capability UI).
 */
export const ADMIN_DEV_ACTOR_HEADER = "X-Tooba-Dev-Actor-User-Id";
export const ADMIN_ACTOR_STORAGE_KEY = "tooba.adminActorUserId";
export const DEFAULT_ADMIN_ACTOR_ID = "";

export type AdminLoadState = "ok" | "denied" | "error";

export interface AdminResult<T> {
  state: AdminLoadState;
  data: T | null;
  status: number;
  message?: string;
}
