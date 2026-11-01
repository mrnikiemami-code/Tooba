/**
 * TB-241: offered lengths of ONE receiving slot, in minutes.
 * Shared by the week editor UI and focused tests so the constant cannot drift.
 */
export const SLOT_DURATION_OPTIONS = [15, 20, 30, 45, 60, 90, 120] as const;
