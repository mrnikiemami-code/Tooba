import fs from "node:fs";
import path from "node:path";

const base = "http://127.0.0.1:3000";
const outDir = path.resolve("docs/evidence/TB-P06-T016");

async function status(url, follow = true) {
  try {
    const response = await fetch(url, { redirect: follow ? "follow" : "manual" });
    return response.status;
  } catch {
    return 0;
  }
}

async function redirectLocation(url) {
  try {
    const response = await fetch(url, { redirect: "manual" });
    return response.headers.get("location");
  } catch {
    return null;
  }
}

const proof = {
  recordedAtUtc: new Date().toISOString(),
  runtime: {
    hostLive: await status("http://127.0.0.1:5088/health/live"),
    hostReady: await status("http://127.0.0.1:5088/health/ready"),
    faHome: await status(`${base}/fa`),
    enHome: await status(`${base}/en`),
    faBlogs: await status(`${base}/fa/blogs`),
    enBlogs: await status(`${base}/en/blogs`),
    faProducts: await status(`${base}/fa/products`),
    enProducts: await status(`${base}/en/products`),
    shopeiva: await status("http://127.0.0.1:3001/"),
  },
  redirects: {
    rootToFa: await redirectLocation(`${base}/`),
    productsToPrefixed: await redirectLocation(`${base}/products`),
    blogsToPrefixed: await redirectLocation(`${base}/blogs`),
    invalidLocale: await status(`${base}/fr/products`),
  },
};

fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(path.join(outDir, "_locale-routing-api-proof.json"), JSON.stringify(proof, null, 2));
console.log(JSON.stringify(proof, null, 2));
