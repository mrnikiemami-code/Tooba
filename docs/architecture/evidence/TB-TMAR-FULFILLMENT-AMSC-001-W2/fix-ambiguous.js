// TB-TMAR-FULFILLMENT-AMSC-001 W2 (part 2): resolve the two ambiguous namespace splits.
//  - Tooba.Fulfillment.Application.Models       -> .Fulfillments.Models | .WorkQueue.Models
//  - Tooba.Fulfillment.Application.Validators.Admin -> .Fulfillments.Validators | .WorkQueue.Validators
const fs = require("fs");
const path = require("path");

const repoRoot = path.resolve(__dirname, "../../../..");

const workQueueModelTypes = [
  "AdminFulfillmentWorkQueueRow",
  "AdminFulfillmentQueueFilters",
  "AdminFulfillmentWorkQueueBulkRequest",
  "AdminFulfillmentWorkQueueBulkItem",
  "AdminFulfillmentWorkQueueBulkResult",
];
const workQueueValidatorTypes = [
  "ExecuteAdminFulfillmentBulkCommandValidator",
  "QueryAdminFulfillmentWorkQueueQueryValidator",
];
const fulfillmentValidatorTypes = ["GetAdminFulfillmentQueryValidator"];

const scanRoots = [
  path.join(repoRoot, "src/backend/Modules/Fulfillment"),
  path.join(repoRoot, "src/backend/Host/Tooba.Host.Tests"),
  path.join(repoRoot, "src/backend/Host/Tooba.Host"),
];

function walk(dir, out) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (["bin", "obj", "artifacts", "Migrations"].includes(e.name)) continue;
      walk(p, out);
    } else if (e.name.endsWith(".cs")) out.push(p);
  }
  return out;
}

const files = [];
for (const r of scanRoots) walk(r, files);

const uses = (text, t) => new RegExp(`\\b${t}\\b`).test(text);

let changed = 0;
for (const file of files) {
  let text = fs.readFileSync(file, "utf8");
  const before = text;

  // --- Models ---------------------------------------------------------------------
  if (text.includes("Tooba.Fulfillment.Application.Models")) {
    const needsWq = workQueueModelTypes.some(t => uses(text, t));
    const needsFf = !needsWq || text.replace(new RegExp(`\\b(${workQueueModelTypes.join("|")})\\b`, "g"), "").match(/\b(FulfillmentSnapshot|ShipmentSnapshot|ShipmentLineSnapshot|FulfillmentItemSnapshot|ConsolidatedPackageSnapshot|ConsolidatedPackageMemberSnapshot|ActivePackageMembershipSnapshot|FulfillmentSelectionCommand|ShipmentLineCommand)\b/);

    if (!needsWq) {
      // Pure fulfillment-lifecycle models.
      text = text.split("using Tooba.Fulfillment.Application.Models;")
        .join("using Tooba.Fulfillment.Application.Fulfillments.Models;");
      text = text.split("using App = Tooba.Fulfillment.Application.Models;")
        .join("using App = Tooba.Fulfillment.Application.Fulfillments.Models;");
      text = text.split("Tooba.Fulfillment.Application.Models.")
        .join("Tooba.Fulfillment.Application.Fulfillments.Models.");
    } else if (!needsFf) {
      // Pure work-queue models.
      text = text.split("using Tooba.Fulfillment.Application.Models;")
        .join("using Tooba.Fulfillment.Application.WorkQueue.Models;");
      text = text.split("Tooba.Fulfillment.Application.Models.")
        .join("Tooba.Fulfillment.Application.WorkQueue.Models.");
    } else {
      // Needs both: keep a deterministic pair of usings.
      text = text.split("using Tooba.Fulfillment.Application.Models;")
        .join("using Tooba.Fulfillment.Application.Fulfillments.Models;\nusing Tooba.Fulfillment.Application.WorkQueue.Models;");
      text = text.split("Tooba.Fulfillment.Application.Models.")
        .join("Tooba.Fulfillment.Application.Fulfillments.Models.");
    }
  }

  // --- Validators.Admin -----------------------------------------------------------
  if (text.includes("Tooba.Fulfillment.Application.Validators.Admin")) {
    const needsWq = workQueueValidatorTypes.some(t => uses(text, t));
    const needsFf = fulfillmentValidatorTypes.some(t => uses(text, t));
    const replacement = [];
    if (needsFf) replacement.push("using Tooba.Fulfillment.Application.Fulfillments.Validators;");
    if (needsWq) replacement.push("using Tooba.Fulfillment.Application.WorkQueue.Validators;");
    if (replacement.length === 0) replacement.push("using Tooba.Fulfillment.Application.Fulfillments.Validators;");
    text = text.split("using Tooba.Fulfillment.Application.Validators.Admin;").join(replacement.join("\n"));
  }

  // --- guard comment / prose mention ----------------------------------------------
  text = text.split("`using AppModels = Tooba.Fulfillment.Application.Models;`")
    .join("`using AppModels = Tooba.Fulfillment.Application.Fulfillments.Models;`");

  if (text !== before) { fs.writeFileSync(file, text); changed++; console.log("fixed", path.relative(repoRoot, file)); }
}
console.log("files changed:", changed);
