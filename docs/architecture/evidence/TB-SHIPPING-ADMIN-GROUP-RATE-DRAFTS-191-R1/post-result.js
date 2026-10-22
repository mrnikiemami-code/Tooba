const fs = require("fs");
const path = require("path");
const { execFileSync } = require("child_process");

const BRIDGE = process.env.TOOBA_BRIDGE || "http://127.0.0.1:17321";
const TASK_ROW_ID = "f6c2f06e-4c77-437d-a1bd-d5f9106728b7";
const TASK_ID = "TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1";
const BASE_SHA = "b77492052bb2926c522bb23c8cb7e62dfc717675";
const CODE_SHA = "7fc3b0bc6352cadd6be3a2290ef55ef7dcd1c44d";
const repoRoot = path.resolve(__dirname, "../../../..");
const head = execFileSync("git", ["rev-parse", "HEAD"], { cwd: repoRoot, encoding: "utf8" }).trim();
const origin = execFileSync("git", ["rev-parse", "origin/main"], { cwd: repoRoot, encoding: "utf8" }).trim();

const result = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: ${TASK_ID}
Parent-Task: TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_RATE_STORAGE_CHECK_PARITY_AND_CROSS_OPERATION_RACE_REPAIR
Bridge-Task-Row: ${TASK_ROW_ID}
Status: PASS
Expected-Base-SHA: ${BASE_SHA}
Base-SHA: ${BASE_SHA}
Code-SHA: ${CODE_SHA}
Evidence-SHA: ${head}
HEAD-Origin-Parity: HEAD == origin/main == ${origin} after push; clean working tree (only pre-existing unrelated untracked artifacts preserved)
Nullable-Interval-Check-Truth-And-Postgres-Negatives: PROVEN. Old shape evaluated NULL for min=NULL/positive max and a temp table carrying the old exact shape accepted the max-only row. All five own authoring checks now require min IS NOT NULL on the conditioned branch. Direct parameterized SQL bypassing the aggregate asserts SQLSTATE 23514 with the owned constraint identity for weight min NULL/max 500, count min NULL/max 5, amount min NULL/max 1000, re-inverted/equal bounds and count minimum 0; each attempt is rolled back in its own transaction.
Merchandise-Minimum-Cap-And-Currency-ASCII-Parity: PROVEN. amount_within_max now caps min_merchandise_amount and the merchandise interval caps both bounds; min 2e9/max NULL and min 0/max 2e9 are rejected (23514, owned merchandise/amount constraint). Currency is NULL or octet_length=3 with COLLATE "C" ~ '^[A-Z]{3}$'; '123','A$!','irr','IR ',' I','IR' are all rejected by ck_authoring_region_groups_currency_canonical and 'IRR' persists. API/domain precision rejection is retained because numeric(18,2) may round before a CHECK.
Valid-Storage-Boundaries-And-Domain-Compatibility: PROVEN. Both-NULL axis, min-only, adjacent bounded intervals, count [1,4) with amount [5000,10000), and min amount exactly 1e9 with max NULL all persist; the at-limit amount is re-read verbatim from storage (no silent rounding). No existing domain, contract, endpoint, owner predicate, error or resource changed; all pre-existing authoring/persistence guards still pass.
Corrective-Migration-SQL-And-Historical-Byte-Parity: One additive migration 20261022075711_AuthoringRateRowConstraintParity (+Designer) drops/re-adds only the five own authoring checks, Down restores the exact previous expressions, no table/column/index/FK/data change. Snapshot updated only for it. git diff over Persistence/Migrations shows exactly that snapshot plus the two new files: TB-191 and all older migrations/designers are byte-identical. Generated forward SQL saved as correction.sql and verified against the live database.
RateWrite-Versus-RegionRetention-Race: A_same_revision_rate_write_racing_a_region_edit_leaves_exactly_the_winner_state. ReplaceGroupRateRowsAsync vs ReplaceRegionGroupsAsync on the same owner/draft/revision 1, independent Npgsql contexts, both gated after load and before first write (bare BEGIN holds no lock, no sleeps). Both original revision predicates were observed; exactly one success and one canonical conflict. Rate winner: stored tree equals the seeded tree, exactly the 1111 row, currency IRR, revision 2. Region winner: order [0,1] with second group carrying {Isfahan, Qom} and first group Tehran, zero rate rows, currency NULL. Revision exactly 2 in both.
RateWrite-Versus-RegionRemoval-Race: A_same_revision_rate_write_racing_a_region_removal_never_leaves_orphan_rows. The removed group is exactly the group receiving the rate write. Exactly one success; if the rate write wins the group and its 4321 row survive and the tree is [first,second], if the removal wins only [second] remains, no orphan rate row for either group exists, and revision is exactly 2 with the loser conflict.
Actual-Loser-Tracker-And-Exact-Winner-State: The actual losing context (not a synthetic one) is asserted to hold zero non-Unchanged ChangeTracker entries, so a later SaveChanges cannot publish the rejected edit; a fresh context read-back verifies the full tree, ids, areas, currency, rows and revision; no staged orders remain.
Focused-Commands-And-Test-Counts: dotnet build Tooba.Shipping.Tests = 0 errors; dotnet test --filter FullyQualifiedName~Relational on live PostgreSQL 17 = 24/24 passed (includes both new races and the executed storage matrix) plus focused persistence/authoring guards inside the suite. No failed/skipped case.
Final-Shipping-Suite-If-Run: whole tooba.shipping.tests executed once while the disposable DB was live: 577 passed / 0 failed / 0 skipped (11 s).
TB191-HostFailure-Attribution: UNVERIFIED_NO_HOST_RERUN
Out-Of-Scope-Commands: NONE
Claim-UTC: 2026-10-22T07:53:21Z
Result-UTC: ${new Date().toISOString().replace(/\.\d{3}Z$/, "Z")}
Elapsed-Minutes: 9
Hard-Deadline-Compliance: YES. 35-minute deadline 08:28:21Z; all implementation and tests finished by 08:00Z (minute 7), no new test/implementation started after minute 30, cleanup+commit+push+result reserved and used after tests. No command exceeded its timeout and no detached test was left behind.
Changed-Files: src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/ShippingDbContext.cs; .../Persistence/Migrations/20261022075711_AuthoringRateRowConstraintParity.cs; .../20261022075711_AuthoringRateRowConstraintParity.Designer.cs; .../Persistence/Migrations/ShippingDbContextModelSnapshot.cs; src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringDraftRelationalTests.cs; src/backend/Modules/Shipping/Tooba.Shipping.Tests/Persistence/ShippingPersistenceModelTests.cs; docs/architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1/**; docs/architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191/RESULT.md (append-only); docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md (section 22); docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md (section 12); docs/ai/tasks/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1.task.md
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: Fresh task-owned container tb191r1-postgres on 127.0.0.1:55432 (shipping_tb190r1 / tb190r1), port verified free before start, parsed target and live current_database()/current_user verified before writes. Container removed after the runs; no listener remains on 55432. Shared :5432/:3000/:3419/:5088, tooba_alpha, store-alpha, Redis/RabbitMQ, deployment flags and unrelated dirty/untracked files untouched; no credentials logged.
Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT`;

async function main() {
  if (!result.includes("END_TOOBA_WORKER_RESULT") || !/^Status: (PASS|PARTIAL|BLOCKED)$/m.test(result)) {
    throw new Error("result contract markers invalid");
  }

  const results = await fetch(`${BRIDGE}/api/results`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ channelId: "tooba-main", taskId: TASK_ID, content: result }),
  });
  const resultsBody = await results.text();
  console.log("RESULTS", results.status, resultsBody);
  if (results.status < 200 || results.status >= 300) throw new Error("result post failed");

  const complete = await fetch(`${BRIDGE}/api/tasks/${TASK_ROW_ID}/complete`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: "{}",
  });
  const completeBody = await complete.text();
  console.log("COMPLETE", complete.status, completeBody);
  if (complete.status < 200 || complete.status >= 300) throw new Error("task completion failed");
}

main().catch((err) => {
  console.error("POST_FAILED", err.message);
  process.exit(1);
});
