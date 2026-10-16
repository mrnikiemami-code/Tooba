// TB-132 evidence: decode a browser CDP screenshot JSON into a PNG under the evidence folder.
const fs = require("fs");
const path = require("path");

const src = process.argv[2];
const dest = process.argv[3];
if (!src || !dest) {
    console.error("usage: node decode-screenshot-132.js <cdp-json> <out.png>");
    process.exit(2);
}
const raw = fs.readFileSync(src, "utf8");
const parsed = JSON.parse(raw);
const data = parsed?.result?.data ?? parsed?.data ?? null;
if (!data) {
    console.error("NO_DATA_FIELD");
    process.exit(3);
}
fs.mkdirSync(path.dirname(dest), { recursive: true });
const buf = Buffer.from(data, "base64");
fs.writeFileSync(dest, buf);
console.log(`WROTE=${dest} BYTES=${buf.length}`);
