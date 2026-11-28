// TB-296 P1-FINAL-CLOSURE — D2-PARTIAL executable oracle.
// Real Inventory owner release/consume/partial-obligation release through the existing owner-contract
// harness against tooba_alpha, on dedicated P1C fixtures with exact cleanup. No bypass/product change.
const fs = require("node:fs");
const path = require("node:path");
const os = require("node:os");
const { execFileSync } = require("node:child_process");
const L = require("./finalacceptance-d2-lib.cjs");
const { check } = L;

function readConn() {
  if (process.env.P1C_DB_CONNECTION) return process.env.P1C_DB_CONNECTION;
  const settings = JSON.parse(fs.readFileSync(path.resolve(__dirname, "../../../../src/backend/Host/Tooba.Host/appsettings.Development.json"), "utf8"));
  const c = settings && settings.Tooba && settings.Tooba.PostgreSQL && settings.Tooba.PostgreSQL.ConnectionReferences && settings.Tooba.PostgreSQL.ConnectionReferences["tenant-alpha"];
  check(c, "tenant-alpha connection reference missing");
  return c;
}
const CONN = readConn();
const HARNESS = process.env.P1C_F_HARNESS_DLL || path.join(os.tmpdir(), "opencode", "p1c-f-harness", "bin", "Debug", "net8.0", "p1c-f-harness.dll");
const LOCAL_HARNESS = path.resolve(__dirname, "p1c-group-f-harness/bin/Debug/net8.0/p1c-f-harness.dll");
const DLL = fs.existsSync(HARNESS) ? HARNESS : LOCAL_HARNESS;

function runHarness(command, cfg) {
  check(fs.existsSync(DLL), "harness missing: " + DLL);
  const cfgPath = path.join(process.env.P1C_EVIDENCE_DIR, "D2-PARTIAL-" + command + "-" + Date.now() + ".cfg.json");
  fs.writeFileSync(cfgPath, JSON.stringify(cfg, null, 2));
  const out = execFileSync("dotnet", [DLL, CONN, command, cfgPath], { encoding: "utf8", timeout: 240000, windowsHide: true, maxBuffer: 32 * 1024 * 1024 });
  let parsed;
  try { parsed = JSON.parse(out); } catch { throw new Error("harness " + command + " output not JSON: " + out.slice(0, 300)); }
  if (parsed.ok !== true) throw new Error("harness " + command + " failed: " + JSON.stringify(parsed.error || parsed).slice(0, 500));
  return { cfgPath, parsed };
}
function step(harness, key, value) {
  const found = (harness.steps || []).find(s => s && s[key] === value);
  check(found, "harness step not found: " + key + "=" + value);
  return found;
}
function snap(harness, label) {
  const found = (harness.steps || []).find(s => s && s.label === label);
  check(found, "harness snapshot not found: " + label);
  return found;
}

async function main() {
  const root = process.env.P1C_EVIDENCE_DIR;
  check(root, "Set P1C_EVIDENCE_DIR");
  fs.mkdirSync(root, { recursive: true });
  const evidence = path.join(root, "D2-PARTIAL.json");
  check(!fs.existsSync(evidence), "Do not overwrite historical evidence");
  const trace = { id: "D2-PARTIAL", status: "NOT_RUN" };
  const save = () => fs.writeFileSync(evidence, JSON.stringify(trace, null, 2));
  const createdOffers = [];

  try {
    const admin = await L.adminContext();
    const sellerId = "01a030d1-40cb-7000-8abe-6d31739956c5";
    const raceFixture = await L.createDraftOffer(admin, sellerId);
    const partialFixture = await L.createDraftOffer(admin, sellerId);
    createdOffers.push(raceFixture.offerId, partialFixture.offerId);
    for (const f of [raceFixture, partialFixture]) {
      const d = await L.adminOfferDetail(admin, f.offerId);
      const a = await L.adminAssign(admin, f.offerId, "Platform", d.body.revisions.offer);
      check(a.status === 200, "could not set Platform on draft fixture: " + a.status + " " + JSON.stringify(a.body).slice(0, 200));
    }
    trace.fixtures = [raceFixture, partialFixture].map(f => ({ offerId: f.offerId, sellerId: f.sellerId, variantId: f.variantId }));
    save();

    const baseConfig = (f) => ({
      edition: "Marketplace", runId: "p1c-d2-partial-" + Date.now().toString(36) + "-" + f.offerId.slice(0, 6),
      adminActor: admin.actor, httpBase: "http://127.0.0.1:5088",
      offers: [{ key: "A", offerId: f.offerId, sellerId: f.sellerId, variantId: f.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
    });

    // ---- concurrent release/consume ----
    const race = runHarness("race-release-consume", baseConfig(raceFixture)).parsed;
    trace.race = race;
    save();
    const concurrent = (race.steps || []).find(s => Array.isArray(s));
    check(concurrent && concurrent.length === 2, "missing concurrent release/consume");
    check(concurrent.filter(x => x.ok === true).length === 1, "release and consume did not have exactly one winner: " + JSON.stringify(concurrent));
    const afterRace = snap(race, "after concurrent release/consume");
    check(Number(afterRace.positions[0].Reserved) >= 0 && Number(afterRace.allocations[0].Held) >= 0, "concurrent release/consume produced negative availability");
    check(Number(afterRace.allocations[0].Held) === 0 && Number(afterRace.positions[0].Reserved) === 0, "release/consume winner left a held/reserved remainder");
    const consumed = concurrent.find(x => x.op === "consume" && x.ok === true);
    if (consumed) {
      check(Number(afterRace.positions[0].OnHand) === 95, "consume winner did not debit exactly 5 (double debit?): " + afterRace.positions[0].OnHand);
      trace.race.winner = "consume";
    } else {
      check(Number(afterRace.positions[0].OnHand) === 100, "release winner changed physical stock: " + afterRace.positions[0].OnHand);
      trace.race.winner = "release";
    }
    save();

    // ---- partial-obligation release + replay ----
    const partial = runHarness("partial-release", baseConfig(partialFixture)).parsed;
    trace.partial = partial;
    save();
    const r2 = step(partial, "phase", "release2");
    check(Number(r2.ReleasedQuantity) === 2 && r2.Outcome === "PartiallyHeld", "partial release of 2 did not report PartiallyHeld: " + JSON.stringify(r2));
    const after2 = snap(partial, "after release2");
    check(Number(after2.positions[0].Reserved) === 3 && Number(after2.allocations[0].Held) === 3 && Number(after2.allocations[0].Quantity) - Number(after2.allocations[0].Held) === 17,
      "partial release 2 remaining quantity wrong: " + JSON.stringify({ reserved: after2.positions[0].Reserved, held: after2.allocations[0].Held, alloc: after2.allocations[0].Quantity }));
    const r3 = step(partial, "phase", "release3");
    check(Number(r3.ReleasedQuantity) === 3 && r3.Outcome === "Released", "final partial release of 3 did not report Released: " + JSON.stringify(r3));
    const after3 = snap(partial, "after release3");
    check(Number(after3.positions[0].Reserved) === 0 && Number(after3.allocations[0].Held) === 0 && Number(after3.allocations[0].Quantity) - Number(after3.allocations[0].Held) === 20,
      "final release remaining quantity wrong: " + JSON.stringify(after3));
    const replay = step(partial, "phase", "replay");
    check(Number(replay.ReleasedQuantity) === 3 && replay.Status === "Replayed", "partial release replay did not return the recorded result: " + JSON.stringify(replay));
    const afterReplay = snap(partial, "after replay");
    check(Number(afterReplay.positions[0].Reserved) === 0 && Number(afterReplay.allocations[0].Held) === 0, "partial release replay double-released");
    const holdStep = (partial.steps || []).find(s => s && s.reservationId);
    const holdRid = holdStep ? holdStep.reservationId : null;
    check(holdRid && r2.ReservationId === holdRid && r3.ReservationId === holdRid, "partial releases did not use the same scoped reservation: " + JSON.stringify({ holdRid, r2: r2.ReservationId, r3: r3.ReservationId }));
    check(String(after2.allocations[0].Revision) !== String(after3.allocations[0].Revision), "allocation revision did not advance across partial releases");
    trace.partialAssertions = {
      reservationId: holdRid,
      release2: { released: Number(r2.ReleasedQuantity), outcome: r2.Outcome, reserved: Number(after2.positions[0].Reserved), held: Number(after2.allocations[0].Held), remaining: Number(after2.allocations[0].Quantity) - Number(after2.allocations[0].Held) },
      release3: { released: Number(r3.ReleasedQuantity), outcome: r3.Outcome, reserved: Number(after3.positions[0].Reserved), held: Number(after3.allocations[0].Held), remaining: Number(after3.allocations[0].Quantity) - Number(after3.allocations[0].Held) },
      replay: { released: Number(replay.ReleasedQuantity), status: replay.Status },
      allocationRevisionAdvanced: true,
    };
    save();

    trace.status = "PASS";
    trace.reason = "Dedicated P1C fixture Inventory owner workflows on tooba_alpha: concurrent release/consume had exactly one winner with zero negative availability and exact physical/reserved outcome; partial-obligation release of 2 then 3 on the same scoped reservation reported PartiallyHeld then Released with exact remaining quantities (17 then 20), no double release on replay, and an advancing allocation revision.";
  } catch (error) {
    trace.status = "FAIL";
    trace.reason = String(error && error.stack ? error.stack : error);
  } finally {
    try { trace.cleanup = L.cleanupDraftOffers(createdOffers); }
    catch (error) { trace.status = "FAIL"; trace.cleanupError = String(error && error.stack ? error.stack : error); }
    save();
  }
  console.log(JSON.stringify({ id: "D2-PARTIAL", status: trace.status, reason: trace.reason, cleanup: trace.cleanup }));
  if (trace.status !== "PASS") process.exit(1);
}

main().catch(error => { console.error(error); process.exit(1); });
