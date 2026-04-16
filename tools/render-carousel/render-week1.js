const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const TEMPLATES_DIR = path.join(__dirname, '..', '..', 'src', 'InstaWriter.Infrastructure', 'Carousel', 'Templates');
const baseCss = fs.readFileSync(path.join(TEMPLATES_DIR, 'base.css'), 'utf-8');

const carousels = {
  'carousel-1-longevity': [
    {
      layout: 'title', template: 'title.html',
      vars: { CATEGORY: 'LONGEVITY', HEADLINE: '5 metrics that predict how long you\'ll live', SUBTEXT: 'Most people track none of them', AUTHOR: '@josephtolandsr', SLIDE_NUMBER: '1' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'METRIC 1', HEADLINE: 'VO2 Max', BODY: 'Your body\'s ability to use oxygen during exercise. The single strongest predictor of all-cause mortality. A low VO2max in your 40s doubles your risk of dying in the next decade compared to someone in the top 25%.', SLIDE_NUMBER: '2', HIGHLIGHT: 'Fix it: 150+ min of Zone 2 cardio per week' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'METRIC 2', HEADLINE: 'Heart Rate Variability', BODY: 'The variation in time between heartbeats. Higher HRV means your nervous system is resilient and adaptable. Low HRV is linked to chronic stress, poor recovery, and increased cardiovascular risk.', SLIDE_NUMBER: '3', HIGHLIGHT: 'Fix it: Sleep, stress management, consistent training' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'METRIC 3', HEADLINE: 'ApoB', BODY: 'The number of particles that can deposit cholesterol in your artery walls. More predictive than LDL alone. Most doctors don\'t test it. If yours is above 80 mg/dL, you have work to do.', SLIDE_NUMBER: '4', HIGHLIGHT: 'Fix it: Diet, exercise, or medication if needed' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'METRIC 4', HEADLINE: 'Fasting Glucose + HbA1c', BODY: 'Your metabolic health in two numbers. Fasting glucose above 100 or HbA1c above 5.6% means your body is already struggling to regulate blood sugar — years before a diabetes diagnosis.', SLIDE_NUMBER: '5', HIGHLIGHT: 'Fix it: Reduce refined carbs, walk after meals, build muscle' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'METRIC 5', HEADLINE: 'Grip Strength', BODY: 'Sounds simple. Predicts everything. Low grip strength is associated with higher risk of heart attack, stroke, and all-cause mortality. It\'s a proxy for your total body functional strength.', SLIDE_NUMBER: '6', HIGHLIGHT: 'Fix it: Dead hangs, farmer carries, resistance training' }
    },
    {
      layout: 'cta-bridge', template: 'cta-bridge.html',
      vars: { HEADLINE: 'Most people track zero of these.', BODY: 'I track all 5 in one app. AI Health Coach pulls your wearables and labs together and scores your longevity risk automatically.' }
    },
    {
      layout: 'cta', template: 'cta.html',
      vars: { HEADLINE: 'Save this post. Check your numbers.', CTA: 'Link in bio for early access', SUBTEXT: 'Which metric surprised you? Comment below.', AUTHOR: '@josephtolandsr' }
    }
  ],

  'carousel-2-testosterone': [
    {
      layout: 'title', template: 'title.html',
      vars: { CATEGORY: 'HORMONE HEALTH', HEADLINE: 'Your testosterone is "normal" but you feel like garbage.', SUBTEXT: 'Here\'s what your doctor isn\'t telling you', AUTHOR: '@josephtolandsr', SLIDE_NUMBER: '1' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'THE PROBLEM', HEADLINE: '"Normal" is a lie.', BODY: 'Lab reference ranges for testosterone are 264\u2013916 ng/dL. That means a 40-year-old man at 280 is told he\'s "normal." But optimal is 500\u2013900+. The range is so wide it\'s almost meaningless.', SLIDE_NUMBER: '2', HIGHLIGHT: '' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'MY STORY', HEADLINE: 'I lived this.', BODY: 'Brain fog, no energy, poor recovery, zero motivation. My labs came back "within range." Doctors said I was fine. I wasn\'t fine. It took too long to get real answers because everyone was looking at the wrong ranges.', SLIDE_NUMBER: '3', HIGHLIGHT: '' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'WHAT TO TEST', HEADLINE: 'Total T is not enough.', BODY: 'You need the full picture: Total Testosterone, Free Testosterone, SHBG, Estradiol, LH, FSH, and Prolactin. Total T alone can look fine while Free T \u2014 the testosterone your body can actually use \u2014 is tanked.', SLIDE_NUMBER: '4', HIGHLIGHT: 'SHBG binds testosterone. High SHBG = low free T even with good total T' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'KNOW YOUR NUMBERS', HEADLINE: 'Optimal vs. "normal"', BODY: 'Total T: optimal 500\u2013900+ (not 264)\nFree T: optimal 15\u201325 pg/mL\nSHBG: 20\u201350 nmol/L\nEstradiol: 20\u201335 pg/mL\n\nIf your doctor only tested Total T and said you\'re fine \u2014 push back.', SLIDE_NUMBER: '5', HIGHLIGHT: '' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'WHAT HELPS', HEADLINE: '6 things that actually move the needle.', BODY: '1. Sleep 7\u20139 hours (T drops 15% with poor sleep)\n2. Resistance training (compound lifts)\n3. Lose excess body fat (aromatase converts T to estrogen)\n4. Reduce alcohol\n5. Get Vitamin D, Zinc, Magnesium tested\n6. Manage stress (cortisol kills T)', SLIDE_NUMBER: '6', HIGHLIGHT: '' }
    },
    {
      layout: 'cta-bridge', template: 'cta-bridge.html',
      vars: { HEADLINE: 'Stop accepting "normal."', BODY: 'I built AI Health Coach to flag exactly this \u2014 when your labs are technically in range but nowhere near optimal. It shows you longevity targets, not just clinical cutoffs.' }
    },
    {
      layout: 'cta', template: 'cta.html',
      vars: { HEADLINE: 'Know someone dealing with this? Share this post.', CTA: 'Link in bio for early access', SUBTEXT: 'Drop your experience in the comments \u2014 how long did it take to get answers?', AUTHOR: '@josephtolandsr' }
    }
  ],

  'carousel-3-hrv': [
    {
      layout: 'title', template: 'title.html',
      vars: { CATEGORY: 'BIOHACKING', HEADLINE: 'What 30 days of tracking my HRV taught me', SUBTEXT: 'The patterns your wearable won\'t show you', AUTHOR: '@josephtolandsr', SLIDE_NUMBER: '1' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'WHAT IS HRV', HEADLINE: 'The metric most people ignore.', BODY: 'Heart Rate Variability measures the gap between heartbeats. Higher is better. It reflects how well your nervous system adapts to stress. It\'s the closest thing you have to a daily recovery score.', SLIDE_NUMBER: '2', HIGHLIGHT: '' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'WEEK 1', HEADLINE: 'The number is meaningless in isolation.', BODY: 'My HRV bounced between 15 and 30 with no pattern. Comparing to someone else\'s number is useless \u2014 HRV is deeply personal. What matters is YOUR trend over time, not a single reading.', SLIDE_NUMBER: '3', HIGHLIGHT: '' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'WEEK 2', HEADLINE: 'Alcohol is the HRV killer.', BODY: 'Even 2 drinks crashed my HRV by 40% the next morning. Every. Single. Time. No other variable had this big an effect. If you want one quick win for recovery \u2014 cut the alcohol.', SLIDE_NUMBER: '4', HIGHLIGHT: 'My HRV: 28ms normal \u2192 16ms morning after 2 drinks' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'WEEK 3', HEADLINE: 'Sleep quality > sleep quantity.', BODY: 'I had nights with 8 hours of sleep and terrible HRV. And nights with 6.5 hours and great HRV. The difference? Deep sleep. When I got 90+ minutes of deep sleep, next-day HRV was consistently 15\u201320% higher.', SLIDE_NUMBER: '5', HIGHLIGHT: '' }
    },
    {
      layout: 'content', template: 'content.html',
      vars: { CATEGORY: 'WEEK 4', HEADLINE: 'The pattern that changed everything.', BODY: 'By day 30, I could see it: sauna sessions on rest days correlated with my highest HRV readings the next morning. Stress + poor sleep predicted my worst days 24 hours in advance. My body was sending signals \u2014 I just needed to connect the data.', SLIDE_NUMBER: '6', HIGHLIGHT: '' }
    },
    {
      layout: 'cta-bridge', template: 'cta-bridge.html',
      vars: { HEADLINE: 'Your wearable gives you the number. Who connects the dots?', BODY: 'That\'s exactly why I\'m building AI Health Coach. It correlates your HRV with sleep, training, supplements, and stress to find YOUR personal patterns automatically.' }
    },
    {
      layout: 'cta', template: 'cta.html',
      vars: { HEADLINE: 'Track your HRV? Drop your average below.', CTA: 'Link in bio for early access', SUBTEXT: 'Save this for reference.', AUTHOR: '@josephtolandsr' }
    }
  ]
};

(async () => {
  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 1080, height: 1080 } });

  for (const [name, slides] of Object.entries(carousels)) {
    const outDir = path.join(__dirname, 'output', name);
    fs.mkdirSync(outDir, { recursive: true });

    for (let i = 0; i < slides.length; i++) {
      const slide = slides[i];
      let html = fs.readFileSync(path.join(TEMPLATES_DIR, slide.template), 'utf-8');
      html = html.replace('<link rel="stylesheet" href="base.css">', `<style>${baseCss}</style>`);

      for (const [key, value] of Object.entries(slide.vars)) {
        // Replace newlines with <br> for body text
        const escaped = value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/\n/g, '<br>');
        html = html.replace(new RegExp(`\\{\\{${key}\\}\\}`, 'g'), escaped);
      }

      if (slide.vars.HIGHLIGHT) {
        html = html.replace(/\{\{#HIGHLIGHT\}\}/g, '').replace(/\{\{\/HIGHLIGHT\}\}/g, '');
      } else {
        html = html.replace(/\{\{#HIGHLIGHT\}\}.*?\{\{\/HIGHLIGHT\}\}/gs, '');
      }

      const page = await context.newPage();
      await page.setContent(html, { waitUntil: 'networkidle' });

      const filename = `slide-${String(i + 1).padStart(2, '0')}-${slide.layout}.png`;
      await page.screenshot({ path: path.join(outDir, filename), clip: { x: 0, y: 0, width: 1080, height: 1080 } });
      await page.close();

      console.log(`[${name}] Rendered: ${filename}`);
    }
    console.log(`\n${name}: Done!\n`);
  }

  await browser.close();
  console.log('All 3 carousels rendered!');
})();
