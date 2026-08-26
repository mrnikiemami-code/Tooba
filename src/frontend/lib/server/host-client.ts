import { cookies } from "next/headers";
import {
  DEFAULT_DEV_ACTOR_ID,
  DEV_ACTOR_HEADER,
  SESSION_COOKIE_NAME,
} from "../auth/constants.ts";

const hostOrigin = process.env.TOOBA_HOST_ORIGIN ?? "http://127.0.0.1:5088";

export function hostBaseUrl(): string {
  return hostOrigin.replace(/\/$/, "");
}

export async function readSessionId(): Promise<string | undefined> {
  const jar = await cookies();
  return jar.get(SESSION_COOKIE_NAME)?.value;
}

export function isDevActorAllowed(): boolean {
  return process.env.NODE_ENV !== "production";
}

export async function buildUpstreamAuthHeaders(json = false): Promise<Record<string, string>> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (json) headers["Content-Type"] = "application/json";
  const sessionId = await readSessionId();
  if (sessionId) {
    headers.Authorization = `Bearer ${sessionId}`;
    return headers;
  }
  if (isDevActorAllowed()) {
    headers[DEV_ACTOR_HEADER] = DEFAULT_DEV_ACTOR_ID;
  }
  return headers;
}

export async function forwardToHost(
  path: string,
  init: RequestInit & { json?: boolean } = {},
): Promise<Response> {
  const headers = await buildUpstreamAuthHeaders(init.json ?? Boolean(init.body));
  const merged = new Headers(init.headers);
  for (const [key, value] of Object.entries(headers)) {
    merged.set(key, value);
  }
  return fetch(`${hostBaseUrl()}${path}`, { ...init, headers: merged, cache: "no-store" });
}

export async function readHostJson(path: string, init?: RequestInit): Promise<{ status: number; payload: unknown }> {
  const response = await forwardToHost(path, init);
  const payload = await response.json().catch(() => null);
  return { status: response.status, payload };
}
