/**
 * Generates public/og-image.png  (1200 × 630 px)
 * Run: node generate-og-image.mjs
 */
import sharp from 'sharp';
import { readFileSync, writeFileSync } from 'fs';
import { fileURLToPath } from 'url';
import path from 'path';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT      = path.join(__dirname, 'public', 'og-image.png');
const LOGO_SRC = path.join(__dirname, 'public', 'logo.png');

// ── Resize logo to fit nicely inside the card ────────────────────────────────
const logoWidth  = 340;
const logoHeight = 133;   // keeps ~1364×535 aspect ratio

const logoResized = await sharp(LOGO_SRC)
  .resize(logoWidth, logoHeight, { fit: 'inside', background: { r:0,g:0,b:0,alpha:0 } })
  .png()
  .toBuffer();

const logoB64 = logoResized.toString('base64');

// ── SVG canvas (1200 × 630) ──────────────────────────────────────────────────
const svg = `
<svg width="1200" height="630" viewBox="0 0 1200 630"
     xmlns="http://www.w3.org/2000/svg"
     xmlns:xlink="http://www.w3.org/1999/xlink">
  <defs>
    <!-- Background gradient: deep navy → dark teal -->
    <linearGradient id="bg" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%"   stop-color="#0f0f23"/>
      <stop offset="60%"  stop-color="#1a1a3e"/>
      <stop offset="100%" stop-color="#0d2137"/>
    </linearGradient>

    <!-- Accent glow circle -->
    <radialGradient id="glow" cx="72%" cy="35%" r="45%">
      <stop offset="0%"   stop-color="#10b981" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#10b981" stop-opacity="0"/>
    </radialGradient>

    <!-- Card gradient -->
    <linearGradient id="card" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%"   stop-color="#1e1e3a"/>
      <stop offset="100%" stop-color="#16213e"/>
    </linearGradient>

    <clipPath id="roundedCard">
      <rect x="60" y="60" width="1080" height="510" rx="28"/>
    </clipPath>
  </defs>

  <!-- Background -->
  <rect width="1200" height="630" fill="url(#bg)"/>
  <rect width="1200" height="630" fill="url(#glow)"/>

  <!-- Decorative grid lines -->
  <g stroke="#10b981" stroke-opacity="0.06" stroke-width="1">
    <line x1="0"    y1="105" x2="1200" y2="105"/>
    <line x1="0"    y1="210" x2="1200" y2="210"/>
    <line x1="0"    y1="315" x2="1200" y2="315"/>
    <line x1="0"    y1="420" x2="1200" y2="420"/>
    <line x1="0"    y1="525" x2="1200" y2="525"/>
    <line x1="150"  y1="0"   x2="150"  y2="630"/>
    <line x1="300"  y1="0"   x2="300"  y2="630"/>
    <line x1="450"  y1="0"   x2="450"  y2="630"/>
    <line x1="600"  y1="0"   x2="600"  y2="630"/>
    <line x1="750"  y1="0"   x2="750"  y2="630"/>
    <line x1="900"  y1="0"   x2="900"  y2="630"/>
    <line x1="1050" y1="0"   x2="1050" y2="630"/>
  </g>

  <!-- Decorative chart bars (bottom right) -->
  <g opacity="0.12">
    <rect x="780" y="440" width="36" height="120" rx="4" fill="#10b981"/>
    <rect x="830" y="390" width="36" height="170" rx="4" fill="#10b981"/>
    <rect x="880" y="360" width="36" height="200" rx="4" fill="#10b981"/>
    <rect x="930" y="300" width="36" height="260" rx="4" fill="#10b981"/>
    <rect x="980" y="340" width="36" height="220" rx="4" fill="#10b981"/>
    <rect x="1030" y="270" width="36" height="290" rx="4" fill="#10b981"/>
    <rect x="1080" y="310" width="36" height="250" rx="4" fill="#10b981"/>
    <rect x="1130" y="250" width="36" height="310" rx="4" fill="#10b981"/>
  </g>

  <!-- Card -->
  <rect x="60" y="60" width="1080" height="510" rx="28"
        fill="url(#card)" fill-opacity="0.85"
        stroke="#10b981" stroke-opacity="0.25" stroke-width="1.5"/>

  <!-- Top accent bar -->
  <rect x="60" y="60" width="1080" height="5" rx="3" fill="#10b981" fill-opacity="0.7"/>

  <!-- Logo (embedded as base64) -->
  <image href="data:image/png;base64,${logoB64}"
         x="100" y="115" width="${logoWidth}" height="${logoHeight}"/>

  <!-- Tagline -->
  <text x="100" y="300"
        font-family="'Segoe UI', 'Helvetica Neue', Arial, sans-serif"
        font-size="32" font-weight="400" fill="#94a3b8" letter-spacing="0.3">
    Free investment portfolio tracker for Irish investors
  </text>

  <!-- Feature pills -->
  ${[
    { label: '📊 Real-time prices',       x: 100 },
    { label: '🧾 Deemed Disposal Tax',    x: 360 },
    { label: '📈 Projections',            x: 640 },
    { label: '🔗 Portfolio Sharing',      x: 840 },
  ].map(({ label, x }) => `
    <rect x="${x}" y="350" width="${label.length * 13.5 + 28}" height="44" rx="22"
          fill="#10b981" fill-opacity="0.15" stroke="#10b981" stroke-opacity="0.5" stroke-width="1"/>
    <text x="${x + 14}" y="378"
          font-family="'Segoe UI', 'Helvetica Neue', Arial, sans-serif"
          font-size="18" fill="#6ee7b7">${label}</text>
  `).join('')}

  <!-- Domain badge -->
  <text x="1140" y="540"
        font-family="'Segoe UI', 'Helvetica Neue', Arial, sans-serif"
        font-size="22" font-weight="600" fill="#475569"
        text-anchor="end">
    prt-fy.com
  </text>

  <!-- Thin bottom line -->
  <line x1="100" y1="490" x2="1100" y2="490"
        stroke="#10b981" stroke-opacity="0.18" stroke-width="1"/>
</svg>
`.trim();

// ── Render ───────────────────────────────────────────────────────────────────
await sharp(Buffer.from(svg))
  .png({ compressionLevel: 9 })
  .toFile(OUT);

console.log(`✅  OG image written to: ${OUT}`);

