// TB-TMAR-RETURNS-AMSC-001-W3-R1 — mutation negative proof (durable, reproducible).
//
// Applies a *temporary* on-disk mutation that replaces one endpoint-dispatched request with another while
// preserving the overall ISender.Send count, runs both the historical W3 guard and the new W3-R1
// set-equality guard, then restores the file byte-identically. The mutation is never committed.
//
// Run from the repository root:  node docs/architecture/evidence/TB-TMAR-RETURNS-AMSC-001-W3-R1/mutation-proof.js
const { execSync } = require("child_process");
const crypto = require("crypto");
const fs = require("fs");
const path = require("path");

const FILE = path.join(
    "src", "backend", "Modules", "Returns",
    "Tooba.Returns.Endpoints", "Customer", "ReturnCustomerEndpoints.cs");
const FROM = "new GetCustomerReturnQuery(actor.Value, returnRequestId)";
const TO = "new ListCustomerReturnsQuery(actor.Value)";

const sha = (buffer) => crypto.createHash("sha256").update(buffer).digest("hex");

function run(filter) {
    // A failing test run is the expected outcome here, so a non-zero exit code must not abort the proof.
    let out;
    try {
        out = execSync(
            `dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~${filter}" --nologo`,
            { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] });
    } catch (err) {
        out = `${err.stdout ?? ""}${err.stderr ?? ""}`;
    }
    const summary = out.match(/(Passed|Failed)!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)/);
    return summary ? summary[0].trim() : "no summary";
}

function main() {
    const original = fs.readFileSync(FILE);
    const originalHash = sha(original);

    try {
        const text = original.toString("utf8");
        if (!text.includes(FROM)) {
            throw new Error("mutation anchor not found; the endpoint dispatch already changed");
        }

        const mutated = text.replace(FROM, TO);
        const sendCountBefore = (text.match(/sender\.Send\(/g) || []).length;
        const sendCountAfter = (mutated.match(/sender\.Send\(/g) || []).length;
        if (sendCountBefore !== sendCountAfter) {
            throw new Error("mutation must preserve the ISender.Send count to be a valid negative proof");
        }

        fs.writeFileSync(FILE, mutated, "utf8");
        console.log(`mutation applied: ${FROM}  ->  ${TO}`);
        console.log(`ISender.Send count preserved: ${sendCountBefore} -> ${sendCountAfter}`);

        console.log("HISTORICAL_W3_GUARD :", run("ReturnsModuleAmsc001W3CertGuardTests"));
        console.log("NEW_W3_R1_GUARD     :", run("ReturnsModuleAmsc001W3R1SetEqualityGuardTests"));
    } finally {
        fs.writeFileSync(FILE, original);
        const restoredHash = sha(fs.readFileSync(FILE));
        console.log(`file restored byte-identically: ${restoredHash === originalHash} (sha256 ${restoredHash})`);
    }
}

main();
