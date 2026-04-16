const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const TEMPLATES_DIR = path.join(__dirname, '..', '..', 'src', 'InstaWriter.Infrastructure', 'Carousel', 'Templates');
const OUTPUT_DIR = path.join(__dirname, 'output');

const baseCss = fs.readFileSync(path.join(TEMPLATES_DIR, 'base.css'), 'utf-8');

const slides = [
  {
    layout: 'title',
    template: 'title.html',
    vars: {
      CATEGORY: 'START HERE',
      HEADLINE: 'New here? Read this first.',
      SUBTEXT: 'Swipe to learn what this account is about',
      AUTHOR: '@josephtolandsr',
      SLIDE_NUMBER: '1'
    }
  },
  {
    layout: 'content',
    template: 'content.html',
    vars: {
      CATEGORY: 'THE PROBLEM',
      HEADLINE: '5 apps. Zero answers.',
      BODY: 'I spent years tracking my health across Garmin, Oura, lab portals, supplement logs, and workout apps. None of them talked to each other.',
      SLIDE_NUMBER: '2',
      HIGHLIGHT: ''
    }
  },
  {
    layout: 'content',
    template: 'content.html',
    vars: {
      CATEGORY: 'THE FRUSTRATION',
      HEADLINE: 'Data everywhere. Clarity nowhere.',
      BODY: 'Lab results sat in PDFs I barely understood. My wearable showed me numbers but never told me what they actually meant together.',
      SLIDE_NUMBER: '3',
      HIGHLIGHT: ''
    }
  },
  {
    layout: 'content',
    template: 'content.html',
    vars: {
      CATEGORY: 'THE BREAKING POINT',
      HEADLINE: 'Nobody connected the dots.',
      BODY: 'When I was dealing with health issues that took too long to diagnose, I realized no tool on the market connects wearables, labs, and supplements into one picture.',
      SLIDE_NUMBER: '4',
      HIGHLIGHT: ''
    }
  },
  {
    layout: 'content',
    template: 'content.html',
    vars: {
      CATEGORY: 'THE SOLUTION',
      HEADLINE: 'So I\'m building one.',
      BODY: 'AI Health Coach pulls your Garmin, WHOOP, Oura, Apple Health, labs, and supplements into one place.',
      SLIDE_NUMBER: '5',
      HIGHLIGHT: 'Longevity Score \u2022 Biological Age \u2022 Recovery \u2022 AI Insights'
    }
  },
  {
    layout: 'content',
    template: 'content.html',
    vars: {
      CATEGORY: 'THE DIFFERENCE',
      HEADLINE: 'Find what moves the needle.',
      BODY: 'AI finds YOUR patterns \u2014 which supplements actually work, how your sleep affects recovery, and exactly what to do next. Real data, not guesses.',
      SLIDE_NUMBER: '6',
      HIGHLIGHT: ''
    }
  },
  {
    layout: 'cta-bridge',
    template: 'cta-bridge.html',
    vars: {
      HEADLINE: 'What you\'ll get following this account',
      BODY: 'Biohacking tips \u2022 Build-in-public updates \u2022 Lab insights \u2022 What I\'m learning on my own health journey'
    }
  },
  {
    layout: 'cta',
    template: 'cta.html',
    vars: {
      HEADLINE: 'Follow along for the journey',
      CTA: 'Link in bio for early access',
      SUBTEXT: 'Drop a \uD83D\uDC4B in the comments if this resonates',
      AUTHOR: '@josephtolandsr'
    }
  }
];

(async () => {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });

  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 1080, height: 1080 } });

  for (let i = 0; i < slides.length; i++) {
    const slide = slides[i];
    let html = fs.readFileSync(path.join(TEMPLATES_DIR, slide.template), 'utf-8');

    // Inline the CSS
    html = html.replace('<link rel="stylesheet" href="base.css">', `<style>${baseCss}</style>`);

    // Replace placeholders
    for (const [key, value] of Object.entries(slide.vars)) {
      html = html.replace(new RegExp(`\\{\\{${key}\\}\\}`, 'g'), value);
    }

    // Handle conditional highlight block
    if (slide.vars.HIGHLIGHT) {
      html = html.replace(/\{\{#HIGHLIGHT\}\}/g, '').replace(/\{\{\/HIGHLIGHT\}\}/g, '');
    } else {
      html = html.replace(/\{\{#HIGHLIGHT\}\}.*?\{\{\/HIGHLIGHT\}\}/gs, '');
    }

    const page = await context.newPage();
    await page.setContent(html, { waitUntil: 'networkidle' });

    const filename = `slide-${String(i + 1).padStart(2, '0')}-${slide.layout}.png`;
    await page.screenshot({ path: path.join(OUTPUT_DIR, filename), clip: { x: 0, y: 0, width: 1080, height: 1080 } });
    await page.close();

    console.log(`Rendered: ${filename}`);
  }

  await browser.close();
  console.log(`\nDone! ${slides.length} slides saved to: ${OUTPUT_DIR}`);
})();
