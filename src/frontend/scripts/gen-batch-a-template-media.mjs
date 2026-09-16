import sharp from "sharp";
import { mkdirSync } from "node:fs";
import { join } from "node:path";

/**
 * Generate isolated Batch A template media (no Fashion asset reuse).
 * Output: public/images/template-{auto-parts|building-materials|tools-hardware}/{1..8}.jpg
 */

const packs = [
  {
    folder: "template-auto-parts",
    hue: "#0f172a",
    accent: "#38bdf8",
    motif: "gear",
  },
  {
    folder: "template-building-materials",
    hue: "#78350f",
    accent: "#fbbf24",
    motif: "brick",
  },
  {
    folder: "template-tools-hardware",
    hue: "#44403c",
    accent: "#facc15",
    motif: "hammer",
  },
];

function sceneSvg(pack, index) {
  const n = index;
  const shift = n * 17;
  if (pack.motif === "gear") {
    return `<svg xmlns="http://www.w3.org/2000/svg" width="960" height="640" viewBox="0 0 960 640">
      <defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0%" stop-color="${pack.hue}"/><stop offset="100%" stop-color="#334155"/></linearGradient></defs>
      <rect width="960" height="640" fill="url(#bg)"/>
      <circle cx="${280 + shift}" cy="320" r="140" fill="#94a3b8"/>
      <circle cx="${280 + shift}" cy="320" r="88" fill="#1e293b"/>
      <circle cx="${280 + shift}" cy="320" r="34" fill="${pack.accent}"/>
      <rect x="520" y="${180 + (n % 3) * 20}" width="320" height="48" rx="10" fill="${pack.accent}" opacity="0.85"/>
      <rect x="520" y="${260 + (n % 3) * 20}" width="260" height="36" rx="8" fill="#cbd5e1" opacity="0.55"/>
      <text x="40" y="60" fill="#e2e8f0" font-size="28" font-family="Arial">AUTO ${n}</text>
    </svg>`;
  }
  if (pack.motif === "brick") {
    const bricks = Array.from({ length: 5 }, (_, r) =>
      Array.from({ length: 8 }, (_, c) => {
        const x = 40 + c * 110 + (r % 2) * 40;
        const y = 80 + r * 90 + (n % 2) * 8;
        return `<rect x="${x}" y="${y}" width="96" height="48" rx="4" fill="${(r + c + n) % 2 ? pack.accent : "#b45309"}"/>`;
      }).join(""),
    ).join("");
    return `<svg xmlns="http://www.w3.org/2000/svg" width="960" height="640" viewBox="0 0 960 640">
      <rect width="960" height="640" fill="#fffbeb"/>
      ${bricks}
      <rect x="0" y="520" width="960" height="120" fill="${pack.hue}" opacity="0.25"/>
      <text x="40" y="60" fill="${pack.hue}" font-size="28" font-family="Arial">BUILD ${n}</text>
    </svg>`;
  }
  return `<svg xmlns="http://www.w3.org/2000/svg" width="960" height="640" viewBox="0 0 960 640">
    <rect width="960" height="640" fill="#f5f5f4"/>
    <rect x="${260 + n * 8}" y="90" width="46" height="340" rx="10" fill="${pack.hue}"/>
    <path d="M180 ${130 + n * 4} H420 L470 ${210 + n * 4} H130 Z" fill="#292524"/>
    <rect x="180" y="430" width="360" height="70" rx="16" fill="${pack.accent}"/>
    <circle cx="240" cy="465" r="16" fill="#57534e"/>
    <circle cx="480" cy="465" r="16" fill="#57534e"/>
    <rect x="620" y="${160 + n * 12}" width="220" height="40" rx="8" fill="${pack.hue}" opacity="0.7"/>
    <text x="40" y="60" fill="${pack.hue}" font-size="28" font-family="Arial">TOOLS ${n}</text>
  </svg>`;
}

for (const pack of packs) {
  const dir = join("public/images", pack.folder);
  mkdirSync(dir, { recursive: true });
  for (let i = 1; i <= 8; i++) {
    const out = join(dir, `${i}.jpg`);
    await sharp(Buffer.from(sceneSvg(pack, i))).jpeg({ quality: 88 }).toFile(out);
    console.log(pack.folder, i, "ok");
  }
}

// Industry selector card for renamed building-materials key
await sharp(Buffer.from(sceneSvg(packs[1], 1)))
  .resize(640, 640)
  .jpeg({ quality: 90 })
  .toFile(join("public/images/industry-templates", "building-materials.jpg"));
console.log("industry-templates/building-materials.jpg ok");
