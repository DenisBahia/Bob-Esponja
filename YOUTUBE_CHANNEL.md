# 📺 Investments Tracker – YouTube Channel Kit

Everything you need to launch and grow the **Investments Tracker** YouTube channel.

---

## 📌 Table of Contents

1. [Channel Description & About Text](#1-channel-description--about-text)
2. [Banner & Thumbnail Templates](#2-banner--thumbnail-templates)
3. [Video Script Templates](#3-video-script-templates)
4. [Social Media Message Templates](#4-social-media-message-templates)

---

## 1. Channel Description & About Text

### 🔤 Channel Name (suggested)
```
Investments Tracker
```

### 🔤 Short Channel Handle (suggested)
```
@InvestmentsTracker
```

### 📄 Channel Description (paste into YouTube Studio → Customisation → Basic info)

```
📊 Investments Tracker — Portfolio management for the modern Irish investor.

We build and teach you how to use a free, open-source web app that helps you:
• Track ETFs, stocks, mutual funds, cryptocurrencies & commodities in real-time
• Automate Irish tax calculations (Deemed Disposal, Exit Tax, CGT, 8-year rule)
• Sell holdings with FIFO cost-basis and real-time tax preview
• Import transaction history in bulk from your broker via CSV
• Set an investment goal and track your progress
• Project your future wealth with our built-in scenario planner
• Share your portfolio securely with advisors or family members
• Monitor multi-period performance (Daily, Weekly, Monthly, YTD)

🆓 The app is completely free.
🔗 Live App: https://your-app-url.com
💻 Source Code: https://github.com/DenisBahia/Bob-Esponja

New tutorials, feature walkthroughs & investing tips every week.
Subscribe and turn on notifications so you never miss an upload!

📧 Contact: your-email@example.com
```

### 📄 About Tab – Extended Description

```
Welcome to Investments Tracker! 👋

I'm Denis, a software engineer and investor based in Ireland. I built Investments Tracker because I couldn't find a single tool that handled the unique tax rules for Irish investors — especially the dreaded Deemed Disposal rule on ETFs.

🎯 What you'll find on this channel:
• Step-by-step feature tutorials for the Investments Tracker app
• Irish tax explainers (Deemed Disposal, CGT, Exit Tax)
• Portfolio management tips and best practices
• Open-source development walkthroughs (Angular + .NET + PostgreSQL)
• Live demos of new features as they ship

🚀 App Features:
• Real-time portfolio dashboard (Daily / Weekly / Monthly / YTD performance)
• Sell holdings with FIFO cost basis and automatic CGT / Exit Tax calculation
• Bulk import transaction history from your broker via CSV
• Full Tax Center — ledger of all deemed disposal and sell tax events
• Investment projections engine for long-term wealth modelling
• Investment goal tracker
• Portfolio sharing with advisors or family members

🛠️ Tech Stack:
Angular 21 · .NET 10 · PostgreSQL · Chart.js · Docker

📌 Quick links:
• App: https://your-app-url.com
• GitHub: https://github.com/DenisBahia/Bob-Esponja
• Docs: https://github.com/DenisBahia/Bob-Esponja/blob/main/README.md

Thanks for being here — subscribe and let's grow our portfolios together! 📈
```

---

## 2. Banner & Thumbnail Templates

Visual assets are stored in the `youtube/` folder:

| File | Description | Dimensions |
|---|---|---|
| `youtube/banner-template.html` | YouTube channel banner | 2560 × 1440 px (safe area 1546 × 423 px) |
| `youtube/thumbnail-template.html` | Video thumbnail template (4 variants) | 1280 × 720 px |
| `imgs/youtube-channel-logo.svg` | Square channel profile logo | 400 × 400 px (scalable SVG) |

### How to export images

1. Open the HTML file in Chrome/Edge.
2. Open DevTools → toggle Device Toolbar (Ctrl+Shift+M).
3. Set the viewport to the exact pixel dimensions shown above.
4. Use the browser's **Full Page Screenshot** or a tool like [GoFullPage](https://gofullpage.com/).
5. Alternatively, use [Puppeteer](https://pptr.dev/) or [Playwright](https://playwright.dev/) to automate screenshot export:

```bash
# Example with Playwright (Node.js)
npx playwright screenshot --full-page youtube/banner-template.html banner.png
```

---

## 3. Video Script Templates

### 🎬 Script Template A – Feature Walkthrough

```
[HOOK – first 15 seconds]
"If you're an Irish investor tracking ETFs, you've probably been hit with the Deemed
 Disposal tax and had NO idea how to calculate it. In the next [X] minutes I'll show
 you exactly how Investments Tracker does it automatically — for free."

[INTRO – 15 to 45 seconds]
"Hey, welcome back to the Investments Tracker channel — I'm Denis.
 Investments Tracker is a free, open-source portfolio management app built
 specifically for Irish investors. Today we're covering [FEATURE NAME]."

[PROBLEM SETUP – 1 minute]
• Describe the pain point / problem this feature solves.
• Use a real-world example (e.g. 'You bought €5,000 of an Irish-domiciled ETF…').
• Keep it relatable.

[DEMO / TUTORIAL – main body, 3–8 minutes]
Step 1: [Action]
  – Show the UI, narrate what you're clicking.
  – Pause on key numbers/results.
Step 2: [Action]
  – …
Step 3: [Action]
  – …

[RESULTS / SUMMARY – 1 minute]
"So to recap — [FEATURE NAME] lets you [BENEFIT 1], [BENEFIT 2], and [BENEFIT 3].
 All of this is built into the app at no cost."

[CTA – last 30 seconds]
"If this was helpful, smash that like button — it really helps the channel grow.
 Subscribe so you don't miss the next video where we'll cover [NEXT TOPIC].
 The link to the free app and the source code are in the description below.
 Drop any questions in the comments — I reply to every one. See you next week! 📈"
```

> **Suggested video topics using this template:**
> - How to sell ETFs and calculate Exit Tax automatically (Sell modal walkthrough)
> - How to import your entire broker history in minutes (CSV Import walkthrough)
> - How to use the Tax Center to see all your Irish tax obligations
> - How to set an investment goal and track your progress
> - How to project your long-term portfolio value (Projections walkthrough)
> - How to share your portfolio with your accountant or partner

---

### 🎬 Script Template B – Channel Trailer (90 seconds)

```
[HOOK]
"Managing investments in Ireland is complicated — between ETFs, CGT,
 Deemed Disposal, and Exit Tax, most tools just don't cut it.
 That's exactly why I built Investments Tracker."

[WHAT THE CHANNEL IS ABOUT]
"On this channel you'll find:
 — Full tutorials for every feature of the free Investments Tracker app
 — Plain-English explainers for Irish tax rules on investments
 — Tips for building a long-term portfolio
 — Open-source development updates"

[SOCIAL PROOF / CREDIBILITY]
"The app is built with Angular, .NET 10, and PostgreSQL.
 It's open-source and completely free to use."

[FEATURE HIGHLIGHTS – quick montage]
"From a real-time dashboard… to automatic Deemed Disposal calculations…
 to selling ETFs with instant Exit Tax preview…
 to importing your entire broker history with one CSV upload…
 to investment projection modelling… it's all here."

[CTA]
"Subscribe and click the bell — new videos drop every week.
 The free app link is pinned in the description. Let's grow our portfolios together!"
```

---

### 🎬 Script Template C – Irish Tax Explainer Series

```
[HOOK]
"Quick question: do you own an ETF in Ireland?
 Then you probably owe tax you don't even know about yet."

[INTRO]
"Hey, I'm Denis from Investments Tracker. Today we're breaking down
 [TAX TOPIC — e.g. Deemed Disposal / Exit Tax / CGT].
 By the end of this video you'll know exactly what it is, when it applies,
 and how to calculate it — and I'll show you how the app handles it automatically."

[EXPLAINER BODY]
• What is [TAX TOPIC]?
• When does it trigger?
• How is it calculated? (walk through a real example with numbers)
• How to report it to Revenue.

[APP DEMO]
"Now let me show you how Investments Tracker automates this calculation…"
[Screen recording of the tax feature in the app]

[SUMMARY & DISCLAIMER]
"Remember, this is for educational purposes only — always consult a tax professional
 for your specific situation."

[CTA]
"If this helped, like and subscribe.
 Next week we're covering [NEXT TAX TOPIC].
 App link in the description — it's free!"
```

---

### 🎬 Script Template D – Quick Tips (Shorts / 60-second format)

```
[HOOK — 0–3 sec]
"Did you know you can import your entire broker history into Investments Tracker in under 2 minutes? Here's how:"

[DEMO — 3–50 sec]
• Quick screen recording showing the feature.
• Narrate each step in one sentence.

[CTA — 50–60 sec]
"Link to the free app in my bio. Follow for more tips every week! 📈"
```

> **Suggested Shorts topics:**
> - Import broker CSV in 2 minutes
> - See your Exit Tax before you sell (Sell Preview)
> - Set an investment goal in 30 seconds
> - Share your portfolio with one link

---

## 4. Social Media Message Templates

### 💬 WhatsApp / iMessage

**General channel launch announcement:**
```
Hey! 🎉 I just launched my YouTube channel — Investments Tracker!

If you invest in Ireland (ETFs, stocks, crypto...) you'll love this:
📊 Free portfolio tracking app
💰 Automatic Irish tax calculations (Deemed Disposal, Exit Tax, CGT)
💸 Sell with real-time FIFO cost basis & tax preview
📥 Bulk import from your broker via CSV
🎯 Investment goal tracker & projection tool

🎬 YouTube channel: https://youtube.com/@InvestmentsTracker
🔗 Free app: https://your-app-url.com

Would mean a lot if you subscribed! 🙏
```

**Sharing a specific video:**
```
Just uploaded a new video on how to track your Irish ETF portfolio for free 📊

Watch here 👉 https://youtube.com/watch?v=YOUR_VIDEO_ID

Takes 5 minutes and could save you a ton of confusion at tax time 💰
```

---

### 🚀 Twitter / X

**Channel launch:**
```
🚀 Just launched the Investments Tracker YouTube channel!

📊 Portfolio tracking
💰 Irish tax automation (Deemed Disposal, Exit Tax, CGT)
💸 FIFO sell with real-time tax preview
📥 CSV import from any broker
🎯 Investment goal tracker
— all FREE & open-source

Subscribe 👉 https://youtube.com/@InvestmentsTracker

#InvestingIreland #ETF #FinancialFreedom #OpenSource
```

**Video drop:**
```
New video: How to automatically calculate Deemed Disposal tax on your Irish ETFs 🇮🇪💰

Watch → [VIDEO LINK]

Free app → [APP LINK]

#Ireland #InvestingTips #ETF #DeemedDisposal
```

---

### 💼 LinkedIn

```
Excited to announce the launch of the Investments Tracker YouTube channel! 📺

After building a free, open-source portfolio management app for Irish investors
(Angular 21 + .NET 10 + PostgreSQL), I'm now creating video tutorials to help
investors get the most out of it.

What you'll find on the channel:
✅ Step-by-step feature tutorials
✅ Irish tax explainers (Deemed Disposal, CGT, Exit Tax)
✅ How to sell ETFs with FIFO cost basis & automatic tax calculation
✅ How to import your broker history via CSV
✅ Portfolio management best practices
✅ Open-source development walkthroughs

🎬 Subscribe: https://youtube.com/@InvestmentsTracker
💻 GitHub: https://github.com/DenisBahia/Bob-Esponja

If you invest in Ireland or are curious about open-source fintech, I'd love to
have you along for the journey!
```

---

### 📸 Instagram / TikTok Caption

```
Managing investments in Ireland just got easier 📊💰

Introducing Investments Tracker — a FREE app that:
✅ Tracks your ETFs, stocks & crypto in real-time
✅ Calculates Irish Deemed Disposal tax automatically
✅ Sells with FIFO cost basis & instant tax preview
✅ Imports your broker history via CSV in minutes
✅ Projects your future wealth
✅ Lets you share portfolios securely

NEW YouTube channel with full tutorials → link in bio 🎬

#InvestmentsTracker #InvestingIreland #ETF #PortfolioManagement
#PersonalFinance #FinancialFreedom #OpenSource #Angular #DotNet
```

---

*Update the placeholder URLs (`https://your-app-url.com`, `https://youtube.com/@InvestmentsTracker`, `YOUR_VIDEO_ID`) with your real links before publishing.*
