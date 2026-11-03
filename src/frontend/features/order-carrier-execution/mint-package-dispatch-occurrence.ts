/**
 * Parcel occurrence ids are UuidV7 (RFC 9562), matching server MintOccurrenceId.
 * Client may mint before the first create POST so retries share one identity.
 */
export function mintPackageDispatchOccurrenceId(nowMs: number = Date.now()): string {
  const bytes = new Uint8Array(16);
  const ms = BigInt(Math.max(0, Math.floor(nowMs)));
  const mask = BigInt(0xff);
  // 48-bit unix milliseconds in big-endian across bytes 0..5
  bytes[0] = Number((ms >> BigInt(40)) & mask);
  bytes[1] = Number((ms >> BigInt(32)) & mask);
  bytes[2] = Number((ms >> BigInt(24)) & mask);
  bytes[3] = Number((ms >> BigInt(16)) & mask);
  bytes[4] = Number((ms >> BigInt(8)) & mask);
  bytes[5] = Number(ms & mask);

  const rand = new Uint8Array(10);
  if (typeof crypto !== "undefined" && typeof crypto.getRandomValues === "function") {
    crypto.getRandomValues(rand);
  } else {
    for (let i = 0; i < rand.length; i += 1) rand[i] = Math.floor(Math.random() * 256);
  }

  bytes[6] = (rand[0] & 0x0f) | 0x70; // version 7
  bytes[7] = rand[1];
  bytes[8] = (rand[2] & 0x3f) | 0x80; // RFC variant
  bytes.set(rand.subarray(3, 10), 9);

  const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join("");
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

/** Keep an existing non-empty locator; otherwise mint a new UuidV7. Never mint solely because of remount. */
export function ensurePackageDispatchOccurrence(existing: string | null | undefined): string {
  const trimmed = typeof existing === "string" ? existing.trim() : "";
  if (trimmed.length > 0) return trimmed;
  return mintPackageDispatchOccurrenceId();
}
