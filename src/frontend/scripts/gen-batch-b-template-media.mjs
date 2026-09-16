import sharp from "sharp";
import { mkdirSync } from "node:fs";
import { join } from "node:path";

/**
 * Generate isolated Batch B template media (no Fashion / Batch A reuse).
 * Output: public/images/template-{tile-ceramic|interior-decor|home-appliances}/{1..8}.jpg
 */

const packs = [
  {
    folder: "template-tile-ceramic",
    card: "tile-ceramic.jpg",
    hue: "#1e3a5f",
    accent: "#67e8f9",
    motif: "tile",
  },
  {
    folder: "template-interior-decor",
    card: "interior-decor.jpg",
    hue: "#4a3728",
    accent: "#d6b48a",
    motif: "decor",
  },
  {
    folder: "template-home-appliances",
    card: "home-appliances.jpg",
    hue: "#0c4a6e",
    accent: "#7dd3fc",
    motif: "appliance",
  },
];

function sceneSvg(pack, index) {
  const n = index;
  if (pack.motif === "tile") {
    const tiles = Array.from({ length: 4 }, (_, r) =>
      Array.from({ length: 6 }, (_, c) => {
        const x = 60 + c * 140;
        const y = 90 + r * 120 + (n % 2) * 6;
        return `<rect x="${x}" y="${y}" width="120" height="100" rx="6" fill="${(r + c + n) % 2 ? pack.accent : "#94a3b8"}" opacity="0.9"/>`;
      }).join(""),
    ).join("");
    return `<svg xmlns="http://www.w3.org/2000/svg" width="960" height="640" viewBox="0 0 960 640">
      <rect width="960" height="640" fill="#0f172a"/>
      ${tiles}
      <rect x="0" y="540" width="960" height="100" fill="${pack.hue}" opacity="0.55"/>
      <text x="40" y="60" fill="#e0f2fe" font-size="28" font-family="Arial">TILE ${n}</text>
    </svg>`;
  }
  if (pack.motif === "decor") {
    return `<svg xmlns="http://www.w3.org/2000/svg" width="960" height="640" viewBox="0 0 960 640">
      <defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0%" stop-color="#faf6f1"/><stop offset="100%" stop-color="${pack.accent}"/></linearGradient></defs>
      <rect width="960" height="640" fill="url(#bg)"/>
      <rect x="${120 + n * 6}" y="180" width="280" height="160" rx="18" fill="${pack.hue}" opacity="0.85"/>
      <rect x="${440 + n * 4}" y="220" width="180" height="220" rx="12" fill="#fff7ed" stroke="${pack.hue}" stroke-width="4"/>
      <circle cx="720" cy="250" r="70" fill="${pack.accent}"/>
      <rect x="80" y="420" width="800" height="28" rx="8" fill="${pack.hue}" opacity="0.35"/>
      <text x="40" y="60" fill="${pack.hue}" font-size="28" font-family="Arial">DECOR ${n}</text>
    </svg>`;
  }
  return `<svg xmlns="http://www.w3.org/2000/svg" width="960" height="640" viewBox="0 0 960 640">
    <rect width="960" height="640" fill="#f0f9ff"/>
    <rect x="${200 + n * 5}" y="120" width="220" height="360" rx="24" fill="${pack.hue}"/>
    <rect x="${240 + n * 5}" y="160" width="140" height="80" rx="8" fill="${pack.accent}"/>
    <rect x="${500 + n * 3}" y="180" width="260" height="160" rx="16" fill="#e2e8f0"/>
    <circle cx="${630 + n}" cy="420" r="48" fill="${pack.accent}"/>
    <text x="40" y="60" fill="${pack.hue}" font-size="28" font-family="Arial">APPL ${n}</text>
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
  mkdirSync(join("public/images/industry-templates"), { recursive: true });
  await sharp(Buffer.from(sceneSvg(pack, 1)))
    .resize(640, 640)
    .jpeg({ quality: 90 })
    .toFile(join("public/images/industry-templates", pack.card));
  console.log("industry-templates/" + pack.card, "ok");
}
