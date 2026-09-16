import sharp from "sharp";
import { mkdirSync } from "node:fs";
import { join } from "node:path";

const packs = [
  { folder: "template-shoes", card: "shoes.jpg", hue: "#1c1917", accent: "#f97316", motif: "shoe" },
  { folder: "template-plants", card: "plants.jpg", hue: "#14532d", accent: "#86efac", motif: "leaf" },
  { folder: "template-beauty", card: "beauty.jpg", hue: "#831843", accent: "#f9a8d4", motif: "beauty" },
];

function sceneSvg(pack, index) {
  const n = index;
  if (pack.motif === "shoe") {
    return `<svg xmlns="http://www.w3.org/2000/svg" width="960" height="640" viewBox="0 0 960 640">
      <rect width="960" height="640" fill="#fafaf9"/>
      <ellipse cx="${380 + n * 4}" cy="360" rx="220" ry="70" fill="${pack.hue}"/>
      <path d="M220 ${300 + n} Q360 220 520 ${280 + n} L560 340 Q400 380 240 340 Z" fill="${pack.accent}"/>
      <rect x="620" y="${160 + n * 8}" width="240" height="40" rx="10" fill="${pack.hue}" opacity="0.7"/>
      <text x="40" y="60" fill="${pack.hue}" font-size="28" font-family="Arial">SHOE ${n}</text>
    </svg>`;
  }
  if (pack.motif === "leaf") {
    return `<svg xmlns="http://www.w3.org/2000/svg" width="960" height="640" viewBox="0 0 960 640">
      <rect width="960" height="640" fill="#ecfdf5"/>
      <ellipse cx="${300 + n * 6}" cy="280" rx="90" ry="160" fill="${pack.accent}" transform="rotate(-20 ${300 + n * 6} 280)"/>
      <ellipse cx="${420 + n * 4}" cy="300" rx="70" ry="140" fill="${pack.hue}" opacity="0.75" transform="rotate(15 ${420 + n * 4} 300)"/>
      <rect x="360" y="420" width="40" height="120" rx="8" fill="#78350f"/>
      <text x="40" y="60" fill="${pack.hue}" font-size="28" font-family="Arial">PLANT ${n}</text>
    </svg>`;
  }
  return `<svg xmlns="http://www.w3.org/2000/svg" width="960" height="640" viewBox="0 0 960 640">
    <defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0%" stop-color="#fff1f2"/><stop offset="100%" stop-color="${pack.accent}"/></linearGradient></defs>
    <rect width="960" height="640" fill="url(#bg)"/>
    <circle cx="${320 + n * 5}" cy="300" r="120" fill="${pack.hue}" opacity="0.85"/>
    <rect x="520" y="${200 + n * 10}" width="280" height="48" rx="24" fill="#fff"/>
    <rect x="520" y="${280 + n * 10}" width="220" height="36" rx="18" fill="${pack.hue}" opacity="0.5"/>
    <text x="40" y="60" fill="${pack.hue}" font-size="28" font-family="Arial">BEAUTY ${n}</text>
  </svg>`;
}

for (const pack of packs) {
  const dir = join("public/images", pack.folder);
  mkdirSync(dir, { recursive: true });
  for (let i = 1; i <= 8; i++) {
    await sharp(Buffer.from(sceneSvg(pack, i))).jpeg({ quality: 88 }).toFile(join(dir, `${i}.jpg`));
    console.log(pack.folder, i, "ok");
  }
  mkdirSync(join("public/images/industry-templates"), { recursive: true });
  await sharp(Buffer.from(sceneSvg(pack, 1))).resize(640, 640).jpeg({ quality: 90 }).toFile(join("public/images/industry-templates", pack.card));
  console.log("industry-templates/" + pack.card, "ok");
}
