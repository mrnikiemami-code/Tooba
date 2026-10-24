// One-shot preflight of the Architect-prepared scenario, using the UNCHANGED production decoders.
import { preflight } from "./save-scenario.mjs";

try {
  const result = preflight();
  console.log(JSON.stringify({ ok: true, result }));
} catch (e) {
  console.log(JSON.stringify({ ok: false, error: String(e?.message ?? e) }));
  process.exitCode = 1;
}
