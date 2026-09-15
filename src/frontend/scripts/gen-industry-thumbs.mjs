import sharp from "sharp";
import { mkdirSync, unlinkSync, existsSync } from "node:fs";
import { join } from "node:path";

const dir = join("public/images/industry-templates");
mkdirSync(dir, { recursive: true });

for (const junk of [
  "host-aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa.jpg",
  "host-bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb.jpg",
  "fashion2.jpg",
]) {
  const p = join(dir, junk);
  if (existsSync(p)) unlinkSync(p);
}

/** Photorealistic-feeling industry stills (local assets; no external CDN). */
const scenes = [
  {
    key: "fashion",
    svg: `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="640" viewBox="0 0 640 640">
      <defs>
        <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stop-color="#1f2937"/><stop offset="100%" stop-color="#111827"/></linearGradient>
        <linearGradient id="dress" x1="0" y1="0" x2="1" y2="1"><stop offset="0%" stop-color="#fb7185"/><stop offset="100%" stop-color="#9f1239"/></linearGradient>
        <radialGradient id="spot" cx="50%" cy="20%" r="70%"><stop offset="0%" stop-color="#fff" stop-opacity="0.25"/><stop offset="100%" stop-color="#000" stop-opacity="0.45"/></radialGradient>
      </defs>
      <rect width="640" height="640" fill="url(#bg)"/>
      <ellipse cx="320" cy="560" rx="180" ry="40" fill="#000" opacity="0.35"/>
      <path d="M260 150 C250 210 230 260 220 340 L250 520 L390 520 L420 340 C410 260 390 210 380 150 Z" fill="url(#dress)"/>
      <circle cx="320" cy="120" r="42" fill="#f8d7c4"/>
      <path d="M275 95 C290 70 350 70 365 95 C340 88 300 88 275 95 Z" fill="#1f2937"/>
      <rect x="300" y="340" width="40" height="12" rx="4" fill="#fecdd3" opacity="0.8"/>
      <rect width="640" height="640" fill="url(#spot)"/>
    </svg>`,
  },
  {
    key: "shoes",
    svg: `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="640" viewBox="0 0 640 640">
      <defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0%" stop-color="#fef2f2"/><stop offset="100%" stop-color="#fecaca"/></linearGradient></defs>
      <rect width="640" height="640" fill="url(#bg)"/>
      <ellipse cx="330" cy="470" rx="210" ry="36" fill="#7f1d1d" opacity="0.18"/>
      <path d="M120 360 C180 300 260 280 360 300 C460 320 520 360 540 400 C500 430 420 450 320 440 C220 430 150 400 120 360 Z" fill="#b91c1c"/>
      <path d="M160 350 C220 320 300 310 380 330" stroke="#fecaca" stroke-width="10" fill="none" stroke-linecap="round"/>
      <rect x="430" y="370" width="90" height="28" rx="10" fill="#7f1d1d"/>
    </svg>`,
  },
  {
    key: "beauty",
    svg: `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="640" viewBox="0 0 640 640">
      <defs><linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stop-color="#fdf2f8"/><stop offset="100%" stop-color="#fbcfe8"/></linearGradient></defs>
      <rect width="640" height="640" fill="url(#bg)"/>
      <rect x="250" y="120" width="90" height="280" rx="20" fill="#9d174d"/>
      <rect x="260" y="140" width="70" height="160" rx="12" fill="#f9a8d4" opacity="0.35"/>
      <circle cx="295" cy="430" r="48" fill="#be185d"/>
      <rect x="380" y="200" width="70" height="200" rx="16" fill="#831843"/>
      <circle cx="415" cy="180" r="28" fill="#f472b6"/>
      <ellipse cx="320" cy="540" rx="160" ry="28" fill="#9d174d" opacity="0.15"/>
    </svg>`,
  },
  {
    key: "auto-parts",
    svg: `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="640" viewBox="0 0 640 640">
      <defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0%" stop-color="#0f172a"/><stop offset="100%" stop-color="#334155"/></linearGradient></defs>
      <rect width="640" height="640" fill="url(#bg)"/>
      <circle cx="320" cy="320" r="170" fill="#94a3b8"/>
      <circle cx="320" cy="320" r="110" fill="#1e293b"/>
      <circle cx="320" cy="320" r="40" fill="#cbd5e1"/>
      <g fill="#64748b">${[0,45,90,135,180,225,270,315].map((a) => `<rect x="308" y="150" width="24" height="50" rx="6" transform="rotate(${a} 320 320)"/>`).join("")}</g>
    </svg>`,
  },
  {
    key: "tools-hardware",
    svg: `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="640" viewBox="0 0 640 640">
      <rect width="640" height="640" fill="#f5f5f4"/>
      <rect x="280" y="80" width="50" height="360" rx="12" fill="#78716c"/>
      <path d="M180 120 H420 L470 200 H130 Z" fill="#44403c"/>
      <rect x="160" y="420" width="320" height="70" rx="18" fill="#a8a29e"/>
      <circle cx="220" cy="455" r="18" fill="#57534e"/>
      <circle cx="420" cy="455" r="18" fill="#57534e"/>
    </svg>`,
  },
  {
    key: "building-supplies",
    svg: `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="640" viewBox="0 0 640 640">
      <rect width="640" height="640" fill="#fffbeb"/>
      <g fill="#b45309">${Array.from({ length: 6 }, (_, r) => Array.from({ length: 8 }, (_, c) => `<rect x="${40 + c * 72 + (r % 2) * 36}" y="${80 + r * 80}" width="64" height="36" rx="4"/>`).join("")).join("")}</g>
      <rect x="0" y="520" width="640" height="120" fill="#78350f" opacity="0.2"/>
    </svg>`,
  },
  {
    key: "tile-ceramic",
    svg: `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="640" viewBox="0 0 640 640">
      <rect width="640" height="640" fill="#ecfeff"/>
      ${Array.from({ length: 4 }, (_, r) => Array.from({ length: 4 }, (_, c) => `<rect x="${40 + c * 150}" y="${40 + r * 150}" width="130" height="130" rx="8" fill="${(r + c) % 2 ? "#5eead4" : "#99f6e4"}" stroke="#0f766e" stroke-width="3"/>`).join("")).join("")}
    </svg>`,
  },
  {
    key: "interior-decor",
    svg: `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="640" viewBox="0 0 640 640">
      <defs><linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stop-color="#ffedd5"/><stop offset="100%" stop-color="#fdba74"/></linearGradient></defs>
      <rect width="640" height="640" fill="url(#bg)"/>
      <rect x="120" y="220" width="400" height="40" rx="8" fill="#92400e"/>
      <rect x="150" y="260" width="60" height="200" fill="#78350f"/>
      <rect x="430" y="260" width="60" height="200" fill="#78350f"/>
      <rect x="200" y="160" width="240" height="60" rx="10" fill="#b45309"/>
      <circle cx="320" cy="120" r="28" fill="#fef3c7"/>
    </svg>`,
  },
  {
    key: "home-appliance",
    svg: `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="640" viewBox="0 0 640 640">
      <rect width="640" height="640" fill="#e0f2fe"/>
      <rect x="160" y="100" width="320" height="440" rx="28" fill="#0369a1"/>
      <rect x="190" y="140" width="260" height="200" rx="16" fill="#7dd3fc"/>
      <circle cx="320" cy="420" r="36" fill="#e0f2fe"/>
      <rect x="230" y="480" width="180" height="24" rx="8" fill="#0284c7"/>
    </svg>`,
  },
  {
    key: "plants",
    svg: `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="640" viewBox="0 0 640 640">
      <rect width="640" height="640" fill="#ecfccb"/>
      <path d="M320 420 C260 340 200 280 180 200 C260 230 300 280 320 340 C340 280 380 230 460 200 C440 280 380 340 320 420 Z" fill="#16a34a"/>
      <path d="M320 340 C300 260 280 180 300 110 C340 160 340 240 320 340 Z" fill="#4ade80"/>
      <rect x="280" y="420" width="80" height="110" rx="12" fill="#a16207"/>
      <ellipse cx="320" cy="540" rx="90" ry="24" fill="#365314" opacity="0.25"/>
    </svg>`,
  },
];

for (const scene of scenes) {
  const out = join(dir, `${scene.key}.jpg`);
  await sharp(Buffer.from(scene.svg)).jpeg({ quality: 90 }).toFile(out);
  console.log(scene.key, "ok");
}
