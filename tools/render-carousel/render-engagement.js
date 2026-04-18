const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const TEMPLATES_DIR = path.join(__dirname, '..', '..', 'src', 'InstaWriter.Infrastructure', 'Carousel', 'Templates');
const baseCss = fs.readFileSync(path.join(TEMPLATES_DIR, 'base.css'), 'utf-8');
const OUTPUT_DIR = path.join(__dirname, 'output', 'engagement-normal-vs-optimal');

const slides = [
  {
    layout: 'title', template: 'title.html',
    vars: {
      CATEGORY: 'HOT TAKE',
      HEADLINE: 'Your doctor says you are "normal." You are not fine.',
      SUBTEXT: 'Normal ranges are designed to catch disease, not optimize health',
      AUTHOR: '@josephtolandsr',
      SLIDE_NUMBER: '1'
    }
  },
  {
    layout: 'content', template: 'content.html',
    vars: {
      CATEGORY: 'THE PROBLEM',
      HEADLINE: '"Normal" is a massive range.',
      BODY: 'Lab reference ranges are built from the general population — including sick, sedentary, and unhealthy people. Being in range just means you do not have a diagnosable disease. It says nothing about whether you feel good.',
      SLIDE_NUMBER: '2',
      HIGHLIGHT: ''
    }
  },
  {
    layout: 'content', template: 'content.html',
    vars: {
      CATEGORY: 'TESTOSTERONE',
      HEADLINE: 'Normal: 264-916 ng/dL',
      BODY: 'A 40-year-old man at 280 is told he is normal. Meanwhile he has brain fog, no energy, and poor recovery. Optimal is 500-900+. The range is so wide it is almost meaningless.',
      SLIDE_NUMBER: '3',
      HIGHLIGHT: 'I was told my labs were fine. I was not fine.'
    }
  },
  {
    layout: 'content', template: 'content.html',
    vars: {
      CATEGORY: 'VITAMIN D',
      HEADLINE: 'Normal: 30-100 ng/mL',
      BODY: 'Most doctors are happy if you are above 30. Longevity research suggests 50-80 is where you want to be. Below 50 is linked to increased inflammation, poor immune function, and low mood.',
      SLIDE_NUMBER: '4',
      HIGHLIGHT: ''
    }
  },
  {
    layout: 'content', template: 'content.html',
    vars: {
      CATEGORY: 'APOB',
      HEADLINE: 'Most doctors do not even test this.',
      BODY: 'ApoB is the best predictor of cardiovascular risk — better than LDL. Optimal is under 80 mg/dL for longevity. Most people have never had it tested because standard panels do not include it.',
      SLIDE_NUMBER: '5',
      HIGHLIGHT: 'Ask your doctor for an ApoB test at your next visit'
    }
  },
  {
    layout: 'cta-bridge', template: 'cta-bridge.html',
    vars: {
      HEADLINE: 'Stop accepting "fine."',
      BODY: 'This is exactly why I built AI Health Coach. It shows you longevity-optimal ranges, not just clinical cutoffs. Because you deserve better than normal.'
    }
  },
  {
    layout: 'cta', template: 'cta.html',
    vars: {
      HEADLINE: 'Agree or disagree? Drop it in the comments.',
      CTA: 'Link in bio to try the app',
      SUBTEXT: 'Share this with someone who needs to see it',
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
    html = html.replace('<link rel="stylesheet" href="base.css">', '<style>' + baseCss + '</style>');
    for (const [key, value] of Object.entries(slide.vars)) {
      const escaped = value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/\n/g, '<br>');
      html = html.replace(new RegExp('\{\{' + key + '\}\}', 'g'), escaped);
    }
    if (slide.vars.HIGHLIGHT) {
      html = html.replace(/\{\{#HIGHLIGHT\}\}/g, '').replace(/\{\{\/HIGHLIGHT\}\}/g, '');
    } else {
      html = html.replace(/\{\{#HIGHLIGHT\}\}[\s\S]*?\{\{\/HIGHLIGHT\}\}/g, '');
    }
    const page = await context.newPage();
    await page.setContent(html, { waitUntil: 'networkidle' });
    const filename = 'slide-' + String(i + 1).padStart(2, '0') + '-' + slide.layout + '.png';
    await page.screenshot({ path: path.join(OUTPUT_DIR, filename), clip: { x: 0, y: 0, width: 1080, height: 1080 } });
    await page.close();
    console.log('Rendered: ' + filename);
  }
  await browser.close();

  // Write caption
  fs.writeFileSync(path.join(OUTPUT_DIR, 'caption.txt'),
    'Your doctor says you are "normal." You are not fine.\n\n' +
    'Lab reference ranges are built from the general population — including sick and sedentary people. Being in range just means you do not have a diagnosable disease yet.\n\n' +
    'Testosterone "normal": 264-916 ng/dL\n' +
    'Vitamin D "normal": 30-100 ng/mL\n' +
    'ApoB: most doctors don\'t even test it\n\n' +
    'You could be at the bottom of every range and be told you\'re fine. Fine is not optimal.\n\n' +
    'I lived this. Brain fog, no energy, poor recovery — and my doctor said everything looked good. It took too long to get real answers.\n\n' +
    'That\'s why I built AI Health Coach — to show longevity-optimal ranges, not just clinical cutoffs.\n\n' +
    'Agree or disagree? Drop it in the comments.\n\n' +
    '#healthoptimization #labwork #biohacking #testosterone #vitamind #apob #longevity #bloodwork #menshealth #aihealthcoach\n'
  );

  console.log('Done! Caption saved.');
})();
