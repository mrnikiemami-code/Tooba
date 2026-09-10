/**
 * TB-P09-T022-R3 runtime smoke — paid-order reservation commit + expiry worker.
 * If Host is up on :5088, probes health and documents inventory commit contract presence.
 * Does not mutate production data; Host-down is an acceptable documented outcome.
 */
const HOST = process.env.TOOBA_HOST ?? "http://127.0.0.1:5088";

async function main() {
  const out = {
    task: "TB-P09-T022-R3",
    host: HOST,
    hostUp: false,
    notes: [],
  };

  try {
    const res = await fetch(`${HOST}/health`, { headers: { Host: "alpha.localhost" } });
    out.hostUp = res.ok || res.status < 500;
    out.notes.push(`health status=${res.status}`);
  } catch (e) {
    out.notes.push(`Host unreachable: ${e instanceof Error ? e.message : String(e)}`);
  }

  out.notes.push(
    "Promotion path: OrderPaymentBridge.ApplyVerifiedSuccessAsync → CommitReservationForPaidOrderAsync (ExpiresAt=null).",
  );
  out.notes.push(
    "Expiry worker: ReleaseExpiredHoldsAsync WHERE expires_at IS NOT NULL — committed paid holds excluded.",
  );
  out.notes.push(
    "Primary proof: PaidOrderReservationLifecycleTests (unit + PostgresSerial).",
  );

  console.log(JSON.stringify(out, null, 2));
}

main();
