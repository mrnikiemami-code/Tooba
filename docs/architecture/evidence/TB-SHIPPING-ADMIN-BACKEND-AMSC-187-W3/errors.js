const fs = require("fs");
const path = require("path");
const { execSync } = require("child_process");

const repoRoot = path.resolve(__dirname, "../../../..");
const read = (p) => fs.readFileSync(path.join(repoRoot, p), "utf8");

const codesFile = execSync(
  'git ls-files "src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Errors/*.cs"',
  { cwd: repoRoot, encoding: "utf8" },
).trim().split(/\r?\n/).filter(Boolean);

const declared = [];
for (const f of codesFile) {
  for (const m of read(f).matchAll(/public const string (\w+)\s*=\s*"([^"]+)"/g)) {
    declared.push({ name: m[1], code: m[2], file: f });
  }
}
console.log("DECLARED_CODES", declared.length);

const contributor = read("src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Errors/ShippingErrorCatalogContributor.cs");
const missingInCatalog = declared.filter((c) => !contributor.includes(`ShippingErrorCodes.${c.name}`));
console.log("MISSING_IN_CATALOG", JSON.stringify(missingInCatalog.map((c) => c.name)));

// duplicate descriptor ownership inside contributor
const catalogRefs = [...contributor.matchAll(/ShippingErrorCodes\.(\w+)/g)].map((m) => m[1]);
const dupCatalog = catalogRefs.filter((n, i) => catalogRefs.indexOf(n) !== i);
console.log("DUPLICATE_DESCRIPTOR_IN_CONTRIBUTOR", JSON.stringify([...new Set(dupCatalog)]));

// resources
for (const resx of ["ShippingErrors.resx", "ShippingErrors.fa.resx"]) {
  const p = `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Resources/${resx}`;
  const text = read(p);
  const keys = [...text.matchAll(/<data name="([^"]+)"/g)].map((m) => m[1]);
  const missing = declared.filter((c) => !keys.includes(c.code)).map((c) => c.code);
  console.log(resx, "KEYS", keys.length, "MISSING", JSON.stringify(missing));
}

// actual emitters across production source
const prodFiles = execSync(
  'git ls-files "src/backend/Modules/Shipping/**/*.cs"',
  { cwd: repoRoot, encoding: "utf8" },
).trim().split(/\r?\n/).filter((f) => f.includes("Tooba.Shipping.") && !f.includes("Tooba.Shipping.Tests"));

const used = new Map();
for (const f of prodFiles) {
  const text = read(f);
  for (const m of text.matchAll(/ShippingErrorCodes\.(\w+)/g)) {
    if (!used.has(m[1])) used.set(m[1], new Set());
    used.get(m[1]).add(f);
  }
}
const known = new Set(declared.map((c) => c.name));
const undeclared = [...used.keys()].filter((k) => !known.has(k));
console.log("USED_CODES", used.size);
console.log("UNDECLARED_USED", JSON.stringify(undeclared));

// raw string literals that look like an error code but bypass the constants
const rawCodeLiterals = new Set();
for (const f of prodFiles) {
  for (const m of read(f).matchAll(/"((?:shipping|foundation|fulfillment|geography|order)\.[a-z0-9_.-]+)"/g)) {
    if (!known.has(m[1])) rawCodeLiterals.add(`${m[1]} @ ${f}`);
  }
}
console.log("RAW_CODE_LITERALS", JSON.stringify([...rawCodeLiterals], null, 1));
