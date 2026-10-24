// TB-207 task-owned READ-ONLY PostgreSQL probe (no dependencies; node:crypto + node:net).
// Read-only by construction: startup options force default_transaction_read_only=on and a 5s
// statement_timeout, and the script only ever issues SELECT statements. It never prints the
// connection string, password, host credentials or any business payload.
import net from "node:net";
import crypto from "node:crypto";
import fs from "node:fs";

const settingsPath = process.argv[2];
const raw = JSON.parse(fs.readFileSync(settingsPath, "utf8").replace(/^\uFEFF/, ""));
const dbRefs = raw.Tooba.PostgreSQL.ConnectionReferences;
const alias = process.argv[3] ?? "tenant-alpha";
const cs = dbRefs[alias];
if (!cs) throw new Error("alias-not-configured");
const parts = Object.fromEntries(cs.split(";").filter(Boolean).map((kv) => {
  const i = kv.indexOf("=");
  return [kv.slice(0, i).toLowerCase(), kv.slice(i + 1)];
}));
const host = parts.host ?? "127.0.0.1";
const port = Number(parts.port ?? 5432);
const user = parts.username;
const password = parts.password;
const database = parts.database;

const buf = [];
function msg(type, payload) {
  const head = Buffer.alloc(5);
  head.write(type, 0, "latin1");
  head.writeInt32BE(payload.length + 4, 1);
  return Buffer.concat([head, payload]);
}
function cstr(s) { return Buffer.from(s + "\0", "utf8"); }

const socket = net.connect({ host, port });
socket.setNoDelay(true);
let pending = Buffer.alloc(0);
let authed = false;
let queryQueue = [];
let current = null;
let saslState = null;
let results = [];
let errorSeen = null;

const startupParams = Buffer.concat([
  cstr("user"), cstr(user),
  cstr("database"), cstr(database),
  cstr("application_name"), cstr("tb207-readonly-probe"),
  cstr("options"), cstr("-c default_transaction_read_only=on -c statement_timeout=5000"),
  Buffer.from([0]),
]);
const startupHead = Buffer.alloc(8);
startupHead.writeInt32BE(startupParams.length + 8, 0);
startupHead.writeInt32BE(196608, 4); // protocol 3.0
const startup = Buffer.concat([startupHead, startupParams]);
socket.write(startup);

function sendQuery(sql) {
  current = { sql, rows: [], columns: [] };
  const p = Buffer.concat([cstr(sql)]);
  socket.write(msg("Q", p));
}

function next() {
  if (queryQueue.length === 0) {
    socket.end();
    return;
  }
  sendQuery(queryQueue.shift());
}

function handleAuth(type, body) {
  if (type === 0) { authed = true; return; }
  if (type === 10) {
    const mechs = body.toString("utf8", 4).split("\0").filter(Boolean);
    if (!mechs.includes("SCRAM-SHA-256")) throw new Error("scram-unavailable");
    const nonce = crypto.randomBytes(18).toString("base64");
    const clientFirstBare = `n=,r=${nonce}`;
    const initial = Buffer.from("n,," + clientFirstBare, "utf8");
    const payload = Buffer.concat([
      cstr("SCRAM-SHA-256"),
      (() => { const b = Buffer.alloc(4); b.writeInt32BE(initial.length, 0); return b; })(),
      initial,
    ]);
    saslState = { nonce, clientFirstBare };
    socket.write(msg("p", payload));
    return;
  }
  if (type === 11) {
    const serverFirst = body.toString("utf8", 4);
    const fields = Object.fromEntries(serverFirst.split(",").map((kv) => [kv[0], kv.slice(2)]));
    const salt = Buffer.from(fields.s, "base64");
    const iterations = Number(fields.i);
    const salted = crypto.pbkdf2Sync(password, salt, iterations, 32, "sha256");
    const clientKey = crypto.createHmac("sha256", salted).update("Client Key").digest();
    const storedKey = crypto.createHash("sha256").update(clientKey).digest();
    const withoutProof = `c=biws,r=${fields.r}`;
    const authMessage = `${saslState.clientFirstBare},${serverFirst},${withoutProof}`;
    const clientSig = crypto.createHmac("sha256", storedKey).update(authMessage).digest();
    const proof = Buffer.alloc(clientKey.length);
    for (let i = 0; i < clientKey.length; i++) proof[i] = clientKey[i] ^ clientSig[i];
    saslState.authMessage = authMessage;
    saslState.salted = salted;
    socket.write(msg("p", Buffer.from(`${withoutProof},p=${proof.toString("base64")}`, "utf8")));
    return;
  }
  if (type === 12) {
    const serverFinal = body.toString("utf8", 4);
    const v = serverFinal.startsWith("v=") ? serverFinal.slice(2) : null;
    const serverKey = crypto.createHmac("sha256", saslState.salted).update("Server Key").digest();
    const expect = crypto.createHmac("sha256", serverKey).update(saslState.authMessage).digest("base64");
    if (v !== expect) throw new Error("scram-server-signature-mismatch");
    return;
  }
  throw new Error("unsupported-auth-type:" + type);
}

socket.on("data", (chunk) => {
  pending = Buffer.concat([pending, chunk]);
  while (pending.length >= 5) {
    const type = String.fromCharCode(pending[0]);
    const len = pending.readInt32BE(1);
    if (pending.length < len + 1) break;
    const body = pending.subarray(5, len + 1);
    pending = pending.subarray(len + 1);
    switch (type) {
      case "R": handleAuth(body.readInt32BE(0), body); break;
      case "S": break;
      case "K": break;
      case "Z":
        if (current) { results.push(current); current = null; }
        next();
        break;
      case "T": {
        const count = body.readInt16BE(0);
        let off = 2; const cols = [];
        for (let i = 0; i < count; i++) {
          const end = body.indexOf(0, off);
          cols.push(body.toString("utf8", off, end));
          off = end + 1 + 18;
        }
        if (current) current.columns = cols;
        break;
      }
      case "D": {
        const count = body.readInt16BE(0);
        let off = 2; const row = [];
        for (let i = 0; i < count; i++) {
          const l = body.readInt32BE(off); off += 4;
          if (l < 0) { row.push(null); continue; }
          row.push(body.toString("utf8", off, off + l)); off += l;
        }
        if (current) current.rows.push(row);
        break;
      }
      case "E": {
        const text = body.toString("utf8", 0);
        errorSeen = text.replace(/\0/g, " ").trim();
        break;
      }
      case "C":
        if (current) { results.push(current); current = null; }
        break;
      case "N": case "I": break;
      default: break;
    }
  }
});

socket.on("error", (e) => {
  console.log(JSON.stringify({ connectionError: e.code ?? e.message }));
  process.exit(2);
});
socket.on("close", () => {
  const out = { alias, ok: !errorSeen, error: errorSeen, results };
  console.log(JSON.stringify(out));
});

queryQueue = process.argv.slice(4);
if (queryQueue.length === 0) {
  queryQueue = [
    "select current_database() as db, current_user as usr, current_setting('default_transaction_read_only') as ro",
    "select table_schema, table_name from information_schema.tables where table_name = '__ef_migrations_history' order by 1",
  ];
}
