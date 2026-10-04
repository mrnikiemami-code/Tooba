// One-off: replace raw Fulfillment stable-code string literals at typed throw sites with
// FulfillmentErrorCodes.<Const> and ensure the Contracts.Errors using is present.
const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "../../../../src/backend/Modules/Fulfillment");
const codesFile = path.join(root, "Tooba.Fulfillment.Contracts/Errors/FulfillmentErrorCodes.cs");
const codesSrc = fs.readFileSync(codesFile, "utf8");

const map = new Map();
for (const m of codesSrc.matchAll(/public const string (\w+) = "([^"]+)";/g)) {
  map.set(m[2], m[1]);
}
console.log("codes:", map.size);

const projects = [
  "Tooba.Fulfillment.Domain",
  "Tooba.Fulfillment.Application",
  "Tooba.Fulfillment.Infrastructure",
  "Tooba.Fulfillment.Endpoints",
];

function walk(dir, out) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (["bin", "obj", "artifacts"].includes(e.name)) continue;
      walk(p, out);
    } else if (e.name.endsWith(".cs")) {
      out.push(p);
    }
  }
  return out;
}

let changed = 0;
let usingAdded = 0;
for (const proj of projects) {
  for (const file of walk(path.join(root, proj), [])) {
    if (file.includes(`${path.sep}Migrations${path.sep}`)) continue;
    let text = fs.readFileSync(file, "utf8");
    const before = text;

    text = text.replace(
      /(throw new (?:ContractOperationException|InvalidOperationException)\()"([^"]+)"(\))/g,
      (full, pre, code, post) => (map.has(code) ? `${pre}FulfillmentErrorCodes.${map.get(code)}${post}` : full)
    );
    text = text.replace(
      /(\?\? throw new (?:ContractOperationException|InvalidOperationException)\()"([^"]+)"(\))/g,
      (full, pre, code, post) => (map.has(code) ? `${pre}FulfillmentErrorCodes.${map.get(code)}${post}` : full)
    );
    text = text.replace(
      /(new SemanticError\()"([^"]+)"(\))/g,
      (full, pre, code, post) => (map.has(code) ? `${pre}FulfillmentErrorCodes.${map.get(code)}${post}` : full)
    );

    if (text === before) continue;

    if (!text.includes("using Tooba.Fulfillment.Contracts.Errors;") && !file.endsWith("FulfillmentErrorCodes.cs")) {
      const m = text.match(/^namespace\s+[\w.]+;/m);
      if (m) {
        text = text.replace(m[0], `using Tooba.Fulfillment.Contracts.Errors;\n\n${m[0]}`);
        usingAdded++;
      }
    }
    fs.writeFileSync(file, text);
    changed++;
    console.log("patched", path.relative(root, file));
  }
}
console.log("files changed:", changed, "usings added:", usingAdded);
