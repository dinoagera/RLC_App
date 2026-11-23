/* Run with NODE_PATH pointing to a Playwright installation.
   RLC_URL defaults to http://127.0.0.1:8080/Web/. */
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: process.env.CHROME_PATH || '/usr/bin/google-chrome',
    args: ['--no-sandbox', '--disable-dev-shm-usage'] });
  const context = await browser.newContext({ viewport: { width: 1920, height: 1080 }, acceptDownloads: true });
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('console', msg => { if (msg.type() === 'error') errors.push(msg.text()); });
  const url = process.env.RLC_URL || 'http://127.0.0.1:8080/Web/';
  const output = path.resolve(__dirname, '../TestResults');
  fs.mkdirSync(output, { recursive: true });
  let checks = 0;
  function check(value, message) { assert.ok(value, message); checks++; }
  function number(text) { return Number(text.replace('−', '-').replace(',', '.').replace(/\s/g, '')); }
  async function read(id) { return number(await page.locator('#' + id).textContent()); }
  async function enter(key, value) {
    await page.locator('#input-' + key).fill(String(value));
    await page.locator('#input-' + key).press('Enter');
  }
  async function settle() { await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))); }
  try {
    await page.goto(url);
    await page.locator('#result-z').filter({ hasText: '100' }).waitFor();
    await page.evaluate(() => document.fonts.ready);
    await page.locator('#pause').click(); await settle();
    check(Math.abs(await read('result-z') - 100) < .01, 'Control impedance');
    check(Math.abs(await read('result-phi') + .24) < .01, 'Control phase');
    check(await page.evaluate(() => document.documentElement.scrollHeight <= innerHeight + 1), 'Base 1920×1080 layout fits without scrolling');
    await page.screenshot({ path: path.join(output, '01-time-1920.png'), fullPage: true });

    // Russian decimal, invalid strings, out-of-range and nonfinite values.
    await enter('R', '50,5'); check(Math.abs(await read('result-z') - 50.5) < .02, 'Russian decimal input');
    for (const invalid of ['abc', 'NaN', 'Infinity', '0', '1001', '1e999']) {
      await enter('R', invalid);
      check(await page.locator('#input-R').getAttribute('aria-invalid') === 'true', 'Reject ' + invalid);
      check(Math.abs(await read('result-z') - 50.5) < .02, 'Invalid input preserves circuit');
    }
    await page.locator('#reset').click();
    await page.locator('#pause').click();
    // Every parameter slider updates the output immediately.
    for (const [key, value] of [['R', 200], ['L', 200], ['C', 200], ['f', 100], ['U', 20]]) {
      await page.locator('#slider-' + key).evaluate((el, v) => { el.value = v; el.dispatchEvent(new Event('input', { bubbles: true })); }, value);
      check(number(await page.locator('#input-' + key).inputValue()) === value, 'Slider ' + key);
    }
    await page.locator('#tune').click();
    check(Math.abs(await read('result-phi')) < .001, 'Exact resonance phase');
    check(Math.abs(await read('result-z') - 200) < .001, 'Exact resonance impedance');
    check(await page.locator('#resonance').evaluate(el => el.classList.contains('active')), 'Resonance indicator');
    const exact = JSON.parse(await page.evaluate(() => new Promise(resolve => setTimeout(() => resolve(localStorage.getItem('rlc-lab.v1')), 350)))).p.f;
    check(Math.abs(exact - 1 / (2 * Math.PI * Math.sqrt(.2 * .0002))) < 1e-10, 'Unrounded resonance saved');

    await page.locator('#tab-phasor').click(); await settle();
    await page.screenshot({ path: path.join(output, '02-phasor-1920.png'), fullPage: true });
    check(await page.locator('#normalize').isHidden(), 'Normalization hidden for phasors');
    await page.locator('#tab-frequency').click(); await settle();
    await page.screenshot({ path: path.join(output, '03-frequency-1920.png'), fullPage: true });
    const graphRect = await page.locator('#graph').boundingBox();
    await page.mouse.move(graphRect.x + graphRect.width * .6, graphRect.y + graphRect.height * .4); await settle();
    check(await page.locator('#plot-tooltip').isVisible(), 'Frequency tooltip');
    await page.mouse.click(graphRect.x + graphRect.width * .6, graphRect.y + graphRect.height * .4);
    check(Math.abs(number(await page.locator('#input-f').inputValue()) - exact) > .1, 'Frequency click changes source');
    const downloadPromise = page.waitForEvent('download');
    await page.locator('#export').click();
    const download = await downloadPromise;
    await download.saveAs(path.join(output, 'frequency.csv'));
    const csv = fs.readFileSync(path.join(output, 'frequency.csv'), 'utf8');
    check(csv.includes('f_Hz;Im_A;Z_Ohm;phi_deg;is_f0') && csv.split('\n').length > 560, 'CSV export');

    await page.locator('#reset').click(); await page.locator('#pause').click(); await settle();
    const before = await page.locator('#graph').evaluate(c => c.toDataURL());
    await settle();
    check(before === await page.locator('#graph').evaluate(c => c.toDataURL()), 'Paused graph stable');
    const fBefore = await page.locator('#input-f').inputValue();
    await page.locator('#speed').evaluate(el => { el.value = 4; el.dispatchEvent(new Event('input', { bubbles: true })); });
    check(await page.locator('#input-f').inputValue() === fBefore, 'Animation speed does not alter frequency');
    await page.locator('#start').click();
    await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 100)));
    check(before !== await page.locator('#graph').evaluate(c => c.toDataURL()), 'Animation advances');
    await page.locator('#pause').click();
    await page.locator('#normalize').click(); await settle();
    check(await page.locator('#normalize').getAttribute('aria-pressed') === 'false', 'Normalization toggle');
    await page.locator('#pause').focus(); await page.keyboard.press('Space');
    check(await page.locator('#start').getAttribute('aria-pressed') === 'true', 'Space toggles even with a focused button');
    await page.locator('#pause').click();
    await page.locator('#help-open').click(); check(await page.locator('#help').evaluate(d => d.open), 'Help opens');
    await page.keyboard.press('Escape'); check(!await page.locator('#help').evaluate(d => d.open), 'Help closes');

    await enter('L', 1); await enter('C', 1);
    check(await page.locator('#tune').isDisabled(), 'Out-of-range resonance cannot be selected');
    await page.locator('#tab-frequency').click(); await settle();
    for (let mask = 0; mask < 32; mask++) {
      await page.evaluate(m => {
        const keys = ['R', 'L', 'C', 'f', 'U'], maximum = [1000, 500, 1000, 1000, 100];
        keys.forEach((key, i) => {
          const el = document.getElementById('slider-' + key); el.value = m & (1 << i) ? maximum[i] : 1;
          el.dispatchEvent(new Event('input', { bubbles: true }));
        });
      }, mask);
      check(Number.isFinite(await read('result-z')) && Number.isFinite(await read('result-im')), 'Extreme UI ' + mask);
    }
    await enter('R', 1); await enter('L', 500); await enter('C', 1);
    await page.locator('#tune').click(); await settle();
    check(Math.abs(await read('result-im') - number(await page.locator('#input-U').inputValue())) < .001, 'High Q resonant current');
    await page.screenshot({ path: path.join(output, '04-high-q.png'), fullPage: true });
    await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 350)));
    await page.reload(); await settle();
    check(number(await page.locator('#input-L').inputValue()) === 500, 'Settings persist');
    check(await page.locator('#tab-frequency').getAttribute('aria-selected') === 'true', 'Mode persists');

    await page.locator('#reset').click(); await page.locator('#pause').click(); await settle();
    await page.locator('h1').click();
    await page.keyboard.press('3');
    check(await page.locator('#tab-frequency').getAttribute('aria-selected') === 'true', 'Keyboard mode');
    await page.keyboard.press('1'); await page.keyboard.press('Space');
    check(await page.locator('#start').getAttribute('aria-pressed') === 'true', 'Keyboard playback');
    await page.keyboard.press('Space'); await page.keyboard.press('F1');
    check(await page.locator('#help').evaluate(d => d.open), 'F1 guide');
    await page.keyboard.press('Escape');

    for (const [width, height] of [[1366, 768], [1024, 768], [390, 844]]) {
      await page.setViewportSize({ width, height }); await settle();
      check(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), `No horizontal overflow at ${width}`);
      for (const mode of ['time', 'phasor', 'frequency']) {
        await page.locator('#tab-' + mode).click(); await settle();
        await page.screenshot({ path: path.join(output, `${mode}-${width}.png`), fullPage: true });
      }
    }
    check(errors.length === 0, 'No console or runtime errors: ' + errors.join('; '));
    const report = { passed: true, checks, errors, browser: await browser.version(),
      viewports: ['1920×1080', '1366×768', '1024×768', '390×844'] };
    fs.writeFileSync(path.join(output, 'browser-report.json'), JSON.stringify(report, null, 2));
    console.log(JSON.stringify(report, null, 2));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
