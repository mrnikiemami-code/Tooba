// Domain cannot reference Contracts (architecture guard). Revert Domain to raw stable code
// literals but throw the typed ContractOperationException consistently with the rest of Domain.
const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "../../../../src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain");
const codesFile = path.resolve(__dirname, "../../../../src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Errors/FulfillmentErrorCodes.cs");
const codesSrc = fs.readFileSync(codesFile, "utf8");

const nameToValue = new Map();
for (const m of codesSrc.matchAll(/public const string (\w+) = "([^"]+)";/g)) {
  nameToValue.set(m[1], m[2]);
}

function walk(dir, out) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (["bin", "obj"].includes(e.name)) continue;
      walk(p, out);
    } else if (e.name.endsWith(".cs")) out.push(p);
  }
  return out;
}

let n = 0;
for (const file of walk(root, [])) {
  let text = fs.readFileSync(file, "utf8");
  const before = text;
  text = text.replace(/FulfillmentErrorCodes\.(\w+)/g, (full, name) =>
    nameToValue.has(name) ? `"${nameToValue.get(name)}"` : full
  );
  text = text.replace(/throw new InvalidOperationException\("/g, 'throw new ContractOperationException("');
  text = text.replace(/using Tooba\.Fulfillment\.Contracts\.Errors;\r?\n\r?\n/g, "");
  if (text !== before) {
    fs.writeFileSync(file, text);
    n++;
    console.log("reverted", path.relative(root, file));
  }
}
console.log("files:", n);
