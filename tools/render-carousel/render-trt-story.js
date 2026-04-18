const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const TEMPLATES_DIR = path.join(__dirname, '..', '..', 'src', 'InstaWriter.Infrastructure', 'Carousel', 'Templates');
const baseCss = fs.readFileSync(path.join(TEMPLATES_DIR, 'base.css'), 'utf-8');
const OUTPUT_DIR = path.join(__dirname, 'output', 'carousel-trt-clinic-story');

const slides = [
  {
    layout: 'title', template: 'title.html',
    vars: {
      CATEGORY: 'MY TRT STORY',
      HEADLINE: 'My TRT clinic missed two critical things on my labs.',
      SUBTEXT: 'What I learned the hard way so you do not have to',
      AUTHOR: '@josephtolandsr',
      SLIDE_NUMBER: '1'
    }
  },
  {
    layout: 'content', template: 'content.html',
    vars: {
      CATEGORY: 'WHAT HAPPENED',
      HEADLINE: 'Everything was going well. Then it was not.',
      BODY: 'I was on TRT and feeling great. Then my clinic pulled me off HCG. Within weeks my strength dropped, stamina disappeared, and my libido was gone. I went from feeling optimized to feeling worse than before I started.',
      SLIDE_NUMBER: '2',
      HIGHLIGHT: ''
    }
  },
  {
    layout: 'content', template: 'content.html',
    vars: {
      CATEGORY: 'MISTAKE 1',
      HEADLINE: 'HCG is about more than testosterone.',
      BODY: 'Most TRT clinics think of HCG as just a fertility add-on. It is not. HCG supports pregnenolone and DHEA production, neurosteroid function, testicular health, and downstream hormones that testosterone alone does not replace. Pulling it without understanding the full picture crashed my system.',
      SLIDE_NUMBER: '3',
      HIGHLIGHT: ''
    }
  },
  {
    layout: 'content', template: 'content.html',
    vars: {
      CATEGORY: 'MISTAKE 2',
      HEADLINE: 'I was completely iron deficient. It was right on my labs.',
      BODY: 'The clinic had my bloodwork. Iron was tanked. They never flagged it. Iron deficiency causes fatigue, poor recovery, brain fog, reduced stamina, and low motivation. Every symptom I was blaming on the HCG change was being made worse by iron they never caught.',
      SLIDE_NUMBER: '4',
      HIGHLIGHT: 'The answer was on the labs they were already looking at.'
    }
  },
  {
    layout: 'content', template: 'content.html',
    vars: {
      CATEGORY: 'HOW I FOUND IT',
      HEADLINE: 'My app connected the dots.',
      BODY: 'I started logging everything in AI Health Coach. Daily check-ins for energy, stamina, libido. My lab results. The timeline of protocol changes. The AI correlated my declining symptoms with the HCG removal date and flagged the iron deficiency that the clinic missed.',
      SLIDE_NUMBER: '5',
      HIGHLIGHT: ''
    }
  },
  {
    layout: 'content', template: 'content.html',
    vars: {
      CATEGORY: 'WHAT TO WATCH',
      HEADLINE: 'If you are on TRT, track these.',
      BODY: 'Beyond Total T and Free T:\n\n- Iron panel (ferritin, serum iron, TIBC)\n- DHEA-S and pregnenolone\n- Estradiol (E2)\n- SHBG\n- Hematocrit and hemoglobin\n- Daily energy, mood, and libido check-ins\n\nYour clinic may not track all of these. You should.',
      SLIDE_NUMBER: '6',
      HIGHLIGHT: ''
    }
  },
  {
    layout: 'content', template: 'content.html',
    vars: {
      CATEGORY: 'SELF-ADVOCATE',
      HEADLINE: 'How to push back on your clinic.',
      BODY: 'You are paying them. You have the right to ask questions.\n\n1. Request a full copy of every lab panel\n2. Ask WHY before any protocol change\n3. Track your own symptoms daily\n4. Get a second opinion on any major change\n5. If something feels off, it probably is',
      SLIDE_NUMBER: '7',
      HIGHLIGHT: ''
    }
  },
  {
    layout: 'cta-bridge', template: 'cta-bridge.html',
    vars: {
      HEADLINE: 'Your clinic manages your prescription. Nobody manages the full picture.',
      BODY: 'That is why I built AI Health Coach. It tracks everything your clinic does not and flags what they miss. Because your health is too important to trust one set of eyes.'
    }
  },
  {
    layout: 'cta', template: 'cta.html',
    vars: {
      HEADLINE: 'Has your clinic ever missed something? Tell me below.',
      CTA: 'Link in bio to try the app',
      SUBTEXT: 'Share this with someone on TRT who needs to see it',
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

  fs.writeFileSync(path.join(OUTPUT_DIR, 'caption.txt'),
`My TRT clinic missed two critical things on my labs. Here is what I learned the hard way.

I was on TRT and feeling great. Then they pulled me off HCG without understanding the full picture. Within weeks — strength gone, stamina gone, libido gone.

At the same time, I was completely iron deficient. It was right there on my bloodwork. They never flagged it.

Two mistakes:
1. HCG is about more than testosterone. It supports pregnenolone, DHEA, and neurosteroid production. You can not just pull it without consequences.
2. Iron deficiency causes every symptom I was experiencing — fatigue, brain fog, poor recovery. And it was sitting on the labs they were already looking at.

How did I figure it out? I built an app for it. AI Health Coach connected my daily symptom check-ins with my lab timeline and protocol changes. It correlated the decline with the HCG removal and flagged the iron that the clinic missed.

Your clinic manages your prescription. Nobody manages the full picture. That is why I built this.

Has your TRT clinic ever missed something? Tell me in the comments.

#trt #testosterone #hormonehealth #irondeficiency #hcg #menshealth #biohacking #labwork #healthoptimization #aihealthcoach #bloodwork #longevity
`);

  console.log('Done! Caption saved.');
})();
