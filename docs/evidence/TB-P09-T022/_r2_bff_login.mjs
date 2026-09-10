import { writeFileSync, readFileSync } from "node:fs";
import { execFileSync } from "node:child_process";

const fx = JSON.parse(readFileSync("docs/evidence/TB-P09-T022/r2-fixture.json", "utf8"));
const cookieJar = ".tmp-r2-cookies.txt";

function curl(args) {
  const raw = execFileSync("curl.exe", ["-sS", "-w", "\n%{http_code}", ...args], { encoding: "utf8" });
  const idx = raw.lastIndexOf("\n");
  return { text: raw.slice(0, idx), status: Number(raw.slice(idx + 1)) };
}

curl(["-c", cookieJar, "http://127.0.0.1:3000/api/auth/csrf"]);
const jar = readFileSync(cookieJar, "utf8");
const csrf = jar
  .split(/\r?\n/)
  .find((l) => l.includes("tooba_csrf"))
  ?.split(/\s+/)
  .at(-1);
if (!csrf) throw new Error("no csrf cookie");

const login = curl([
  "-b",
  cookieJar,
  "-c",
  cookieJar,
  "-H",
  "Content-Type: application/json",
  "-H",
  `X-Tooba-Csrf: ${csrf}`,
  "--data-binary",
  JSON.stringify({
    identifierKind: "Email",
    identifier: fx.email,
    password: fx.password,
  }),
  "http://127.0.0.1:3000/api/auth/login",
]);
console.log("login", login.status, login.text);

const ful = curl([
  "-b",
  cookieJar,
  `http://127.0.0.1:3000/api/customer/orders/${fx.checkoutId}/fulfillments`,
]);
console.log("ful", ful.status, ful.text.slice(0, 400));

const od = curl([
  "-b",
  cookieJar,
  `http://127.0.0.1:3000/api/customer/orders/${fx.checkoutId}`,
]);
console.log("order", od.status, od.text.slice(0, 200));

// Export cookies for CDP
const cookies = [];
for (const line of readFileSync(cookieJar, "utf8").split(/\r?\n/)) {
  if (!line || line.startsWith("#")) continue;
  const parts = line.split(/\t/);
  if (parts.length < 7) continue;
  cookies.push({
    name: parts[5],
    value: parts[6],
    domain: "127.0.0.1",
    path: parts[2] || "/",
    httpOnly: parts[5] !== "tooba_csrf",
    secure: false,
  });
}
writeFileSync(".tmp-r2-browser-cookies.json", JSON.stringify({ checkoutId: fx.checkoutId, cookies }, null, 2));
console.log("cookies", cookies.map((c) => c.name));
