// TB-TMAR-FULFILLMENT-AMSC-001 W2 (part 4): capability validators need the shared
// Application/Validators rules + codes namespace explicitly now that they no longer
// sit under the technical Validators.* axis.
const fs = require("fs");
const path = require("path");
const repoRoot = path.resolve(__dirname, "../../../..");
const app = path.join(repoRoot, "src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application");

const targets = [
  "Shipping/Validators/CreateShippingServiceCommandValidator.cs",
  "Shipping/Validators/UpdateShippingServiceCommandValidator.cs",
  "Shipping/Validators/DeactivateShippingServiceCommandValidator.cs",
  "Shipping/Validators/GetShippingServiceQueryValidator.cs",
  "Fulfillments/Validators/GetAdminFulfillmentQueryValidator.cs",
  "Fulfillments/Validators/GetSellerFulfillmentQueryValidator.cs",
  "Fulfillments/Validators/SellerMutateFulfillmentCommandValidator.cs",
  "WorkQueue/Validators/ExecuteAdminFulfillmentBulkCommandValidator.cs",
  "WorkQueue/Validators/QueryAdminFulfillmentWorkQueueQueryValidator.cs",
  "Checkout/Validators/ListCustomerCheckoutFulfillmentsQueryValidator.cs",
];

const ns = "Tooba.Fulfillment.Application.Validators";
for (const rel of targets) {
  const p = path.join(app, rel);
  let t = fs.readFileSync(p, "utf8");
  if (t.includes(`using ${ns};`)) { console.log("already", rel); continue; }
  const lines = t.split("\n");
  let last = -1;
  for (let i = 0; i < lines.length; i++) if (/^using .*;$/.test(lines[i].trim())) last = i;
  if (last < 0) throw new Error("no using block in " + rel);
  lines.splice(last + 1, 0, `using ${ns};`);
  fs.writeFileSync(p, lines.join("\n"));
  console.log("added ->", rel);
}
