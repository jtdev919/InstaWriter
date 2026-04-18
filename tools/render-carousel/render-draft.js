/**
 * Render carousel PNGs from a saved draft's carouselCopyJson.
 *
 * Usage:
 *   node render-draft.js <draft-id> [--api <base-url>] [--upload]
 *   node render-draft.js --file <path-to-slides.json> [--upload]
 *
 * --upload sends rendered PNGs to Telegram (save to phone for Instagram).
 * Requires TELEGRAM_BOT_TOKEN and TELEGRAM_CHAT_ID env vars (or .env file).
 */

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const TEMPLATES_DIR = path.join(__dirname, '..', '..', 'src', 'InstaWriter.Infrastructure', 'Carousel', 'Templates');

// Load .env file if present
const envPath = path.join(__dirname, '.env');
if (fs.existsSync(envPath)) {
  for (const line of fs.readFileSync(envPath, 'utf-8').split('\n')) {
    const match = line.match(/^\s*([\w]+)\s*=\s*(.+)\s*$/);
    if (match && !process.env[match[1]]) process.env[match[1]] = match[2];
  }
}

// Parse CLI args
const args = process.argv.slice(2);
if (args.length === 0 || args[0] === '--help') {
  console.log('Usage: node render-draft.js <draft-id> [--api <base-url>] [--upload]');
  console.log('       node render-draft.js --file <path-to-slides.json> [--upload]');
  process.exit(0);
}

let draftId = null;
let apiBase = 'https://localhost:7201';
let jsonFile = null;
let uploadToTelegram = false;

for (let i = 0; i < args.length; i++) {
  if (args[i] === '--api' && args[i + 1]) {
    apiBase = args[++i];
  } else if (args[i] === '--file' && args[i + 1]) {
    jsonFile = args[++i];
  } else if (args[i] === '--upload') {
    uploadToTelegram = true;
  } else if (!args[i].startsWith('--')) {
    draftId = args[i];
  }
}

// Map SlideContent type to template file and vars
function slideToTemplate(slide, index, totalSlides) {
  const type = slide.type || 'content';
  const templateMap = {
    'title': 'title.html',
    'content': 'content.html',
    'cta-bridge': 'cta-bridge.html',
    'cta': 'cta.html',
  };

  const template = templateMap[type] || 'content.html';
  const vars = {};

  switch (type) {
    case 'title':
      vars.CATEGORY = slide.category || '';
      vars.HEADLINE = slide.headline || '';
      vars.SUBTEXT = slide.body || slide.subtext || '';
      vars.AUTHOR = '@josephtolandsr';
      vars.SLIDE_NUMBER = String(index + 1);
      break;

    case 'content':
      vars.CATEGORY = slide.category || '';
      vars.HEADLINE = slide.headline || '';
      vars.BODY = slide.body || '';
      vars.SLIDE_NUMBER = String(index + 1);
      vars.HIGHLIGHT = '';
      break;

    case 'cta-bridge':
      vars.HEADLINE = slide.headline || '';
      vars.BODY = slide.body || '';
      break;

    case 'cta':
      vars.HEADLINE = slide.headline || '';
      vars.CTA = slide.cta || 'Get Started Free';
      vars.SUBTEXT = slide.subtext || '';
      vars.AUTHOR = '@josephtolandsr';
      break;
  }

  return { type, template, vars };
}

async function fetchDraftSlides(draftId, apiBase) {
  const url = `${apiBase}/api/content/drafts/${draftId}`;
  console.log(`Fetching draft from: ${url}`);

  const resp = await fetch(url);
  if (!resp.ok) {
    throw new Error(`API returned ${resp.status}: ${resp.statusText}`);
  }

  const draft = await resp.json();
  if (!draft.carouselCopyJson) {
    throw new Error('Draft has no saved carousel slides (carouselCopyJson is empty). Edit and save slides in the dashboard first.');
  }

  const slides = JSON.parse(draft.carouselCopyJson);
  console.log(`Loaded ${slides.length} slides from draft "${draft.coverText || draft.id}"`);
  return { slides, draftTitle: draft.coverText || draft.contentIdea?.title || draftId };
}

async function loadSlidesFromFile(filePath) {
  const raw = fs.readFileSync(filePath, 'utf-8');
  const slides = JSON.parse(raw);
  console.log(`Loaded ${slides.length} slides from file: ${filePath}`);
  return { slides, draftTitle: path.basename(filePath, '.json') };
}

async function renderSlides(slides, outputDir) {
  const baseCss = fs.readFileSync(path.join(TEMPLATES_DIR, 'base.css'), 'utf-8');

  fs.mkdirSync(outputDir, { recursive: true });

  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 1080, height: 1080 } });

  for (let i = 0; i < slides.length; i++) {
    const mapped = slideToTemplate(slides[i], i, slides.length);

    let html = fs.readFileSync(path.join(TEMPLATES_DIR, mapped.template), 'utf-8');

    // Inline CSS
    html = html.replace('<link rel="stylesheet" href="base.css">', `<style>${baseCss}</style>`);

    // Replace placeholders
    for (const [key, value] of Object.entries(mapped.vars)) {
      html = html.replace(new RegExp(`\\{\\{${key}\\}\\}`, 'g'), value);
    }

    // Handle conditional highlight block
    if (mapped.vars.HIGHLIGHT) {
      html = html.replace(/\{\{#HIGHLIGHT\}\}/g, '').replace(/\{\{\/HIGHLIGHT\}\}/g, '');
    } else {
      html = html.replace(/\{\{#HIGHLIGHT\}\}.*?\{\{\/HIGHLIGHT\}\}/gs, '');
    }

    const page = await context.newPage();
    await page.setContent(html, { waitUntil: 'networkidle' });

    const filename = `slide-${String(i + 1).padStart(2, '0')}-${mapped.type}.png`;
    await page.screenshot({ path: path.join(outputDir, filename), clip: { x: 0, y: 0, width: 1080, height: 1080 } });
    await page.close();

    console.log(`  Rendered: ${filename}`);
  }

  await browser.close();
  return slides.length;
}

async function sendToTelegram(outputDir, title) {
  const botToken = process.env.TELEGRAM_BOT_TOKEN;
  const chatId = process.env.TELEGRAM_CHAT_ID;

  if (!botToken || !chatId) {
    console.error('\nTelegram upload requires TELEGRAM_BOT_TOKEN and TELEGRAM_CHAT_ID.');
    console.error('Set them in tools/render-carousel/.env or as environment variables.');
    process.exit(1);
  }

  const pngs = fs.readdirSync(outputDir)
    .filter(f => f.endsWith('.png'))
    .sort()
    .map(f => path.join(outputDir, f));

  if (pngs.length === 0) {
    console.log('No PNGs found to upload.');
    return;
  }

  console.log(`\nUploading ${pngs.length} slides to Telegram...`);

  // Telegram sendMediaGroup supports up to 10 items per batch
  for (let batch = 0; batch < pngs.length; batch += 10) {
    const chunk = pngs.slice(batch, batch + 10);
    const formData = new FormData();

    const media = chunk.map((filePath, idx) => {
      const fieldName = `photo${batch + idx}`;
      const fileBuffer = fs.readFileSync(filePath);
      formData.append(fieldName, new Blob([fileBuffer]), path.basename(filePath));
      return { type: 'photo', media: `attach://${fieldName}` };
    });

    if (batch === 0) {
      media[0].caption = `📸 ${title} (${pngs.length} slides)`;
    }

    formData.append('chat_id', chatId);
    formData.append('media', JSON.stringify(media));

    const resp = await fetch(`https://api.telegram.org/bot${botToken}/sendMediaGroup`, {
      method: 'POST',
      body: formData,
    });

    if (!resp.ok) {
      const err = await resp.json();
      throw new Error(`Telegram API error: ${JSON.stringify(err)}`);
    }

    console.log(`  Sent batch ${Math.floor(batch / 10) + 1} (${chunk.length} images)`);
  }

  console.log('\nAll slides sent to Telegram! Save them to your phone gallery for Instagram.');
}

(async () => {
  try {
    let slides, draftTitle;

    if (jsonFile) {
      ({ slides, draftTitle } = await loadSlidesFromFile(jsonFile));
    } else if (draftId) {
      ({ slides, draftTitle } = await fetchDraftSlides(draftId, apiBase));
    } else {
      console.error('Provide a draft ID or --file <path>');
      process.exit(1);
    }

    // Create output folder named after draft
    const safeName = draftTitle.replace(/[^a-zA-Z0-9-_ ]/g, '').replace(/\s+/g, '-').toLowerCase();
    const outputDir = path.join(__dirname, 'output', safeName);

    console.log(`\nRendering ${slides.length} slides to: ${outputDir}\n`);
    const count = await renderSlides(slides, outputDir);
    console.log(`\nDone! ${count} slides saved to: ${outputDir}`);

    if (uploadToTelegram) {
      await sendToTelegram(outputDir, draftTitle);
    }

  } catch (err) {
    console.error(`Error: ${err.message}`);
    process.exit(1);
  }
})();
