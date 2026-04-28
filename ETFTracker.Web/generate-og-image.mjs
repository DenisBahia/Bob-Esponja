/**
 * Generates public/og-image.png  (1200 × 630 px)
 * Run: node generate-og-image.mjs
 */
import sharp from 'sharp';
import { fileURLToPath } from 'url';
import path from 'path';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT      = path.join(__dirname, 'public', 'og-image.png');
const LOGO_SRC = path.join(__dirname, 'public', 'logo.png');

// ── Resize logo ──────────────────────────────────────────────────────────────
const logoResized = await sharp(LOGO_SRC)
  .resize(380, 149, { fit: 'inside', background: { r:0,g:0,b:0,alpha:0 } })
  .png()
  .toBuffer();

const logoB64   = logoResized.toString('base64');
const logoMeta  = await sharp(logoResized).metadata();
const logoW     = logoMeta.width;
const logoH     = logoMeta.height;
// Centre the logo horizontally
const logoX     = Math.round((1200 - logoW) / 2);
const logoY     = 150;

// ── Feature list (left-aligned, 2 columns) ───────────────────────────────────
const features = [
  'Real-time portfolio tracking',
  'Deemed Disposal tax calculator',
  'ETFs, stocks, crypto &amp; funds',
  'Portfolio sharing',
];

// ── SVG ──────────────────────────────────────────────────────────────────────
const svg = `<svg width="1200" height="630" viewBox="0 0 1200 630"
     xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%"   stop-color="#111827"/>
      <stop offset="100%" stop-color="#0f172a"/>
    </linearGradient>
    <!-- Subtle green glow, top-centre -->
    <radialGradient id="glow" cx="50%" cy="0%" r="60%">
      <stop offset="0%"   stop-color="#10b981" stop-opacity="0.12"/>
      <stop offset="100%" stop-color="#10b981" stop-opacity="0"/>
    </radialGradient>
  </defs>

  <!-- Background -->
  <rect width="1200" height="630" fill="url(#bg)"/>
  <rect width="1200" height="630" fill="url(#glow)"/>

  <!-- Green top border line -->
  <rect x="0" y="0" width="1200" height="4" fill="#10b981"/>

  <!-- Logo centred -->
  <image href="data:image/png;base64,${logoB64}"
         x="${logoX}" y="${logoY}" width="${logoW}" height="${logoH}"/>

  <!-- Divider under logo -->
  <line x1="500" y1="${logoY + logoH + 32}" x2="700" y2="${logoY + logoH + 32}"
        stroke="#10b981" stroke-opacity="0.4" stroke-width="1"/>

  <!-- Tagline, centred -->
  <text x="600" y="${logoY + logoH + 76}"
        font-family="'Helvetica Neue', Arial, sans-serif"
        font-size="28" font-weight="300" fill="#94a3b8"
        text-anchor="middle" letter-spacing="0.5">
    Investment tracker built for Irish investors
  </text>

  <!-- Feature row, centred -->
  ${features.map((f, i) => {
    const col   = i % 2;          // 0 = left, 1 = right
    const row   = Math.floor(i / 2);
    const baseY = logoY + logoH + 130;
    const cx    = col === 0 ? 300 : 900;
    const cy    = baseY + row * 54;
    return `
    <!-- dot + text -->
    <circle cx="${cx - 18}" cy="${cy - 6}" r="5" fill="#10b981"/>
    <text x="${cx}" y="${cy}"
          font-family="'Helvetica Neue', Arial, sans-serif"
          font-size="22" font-weight="400" fill="#e2e8f0"
          text-anchor="middle">${f}</text>`;
  }).join('')}

  <!-- Domain, bottom-centre -->
  <text x="600" y="596"
        font-family="'Helvetica Neue', Arial, sans-serif"
        font-size="20" font-weight="500" fill="#334155"
        text-anchor="middle" letter-spacing="2">
    prt-fy.com
  </text>
</svg>`;

// ── Render ───────────────────────────────────────────────────────────────────
await sharp(Buffer.from(svg))
  .png({ compressionLevel: 9 })
  .toFile(OUT);

console.log(`✅  OG image written to: ${OUT}`);

