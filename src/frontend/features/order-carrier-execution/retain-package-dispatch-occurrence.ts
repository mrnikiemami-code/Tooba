/**
 * Decides which parcel occurrence id the UI should hold after a refresh or create response.
 * - Pending/Unknown → keep that occurrence for retry (no new mint).
 * - Succeeded/Failed while canCreate → clear so the next create mints a new occurrence.
 * - Explicit create response always wins when it carries a packageDispatchId.
 */
export function resolveRetainedPackageDispatchId(input: {
  current: string | null | undefined;
  canCreate: boolean;
  createResponsePackageDispatchId?: string | null;
  openOccurrenceId?: string | null;
  openState?: "Pending" | "Succeeded" | "Failed" | "Unknown" | null;
}): string | null {
  const fromResponse =
    typeof input.createResponsePackageDispatchId === "string"
      ? input.createResponsePackageDispatchId.trim()
      : "";
  if (fromResponse.length > 0) {
    return fromResponse;
  }

  const openId =
    typeof input.openOccurrenceId === "string" ? input.openOccurrenceId.trim() : "";
  if (
    openId.length > 0
    && (input.openState === "Pending" || input.openState === "Unknown")
  ) {
    return openId;
  }

  if (input.canCreate && (input.openState === "Succeeded" || input.openState === "Failed" || !input.openState)) {
    return null;
  }

  const current = typeof input.current === "string" ? input.current.trim() : "";
  return current.length > 0 ? current : null;
}
