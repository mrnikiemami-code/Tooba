// TB-TMAR-FULFILLMENT-AMSC-001 W2: capability-first Application restructure.
// Moves Application files into Shipping/Fulfillments/WorkQueue/Checkout capability folders,
// splits the mixed Shipping *Contracts bundles by responsibility, rewrites namespaces and
// every repo-wide reference to the moved namespaces.
const fs = require("fs");
const path = require("path");

const repoRoot = path.resolve(__dirname, "../../../..");
const moduleRoot = path.join(repoRoot, "src/backend/Modules/Fulfillment");
const appRoot = path.join(moduleRoot, "Tooba.Fulfillment.Application");

// --- 1. deterministic moves: old relative path -> new relative path -----------------
const moves = [
  // Shipping capability
  ["Commands/CreateShippingService/CreateShippingServiceCommand.cs", "Shipping/Commands/CreateShippingServiceCommand.cs"],
  ["Commands/UpdateShippingService/UpdateShippingServiceCommand.cs", "Shipping/Commands/UpdateShippingServiceCommand.cs"],
  ["Commands/DeactivateShippingService/DeactivateShippingServiceCommand.cs", "Shipping/Commands/DeactivateShippingServiceCommand.cs"],
  ["Commands/EnsureShippingCatalogSeed/EnsureShippingCatalogSeedCommand.cs", "Shipping/Commands/EnsureShippingCatalogSeedCommand.cs"],
  ["Queries/GetShippingService/GetShippingServiceQuery.cs", "Shipping/Queries/GetShippingServiceQuery.cs"],
  ["Queries/ListShippingServices/ListShippingServicesQuery.cs", "Shipping/Queries/ListShippingServicesQuery.cs"],
  ["Queries/ListEnabledShippingMethodsTree/ListEnabledShippingMethodsTreeQuery.cs", "Shipping/Queries/ListEnabledShippingMethodsTreeQuery.cs"],
  ["Validators/Shipping/CreateShippingServiceCommandValidator.cs", "Shipping/Validators/CreateShippingServiceCommandValidator.cs"],
  ["Validators/Shipping/UpdateShippingServiceCommandValidator.cs", "Shipping/Validators/UpdateShippingServiceCommandValidator.cs"],
  ["Validators/Shipping/DeactivateShippingServiceCommandValidator.cs", "Shipping/Validators/DeactivateShippingServiceCommandValidator.cs"],
  ["Validators/Shipping/GetShippingServiceQueryValidator.cs", "Shipping/Validators/GetShippingServiceQueryValidator.cs"],
  ["Shipping/ShippingMethodRegistry.cs", "Shipping/ShippingProviderMetadata.cs"],

  // Fulfillment lifecycle capability
  ["Commands/SellerMutateFulfillment/SellerMutateFulfillmentCommand.cs", "Fulfillments/Commands/SellerMutateFulfillmentCommand.cs"],
  ["Queries/GetAdminFulfillment/GetAdminFulfillmentQuery.cs", "Fulfillments/Queries/GetAdminFulfillmentQuery.cs"],
  ["Queries/GetSellerFulfillment/GetSellerFulfillmentQuery.cs", "Fulfillments/Queries/GetSellerFulfillmentQuery.cs"],
  ["Queries/ListAdminFulfillments/ListAdminFulfillmentsQuery.cs", "Fulfillments/Queries/ListAdminFulfillmentsQuery.cs"],
  ["Queries/ListSellerFulfillments/ListSellerFulfillmentsQuery.cs", "Fulfillments/Queries/ListSellerFulfillmentsQuery.cs"],
  ["Validators/Admin/GetAdminFulfillmentQueryValidator.cs", "Fulfillments/Validators/GetAdminFulfillmentQueryValidator.cs"],
  ["Validators/Seller/GetSellerFulfillmentQueryValidator.cs", "Fulfillments/Validators/GetSellerFulfillmentQueryValidator.cs"],
  ["Validators/Seller/SellerMutateFulfillmentCommandValidator.cs", "Fulfillments/Validators/SellerMutateFulfillmentCommandValidator.cs"],
  ["Models/ActivePackageMembershipSnapshot.cs", "Fulfillments/Models/ActivePackageMembershipSnapshot.cs"],
  ["Models/ConsolidatedPackageMemberSnapshot.cs", "Fulfillments/Models/ConsolidatedPackageMemberSnapshot.cs"],
  ["Models/ConsolidatedPackageSnapshot.cs", "Fulfillments/Models/ConsolidatedPackageSnapshot.cs"],
  ["Models/FulfillmentItemSnapshot.cs", "Fulfillments/Models/FulfillmentItemSnapshot.cs"],
  ["Models/FulfillmentSelectionCommand.cs", "Fulfillments/Models/FulfillmentSelectionCommand.cs"],
  ["Models/FulfillmentSnapshot.cs", "Fulfillments/Models/FulfillmentSnapshot.cs"],
  ["Models/ShipmentLineCommand.cs", "Fulfillments/Models/ShipmentLineCommand.cs"],
  ["Models/ShipmentLineSnapshot.cs", "Fulfillments/Models/ShipmentLineSnapshot.cs"],
  ["Models/ShipmentSnapshot.cs", "Fulfillments/Models/ShipmentSnapshot.cs"],
  ["Ports/IFulfillmentDirectory.cs", "Fulfillments/Ports/IFulfillmentDirectory.cs"],
  ["Ports/IFulfillmentInventoryGateway.cs", "Fulfillments/Ports/IFulfillmentInventoryGateway.cs"],
  ["Ports/IFulfillmentUseCaseGuard.cs", "Fulfillments/Ports/IFulfillmentUseCaseGuard.cs"],
  ["Ports/ISellerFulfillmentAuthorizer.cs", "Fulfillments/Ports/ISellerFulfillmentAuthorizer.cs"],

  // Work queue capability
  ["Commands/ExecuteAdminFulfillmentBulk/ExecuteAdminFulfillmentBulkCommand.cs", "WorkQueue/Commands/ExecuteAdminFulfillmentBulkCommand.cs"],
  ["Queries/QueryAdminFulfillmentWorkQueue/QueryAdminFulfillmentWorkQueueQuery.cs", "WorkQueue/Queries/QueryAdminFulfillmentWorkQueueQuery.cs"],
  ["Validators/Admin/ExecuteAdminFulfillmentBulkCommandValidator.cs", "WorkQueue/Validators/ExecuteAdminFulfillmentBulkCommandValidator.cs"],
  ["Validators/Admin/QueryAdminFulfillmentWorkQueueQueryValidator.cs", "WorkQueue/Validators/QueryAdminFulfillmentWorkQueueQueryValidator.cs"],
  ["Models/AdminFulfillmentWorkQueueModels.cs", "WorkQueue/Models/AdminFulfillmentWorkQueueModels.cs"],

  // Checkout capability
  ["Queries/ListCustomerCheckoutFulfillments/ListCustomerCheckoutFulfillmentsQuery.cs", "Checkout/Queries/ListCustomerCheckoutFulfillmentsQuery.cs"],
  ["Validators/Customer/ListCustomerCheckoutFulfillmentsQueryValidator.cs", "Checkout/Validators/ListCustomerCheckoutFulfillmentsQueryValidator.cs"],
];

const dirOf = (rel) => {
  const d = path.dirname(rel).replace(/\\/g, "/");
  return d === "." ? "" : d;
};
const nsOf = (rel) => {
  const d = dirOf(rel);
  return d === "" ? "Tooba.Fulfillment.Application" : "Tooba.Fulfillment.Application." + d.replace(/\//g, ".");
};
const nsOfOld = (rel) => {
  const d = dirOf(rel);
  return d === "" ? "Tooba.Fulfillment.Application" : "Tooba.Fulfillment.Application." + d.replace(/\//g, ".");
};

const nsRemaps = new Map();
for (const [from, to] of moves) {
  const a = nsOfOld(from);
  const b = nsOf(to);
  if (a === b) continue;
  if (!nsRemaps.has(a)) nsRemaps.set(a, new Set());
  nsRemaps.get(a).add(b);
}

// Only unambiguous old->new namespace pairs may be rewritten globally.
const globalRemaps = [];
const ambiguous = [];
for (const [a, set] of nsRemaps) {
  if (set.size === 1) globalRemaps.push([a, [...set][0]]);
  else ambiguous.push([a, [...set]]);
}
globalRemaps.sort((x, y) => y[0].length - x[0].length);

console.log("== unambiguous global namespace remaps ==");
for (const [a, b] of globalRemaps) console.log(`  ${a} -> ${b}`);
console.log("== ambiguous (fixed manually) ==");
for (const [a, set] of ambiguous) console.log(`  ${a} -> ${set.join(" | ")}`);

// --- 2. move files, rewrite their own namespace declaration -------------------------
for (const [from, to] of moves) {
  const src = path.join(appRoot, from);
  const dst = path.join(appRoot, to);
  if (!fs.existsSync(src)) { console.log("MISSING", from); continue; }
  fs.mkdirSync(path.dirname(dst), { recursive: true });
  let text = fs.readFileSync(src, "utf8");
  const a = nsOfOld(from);
  const b = nsOf(to);
  if (a !== b) text = text.split(`namespace ${a};`).join(`namespace ${b};`);
  fs.writeFileSync(dst, text);
  fs.unlinkSync(src);
  console.log("moved", from, "->", to);
}

// --- 3. split the mixed Shipping bundles by responsibility --------------------------
// 3a. ShippingServiceReadContracts.cs -> ShippingServiceReadModels.cs + ShippingServiceSemantic.cs
const readSrc = path.join(appRoot, "Shipping/ShippingServiceReadContracts.cs");
if (fs.existsSync(readSrc)) {
  const text = fs.readFileSync(readSrc, "utf8");
  const marker = "/// <summary>نگاشت خطاهای معنایی shipping_service.* به SemanticError.</summary>";
  const idx = text.indexOf(marker);
  if (idx < 0) throw new Error("ShippingServiceSemantic marker not found");
  const dtoPart = text.slice(0, idx).trimEnd() + "\n";
  const semanticPart = text.slice(idx);
  fs.writeFileSync(path.join(appRoot, "Shipping/ShippingServiceReadModels.cs"), dtoPart);
  fs.writeFileSync(
    path.join(appRoot, "Shipping/ShippingServiceSemantic.cs"),
    `using Tooba.BuildingBlocks;\nusing Tooba.BuildingBlocks.Results;\nusing Tooba.Fulfillment.Application.Errors;\nusing Tooba.Fulfillment.Contracts.Errors;\nusing Tooba.Localization.Contracts.Ports;\n\nnamespace Tooba.Fulfillment.Application.Shipping;\n\n${semanticPart}`);
  fs.unlinkSync(readSrc);
  console.log("split ShippingServiceReadContracts.cs");
}

// 3b. ShippingServiceWriteContracts.cs -> ShippingServiceWriteModels.cs + Ports/IShippingServiceDirectory.cs
const writeSrc = path.join(appRoot, "Shipping/ShippingServiceWriteContracts.cs");
if (fs.existsSync(writeSrc)) {
  const text = fs.readFileSync(writeSrc, "utf8");
  const marker = "/// <summary>نوشتن کاتالوگ سرویس ارسال روی مالک Fulfillment.</summary>";
  const idx = text.indexOf(marker);
  if (idx < 0) throw new Error("IShippingServiceDirectory marker not found");
  const modelsPart = text.slice(0, idx).trimEnd() + "\n";
  const portPart = text.slice(idx);
  fs.writeFileSync(path.join(appRoot, "Shipping/ShippingServiceWriteModels.cs"), modelsPart);
  fs.mkdirSync(path.join(appRoot, "Shipping/Ports"), { recursive: true });
  fs.writeFileSync(
    path.join(appRoot, "Shipping/Ports/IShippingServiceDirectory.cs"),
    `namespace Tooba.Fulfillment.Application.Shipping.Ports;\n\n${portPart}`);
  fs.unlinkSync(writeSrc);
  console.log("split ShippingServiceWriteContracts.cs");
}

// --- 4. rewrite repo-wide references ------------------------------------------------
const scanRoots = [
  path.join(moduleRoot, "Tooba.Fulfillment.Application"),
  path.join(moduleRoot, "Tooba.Fulfillment.Infrastructure"),
  path.join(moduleRoot, "Tooba.Fulfillment.Endpoints"),
  path.join(moduleRoot, "Tooba.Fulfillment.Tests"),
  path.join(moduleRoot, "Tooba.Fulfillment.Contracts"),
  path.join(moduleRoot, "Tooba.Fulfillment.Domain"),
  path.join(repoRoot, "src/backend/Host/Tooba.Host"),
  path.join(repoRoot, "src/backend/Host/Tooba.Host.Tests"),
];

function walk(dir, out) {
  if (!fs.existsSync(dir)) return out;
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

let rewrote = 0;
for (const file of files) {
  let text = fs.readFileSync(file, "utf8");
  const before = text;
  for (const [a, b] of globalRemaps) text = text.split(a).join(b);
  if (text !== before) { fs.writeFileSync(file, text); rewrote++; }
}
console.log("files rewritten:", rewrote);
console.log("ambiguous namespaces still to fix manually:", ambiguous.map(x => x[0]).join(", "));
