// One-off resx generator for the W1 Fulfillment error resources (not shipped).
const fs = require("fs");
const path = require("path");

const src = fs.readFileSync(path.join(__dirname, "gen-catalog.js"), "utf8");
const m = src.match(/const rows = \[([\s\S]*?)\n\];/);
// eslint-disable-next-line no-eval
const rows = eval("[" + m[1] + "]");

const head =
  '<?xml version="1.0" encoding="utf-8"?>\n' +
  "<root>\n" +
  '  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>\n' +
  '  <resheader name="version"><value>2.0</value></resheader>\n' +
  '  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>\n' +
  '  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>\n';

const esc = (s) => s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");

let en = head;
let fa = head;
for (const r of rows) {
  en += `  <data name="${r[1]}" xml:space="preserve">\n    <value>${esc(r[4])}</value>\n  </data>\n`;
  fa += `  <data name="${r[1]}" xml:space="preserve">\n    <value>${esc(r[5])}</value>\n  </data>\n`;
}
en += "</root>\n";
fa += "</root>\n";

const dir = path.resolve(__dirname, "../../../../src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Resources");
fs.mkdirSync(dir, { recursive: true });
fs.writeFileSync(path.join(dir, "FulfillmentErrors.resx"), en);
fs.writeFileSync(path.join(dir, "FulfillmentErrors.fa.resx"), fa);
console.log("resx keys:", rows.length, "->", dir);
