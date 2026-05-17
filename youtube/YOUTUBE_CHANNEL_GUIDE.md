# 🎬 Prtfy — YouTube Channel Kit

> Complete guide to set up, brand, and grow your YouTube channel for **Prtfy - Grow Your Portfolio**.

---

## 📁 Files Included in This Folder

| File | Purpose | Dimensions |
|---|---|---|
| `yt-profile-picture.svg` | Channel profile picture (uses your existing app icon) | 800 × 800 px (export as PNG) |
| `yt-banner.svg` | Channel art / banner | 2560 × 1440 px (export as PNG) |
| `yt-thumbnail-template.svg` | Reusable video thumbnail (uses your existing app icon) | 1280 × 720 px (export as PNG) |

> ⚠️ **Logo note**: The SVGs reference your existing `favicon-symbol.png` at `../ETFTracker.Web/public/favicon-symbol.png`. This relative path works when viewed from inside the `/youtube/` folder. When exporting with Inkscape, run the command from within the `/youtube/` folder.

---

## 🎨 Brand Identity

| Element | Value |
|---|---|
| **Primary Background** | `#0F172A` (Deep Navy) |
| **Surface / Card** | `#1E293B` (Dark Slate) |
| **Accent Green** | `#10B981` (Emerald) |
| **Accent Light** | `#34D399` (Light Emerald) |
| **Text Primary** | `#FFFFFF` |
| **Text Secondary** | `rgba(229,231,235,0.75)` |
| **Font** | Inter (Google Fonts) |
| **App Name** | **Prtfy** |
| **App Tagline** | **Grow Your Portfolio** |

---

## 🖼️ STEP 1 — Export SVG Files to PNG

### Option A — Browser (fastest, free)
1. Open each `.svg` file in **Chrome or Safari** (from within the `/youtube/` folder so the logo path resolves)
2. Right-click on the image → **Save image as…** → PNG
3. Or press `Cmd+Shift+4` to screenshot the rendered area

### Option B — Inkscape (free, best quality)
```bash
# Run from inside the /youtube/ folder so relative paths resolve
cd "/Users/denisbahia/RiderProjects/Bob Esponja/youtube"

inkscape yt-profile-picture.svg --export-type=png --export-filename=yt-profile-picture.png -w 800 -h 800
inkscape yt-banner.svg --export-type=png --export-filename=yt-banner.png -w 2560 -h 1440
inkscape yt-thumbnail-template.svg --export-type=png --export-filename=yt-thumbnail-template.png -w 1280 -h 720
```

### Option C — Figma / Canva
1. Import the SVG into Figma or Canva
2. Also import `favicon-symbol.png` and place it manually if the path doesn't auto-resolve
3. Export as PNG at 2× scale

---

## 📺 STEP 2 — Set Up Your YouTube Channel

### 2.1 — Profile Picture
1. Go to [studio.youtube.com](https://studio.youtube.com)
2. Click your channel icon (top right) → **Manage your Google Account**
3. Click your profile photo → **Change photo**
4. Upload `yt-profile-picture.png` (800×800)
5. Crop to circle — keep the app icon centred
6. Save

> ⚠️ YouTube displays it at 98×98 px on desktop. The app icon symbol works great at small sizes — the text below it is a bonus for previews.

### 2.2 — Channel Art / Banner
1. Go to [studio.youtube.com](https://studio.youtube.com) → **Customisation** → **Branding**
2. Under **Banner image**, click **Upload**
3. Upload `yt-banner.png` (2560×1440)
4. In the preview, confirm the **safe area** shows: logo + "Prtfy" + "Grow Your Portfolio" + pills
5. Save

> 📐 **Safe zone**: Everything important must be within the inner 1546×423 px band. The outer edges are only visible on TVs.

### 2.3 — Channel Name & Handle
- **Channel Name:** `Prtfy`
- **Handle:** `@PrtfyApp` (or `@GrowYourPortfolio`)
- Set in: **Studio → Customisation → Basic info**

### 2.4 — Channel Description
Copy-paste into **Studio → Customisation → Basic info → Description**:

```
📊 Prtfy — Grow Your Portfolio

Track ETFs, stocks, mutual funds, cryptocurrencies, and more across multiple brokers — all in one place.

On this channel you'll find:
✅ App tutorials & feature walkthroughs
✅ Investment tips for Irish investors (deemed disposal, CGT)
✅ Portfolio management strategies
✅ Live demos and new feature updates

🌐 Try Prtfy: https://etf-tracker-app.onrender.com
📩 Contact: [YOUR EMAIL]

Built with Angular + .NET + PostgreSQL
```

### 2.5 — Channel Links (Social)
Add in **Studio → Customisation → Basic info → Links**:
- **Website:** Your deployed app URL
- **GitHub:** Your repository (if public)
- **Twitter/X:** Your account (optional)

### 2.6 — Channel Keywords (SEO)
Add in **Studio → Settings → Channel → Basic info → Keywords**:
```
prtfy, investment tracker, portfolio management, ETF tracker, Irish investor, deemed disposal, CGT calculator, stock tracker, crypto tracker, FIRE investing, personal finance, Angular app, portfolio app, grow your portfolio
```

---

## 🎬 STEP 3 — Channel Sections & Featured Content

### 3.1 — Channel Trailer (for new visitors)
Create a 60–90 second screen recording showing:
1. **0–5s**: App logo / landing page on a clean browser
2. **5–20s**: Dashboard overview — scroll through holdings, show gains
3. **20–35s**: Add a new transaction (Buy transaction flow)
4. **35–50s**: Projections page — change sliders, show chart update
5. **50–65s**: Tax summary page — show deemed disposal panel
6. **65–80s**: Share portfolio feature — invite by email demo
7. **80–90s**: End card with "Prtfy — Grow Your Portfolio" + subscribe CTA

**Screen recording tools (free):**
- **macOS**: `Cmd+Shift+5` → Record selected area
- **OBS Studio** (free, professional): https://obsproject.com
- **Loom**: https://loom.com (easy, auto-uploads)

### 3.2 — Sections to Add
Go to **Studio → Customisation → Layout** and add:
1. **Featured video**: Your channel trailer
2. **Section 1** — "Getting Started" playlist
3. **Section 2** — "Tax & Irish Investors" playlist
4. **Section 3** — "Advanced Features" playlist

---

## 🎥 STEP 4 — First 10 Video Ideas

### 🟢 Getting Started Series
| # | Title | Length | What to Show |
|---|---|---|---|
| 1 | **Prtfy — Full App Walkthrough** | 8–12 min | Every page of the app, overview tour |
| 2 | **How to Add Your First ETF Holding** | 4–6 min | Add holding → add buy transaction → see it on dashboard |
| 3 | **Understanding Your Portfolio Dashboard** | 5–7 min | Daily/Weekly/Monthly/YTD panels, what each metric means |
| 4 | **Setting Up Investment Projections** | 6–8 min | Configure projection settings, show different scenarios |

### 🟡 Tax & Irish Investors Series
| # | Title | Length | What to Show |
|---|---|---|---|
| 5 | **Deemed Disposal Explained — Irish ETF Tax** | 8–10 min | What it is, enable it in settings, show calculations |
| 6 | **CGT vs Deemed Disposal: What's the Difference?** | 6–8 min | Side-by-side explanation using the Tax page |
| 7 | **How to Use the Tax Summary Page** | 5–7 min | Annual tax summary, how to mark as paid |

### 🔵 Advanced Features Series
| # | Title | Length | What to Show |
|---|---|---|---|
| 8 | **Share Your Portfolio with Others** | 4–5 min | Invite by email, permissions, view shared portfolio |
| 9 | **Saving & Comparing Projection Versions** | 5–6 min | Save multiple scenarios, compare different strategies |
| 10 | **How to Self-Host Prtfy** | 10–12 min | Docker deployment, setting up API keys |

---

## 📝 STEP 5 — Video Description Template

```
📊 [VIDEO TITLE] | Prtfy — Grow Your Portfolio

In this video: [1-2 sentence summary]

⏱️ Chapters:
0:00 — Intro
0:30 — [Chapter 1]
X:XX — [Chapter 2]
X:XX — [Chapter 3]
X:XX — Outro

🔗 Try Prtfy for free:
→ App: [YOUR APP URL]
→ GitHub: [REPO URL]

📋 Related videos:
→ [Related video 1]
→ [Related video 2]

🔔 Subscribe for weekly Prtfy updates!

---
Tags: prtfy, investment tracker, portfolio management, ETF, Irish investor, deemed disposal, CGT, stock tracker, Angular, .NET, FIRE, grow your portfolio
```

---

## 🖼️ STEP 6 — Custom Thumbnails Per Video

Each video thumbnail uses `yt-thumbnail-template.svg`. To customise per video:

1. Open `yt-thumbnail-template.svg` in a text editor or Figma
2. Find lines ~46–50 and change the 3 title lines:
   ```xml
   <text ...>How to</text>
   <text ...>Track Your</text>
   <text ...>Portfolio</text>
   ```
3. Change the **label pill** (~line 38):
   ```xml
   <text ...>TUTORIAL</text>
   <!-- Options: TUTORIAL | TAX GUIDE | DEEP DIVE | NEW FEATURE | DEMO -->
   ```
4. Export as PNG 1280×720
5. Upload in **YouTube Studio → Video details → Thumbnail**

### Thumbnail Text Examples Per Video:
| Video | Line 1 | Line 2 | Line 3 | Label |
|---|---|---|---|---|
| Walkthrough | Full App | Tour | 2026 | DEMO |
| Add Holding | Add Your | First ETF | Holdings | TUTORIAL |
| Deemed Disposal | Deemed | Disposal | Explained | TAX GUIDE |
| Projections | Plan Your | Financial | Future | DEEP DIVE |
| Share Portfolio | Share Your | Portfolio | Securely | NEW FEATURE |

---

## 📣 STEP 7 — Channel Trailer Script (90 seconds)

```
[0–5s — App landing page / Prtfy logo on screen]
"Managing your investments across multiple brokers is complicated.
Prtfy makes it simple."

[5–20s — Dashboard scrolling]
"One dashboard. All your ETFs, stocks, mutual funds, and crypto —
with real-time prices and multi-period performance tracking."

[20–30s — Add transaction modal]
"Adding a new purchase takes under 10 seconds. Enter your ticker,
quantity, price, and date — done."

[30–45s — Projections chart]
"Want to see where your portfolio goes in 10 years? The projections
engine models your future wealth with inflation and tax included."

[45–60s — Tax page]
"And for Irish investors: automatic deemed disposal calculations,
CGT summaries, and tax event tracking — all built in."

[60–75s — Share profile]
"You can even share your portfolio with your financial advisor or
a fellow investor — with full permission controls."

[75–90s — App logo end card]
"Subscribe for tutorials, tips, and updates — new videos every week.
I'm [YOUR NAME], and this is Prtfy — Grow Your Portfolio."
```

---

## ✅ Launch Checklist

- [ ] Export `yt-profile-picture.svg` → upload as channel icon
- [ ] Export `yt-banner.svg` → upload as channel art
- [ ] Set channel name: **Prtfy**
- [ ] Set handle: **@PrtfyApp**
- [ ] Add channel description (from Step 2.4)
- [ ] Add website & social links
- [ ] Add channel keywords (SEO)
- [ ] Upload channel trailer video (60–90s)
- [ ] Create "Getting Started" playlist
- [ ] Create "Tax & Irish Investors" playlist
- [ ] Upload first video with custom thumbnail
- [ ] Add channel sections on homepage layout

---

## 🛠️ Recommended Free Tools

| Tool | Use Case | Link |
|---|---|---|
| **OBS Studio** | Screen recording | obsproject.com |
| **DaVinci Resolve** | Video editing | blackmagicdesign.com |
| **Inkscape** | Edit/export SVGs | inkscape.org |
| **Figma** | Design thumbnails | figma.com |
| **TubeBuddy** | YouTube SEO & analytics | tubebuddy.com |
| **Canva** | Quick thumbnail edits | canva.com |

---

*Generated for Prtfy — Grow Your Portfolio — May 2026*

