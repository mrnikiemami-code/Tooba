// TB-TMAR-FULFILLMENT-AMSC-001 W2 (part 6): split the 1218-LOC FulfillmentDirectory god-file into
// cohesive partials (ARCH-SIZE-001 800 ceiling, MULTI_RESPONSIBILITY_COHESION_VIOLATION).
// Pure member move: no signature, body, ordering or behavior change.
const fs = require("fs");
const path = require("path");
const repoRoot = path.resolve(__dirname, "../../../..");
const dir = path.join(repoRoot, "src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Directories");
const main = path.join(dir, "FulfillmentDirectory.cs");

const lines = fs.readFileSync(main, "utf8").split(/\r?\n/);
const idx = (n) => n - 1; // 1-based -> 0-based

// Class header: make the type partial so the cohesive packages surface can live beside it.
const classLine = lines.findIndex(l => l.startsWith("public sealed class FulfillmentDirectory"));
if (classLine < 0) throw new Error("class declaration not found");
lines[classLine] = lines[classLine].replace(
  "public sealed class FulfillmentDirectory",
  "public sealed partial class FulfillmentDirectory");

// Packages surface: from the split banner through the end of the class (closing brace).
const banner = idx(794);
const classClose = lines.findIndex((l, i) => i > banner && l === "}");
if (classClose < 0) throw new Error("class close brace not found");

const packagesBody = lines.slice(banner, classClose);
const mainBody = lines.slice(0, banner).concat(lines.slice(classClose));

// Trailing usings needed only by the packages surface (DbContext access is on the shared `_db`).
const packagesHeader = [
  "using Microsoft.EntityFrameworkCore;",
  "using Tooba.BuildingBlocks;",
  "using Tooba.Fulfillment.Application.Fulfillments.Models;",
  "using Tooba.Fulfillment.Domain.Aggregates;",
  "using Tooba.Fulfillment.Domain.ValueObjects;",
  "using Tooba.Fulfillment.Infrastructure.Persistence;",
  "",
  "namespace Tooba.Fulfillment.Infrastructure.Directories;",
  "",
  "/// <summary>",
  "/// سطح بسته‌های تلفیقی ارسال (cohesive partial — AMSC-001 W2، بدون تغییر رفتار).",
  "/// </summary>",
  "public sealed partial class FulfillmentDirectory",
  "{",
];

fs.writeFileSync(main, mainBody.join("\n"));
fs.writeFileSync(path.join(dir, "FulfillmentDirectory.Packages.cs"), packagesHeader.concat(packagesBody).join("\n") + "\n");

console.log("main LOC:", mainBody.length);
console.log("packages LOC:", packagesHeader.length + packagesBody.length);
