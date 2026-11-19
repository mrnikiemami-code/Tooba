/**
 * Shared durable Story mutation executor — freeze intent, fence epoch, one in-flight lock.
 */
import {
  clearStoryIntentIfCurrent,
  storeStoryIntent,
  type StoryIntentEnvelope,
} from "./story-intent";

export type StoryExecutorGate = {
  inFlightRef: { current: boolean };
  epochRef: { current: number };
};

export type StoryExecutorResult<T> =
  | { state: "ok"; data: T; envelope: StoryIntentEnvelope }
  | { state: "blocked"; reason: "in_flight" | "storage" | "actor" | "stale_epoch" }
  | { state: "failed"; message: string; envelope: StoryIntentEnvelope };

export async function runDurableStoryMutation<T>(args: {
  gate: StoryExecutorGate;
  envelope: StoryIntentEnvelope;
  send: () => Promise<{ ok: boolean; data?: T; message?: string }>;
}): Promise<StoryExecutorResult<T>> {
  if (args.gate.inFlightRef.current) return { state: "blocked", reason: "in_flight" };
  if (!args.envelope.actorUserId?.trim()) return { state: "blocked", reason: "actor" };
  if (!storeStoryIntent(args.envelope)) return { state: "blocked", reason: "storage" };

  args.gate.inFlightRef.current = true;
  const started = args.gate.epochRef.current;
  try {
    const result = await args.send();
    if (started !== args.gate.epochRef.current) return { state: "blocked", reason: "stale_epoch" };
    if (!result.ok) {
      return {
        state: "failed",
        message: result.message ?? "اقدام ممکن نشد.",
        envelope: args.envelope,
      };
    }
    clearStoryIntentIfCurrent(args.envelope);
    return { state: "ok", data: result.data as T, envelope: args.envelope };
  } finally {
    if (started === args.gate.epochRef.current) args.gate.inFlightRef.current = false;
  }
}
